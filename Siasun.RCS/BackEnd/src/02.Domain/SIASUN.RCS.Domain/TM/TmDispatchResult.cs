namespace SIASUN.RCS.TM
{
    /// <summary>
    /// TM 下发派发调用响应结果
    /// </summary>
    public class TmDispatchResult
    {
        /// <summary>
        /// 下发是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 底层 TM 生成或派发绑定的任务流水号
        /// </summary>
        public string TmSerial { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 成功构造函数
        /// </summary>
        public static TmDispatchResult Succeeded(string tmSerial) => new()
        {
            Success = true,
            TmSerial = tmSerial
        };

        /// <summary>
        /// 失败构造函数
        /// </summary>
        public static TmDispatchResult Failed(string errorMessage) => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
