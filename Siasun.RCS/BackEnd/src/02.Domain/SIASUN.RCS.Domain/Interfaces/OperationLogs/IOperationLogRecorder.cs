using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;

namespace SIASUN.RCS.Interfaces.OperationLogs
{
    /// <summary>
    /// 调度员操作与系统自审计日志记录器接口
    /// 承载现场第 2 层不可抵赖的操作审计，非阻塞式异步写入内存通道
    /// </summary>
    public interface IOperationLogRecorder
    {
        /// <summary>
        /// 记录包含完整上下文的操作审计条目（推荐使用）
        /// </summary>
        /// <param name="context">操作审计上下文（含状态变更、原因与关联 Task/AGV）</param>
        /// <param name="status">执行状态 (Success / Failed)</param>
        /// <param name="errorMessage">失败时的错误消息</param>
        void Record(OperationLogContext context, OperationLogStatus status = OperationLogStatus.Success, string? errorMessage = null);

        /// <summary>
        /// 记录成功操作（放入异步通道，不阻塞主业务）
        /// </summary>
        /// <param name="module">所属业务模块</param>
        /// <param name="action">操作动作</param>
        /// <param name="targetType">目标对象类型</param>
        /// <param name="targetKey">目标对象键值</param>
        /// <param name="description">操作描述</param>
        void RecordSuccess(string module, string action, string targetType, string targetKey, string description);

        /// <summary>
        /// 记录失败操作（放入异步通道，确保即便主事务回滚也能落盘）
        /// </summary>
        /// <param name="module">所属业务模块</param>
        /// <param name="action">操作动作</param>
        /// <param name="targetType">目标对象类型</param>
        /// <param name="targetKey">目标对象键值</param>
        /// <param name="description">操作描述</param>
        /// <param name="errorMessage">错误异常信息</param>
        void RecordFailure(string module, string action, string targetType, string targetKey, string description, string errorMessage);
    }
}