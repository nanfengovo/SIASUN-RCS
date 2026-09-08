using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.OptionCodes.Dtos
{
    /// <summary>
    /// 任务 OptionCode 快照冻结请求入参
    /// </summary>
    public class FreezeTaskOptionCodeInput
    {
        /// <summary>
        /// 目标任务实体全局唯一 ID
        /// </summary>
        [Required]
        public Guid TaskId { get; set; }

        /// <summary>
        /// 使用的 Schema 业务代号（例如 "txc_demo"、"erack"、"molding"）
        /// </summary>
        [Required]
        public string SchemaCode { get; set; } = string.Empty;

        /// <summary>
        /// 使用的 Schema 版本号（可选，默认最新版本）
        /// </summary>
        public int? SchemaVersion { get; set; }

        /// <summary>
        /// 任务动态参数字典（可选）
        /// </summary>
        public Dictionary<string, object?>? TaskArgs { get; set; }

        /// <summary>
        /// 点位主数据字典（可选）
        /// </summary>
        public Dictionary<string, object?>? MasterValues { get; set; }

        /// <summary>
        /// 激活的程段（例如 "Fetch"、"Put"）
        /// </summary>
        public string? ActiveLeg { get; set; }

        /// <summary>
        /// 库位槽位端口
        /// </summary>
        public string? Port { get; set; }

        /// <summary>
        /// 显式指定的 OptionCode 报文字符串（若传入则直接固化，不再重新装配编译）
        /// </summary>
        public string? ExplicitOptionCode { get; set; }
    }
}

