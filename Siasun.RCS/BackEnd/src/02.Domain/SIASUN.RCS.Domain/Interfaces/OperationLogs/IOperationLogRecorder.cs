
namespace SIASUN.RCS.Interfaces.OperationLogs
{
    public interface IOperationLogRecorder
    {
        /// <summary>
        /// 记录成功操作（放入异步通道，不阻塞主业务）
        /// </summary>
        /// <param name="module"></param>
        /// <param name="action"></param>
        /// <param name="targetType"></param>
        /// <param name="targetKey"></param>
        /// <param name="description"></param>
        void RecordSuccess(string module, string action, string targetType, string targetKey, string description);

        /// <summary>
        /// 记录失败操作（放入异步通道，确保即便主事务回滚也能落盘）
        /// </summary>
        /// <param name="module"></param>
        /// <param name="action"></param>
        /// <param name="targetType"></param>
        /// <param name="targetKey"></param>
        /// <param name="description"></param>
        /// <param name="errorMessage"></param>
        void RecordFailure(string module, string action, string targetType, string targetKey, string description, string errorMessage);
    }
}