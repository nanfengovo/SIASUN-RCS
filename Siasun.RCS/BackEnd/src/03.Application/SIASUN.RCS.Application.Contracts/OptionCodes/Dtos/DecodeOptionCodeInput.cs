using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.OptionCodes.Dtos
{
    /// <summary>
    /// OptionCode 逆向反解请求入参
    /// </summary>
    public class DecodeOptionCodeInput
    {
        /// <summary>
        /// 待解析的 OptionCode 报文字符串（例如 "33621253,33687299"）
        /// </summary>
        [Required]
        public string OptionCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 业务代号（例如 "txc_demo"、"molding"）
        /// </summary>
        [Required]
        public string SchemaCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 版本号（可选，默认最新版本）
        /// </summary>
        public int? SchemaVersion { get; set; }
    }
}

