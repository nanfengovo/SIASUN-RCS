using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SIASUN.RCS.Swagger
{
    public class SwaggerTagDescriptionFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc.Tags == null)
            {
                swaggerDoc.Tags = new HashSet<OpenApiTag>();
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BackgroundJob", "后台定时任务监控与管理" },
                { "LogControl", "系统日志动态调级" },
                { "SystemMonitor", "系统硬件与资源监控视图" },
                { "AuditLogFilterRule", "接口审计日志过滤规则" },
                { "EntityAuditRule", "实体变更审计规则" },
                { "FrontendAudit", "前端操作审计打点" },
                { "Features", "系统特性与租户开关" },
                { "Permissions", "角色权限树分配" },
                { "User", "用户身份与账号管理" },
                { "Role", "系统角色与权限组管理" },
                { "Tenant", "多租户隔离管理" }
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
            var orphanedTags = swaggerDoc.Tags.Where(t => !activeTagNames.Contains(t.Name)).ToList();
            foreach (var orphan in orphanedTags)
            {
                swaggerDoc.Tags.Remove(orphan);
            }
        }
    }
}
