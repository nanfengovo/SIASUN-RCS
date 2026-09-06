# SIASUN RCS 研发任务与架构审查问题跟踪表 (Issue Tracker)

## 一、 L2–L4 工业日志与调度自治阶梯里程碑

| 阶梯 | 目标能力 | 状态 | 落地核心组件与契约 | 闭环提交 |
|---|---|---|---|---|
| **L2 可串** | 同一次事故一条时间线，全链路不可抵赖 TraceId 穿透 | ✅ **已闭环** | `RcsTraceContext` 统一在 Inbound 中间件、Outbound 委托处理程序、实体拦截器、后台作业（`SetScoped`）及领域事件总线中穿透锁死 | `0ac0df6`, `b7b964b` |
| **L3 可证** | 离线黑匣子 (.rcspack) 真实定责，调度员人工干预证据去伪求真 | ✅ **已闭环** | `DispatchInterventionAppService` 强制仓储校验（未找到则记 Failed + null，禁止伪造 Before/After 状态）；`FlightPackCollector` 多源自包含归档 | `dbe41e5`, `b56b7d5` |
| **L4 可自治** | 长期无人值守自治：零静默丢特权证据、溢流落盘、确定性规则降级、容量监控升危 | ✅ **已闭环** | `EvidenceSpillBuffer<T>` 本地持久化保全；`RuleBasedIncidentAnalysisProvider` 确定性规则降级；`PriorityChannelReader<T>` 优先回放；`CapacityHealthReportDto` 深度可观测 | `b56b7d5`, `0cd76ad` |

---

## 二、 审查问题闭环明细

### 1. Spec 关键项
- [x] **特权事件洪峰下绝对零丢弃**：引入 `EvidenceSpillBuffer<T>`，超时自动落盘 `.spill/*.jsonl`，拒绝返回 false/丢弃。
- [x] **`EntityAuditInterceptor` 检查写入结果**：显式判断 `TryWrite` 结果并在必要时回退至 `WriteAsync`。
- [x] **LiveStream 特权与常规推流双轨隔离**：`_priorityPendingQueue` 永不丢弃，常规队列背压限流丢弃低频遥测。
- [x] **容量观测 Telemetry 补充**：暴露 `ApiChannelDepth`、`EntityChannelDepth`、`LiveStreamPendingCount`、`PrivilegeSpillCount`、`GovernorCurrentEps`、`GovernorDropCount`，且溢流发生时自动升级为 `Critical`。

### 2. Standards 关键项
- [x] **XML 文档注释 100% 覆盖**：补齐所有公共类、方法、属性及 12 个 ABP 模块的 `/// <summary>`。
- [x] **AI 事故推演弱依赖与本地规则降级**：实现 `RuleBasedIncidentAnalysisProvider` 并由 `OpenAiCompatibleAiIncidentAnalysisProvider` 在不可用时自动调用。
- [x] **Swagger 动态路由中文注释补齐**：在 `AbpBuiltInApiCommentsFilter` 补全调度干预与容量健康路由。
- [x] **特权策略单一真实源**：提炼 `IEvidencePrivilegePolicy` / `DefaultEvidencePrivilegePolicy`。
- [x] **消除 Primitive Obsession**：全面采用强类型 `DiagnosticTracks` 与 `DiagnosticLevels`。
