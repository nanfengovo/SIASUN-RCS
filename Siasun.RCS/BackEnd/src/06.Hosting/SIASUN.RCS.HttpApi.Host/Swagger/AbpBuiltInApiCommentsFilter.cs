using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// ABP 框架底层动态 Web API 操作中文注释过滤器。
    /// 为 ABP 内置的租户、账号、权限、特性、多语言等接口补充中文标题（Summary）与详细描述（Description）。
    /// </summary>
    public class AbpBuiltInApiCommentsFilter : IOperationFilter
    {
        /// <summary>
        /// 为操作注入中文说明元数据。
        /// </summary>
        /// <param name="operation">OpenAPI 操作对象</param>
        /// <param name="context">操作过滤器上下文</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var path = context.ApiDescription.RelativePath?.ToLower();
            var method = context.ApiDescription.HttpMethod?.ToUpper();
            if (string.IsNullOrEmpty(path)) return;

            // --- 租户管理 (Multi-Tenancy) ---
            if (path.StartsWith("api/multi-tenancy/tenants") || path.StartsWith("api/abp/multi-tenancy/tenants"))
            {
                if (path.EndsWith("default-connection-string"))
                {
                    if (method == "GET") { operation.Summary = "获取租户专属数据库连接字符串"; operation.Description = "【ABP底层】获取指定租户的独立数据库连接字符串（多库模式）。"; }
                    else if (method == "PUT") { operation.Summary = "设置/修改租户专属数据库连接字符串"; operation.Description = "【ABP底层】为指定租户配置独立的数据库连接字符串。"; }
                    else if (method == "DELETE") { operation.Summary = "删除租户专属数据库连接字符串"; operation.Description = "【ABP底层】清除后该租户将回退使用系统默认主数据库。"; }
                }
                else if (path.Contains("by-name"))
                {
                    operation.Summary = "通过名称解析租户信息"; operation.Description = "【ABP底层】用于多租户系统登录前的租户解析。前端输入企业名称后调用此接口换取 TenantId。";
                }
                else if (path.Contains("by-id"))
                {
                    operation.Summary = "通过 ID 解析租户信息"; operation.Description = "【ABP底层】根据 TenantId 获取租户名称等基本信息。";
                }
                else if (path.EndsWith("api/multi-tenancy/tenants/{id}") || path.EndsWith("api/multi-tenancy/tenants"))
                {
                    if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取租户详情"; }
                    else if (method == "GET") { operation.Summary = "获取租户分页列表"; }
                    else if (method == "POST") { operation.Summary = "创建新租户"; }
                    else if (method == "PUT") { operation.Summary = "修改租户基本信息"; }
                    else if (method == "DELETE") { operation.Summary = "删除租户"; }
                }
            }

            // --- 身份认证与个人资料 (Account / Profile / Login) ---
            else if (path.StartsWith("api/account/register")) { operation.Summary = "用户注册"; operation.Description = "【ABP底层】供外部开放注册使用的新用户注册接口。"; }
            else if (path.StartsWith("api/account/send-password-reset-code")) { operation.Summary = "发送密码重置验证码"; operation.Description = "【ABP底层】找回密码第一步：向用户邮箱或手机发送验证码。"; }
            else if (path.StartsWith("api/account/verify-password-reset-token")) { operation.Summary = "验证密码重置 Token"; operation.Description = "【ABP底层】找回密码第二步：验证重置令牌是否有效。"; }
            else if (path.StartsWith("api/account/reset-password")) { operation.Summary = "重置密码"; operation.Description = "【ABP底层】找回密码第三步：提交新密码。"; }
            else if (path.StartsWith("api/account/my-profile"))
            {
                if (path.EndsWith("change-password")) { operation.Summary = "修改当前登录用户的密码"; }
                else if (method == "GET") { operation.Summary = "获取当前登录用户的个人资料"; operation.Description = "【ABP底层】获取当前用户的姓名、手机号、邮箱等资料。"; }
                else if (method == "PUT") { operation.Summary = "更新当前登录用户的个人资料"; }
            }
            else if (path.StartsWith("api/account/login")) { operation.Summary = "账号密码登录"; operation.Description = "【ABP底层】使用账号密码登录系统，通常返回会话凭证。"; }
            else if (path.StartsWith("api/account/logout")) { operation.Summary = "退出登录"; operation.Description = "【ABP底层】注销当前登录会话。"; }
            else if (path.StartsWith("api/account/check-password")) { operation.Summary = "验证当前密码是否正确"; operation.Description = "【ABP底层】常用于敏感操作前的二次安全验证。"; }
            else if (path.StartsWith("api/account/dynamic-claims/refresh")) { operation.Summary = "刷新当前用户的动态声明"; operation.Description = "【ABP底层】当用户的角色或权限在后台被修改后，通过此接口刷新前端 Token 内载荷的身份声明。"; }

            // --- 系统设置 (Setting Management: TimeZone / Emailing) ---
            else if (path.StartsWith("api/setting-management/timezone"))
            {
                if (path.EndsWith("timezones")) { operation.Summary = "获取系统支持的所有时区列表"; operation.Description = "【ABP底层】返回一个下拉框可用的全球时区枚举列表。"; }
                else if (method == "GET") { operation.Summary = "获取当前应用配置的时区"; }
                else if (method == "POST") { operation.Summary = "更新系统默认时区"; }
            }
            else if (path.StartsWith("api/setting-management/emailing"))
            {
                if (path.EndsWith("send-test-email")) { operation.Summary = "发送测试邮件"; operation.Description = "【ABP底层】用于验证当前配置的 SMTP 邮件服务器账号密码是否正确连通。"; }
                else if (method == "GET") { operation.Summary = "获取全局 SMTP 邮件服务器配置"; }
                else if (method == "POST") { operation.Summary = "保存全局 SMTP 邮件服务器配置"; }
            }

            // --- 权限分配 (Permission Management) ---
            else if (path.StartsWith("api/permission-management/permissions"))
            {
                if (path.Contains("by-group")) { operation.Summary = "读取完整权限树 (按权限组划分)"; operation.Description = "【ABP底层】在为角色或用户分配权限时，前端调用此接口渲染多选树形控件。"; }
                else if (path.Contains("resource")) { operation.Summary = "权限资源定义查询操作 (内部接口)"; }
                else if (method == "GET") { operation.Summary = "读取指定目标的权限集合"; }
                else if (method == "PUT") { operation.Summary = "保存对权限树的修改"; }
            }

            // --- 特性开关 (Feature Management) ---
            else if (path.StartsWith("api/feature-management/features"))
            {
                if (method == "GET") { operation.Summary = "读取系统特性(SaaS功能开关)状态"; }
                else if (method == "PUT") { operation.Summary = "保存/更新系统特性开关"; }
                else if (method == "DELETE") { operation.Summary = "重置系统特性到默认状态"; }
            }

            // --- 基础应用与多语言 (Abp Application) ---
            else if (path.Contains("api/abp/application-configuration"))
            {
                operation.Summary = "获取前端应用初始化配置大全";
                operation.Description = "【ABP底层】返回当前用户的权限、本地化多语言文本、全局设置、特性开关等巨型 JSON。通常前端启动时调用一次，用于初始化状态机 (Vuex/Redux)。";
            }
            else if (path.Contains("api/abp/api-definition"))
            {
                operation.Summary = "获取后端 API 定义树";
                operation.Description = "【ABP底层】自动生成前端代理、代码生成器使用的 API 结构描述，包含所有接口的路由、参数、返回值元数据。";
            }
            else if (path.Contains("api/abp/application-localization"))
            {
                operation.Summary = "获取应用的本地化翻译文本";
            }

            // --- 用户与角色 (Identity) ---
            else if (path.StartsWith("api/identity/users"))
            {
                if (path.Contains("lookup"))
                {
                    if (path.Contains("by-username")) { operation.Summary = "按用户名精确查询用户简要信息"; }
                    else if (path.Contains("search")) { operation.Summary = "多条件模糊搜索匹配的用户列表"; }
                    else if (path.Contains("count")) { operation.Summary = "统计符合查询条件的用户总数量"; }
                    else if (path.Contains("{id}")) { operation.Summary = "按用户主键查询用户简要信息"; }
                }
                else if (path.Contains("roles") && method == "GET") { operation.Summary = "获取指定用户拥有的角色列表"; }
                else if (path.Contains("roles") && method == "PUT") { operation.Summary = "修改指定用户拥有的角色"; }
                else if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取用户详情"; }
                else if (method == "GET") { operation.Summary = "分页获取用户列表"; }
                else if (method == "POST") { operation.Summary = "创建新用户"; }
                else if (method == "PUT") { operation.Summary = "修改用户信息"; }
                else if (method == "DELETE") { operation.Summary = "删除用户"; }
            }
            else if (path.StartsWith("api/identity/roles"))
            {
                if (path.Contains("all") && method == "GET") { operation.Summary = "获取所有角色的简要列表(无分页)"; }
                else if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取角色详情"; }
                else if (method == "GET") { operation.Summary = "分页获取角色列表"; }
                else if (method == "POST") { operation.Summary = "创建新角色"; }
                else if (method == "PUT") { operation.Summary = "修改角色信息"; }
                else if (method == "DELETE") { operation.Summary = "删除角色"; }
            }

            // --- 实体审计规则 (Entity Audit Rule) ---
            else if (path.StartsWith("api/app/entity-audit-rule"))
            {
                if (path.EndsWith("discoverable-entity-types")) { operation.Summary = "获取系统中支持审计的实体类型列表"; operation.Description = "获取所有继承自业务实体的类名，用于下拉框选择。"; }
                else if (path.EndsWith("toggle")) { operation.Summary = "启停指定的实体审计规则"; operation.Description = "开启或关闭针对某个实体的变更抓取。"; }
                else if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取实体审计规则详情"; }
                else if (method == "GET") { operation.Summary = "分页查询实体审计规则"; }
                else if (method == "POST") { operation.Summary = "创建实体审计规则"; }
                else if (method == "PUT") { operation.Summary = "修改实体审计规则"; }
                else if (method == "DELETE") { operation.Summary = "删除实体审计规则"; }
            }

            // --- 接口审计日志过滤规则 (Audit Log Filter Rule) ---
            else if (path.StartsWith("api/app/audit-log-filter-rule"))
            {
                if (path.EndsWith("toggle")) { operation.Summary = "启停指定的审计日志过滤规则"; operation.Description = "开启或关闭该黑白名单过滤规则。"; }
                else if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取审计日志过滤规则详情"; }
                else if (method == "GET") { operation.Summary = "分页查询审计日志过滤规则"; }
                else if (method == "POST") { operation.Summary = "创建审计日志过滤规则"; }
                else if (method == "PUT") { operation.Summary = "修改审计日志过滤规则"; }
                else if (method == "DELETE") { operation.Summary = "删除审计日志过滤规则"; }
            }

            // --- 定时任务中台 (Background Job) ---
            else if (path.StartsWith("api/app/background-job"))
            {
                if (path.EndsWith("pause")) { operation.Summary = "暂停指定的后台任务"; operation.Description = "停止某个 Quartz Job 的定时触发。"; }
                else if (path.EndsWith("resume")) { operation.Summary = "恢复指定的后台任务"; operation.Description = "恢复某个 Quartz Job 的定时触发。"; }
                else if (path.EndsWith("trigger-now")) { operation.Summary = "立即手动触发一次指定的后台任务"; operation.Description = "无视 Cron 表达式，强制立即执行一次。"; }
                else if (path.EndsWith("cron") && method == "PUT") { operation.Summary = "动态修改指定后台任务的 Cron 表达式"; }
                else if (method == "GET") { operation.Summary = "获取所有后台任务状态监控列表"; }
                else if (method == "POST") { operation.Summary = "操作后台任务"; }
            }

            // --- 日志动态降级 (Log Control) ---
            else if (path.StartsWith("api/app/log-control"))
            {
                if (path.EndsWith("levels") && method == "GET") { operation.Summary = "获取所有支持动态调级的日志命名空间及其当前级别"; }
                else if (path.EndsWith("set-level") && method == "POST") { operation.Summary = "动态调整指定命名空间的日志级别"; operation.Description = "允许现场排障时一键将系统的日志级别从 Info 降为 Debug，抓完报文再调回，无需重启服务。"; }
            }

            // --- 系统资源与容量健康监控 (System Monitor) ---
            else if (path.StartsWith("api/app/system-monitor"))
            {
                if (path.EndsWith("system-resources") && method == "GET") { operation.Summary = "获取系统资源全局监控视图模型"; operation.Description = "前端大屏直接拉取此接口获取当前进程内存占用状态、日志磁盘水位与容量百分比，用于直接在前端仪表盘上渲染进度条。"; }
                else if (path.EndsWith("capacity-health") && method == "GET") { operation.Summary = "获取长期容量可观测与前瞻健康评估报告"; operation.Description = "【L4工业自治观测】评估工控机磁盘水位、持久化日志膨胀行数、异步审计通道积压与特权溢流保全健康等级，提前识别容量瓶颈。"; }
            }

            // --- 调度干预操作 (Dispatch Intervention) ---
            else if (path.StartsWith("api/app/dispatch-intervention"))
            {
                if (path.EndsWith("cancel-task") && method == "POST") { operation.Summary = "调度员人工干预：强制取消任务"; operation.Description = "【调度干预】记录操作人、原因、BeforeState/AfterState，终止任务并触发状态步进收敛。"; }
                else if (path.EndsWith("force-end-task") && method == "POST") { operation.Summary = "调度员人工干预：强制结单任务"; operation.Description = "【调度干预】记录操作人、原因、BeforeState/AfterState，标记任务成功强制结单并释放库位锁。"; }
                else if (path.EndsWith("assign-vehicle") && method == "POST") { operation.Summary = "调度员人工干预：强制指派执行车辆"; operation.Description = "【调度干预】记录操作人、原因、BeforeState/AfterState，解绑原车并重新人工强制指派目标 AGV。"; }
                else if (path.EndsWith("reset-vehicle") && method == "POST") { operation.Summary = "调度员人工干预：复位车辆异常状态与清除警报"; operation.Description = "【调度干预】记录操作人、原因、BeforeState/AfterState，复位目标车辆报警并解除工控锁。"; }
                else if (path.EndsWith("resume-task") && method == "POST") { operation.Summary = "调度员人工干预：恢复失败任务"; operation.Description = "【调度干预】显式将 Failed 状态重置为 Running 并断点恢复步进推进，记录操作人、原因、前后状态审计。"; }
                else if (path.EndsWith("rollback-and-retry") && method == "POST") { operation.Summary = "调度员人工干预：回滚步骤并重试"; operation.Description = "【SAGA补偿】将任务步进安全回滚到指定历史步骤，触发补偿事件并重新调度执行。"; }
            }

            // --- 前端审计留痕 (Frontend Audit) ---
            else if (path.StartsWith("api/app/frontend-audit"))
            {
                if (path.EndsWith("page-access") && method == "POST") { operation.Summary = "前端页面访问审计留痕"; operation.Description = "记录调度员在 Web 前端访问的页面、路由及停留时间，纳入定分止争操作日志链。"; }
            }

            // --- 调度员操作日志 (Operation Log) ---
            else if (path.StartsWith("api/app/operation-log"))
            {
                if (method == "GET" && path.Contains("{id}"))
                {
                    operation.Summary = "获取操作审计日志详情";
                    operation.Description = "根据唯一标识获取单条操作审计日志的详细变更快照、操作人及执行上下文。";
                }
                else if (method == "GET")
                {
                    operation.Summary = "分页查询操作审计日志";
                    operation.Description = "多条件筛选调度员手工干预、系统自动触发及外部交互的全量操作历史记录。";
                }
            }

            // --- 事故排障黑匣子 (Flight Pack) ---
            else if (path.StartsWith("api/app/flight-pack"))
            {
                if (path.EndsWith("export") && method == "POST")
                {
                    operation.Summary = "导出事故排障黑匣子压缩包 (.rcspack)";
                    operation.Description = "根据任务号或车辆编号锚点，汇聚关联 API 报文、操作轨迹与系统异常三源证据，生成离线排障飞行数据包，并可选用 AI 深度推理分析。";
                }
            }

            // --- 空间库位原子锁与人工运维 (Location Lock) ---
            else if (path.StartsWith("api/app/location-lock"))
            {
                if (path.EndsWith("active-locks") && method == "GET") { operation.Summary = "获取当前所有活跃库位锁与维护状态"; operation.Description = "【核心微内核】大屏实时拉取当前所有被小车占用或处于人工检修维护中的库位列表。"; }
                else if (path.EndsWith("lock") && method == "GET") { operation.Summary = "查询指定库位的实时锁定详情"; operation.Description = "【核心微内核】获取指定业务库位的持锁小车、任务ID、租约到期时间与锁类型。"; }
                else if (path.EndsWith("force-unlock") && method == "POST") { operation.Summary = "调度员人工干预：强制解除库位锁"; operation.Description = "【定分止争审计】调度员人工强制释放库位锁定状态，记录修改前状态、修改后状态及人工原因。"; }
                else if (path.EndsWith("lock-for-maintenance") && method == "POST") { operation.Summary = "调度员人工干预：库位维护封锁"; operation.Description = "【运维安全】将库位标记为维护状态，排斥所有作业小车进入或停靠。"; }
                else if (path.EndsWith("unlock-maintenance") && method == "POST") { operation.Summary = "调度员人工干预：解除库位维护封锁"; operation.Description = "【运维安全】机台检修完毕，解除人工封锁，恢复调度系统正常可用。"; }
            }

            // --- 业务库位与 AGV 地图点位映射 (Location Map) ---
            else if (path.StartsWith("api/app/location-map"))
            {
                if (path.EndsWith("active-list") && method == "GET") { operation.Summary = "获取全部已启用的点位映射列表"; operation.Description = "【地图解耦】获取系统中全部有效配置的机台库位与 AGV 实际站点映射关系。"; }
                else if (path.EndsWith("by-location-code") && method == "GET") { operation.Summary = "根据业务库位编码精准反查地图站点"; operation.Description = "【地图解耦】查询库位对应的实际 TM 停靠站点、前置引导点及姿态角。"; }
                else if (path.EndsWith("refresh-cache") && method == "POST") { operation.Summary = "手动触发微内核点位高速缓存热刷新"; operation.Description = "【系统自治】主动广播领域事件，即刻热重载内存并发字典点位映射。"; }
                else if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取单个点位映射详情"; }
                else if (method == "GET") { operation.Summary = "分页查询点位映射列表"; operation.Description = "支持按库位编码、站点编码或区域关键字进行多条件模糊筛选。"; }
                else if (method == "POST") { operation.Summary = "创建新的库位地图点位映射"; }
                else if (method == "PUT") { operation.Summary = "修改点位映射配置信息"; }
                else if (method == "DELETE") { operation.Summary = "删除指定的点位映射"; }
            }

            // --- 库位 PLC 硬件联锁点表配置 (Location PLC Config) ---
            else if (path.StartsWith("api/app/location-plc-config"))
            {
                if (path.EndsWith("by-location-code") && method == "GET") { operation.Summary = "根据业务库位编码获取关联的 PLC 硬件联锁点表"; }
                else if (method == "GET" && path.Contains("{id}")) { operation.Summary = "获取单个库位 PLC 联锁配置详情"; }
                else if (method == "GET") { operation.Summary = "分页查询库位 PLC 硬件联锁配置列表"; }
                else if (method == "POST") { operation.Summary = "创建库位关联的 PLC 硬件联锁点表"; }
                else if (method == "PUT") { operation.Summary = "修改库位关联的 PLC 硬件联锁配置"; }
                else if (method == "DELETE") { operation.Summary = "删除库位关联的 PLC 硬件联锁配置"; }
            }

            // --- 任务步骤剖析与时序画像 (Task Profiling) ---
            else if (path.StartsWith("api/app/task-profiling"))
            {
                if (path.EndsWith("task-timeline-profiling") && method == "GET") { operation.Summary = "获取指定任务的时序流转画像与多系统耗时拆解"; operation.Description = "【大屏流转】支撑前端任务详情弹窗：TM 状态轴、上游批次出库轴、多系统交互流水与耗时拆解。"; }
                else if (path.EndsWith("metrics-summary") && method == "GET") { operation.Summary = "获取宏观任务执行指标与 Dashboard 性能统计"; operation.Description = "【效能度量】支撑 Dashboard_数据统计(1).xlsx 核心 P0 指标（任务总耗时、臂动作耗时、车体运动耗时、上下极限值、稼动率及 MTBA）。"; }
                else if (path.EndsWith("step-list") && method == "GET") { operation.Summary = "分页查询细粒度步骤剖析流水明细"; operation.Description = "支持按任务号、子系统、车辆编号及时间区间查询每一毫秒的步骤调用明细。"; }
            }

            // --- 32位 OptionCode 动态位图编解码与快照固化 ---
            else if (path.StartsWith("api/app/option-code"))
            {
                if (path.EndsWith("schemas") && method == "GET") { operation.Summary = "获取系统所有已注册的 OptionCode Schema 列表"; operation.Description = "供前端可视化位图设计器、大屏下拉选择以及配置中心使用。"; }
                else if (path.EndsWith("schema") && method == "GET") { operation.Summary = "获取指定代号与版本的 OptionCode Schema 详情"; operation.Description = "返回包含各 Part、位宽、起始位、掩码、数据源与枚举映射字典的完整位图结构。"; }
                else if (path.EndsWith("compile") && method == "POST") { operation.Summary = "动态装配入参并编译为 32 位 OptionCode 指令"; operation.Description = "支持从 const、args、master、leg、port 提取多源数据并执行 LSB 32 位位掩码正向编译，并返回即时反解预览。"; }
                else if (path.EndsWith("decode") && method == "POST") { operation.Summary = "对 32 位 OptionCode 报文字符串进行逆向反解"; operation.Description = "分解为二进制位图、十进制数值，并结合现场 Schema 枚举映射表翻译为业务中文描述。"; }
                else if (path.EndsWith("freeze-task-option-code") && method == "POST") { operation.Summary = "为在途任务编译并固化 OptionCode 快照"; operation.Description = "建单或派发时立即冻结快照至 AgvTask 聚合根，杜绝后续主数据修改导致在途运单参数漂移。"; }
            }
        }
    }
}
