using System;
using System.Text.RegularExpressions;

namespace SIASUN.RCS.Infrastructure.Logging.Masking
{
    /// <summary>
    /// 工业级日志敏感凭据脱敏工具类
    /// 在报文进入持久化通道与黑匣子前，自动对口令、密钥与 Token 进行掩码处理
    /// </summary>
    public static class AuditDataMasker
    {
        private static readonly Regex JsonSensitiveFieldRegex = new(
            @"(""(?:password|pwd|secret|token|access_token|authorization|apikey|api_key)""\s*:\s*"")([^""]+)("")",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BearerTokenRegex = new(
            @"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 对报文字符串进行敏感数据脱敏处理
        /// </summary>
        /// <param name="content">待脱敏报文</param>
        /// <returns>脱敏后的安全字符串</returns>
        public static string? Mask(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var masked = BearerTokenRegex.Replace(content, "Bearer ******");
            masked = JsonSensitiveFieldRegex.Replace(masked, "$1******$3");
            return masked;
        }
    }
}

