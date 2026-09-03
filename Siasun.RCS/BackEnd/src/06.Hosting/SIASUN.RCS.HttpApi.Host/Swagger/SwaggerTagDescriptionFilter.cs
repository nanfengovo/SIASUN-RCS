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

            var dict = new Dictionary<string, string>
            {
                { "BackgroundJob", "后台定时任务监控与管理" },
                { "LogControl", "系统日志动态调级" },
                { "SystemMonitor", "系统硬件与资源监控视图" },
                { "AuditLogFilterRule", "接口审计日志过滤规则" },
                { "EntityAuditRule", "实体变更审计规则" },
                { "Features", "系统特性与租户开关" },
                { "Permissions", "角色权限树分配" }
            };

            foreach (var kvp in dict)
            {
                var tag = swaggerDoc.Tags.FirstOrDefault(t => t.Name == kvp.Key);
                if (tag == null)
                {
                    swaggerDoc.Tags.Add(new OpenApiTag { Name = kvp.Key, Description = kvp.Value });
                }
                else
                {
                    tag.Description = kvp.Value;
                }
            }
        }
    }
}
