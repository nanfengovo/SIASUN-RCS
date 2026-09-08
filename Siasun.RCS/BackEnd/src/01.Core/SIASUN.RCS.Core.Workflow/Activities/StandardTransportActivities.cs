using System;

namespace SIASUN.RCS.Tasks.Workflow.Activities
{
    /// <summary>
    /// 标准化 10 步物料搬运微内核工序常量与活动阶段定义
    /// 固化半导体洁净室（FOUP/晶圆盒）、高精密制造与智能立库搬运的标准业务闭环
    /// </summary>
    public static class StandardTransportActivities
    {
        /// <summary>第 1 步：下发空车前往取货工位路径</summary>
        public const string DispatchPickLeg = "DispatchPickLeg";

        /// <summary>第 2 步：到达取货工位微调对位</summary>
        public const string ArrivePickStation = "ArrivePickStation";

        /// <summary>第 3 步：取货点机台/风淋门 PLC 安全联锁确认</summary>
        public const string PickInterlock = "PickInterlock";

        /// <summary>第 4 步：执行伸臂/举升取货动作</summary>
        public const string FetchCarrier = "FetchCarrier";

        /// <summary>第 5 步：载具 RFID/条码一致性防呆核验</summary>
        public const string ValidateCarrier = "ValidateCarrier";

        /// <summary>第 6 步：重载行驶前往目标放货工位</summary>
        public const string DispatchDropLeg = "DispatchDropLeg";

        /// <summary>第 7 步：到达放货目标工位精准对位</summary>
        public const string ArriveDropStation = "ArriveDropStation";

        /// <summary>第 8 步：放货目标位空闲光电检测与 PLC 联锁</summary>
        public const string DropInterlock = "DropInterlock";

        /// <summary>第 9 步：执行伸臂/下降放货卸载动作</summary>
        public const string PutCarrier = "PutCarrier";

        /// <summary>第 10 步：放货后光电复检、释放库位锁与任务完结</summary>
        public const string PostCheckComplete = "PostCheckComplete";

        /// <summary>
        /// 获取标准化 10 步流程的步骤名称清单
        /// </summary>
        public static readonly string[] StandardSteps =
        {
            DispatchPickLeg,
            ArrivePickStation,
            PickInterlock,
            FetchCarrier,
            ValidateCarrier,
            DispatchDropLeg,
            ArriveDropStation,
            DropInterlock,
            PutCarrier,
            PostCheckComplete
        };
    }
}
