using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.OptionCodes.Dtos
{
    /// <summary>
    /// 动态装配并编译 OptionCode 请求入参
    /// </summary>
    public class CompileOptionCodeInput
    {
        /// <summary>
        /// 使用的 Schema 业务代号（例如 "txc_demo"、"erack"、"molding"）
        /// </summary>
        [Required]
        public string SchemaCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 版本号（可选，若为空则使用最新版本）
        /// </summary>
        public int? SchemaVersion { get; set; }

        /// <summary>
        /// 任务动态参数字典（对应 Source == Args）
        /// </summary>
        public Dictionary<string, object?>? TaskArgs { get; set; }

        /// <summary>
        /// 点位/设备主数据字典（对应 Source == Master）
        /// </summary>
        public Dictionary<string, object?>? MasterValues { get; set; }

        /// <summary>
        /// 当前程段或动作方向（例如 "Fetch"、"Put"，对应 Source == Leg）
        /// </summary>
        public string? ActiveLeg { get; set; }

        /// <summary>
        /// 目标设备库位槽位/端口字符串（例如 "1"、"Slot-02"，对应 Source == Port）
        /// </summary>
        public string? Port { get; set; }

        /// <summary>
        /// 载具编号
        /// </summary>
        public string? CarrierCode { get; set; }

        /// <summary>
        /// 车辆编号
        /// </summary>
        public string? AgvCode { get; set; }
    }
}

