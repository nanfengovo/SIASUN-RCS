# 标准化企业级 RCS 机器人调度平台产品需求文档 (PRD)

> **文档编号**：PRD-SIASUN-RCS-2026-V3.0  
> **面向系统**：半导体晶圆/FOUP搬运、封测焊线、注塑成型、石英晶体搬运等多场景通用 RCS 平台  
> **技术基线**：后端 .NET 8 + ABP 纯净 Web API 模板（DDD 架构） \| 前端 Vue 3 + Soybean Admin (Naive UI)  
> **文档归档路径**：`/Users/feng/Documents/Code/研发/项目/RCS/08_standard_rcs_platform_prd_and_architecture_prompts.md`

---

## 📑 目录
1. **产品概述与业务定位**
2. **全系统 8 大业务模块与 42 项精细功能点规格说明 (PRD Specification)**
3. **核心业务流转时序与状态机规范**
4. **系统非功能性需求 (NFR)**
5. **硬件与三方系统对接全景规范**
6. **AI 架构图生成专用提示词 (GPT / Midjourney / DALL-E 3 提示词)**

---

# 一、产品概述与业务定位

### 1.1 产品背景
过去在多个厂区（NXP 台湾焊线、NXP 天津注塑、台湾晶技 TXC、SanDisk 晶圆转运）的开发过程中，存在**“过程式 22 状态枚举、2180 行上帝类、1205 行 JSON 配置地狱、代码分叉无法复用”**的严重历史痛点。
本项目旨在打造一套**标准化的企业级 RCS（Robot Control System）平台**，实现：
- **80% 通用核心资产固化**：任务调度微内核、SAGA 逆向事务补偿、库位原子锁、动态位图编译、S7 PLC 批量扫描、全量报文审计；
- **20% 现场差异插件化**：立库、传递窗、MES 等通过独立 Adapter 插件接入，内核代码 0 修改。

---

# 二、全系统 8 大业务模块与 42 项精细功能点规格说明

---

## 模块 01：任务调度与编排微内核 (`Module.TaskOrchestration`)

### F1.1 请求指纹与毫秒级幂等防重
- **业务价值**：防止上游 MES / AMA 因网络超时重试导致在 1 秒内重复创建搬运运单，杜绝物理两车撞车或重复取料。
- **算法与逻辑**：
  - 计算请求指纹：`Fingerprint = SHA256($"{TaskType}|{FromLocation}|{ToLocation}|{CarrierId}|{LotId}")`；
  - 存入分布式缓存（Redis/MemoryCache），设定 1000ms 滑动窗口；
  - 若命中相同指纹，直接返回已有任务编号，错误码定义为 `50001 (Duplicate Request Ignored)`。

### F1.2 声明式 DAG 工作流微内核
- **业务价值**：淘汰 22 状态 switch-case，以 10 步声明式 Activity 节点驱动任务流转。
- **核心流程**：
  - 任务节点：`FetchLeg (Nav -> Interlock -> ArmAction -> Complete)` ➔ `PutLeg (Nav -> Interlock -> ArmAction -> Complete)`；
  - 遇到需要外部信号（如立库放行、PLC 门开启）时调用 `context.Suspend(EventKey)` 挂起任务；
  - 收到 TM/PLC 回调后调用 `WorkflowEngine.SignalAsync(EventKey)` 唤醒。

### F1.3 乱序与重复信号幂等容错
- **业务价值**：工业 Wi-Fi 偶发抖动会导致 TM 回调重复发送或乱序到达。
- **处理逻辑**：
  - 每个 Activity 维护 `WasAlreadyConsumed` 标志位；
  - 当收到已经执行过的步骤信号时，直接回放缓存的成功响应，**严禁推进步骤计数器**，防止任务状态跳跃导致物理撞车。

### F1.4 SAGA 四级逆向事务补偿回滚
- **业务价值**：当搬运途中遭遇机械臂急停、立库不可用或人工强制取消时，系统必须优雅清理现场资源，严禁残留死锁。
- **回滚执行顺序**：
  1. **第一级（TM 车队）**：调用 TM `task_delete` 撤销在途运单；
  2. **第二级（三方设备）**：调用立库 STKC / WMS `cancel` 释放预定仓位；
  3. **第三级（本地资源）**：释放起点/终点库位锁（`UnlockLocation`）；
  4. **第四级（上游系统）**：向 MES 发送 `TaskCanceled` 完工报文，标记取消原因。

### F1.5 多车同批次门禁过滤器
- **业务价值**：当同批次 10 台 AGV 连续进入立库/传递窗时，避免立库门开关 10 次导致产能下降与机械磨损。
- **规则逻辑**：
  - 首车到达触发 `LOADREQ`（开门放行）；
  - 中间车到达直接放行（门保持常开）；
  - 最后一辆车完成物理放货后触发 `LOADCOMPLETED`（关门复位）。

### F1.6 动态改点与空闲端口重分配
- **业务价值**：支持 2026 最新新松 `task_arrive_target_pre` 预到达协议。
- **处理逻辑**：AGV 距目标点 2 米前上报预到达，RCS 动态查询当前空闲立库口，返回实时分配的 `port`, `target`, `option_code`。

### F1.7 任务优先级动态重排
- **业务价值**：允许 MES 动态插入特急晶圆盒（Hot Lot）。
- **处理逻辑**：调用 TM `task_priority` 接口，将指定运单优先级实时调整至 1~100，TM 自动调整车队路径规划。

---

## 模块 02：库位与物料全息拓扑 (`Module.LocationAndMaterial`)

### F2.1 动态库位状态机与原子锁
- **库位 5 状态模型**：
  - `Empty (2)`：空闲可用；
  - `Occupied (1)`：物料在位；
  - `Reserved (3)`：已被运单锁定在途；
  - `Disabled (4)`：人工维护禁用；
  - `Warn (5)`：传感器硬件异常。
- **并发控制**：基于数据库行锁或 Redis RedLock，确保两个并发建单绝不会抢占同一库位。

### F2.2 物料在位全息数字孪生
- **业务价值**：实时追溯 Carrier（FOUP/晶圆盒/模具）所处的物理位置。
- **流转事件**：
  - 取货完工：发布 `MaterialLocationChanged(CarrierId, FromLocation, AgvId)`（标记物料在车上）；
  - 放货完工：发布 `MaterialLocationChanged(CarrierId, AgvId, ToLocation)`（标记物料在库位）。

### F2.3 S7 传感器白名单毫秒级同步
- **业务价值**：防止数据库记录与现场物理光电传感器脱节。
- **白名单规则**：
  - 系统处于 `Reserved` 且 PLC 处于 `Occupied` 属于合法在途过渡态，允许放行；
  - 系统处于 `Empty` 但 PLC 突变检测到 `Occupied`，立即触发 **未知物料非法侵入告警**。

### F2.4 S7 内存 46B 槽位映射计算
- **偏移量公式**：`Offset = Base + [(C - 1) * M * K + (L - 1) * K + (S - 1)] * 46`；
- **解析字段**：`Presence_Code (2B)`、`Tracking_Code (10B)`、`Box_Type (2B)`、`Lock_State (2B)`。

---

## 模块 03：车队协同与交通管制 (`Module.FleetCoordination`)

### F3.1 车辆智能选型指派
- **综合评分模型**：`Score = (100 - DistFactor * 距离) + (0.5 * 剩余电量) + 车型匹配权重`；
- 自动根据物料类型（FOUP/料盒）匹配机械臂复合车或潜伏式顶升车。

### F3.2 物理狭窄干涉区互锁防撞
- **互锁时序**：
  1. AGV 到达机台干涉区前置位 PLC 信号 `DB100.DBX0.3 (AGV_In_Zone = 1)`；
  2. PLC 硬件切断机台开合模回路，锁定安全门；
  3. 机械臂完成取放料并退出后，复位 `AGV_In_Zone = 0`。

### F3.3 车体独立聚合根 (`ARVEntity`)
- 实时记录 `VehicleId`, `OnlineStatus`, `BatteryLevel`, `X, Y, Theta`, `CurrentSpeed`, `TotalOdometer`。

### F3.4 十六进制故障码中文自动翻译
- 接收车载底层报警报文（如 `B0LP010004`），自动关联 `ARVAlarmDescDo` 字典表，向前端展示：“左侧安全触边传感器触发物理急停”。

### F3.5 安全激光雷达动态屏蔽 (Muting)
- 在进入狭窄料架区域时，置位 PLC `DB100.DBX0.2 (Curtain_Muted = 1)` 屏蔽光幕报警；离开后恢复。

---

## 模块 04：工装治具与工艺控制 (`Module.ProcessTooling`)

### F4.1 32位 OptionCode 动态位图编译
- **核心算法**：基于 JSON Schema 动态解析字段定义，执行无损 LSB/MSB 位拼装：
  $$\text{OptionCode} = \sum_{i} (\text{Value}_i \ \& \ (2^{\text{BitWidth}_i} - 1)) \ll \text{BitOffset}_i$$
- 彻底替代在 C# 代码中硬编码 `<< 16`、`<< 8` 的历史坏味道。

### F4.2 操作码快照固化 (Freeze)
- 任务建单时立即计算 OptionCode 并冻结至任务实体，后续放行时直接回放，杜绝主数据修改导致在途运单参数漂移。

### F4.3 手眼视觉定位与 RFID 比对
- 机械臂执行动作前调用 `POST /check_cargo_rfid`；
- 比对车载读头读取的标签与 `Task.CarrierId`，返回 `check_code: 1 (一致放行) / 2 (不符报警)`。

### F4.4 机械臂 6 轴运动学状态监控
- 实时采集机械臂 J1~J6 关节角度与力矩，监控夹爪开闭传感器与气缸气压状态。

---

## 模块 05：上下游系统集成网关 (`Module.UpstreamGateway`)

### F5.1 MES 入站派工 (RCS-001)
- 接收 MES 下发的 1~2 条绑定运单，执行原子校验与 `MatchesMesDispatch` 幂等防重。

### F5.2 MES 出站完工上报 (RCS-101)
- 订阅 `TaskLifecycleEndedEvent`，成功上报 `result: 1`，取消上报 `result: 2`，失败静默等待人工处理。

### F5.3 蒙莹 STKC 6 接口适配
- 对接立库 `/reserve`, `/exist`, `/transfer/out`, `/arv/request`, `/arv/completed`, `/cancel`。

### F5.4 晖哲传递窗 8 接口适配
- 实现洁净区双门互锁状态机：左门开 ➔ 物料送入 ➔ 风淋吹淋 ➔ 右门开 ➔ 物料取出。

### F5.5 Mica WMS WCF SOAP 适配
- 封装 NetHttp + BinaryMessageEncoding 高性能 WCF 客户端，对接 24 个 SOAP 契约方法。

### F5.6 事务发件箱 (Transactional Outbox)
- 本地事务持久化 `OutboxMessage`，独立后台 Worker 结合 Polly 指数退避重试，**保证 MES 通知 100% 投递**。

---

## 模块 06：3D/2D 数字孪生与态势看板 (`Module.DigitalTwin`)

### F6.1 Three.js 3D 设备工装数字孪生
- 3D 渲染机台模型、机械臂 J1~J6 实时关节姿态、夹爪开闭、料盒在位与安全激光雷达扇形光束。

### F6.2 2D 拓扑车队轨迹监控
- 渲染车间 2D CAD 拓扑图、AGV 实时位置坐标与站点热力图。

### F6.3 RxJS 500ms 批量节流管道
- 针对 100ms 高频坐标流进行前端缓冲聚合，保证 UI 界面恒定维持 **60 FPS**。

### F6.4 10 步脉冲任务执行看板
- 动态高亮当前任务所处的物理动作节点（待机 ➔ 导航 ➔ 申请放行 ➔ 取货 ➔ 搬运 ➔ 放货 ➔ 完工）。

---

## 模块 07：低代码设计器与 Schema 工具 (`Module.LowCodeDesigner`)

### F7.1 可视化 DAG 流程编排器
- 基于 Vue Flow / AntV X6 拖拽节点定义搬运工序，**彻底消灭 1200 行手写 JSON 配置**。

### F7.2 OptionCode 32 位位图设计器
- 可视化配置位宽、偏移、枚举与数据源绑定。

### F7.3 PLC 点表 Excel 动态导入
- 一键解析现场 Excel 点表并自动与系统库位建立 S7 地址映射。

---

## 模块 08：全链路审计与脱机仿真沙箱 (`Module.AuditAndDiagnostics`)

### F8.1 全量出入站报文拦截审计
- 自动抓取并持久化所有出入站 HTTP/SOAP 调用的原始报文、耗时、状态码与 TraceId。

### F8.2 硬件脱机仿真沙箱 (Sandbox)
- 启动内置 Mock AGV、Mock PLC、Mock Stocker，零硬件依赖完成 100% 业务流程闭环测试。

---

# 三、AI 架构图生成专用提示词 (Architecture Diagram Prompts)

如果您使用 **GPT-4o / Midjourney v6 / DALL-E 3 / Nano Banana / SDXL** 生成专业系统架构图，请使用以下为您量身定制的双语高精度提示词：

### 🎨 英文提示词 (DALL-E 3 / Midjourney 最佳)
```text
A professional, ultra-detailed 3D isometric technical architecture diagram of an Enterprise Robot Control System (RCS) and Semiconductor AGV Fleet Platform. Dark futuristic tech aesthetic with glowing cyan, electric blue, emerald green, and amber neon accents. 

Four distinct horizontal floating glass layers stacked from top to bottom:
1. Top Layer: Modern Web UI Dashboard (Vue 3 Soybean Admin, 3D Digital Twin visualization of robotic arm and AGV, live telemetry charts, 10-step pulse pipeline).
2. Second Layer: Business Core & Domain (ABP Framework .NET 8, Declarative DAG Workflow Engine, SAGA Compensation, OptionCode 32-bit Bitmask Compiler, Location Lock Manager).
3. Third Layer: Pluggable Hardware Adapters (Universal TM 12-Endpoint Driver, Siemens S7 PLC 300ms Batch Scan Engine, Stocker REST/SOAP Adapter, Passbox Interlock, SECS-GEM / MES Ingress).
4. Bottom Layer: Industrial Physical World (Mobile AGV robots with 6-DOF robotic arms, SICK laser LiDAR pulse beams, semiconductor FOUP wafer cassettes, AS/RS stocker racks, industrial cleanroom environment).

Transparent glowing data conduits with glowing particles connecting the layers, high-tech industrial aesthetic, Unreal Engine 5 render, clean cybernetic schematic style, 8k resolution, crisp typography, octane render, photorealistic lighting --ar 16:9 --v 6.0
```

### 🎨 中文提示词 (国内大模型 / 翻译辅助)
```text
工业级半导体机器人调度控制系统 (RCS) 3D 等轴测全景技术架构图。深色未来科技感工业背景，带有青色、电光蓝、翠绿与琥珀金霓虹微光。

画面包含自上而下悬浮堆叠的 4 层发光玻璃层级结构：
1. 顶层（展示层）：现代化 Web 监控大屏（Vue 3 + Soybean Admin，展示机械臂与 AGV 3D 数字孪生模型、实时遥测曲线、10 步脉冲任务步进器）；
2. 第二层（核心业务层）：ABP Framework .NET 8 领域层（声明式 DAG 工作流微内核、SAGA 逆向事务补偿、32 位 OptionCode 动态位图编译器、库位原子锁管理）；
3. 第三层（协议适配层）：可插拔硬件适配器（新松 TM 12 端点驱动、西门子 S7 PLC 300ms 批量扫描引擎、立库 REST/SOAP 适配器、风淋传递窗互锁、MES 接入网关）；
4. 底层（物理工业世界）：移动复合机器人（AGV 配备 6 自由度机械臂、SICK 激光雷达脉冲扫描扇面、12 寸半导体 FOUP 晶圆盒、智能立体库货架、洁净车间环境）。

层与层之间有透明发光的数据管道与光子粒子流贯穿连接，虚幻引擎 5 级高精渲染，清晰的工程示意图标注，8K 超高清分辨率，工业科技美学。
```
