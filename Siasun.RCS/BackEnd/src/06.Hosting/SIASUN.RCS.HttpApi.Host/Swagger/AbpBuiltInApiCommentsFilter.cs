using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    public class AbpBuiltInApiCommentsFilter : IOperationFilter
    {
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
                if (path.Contains("roles") && method == "GET") { operation.Summary = "获取指定用户拥有的角色列表"; }
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

            // --- 系统资源监控 (System Monitor) ---
            else if (path.StartsWith("api/app/system-monitor"))
            {
                if (path.EndsWith("system-resources") && method == "GET") { operation.Summary = "获取系统资源全局监控视图模型"; operation.Description = "前端大屏直接拉取此接口获取当前进程内存占用状态、日志磁盘水位与容量百分比，用于直接在前端仪表盘上渲染进度条。"; }
            }
        }
    }
}
