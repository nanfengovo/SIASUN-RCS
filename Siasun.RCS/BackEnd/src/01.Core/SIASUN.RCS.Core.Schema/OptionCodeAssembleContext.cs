using System;
using System.Collections.Generic;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 字段多源装配执行上下文
    /// </summary>
    public class OptionCodeAssembleContext
    {
        /// <summary>
        /// 任务动态业务参数字典（对应 Source == Args）
        /// </summary>
        public IReadOnlyDictionary<string, object?>? TaskArgs { get; set; }

        /// <summary>
        /// 库位/站点主数据字典（对应 Source == Master）
        /// </summary>
        public IReadOnlyDictionary<string, object?>? MasterValues { get; set; }

        /// <summary>
        /// 当前激活的程段或动作方向（例如 "Fetch"、"Put"，对应 Source == Leg）
        /// </summary>
        public string? ActiveLeg { get; set; }

        /// <summary>
        /// 目标物理库位槽位/端口字符串（例如 "1"、"P02"，对应 Source == Port）
        /// </summary>
        public string? Port { get; set; }

        /// <summary>
        /// 载具/晶圆盒编号
        /// </summary>
        public string? CarrierCode { get; set; }

        /// <summary>
        /// 执行车体编号
        /// </summary>
        public string? AgvCode { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public OptionCodeAssembleContext()
        {
        }

        /// <summary>
        /// 快速构造函数
        /// </summary>
        /// <param name="taskArgs">任务入参</param>
        /// <param name="masterValues">点位主数据</param>
        /// <param name="activeLeg">当前程段</param>
        /// <param name="port">槽位端口</param>
        public OptionCodeAssembleContext(
            IReadOnlyDictionary<string, object?>? taskArgs = null,
            IReadOnlyDictionary<string, object?>? masterValues = null,
            string? activeLeg = null,
            string? port = null)
        {
            TaskArgs = taskArgs;
            MasterValues = masterValues;
            ActiveLeg = activeLeg;
            Port = port;
        }
    }
}

