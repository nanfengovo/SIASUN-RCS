namespace SIASUN.RCS.Diagnostics.AI
{
    /// <summary>
    /// AI 事故根因智能诊断引擎系统配置选项
    /// </summary>
    public class AiDiagnosticsOptions
    {
        /// <summary>
        /// 是否启用 AI 深度根因诊断模块（默认 false，零性能损失）
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// AI 提供者类型（如 Ollama, OpenAICompatible）
        /// </summary>
        public string Provider { get; set; } = "Ollama";

        /// <summary>
        /// 兼容 OpenAI 格式的模型接口服务地址 (如 http://localhost:11434/v1)
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:11434/v1";

        /// <summary>
        /// API 密钥 (本地 Ollama 可为空，云端或代理需填写)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 使用的大语言模型名称 (如 deepseek-r1:7b, qwen2.5:7b)
        /// </summary>
        public string Model { get; set; } = "deepseek-r1:7b";

        /// <summary>
        /// 推理超时时间（秒），默认 60 秒
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 最大输出 Token 数
        /// </summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        /// 模型采样温度 (工业排障推断建议 0.1 ~ 0.3 确保严谨确定性)
        /// </summary>
        public double Temperature { get; set; } = 0.2;
    }
}
