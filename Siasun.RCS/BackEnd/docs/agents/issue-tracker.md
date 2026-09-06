# SIASUN RCS 研发任务与架构审查问题跟踪表 (Issue Tracker)

## 一、 L2–L4 工业日志与调度自治阶梯里程碑

| 阶梯 | 目标能力 | 状态 | 落地核心组件与契约 | 闭环说明 |
|---|---|---|---|---|
| **L2 可串** | 同一次事故一条时间线，全链路不可抵赖 TraceId 穿透 | ✅ **已闭环** | `RcsTraceContext` 统一在 Inbound 中间件、Outbound 委托处理程序、实体拦截器、后台作业（`SetScoped`）及领域事件总线中穿透锁死 | 彻底杜绝日志孤岛，全链路 TraceId 贯穿三层审计 |
| **L3 可证** | 离线黑匣子 (.rcspack) 真实定责，调度员人工干预证据去伪求真 | ✅ **已闭环** | `DispatchInterventionAppService` 强制仓储校验（未找到则记 Failed + null，禁止伪造 Before/After 状态）；`FlightPackCollector` 多源自包含归档 | 责任可界定、不可抵赖、自包含离线回放包导出 |
| **L4 可自治** | 长期无人值守自治：全轨道零静默丢证据、溢流落盘、启动自愈回放、确定性规则降级、容量监控闭环 | ✅ **已闭环** | `EvidenceSpillBuffer<T>` 本地持久化保全；启动自愈 `RecoverDiskSpills`；`PriorityChannelReader<T>` 优先捞取；`RuleBasedIncidentAnalysisProvider` 确定性规则降级；`CapacityHealthReportDto` 深度可观测 | API 报文、实体变更、调度员操作三大特权轨全面实现 Spill 应急落盘与断电自愈回放 |

---

## 二、 审查问题闭环明细

### 1. Spec 关键项
- [x] **全特权轨道洪峰与阻塞下绝对零丢弃**：
  - API 审计通道（`ApiAuditLogChannel`）、实体审计通道（`EntityAuditLogChannel`）、调度员操作通道（`OperationLogChannelManager` / `OperationLogRecorder`）全面接入 `EvidenceSpillBuffer<T>`。
  - 排队阻塞超时时自动落盘至 `App_Data/spill/{buffer}_spill_{yyyyMMdd}.jsonl`，坚决杜绝任何关键铁证静默丢弃。
- [x] **本地磁盘溢流启动自愈与断电保全**：
  - `EvidenceSpillBuffer<T>.RecoverDiskSpills()` 在消费者（`ApiAuditLogConsumer`、`EntityAuditLogConsumer`、`OperationLogPersistenceWorker`）启动时自动执行。
  - 读取未入库的 `.jsonl` 溢流记录重入内存待处理队列，并原子重命名为 `.replayed`，杜绝重复落库与崩溃数据丢失。
- [x] **`EntityAuditInterceptor` 写入逻辑去重与保障**：
  - 清理重复的 `TryWrite` 检查代码，统一走标准写入并在必要时回退至 `WriteAsync(500ms)` 与 `SpillBuffer`。
- [x] **LiveStream 特权与常规推流双轨隔离**：
  - `_priorityPendingQueue` 永不丢弃，常规队列在背压突发时根据限流策略降采样低频遥测。
- [x] **容量观测 Telemetry 与健康状态生命周期**：
  - 暴露 `ApiChannelDepth`、`EntityChannelDepth`、`OperationChannelDepth`、`PendingSpillCount`、`LiveStreamPendingCount`、`PrivilegeSpillCount`、`GovernorCurrentEps`、`GovernorDropCount`。
  - 当 `PendingSpillCount > 0`（存在未消化的应急溢流）时严格升级为 `Critical`；当历史溢流全部恢复入库且无积压（`PendingSpillCount == 0 && PrivilegeSpillCount > 0`）时转为 `Warning`，避免系统永久锁定在 Critical 告警状态。

### 2. Standards 关键项
- [x] **XML 文档注释 100% 覆盖**：
  - 所有新增与修改的公共类、方法、属性、接口及领域模型（如 `IOperationLogChannel`、`CapacityHealthReportDto`、`OperationLogPersistenceWorker` 等）全面补充 C# XML 文档注释，0 编译文档警告。
- [x] **AI 事故推演弱依赖与本地规则降级**：
  - 实现 `RuleBasedIncidentAnalysisProvider`，在 OpenAI/远端工业大模型不可用或抛出异常时无缝降级执行确定性规则推演，保障工控调度核心不受阻。
- [x] **Swagger 路由元数据精准对齐**：
  - 在 `AbpBuiltInApiCommentsFilter` 中剔除历史幽灵路由（`retry-task`、`reassign-vehicle`），精准对齐真实的 `assign-vehicle` 与 `reset-vehicle` 路由中文业务说明。
- [x] **特权判定精准化与单一真实源**：
  - 提炼 `IEvidencePrivilegePolicy` / `DefaultEvidencePrivilegePolicy`，采用精准分词与路径段匹配（Path Segment Tokenization），彻底剔除 Query String 与无关字串（如 `multitasking`）偶然碰撞造成的误判。
- [x] **消除 Primitive Obsession**：
  - 全面采用强类型 `DiagnosticTracks` 与 `DiagnosticLevels`。
- [x] **双数据库（SQL Server / SQLite）兼容性**：
  - 迁移与实体定义严禁硬编码方言特异类型，保证 SQL Server 与单机 SQLite 100% 兼容。


