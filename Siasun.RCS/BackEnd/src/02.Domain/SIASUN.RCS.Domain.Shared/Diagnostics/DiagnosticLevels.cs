namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 诊断与遥测日志级别标准常量定义
    /// 统一各层日志、推流与自适应限流过滤评级标准
    /// </summary>
    public static class DiagnosticLevels
    {
        /// <summary>
        /// 细粒度跟踪级别
        /// </summary>
        public const string Trace = "Trace";

        /// <summary>
        /// 调试级别
        /// </summary>
        public const string Debug = "Debug";

        /// <summary>
        /// 常规业务与运行信息级别
        /// </summary>
        public const string Information = "Information";

        /// <summary>
        /// 业务预警或潜在故障隐患级别（铁证特权放行）
        /// </summary>
        public const string Warning = "Warning";

        /// <summary>
        /// 运行错误与故障级别（铁证特权放行）
        /// </summary>
        public const string Error = "Error";

        /// <summary>
        /// 致命严重事故级别（铁证特权放行）
        /// </summary>
        public const string Fatal = "Fatal";
    }
}
