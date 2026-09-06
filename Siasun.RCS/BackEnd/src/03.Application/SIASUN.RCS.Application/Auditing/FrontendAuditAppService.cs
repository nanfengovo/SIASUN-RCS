using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 提供给前端专用的纯操作审计服务，用于追踪前端无业务逻辑的按钮点击或页面访问行为
    /// 提供给前端专用的纯界面操作审计服务
    /// 用于受控追踪前端按钮点击、视图切换等纯展示层行为，防止未授权伪造
    /// </summary>
    [Authorize(RCSPermissions.FrontendAudit.Default)]
    public class FrontendAuditAppService : ApplicationService
    {
        private readonly IOperationLogRecorder _operationLog;

        /// <summary>
        /// 构造函数注入操作日志记录器
        /// </summary>
        /// <param name="operationLog">操作日志记录器</param>
        public FrontendAuditAppService(IOperationLogRecorder operationLog)
        {
            _operationLog = operationLog;
        }

        /// <summary>
        /// 记录前端界面交互行为事件
        /// </summary>
        /// <param name="module">前端所属模块/页面名（例如：报表中心、监控大屏）</param>
        /// <param name="action">前端具体动作（例如：切换主题、点击刷新）</param>
        /// <param name="targetType">目标类型（必须以 UI_ 开头，如 UI_Button, UI_Page）</param>
        /// <param name="targetKey">目标元素标识（例如：btn_export, view_fleet）</param>
        /// <param name="description">操作详细描述</param>
        public Task RecordFrontendActionAsync(string module, string action, string targetType, string targetKey, string description)
        {
            // 防御性校验：禁止前端伪造核心调度与底层车体控制命令
            if (string.IsNullOrWhiteSpace(targetType) || !targetType.StartsWith("UI_", StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException("前端埋点仅允许记录界面级交互事件（targetType 必须以 'UI_' 开头）！");
            }

            _operationLog.RecordSuccess(
                module: string.IsNullOrWhiteSpace(module) ? "Frontend" : module,
                action: string.IsNullOrWhiteSpace(action) ? "Click" : action,
                targetType: targetType,
                targetKey: targetKey ?? string.Empty,
                description: description ?? string.Empty
            );

            return Task.CompletedTask;
        }
    }
}
