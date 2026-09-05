# SIASUN RCS 通用架构规范与 AI Agent 编码守则
> **Universal Architecture & Coding Guidelines for AI Agents**

所有在此代码库工作的 AI Agent，在理解需求、设计方案与编写/修改代码前，**必须严格遵守**以下规范。

---

## 一、 系统定位与命名约束 (System Identity & Philosophy)

1. **系统正式名称**：**SIASUN RCS**（新松移动机器人调度控制系统 / Robot Control System）。
2. **严禁版本代号固化**：**严禁称呼本系统为“RCS 3.0”或任何特定版本代号**。本系统是面向半导体洁净室（晶圆/FOUP）、高精密制造与智能仓储场景的标准产品化平台，吸收了 NXP ERACK、NXP 天津 Molding、TXC 台湾晶技、Sandisk Murata 等现场经验。
3. **微内核与插件化哲学**：
   - 80% 通用核心资产固化（任务调度微内核、SAGA 补偿、原子库位锁、状态步进、报文审计）；
   - 20% 现场差异插件化（立库、风淋门/传递窗、机台设备、MES/WMS 均通过独立 Adapter 接入，严禁侵入核心内核）。
4. **严禁硬编码项目分支**：绝对禁止在调度主链路中编写类似 `if (customer == "NXP")` 的现场特异性硬编码，所有现场特异逻辑必须抽象为策略接口、配置化 Schema 或独立插件模块。

---

## 二、 核心调度架构 7 大铁律 (Core Architectural Constraints)

### 1. 声明式工作流驱动，禁止大状态机 (Workflow Engine over State Machines)
- **DO NOT**：严禁构建通用的巨型 DAG 图引擎，严禁编写上千行的巨型 `switch-case` 状态机（如历史遗留的 22 状态分支）。
- **DO**：采用轻量步进式 `TaskWorkflow` 引擎驱动任务执行。
- 领域模型 `AgvTask` 严格保持 5 状态粗粒度生命周期：
  - `Pending`（待处理）
  - `Running`（执行中）
  - `Succeeded`（已成功）
  - `Failed`（已失败）
  - `Canceled`（已取消）
- 任务内部的细粒度推进严格由 `StepIndex`（步骤步进）、`WaitingEvent`（异步信号/心跳唤醒）、`ActiveLeg`（多程段执行）驱动。

### 2. Schema 驱动的 OptionCode 编解码 (OptionCode Schema-Driven Encoding)
- **DO NOT**：严禁使用脆弱的位运算硬编码（如 ERACK 的 `TaskCode1/2`）。
- **DO**：采用 Schema 驱动的流水线架构（`OptionCodeSchema`、`Assembler`、`Encoder`、`Decoder`）。
- 必须支持版本化 Schema（如 `erack.v1`、`molding.v1`、`txc.v1` 等），并支持前端大屏与运维看板的双向反向解析展示。

### 3. TM 回调映射统一采用注册表 (TaskSerialRegistry)
- **DO NOT**：严禁使用字符串拼接或正则替换 hack（如 `.Replace("0_fetch", "")`）反查内部任务。
- **DO**：通过专用注册表 `TaskSerialRegistry` 维护底层 TM 报文序列号、`AgvSerial` 与平台内部任务的双向映射，安全处理多程段（Fetch / Put / 中间停靠点）的连续流转。

### 4. 工业硬件与 PLC 纯插件化隔离 (Hardware & PLC as Optional Plugins)
- **DO NOT**：严禁将 S7 / Modbus / Ethernet-IP 等 PLC 轮询直接编写在核心调度主循环中。
- **DO**：所有硬件与现场设备交互统一抽象在 `IHardwareGate` 端口适配器之后。双臂同步、安全门禁、库位硬件联锁等现场强相关逻辑必须作为独立可插拔插件注入。

### 5. 六边形架构：上游系统接入适配 (Inbound Ports and Adapters)
- **DO NOT**：严禁强行统一单一种类的 Inbound API。
- **DO**：上游系统通信协议差异极大（AMA/MES 为 RESTful，Mica WMS 为 WCF/SOAP，半导体设备为 SECS/GEM），必须采用六边形架构的端口与适配器模式（`IInboundPort`、`IOutboundAdapter`）。

### 6. 批次与多车编排 (Batch & Multi-AGV Orchestration)
- 领域模型原生支持批次分解（Batch Management）与多车协作。
- 支持一单分拆多子任务、汇聚同步、载具绑定（Carrier）与安全互锁编排。

### 7. 领域事件解耦跨域副作用 (Domain Events for Side Effects)
- **DO NOT**：严禁在领域实体或工作流步进中直接编写过程式方法调用跨领域副作用（例如任务完成时直接同步调用 MES API）。
- **DO**：必须发布本地领域事件（如 `TaskLifecycleEndedEvent`），由独立 Event Handler 异步解耦处理。

---

## 三、 工业级日志、可观测性与黑匣子排障规范 (Observability & Diagnostics)

### 1. 三层日志联动与定分止争内核
工控现场发生异常时，必须具备不可抵赖、快速界定责任（车体问题、MES 报文问题、现场操作失误）的三层审计体系：
- **第 1 层：接口级审计 (Inbound & Outbound API Audit)**：记录外部调用（MES/WMS/TM/PLC）的原始报文、`TraceId`、耗时、调用方身份、HTTP 状态码。
- **第 2 层：操作与自审计 (OperationLog)**：记录所有调度员人工干预和系统自愈关键操作（必须记录：操作人、动作类型、修改前状态 `BeforeState`、修改后状态 `AfterState`、操作原因、关联 AGV 与 Task）。
- **第 3 层：实体变更与时序监控 (Entity Audit & SystemEventLog)**：记录核心实体关键字段变更快照与 AGV 遥测/底盘时序事件。

### 2. TraceId 全链路贯穿
- 入站网关生成的 `TraceId` 必须贯穿至调度任务上下文、领域事件、底层 TM 报文以及各层审计日志中，作为事故追溯的统一索引锚点。

### 3. 黑匣子取证排障包 (.rcspack)
- 工控车间多处于物理断网环境，系统必须支持通过 `IFlightPackCollector` 和 `IFlightPackAppService` 一键按任务或时间段导出自包含的 `.rcspack` 压缩包（包含 `manifest.json`、`timeline.json`、`narrative.md`、离线静态回放播放器及原始日志），严禁依赖外网进行故障回溯。

### 4. 实时推送与诊断流可配置
- SignalR 实时诊断监控必须通过配置驱动（`DiagnosticLiveStreamOptions: Enabled / SampleIntervalMs / MaxBufferedEvents`）。
- 必须具备环形缓冲与降采样节流保护，严禁冲击工控机性能或耗尽网络带宽。

### 5. AI 排障插件化与弱依赖
- AI 事故根因推演（`IAiIncidentAnalysisProvider`）属于增强型可选插件，必须支持本地 Mock/规则降级，调度核心流程严禁因 AI 服务不可用而受阻。

---

## 四、 代码工程规范、Swagger 文档与注释铁律 (Coding, Swagger & Engineering Discipline)

### 1. 无死角注释铁律（强制执行）
- **任何新增或修改的类、接口、公共方法、公共属性、DTO、枚举、配置选项（Options），必须添加完备的 C# XML 文档注释 (`/// <summary>`)！**
- **严禁编写和合并无注释的“裸代码”。**
- 复杂方法必须明确说明 `<param>` 和 `<returns>`，严禁引发编译文档警告（如 CS1572 / CS1573 / CS1591）。

### 2. Swagger UI 元数据规范
每次新增应用服务或暴露新接口时，必须同步更新 Swagger 中文展示：
- **服务标签（Tag）注册**：必须在 `src/06.Hosting/SIASUN.RCS.HttpApi.Host/Swagger/SwaggerTagDescriptionFilter.cs` 的字典中登记应用服务名称及其精准的中文业务描述（例如 `{ "OperationLog", "调度员操作与系统自审计日志" }`）。
- **动态接口说明注册**：对于 ABP 动态 Web API 暴露的路由，若未通过 XML 自动识别，必须在 `src/06.Hosting/SIASUN.RCS.HttpApi.Host/Swagger/AbpBuiltInApiCommentsFilter.cs` 中添加对应路由的中文标题与描述。

### 3. 分层架构与依赖方向 (DDD + ABP vNext)
严格遵循自底向上的单向依赖，严禁循环引用与越级引用：
```
01.Shared (SIASUN.RCS.Domain.Shared)
   ↓
02.Domain (SIASUN.RCS.Domain - 保持纯净，绝不依赖 EF Core 或 Web 框架)
   ↓
03.Application.Contracts (SIASUN.RCS.Application.Contracts - DTO 与服务契约)
   ↓
03.Application (SIASUN.RCS.Application - 业务用例与编排)
   ↓
05.Infrastructure (EFCore、Logging、AuditLog.Sqlite 等技术实现)
   ↓
06.Hosting (SIASUN.RCS.HttpApi.Host - 启动宿主、中间件与管道)
```

### 4. 双数据库（SQL Server / SQLite）兼容性
- 系统必须同时支持**生产主流集中式数据库（SQL Server）**与**现场独立/离线单机轻量部署（SQLite）**。
- EF Core 映射配置统一在 `SIASUN.RCS.EntityFrameworkCore` 中维护。
- 禁止使用特定数据库专有的 SQL 方言或特异函数；模型字段长度（字符串必须限长）、主外键关系、联合索引必须在 `OnModelCreating` 中显式定义。

### 5. 质量门禁与全绿测试 (Quality Gate)
- **测试驱动与完备性**：新增或修改业务逻辑后，必须编写或更新配套的单元测试（`Domain.Tests` / `Application.Tests` / `Infrastructure.Tests`）。
- **测试验证铁律**：任何代码提交或任务交付前，必须在终端执行：
  ```bash
  dotnet test SIASUN.RCS.sln
  ```
  **必须保证 100% 测试通过（0 失败）、0 编译错误、0 严重编译警告。**

