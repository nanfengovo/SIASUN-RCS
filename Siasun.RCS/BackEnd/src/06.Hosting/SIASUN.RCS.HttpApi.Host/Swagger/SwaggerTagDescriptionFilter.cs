using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    /// <summary>
    /// Swagger 分组与标签中文业务描述过滤器。
    /// 负责为 OpenApi 文档中出现的各类业务与 ABP 底层服务 Tag 补充精准的中文功能说明，并剔除孤儿 Tag。
    /// </summary>
    public class SwaggerTagDescriptionFilter : IDocumentFilter
    {
        /// <summary>
        /// 应用标签描述到 OpenAPI 文档。
        /// </summary>
        /// <param name="swaggerDoc">OpenAPI 文档对象</param>
        /// <param name="context">过滤器上下文</param>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc.Tags == null)
            {
                swaggerDoc.Tags = new HashSet<OpenApiTag>();
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // --- ABP 底层基础服务标签说明 ---
                { "AbpApiDefinition", "后端 API 架构元数据与接口定义导出" },
                { "AbpApplicationConfiguration", "前端应用初始化全局配置与多语言权限全集" },
                { "AbpApplicationLocalization", "应用多语言与本地化翻译资源字典" },
                { "AbpTenant", "多租户信息解析与连接字符串管理" },
                { "Account", "用户账户注册与密码重置" },
                { "DynamicClaims", "动态身份声明与凭证刷新" },
                { "EmailSettings", "系统邮件服务与 SMTP 服务器配置" },
                { "Features", "系统特性与租户开关" },
                { "Login", "用户登录认证、登出与会话鉴权" },
                { "Permissions", "角色权限树分配" },
                { "Profile", "当前登录用户个人中心资料与密码修改" },
                { "Role", "系统角色与权限组管理" },
                { "Tenant", "多租户隔离管理" },
                { "TimeZoneSettings", "系统时区设置与时区列表" },
                { "User", "用户身份与账号管理" },
                { "UserLookup", "用户跨模块快速检索与简要信息查询" },
                { "AuditLogs", "系统底层审计日志与接口追踪" },
                { "SecurityLogs", "用户安全与登录审计日志" },
                { "Settings", "系统通用基础设置管理" },

                // --- RCS 核心调度与监控服务标签说明 ---
                { "BackgroundJob", "后台定时任务监控与管理" },
                { "LogControl", "系统日志动态调级" },
                { "SystemMonitor", "系统硬件与资源监控视图" },
                { "AuditLogFilterRule", "接口审计日志过滤规则" },
                { "EntityAuditRule", "实体变更审计规则" },
                { "FrontendAudit", "前端操作审计打点" },
                { "OperationLog", "调度员操作与系统自审计日志" },
                { "FlightPack", "黑匣子全时序事故取证排障包" },
                { "DispatchIntervention", "调度员人工干预控制（取消、强制完结、指派、复位）" }
            };

            // 获取当前文档中实际被接口使用的所有 Tag
            var activeTagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (swaggerDoc.Paths != null)
            {
                foreach (var pathItem in swaggerDoc.Paths.Values)
                {
                    if (pathItem.Operations == null) continue;
                    foreach (var op in pathItem.Operations.Values)
                    {
                        if (op.Tags == null) continue;
                        foreach (var tag in op.Tags)
                        {
                            if (!string.IsNullOrEmpty(tag.Name))
                            {
                                activeTagNames.Add(tag.Name);
                            }
                        }
                    }
                }
            }

            // 1. 对于当前文档中真实存在的 Tag，如果字典中有说明，为其补充 Description
            foreach (var kvp in dict)
            {
                if (!activeTagNames.Contains(kvp.Key))
                {
                    continue;
                }

                var tag = swaggerDoc.Tags.FirstOrDefault(t => string.Equals(t.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (tag == null)
                {
                    swaggerDoc.Tags.Add(new OpenApiTag { Name = kvp.Key, Description = kvp.Value });
                }
                else
                {
                    tag.Description = kvp.Value;
                }
            }

            // 2. 清理掉当前文档中没有任何接口引用的孤儿 Tag（避免出现空分组折叠栏）
            var orphanedTags = swaggerDoc.Tags.Where(t => string.IsNullOrEmpty(t.Name) || !activeTagNames.Contains(t.Name)).ToList();
            foreach (var orphan in orphanedTags)
            {
                swaggerDoc.Tags.Remove(orphan);
            }
        }
    }
}
