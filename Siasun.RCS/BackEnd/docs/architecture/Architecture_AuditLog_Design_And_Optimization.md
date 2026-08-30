# RCS 3.0 审计日志架构：设计思想、缺陷与优化方向

基于当前确定的“后端单 PR + 前端独立 PR”的最终规格，本文档完整复盘了 RCS 3.0 在 API 审计与实体审计（Entity Tracker）模块上的设计哲学，并客观剖析了当前单机架构下的局限性，为未来的系统高并发演进提供优化蓝图。

---

## 一、 核心设计思想 (Design Philosophy)

### 1. 业务主线程与持久化严格物理隔离
*   **设计点**：无论是最外层的 HTTP 请求拦截（`Middleware` / `DelegatingHandler`），还是最底层的数据变更追踪（`SaveChangesInterceptor`），所有抓取到的日志数据**均不直接写库**，而是通过极低延迟的 `TryWrite` 压入基于内存的 `BoundedChannel`。
*   **收益**：保证了核心工控业务流（如 AGV 任务分配、心跳上报）绝对不会因为磁盘 I/O 抖动、SQLite 锁表而发生阻塞。

### 2. 内存防爆与降级保护 (DropOldest)
*   **设计点**：`BoundedChannel` 容量上限设定为 20,000。当极端并发（如底层 PLC 突发海量告警导致大量数据更新）导致消费者处理不及、通道溢出时，采用 `DropOldest` 策略静默丢弃旧日志。
*   **收益**：宁可丢失部分调试追踪日志，也**绝对不允许**拖垮 RCS 核心服务的内存导致 OOM。

### 3. 单机极致轻量化 (SQLite WAL + 定时自我清理)
*   **设计点**：没有引入笨重的 ElasticSearch 或外部 MQ，而是采用与 Serilog 文件同级的独立 SQLite 数据库，并强制开启 `PRAGMA journal_mode=WAL`。同时依托 Quartz 调度 `AuditLogCleanupWorker` 每天自动 `ExecuteDeleteAsync`。
*   **收益**：满足了单机工控机（IPC）极简部署的要求，无需额外部署中间件；通过定时清理解决了 IPC 硬盘容量极小（通常仅 64G/128G SSD）导致的数据膨胀问题。

### 4. 规则引擎：动态热刷与精准控制
*   **设计点**：设计了基于 `Priority` 的 `EntityAuditRule` 规则链（First-Match Wins），支持动态配置 `Skip` / `Summary` / `Full`，并且采用领域事件（Domain Event）驱动内存规则树（`EntityAuditRuleEvaluator`）热更新。
*   **收益**：现场实施人员可以随时在 UI 调整监控级别（如只看 `*Mission` 的 Summary），且无需重启后端服务。

### 5. 富聚合根 (Rich Domain Model) 拒绝贫血
*   **设计点**：将规则状态的校验、启停控制（`Enable()` / `Disable()`）封装在实体 `EntityAuditRule` 与 `AuditLogFilterRule` 内部，而不是写在 AppService 中。
*   **收益**：逻辑高内聚，防止随着后期逻辑复杂化而导致 Service 层变为庞大的“意大利面条代码”。

---

## 二、 目前架构的缺点 (Architectural Shortcomings)

尽管目前的架构对于现阶段的 RCS 3.0 IPC 单机部署堪称“甜点级”，但在面临未来更高吞吐或集群化部署时，存在以下隐患：

1. **拦截器内的序列化开销（Serialization Overhead）**
   *   **现状**：在 EF Core 的 `SaveChangesInterceptor` 中，我们直接对改动的字段执行了 `JsonSerializer.Serialize(changesDict)`。
   *   **痛点**：序列化是一个 CPU 密集型操作。在极高频的实体保存场景下，这会拉长 `SaveChanges` 的同步耗时，变相降低了数据库操作的吞吐量。

2. **SQLite 删除操作的表级锁与磁盘碎片**
   *   **现状**：Quartz Worker 使用 `ExecuteDeleteAsync` 基于时间线（如 30 天前）大批量删除历史数据。
   *   **痛点**：尽管开启了 WAL 模式，但 SQLite 归根结底是文件型数据库。大批量的 Delete 会长时间持有写锁（可能与 Consumer 的批量 Insert 发生锁冲突），并且 Delete 不会立刻释放磁盘空间，只会留下逻辑碎片，需要额外的 `VACUUM` 操作才能缩容文件。

3. **内存快照同步仅支持单实例（Single-Node Memory Sync）**
   *   **现状**：规则的热更新依赖于 `ILocalEventHandler`，这在单体单实例应用中工作完美。
   *   **痛点**：如果未来 RCS 扩展为高可用双活集群，节点 A 修改了 UI 规则，只会触发节点 A 的内存热刷，节点 B 的 `EntityAuditRuleEvaluator` 依然是旧配置，导致审计规则出现“脑裂”。

4. **粗粒度的丢弃策略（Dumb Drop Strategy）**
   *   **现状**：通道满时触发 `DropOldest`。
   *   **痛点**：在混合场景下，可能会把重要实体（如 `AgvTask` 变更）的日志丢弃，而保留了海量且不重要的次要实体日志。没有基于权重的 QoS 队列管理。

---

## 三、 未来优化方向 (Optimization Roadmap)

为了应对未来更复杂的业务场景，建议分阶段进行以下优化改造：

### 1. 序列化操作异步化 (Offloading CPU-Bound Work)
*   **方案**：在 `EntityAuditInterceptor` 中，不再直接 Serialize。而是将 `PropertyChanges` 存入一个 `Dictionary<string, object>` 随 `EntityAuditLogEntry` 压入 Channel。
*   **实施**：在 `EntityAuditLogConsumer` 消费者后台线程中，取出数据后再统一执行 JSON 序列化落盘。
*   **收益**：彻底解放主线程，将 CPU 消耗转移到后台线程。

### 2. SQLite 文件时间分片 (Time-Based Sharding / Rolling Files)
*   **方案**：废弃“单文件大库 + 定时 Delete”的策略。采用类似 Serilog 的 Rolling 机制，每月或每周生成一个独立的数据库文件（如 `api_audit_log_202608.db`）。
*   **实施**：Quartz Worker 清理时，不再执行 SQL Delete，而是直接通过 `File.Delete()` 删除整个过期月份的 `.db` 文件。
*   **收益**：0 锁表冲突，瞬间释放磁盘空间，极致的清理性能。

### 3. 分布式演进：引入 Redis Pub/Sub 与 Stream
*   **方案**：
    1. 将 `ILocalEventHandler` 升级为 `IDistributedEventHandler`（基于 Redis Pub/Sub），实现全集群的审计规则毫秒级同步。
    2. 将内存 `BoundedChannel` 替换为 Redis Stream (`XADD`)，消费者变更为 Consumer Group (`XREADGROUP`)。
*   **收益**：完美适配 RCS 微服务/多节点高可用集群，重启不丢尚未落盘的日志数据。

### 4. 智能化采样与动态限流 (Adaptive Sampling)
*   **方案**：在 `IEntityAuditRuleEvaluator` 中加入“滑动时间窗口”统计机制。例如设定 `MaxLogsPerSecond = 50`。
*   **实施**：当某一实体的变更频率突然飙升引发雪崩预警时，拦截器自动将该实体的追踪策略从 `Full` 降级为 `Summary`，甚至短时间内强制 `Skip`，并在系统内广播“审计日志降级告警”。
*   **收益**：使系统具备自愈能力，防止异常的外部系统高频调用或者错误的循环更新直接打爆数据库或磁盘。
