using System.Threading.Tasks;
using SIASUN.RCS.Interfaces.OperationLogs;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 提供给前端专用的纯操作审计服务，用于追踪前端无业务逻辑的按钮点击或页面访问行为
    /// </summary>
    public class FrontendAuditAppService : ApplicationService
    {
        private readonly IOperationLogRecorder _operationLog;

        public FrontendAuditAppService(IOperationLogRecorder operationLog)
        {
            _operationLog = operationLog;
        }

        /// <summary>
        /// 记录前端通用按钮点击或行为事件
        /// </summary>
        /// <param name="module">前端所属模块/页面名（例如：报表中心）</param>
        /// <param name="action">前端具体动作（例如：导出昨天报表）</param>
        /// <param name="targetType">目标类型（例如：UI_Button, UI_Page）</param>
        /// <param name="targetKey">目标元素标识（例如：btn_export）</param>
        /// <param name="description">操作详细描述</param>
        public Task RecordFrontendActionAsync(string module, string action, string targetType, string targetKey, string description)
        {
            _operationLog.RecordSuccess(
                module: module,
                action: action,
                targetType: targetType,
                targetKey: targetKey,
                description: description
            );

            return Task.CompletedTask;
        }
    }
}
