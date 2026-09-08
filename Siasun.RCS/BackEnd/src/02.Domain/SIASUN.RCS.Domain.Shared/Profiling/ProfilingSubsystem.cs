using System;

namespace SIASUN.RCS.Profiling
{
    /// <summary>
    /// 任务步骤剖析子系统常量定义
    /// 涵盖上游接口、工业硬件、调度内核与车体控制
    /// </summary>
    public static class ProfilingSubsystem
    {
        /// <summary>AMA 物料搬运控制系统</summary>
        public const string AMA = "AMA";

        /// <summary>Mica 立库/工作站系统</summary>
        public const string Mica = "Mica";

        /// <summary>工业 PLC 传感器与硬件联锁</summary>
        public const string PLC = "PLC";

        /// <summary>空间库位原子锁竞争与释放</summary>
        public const string LocationLock = "LocationLock";

        /// <summary>底层 AGV 交通与任务管理器 (TM)</summary>
        public const string TM = "TM";

        /// <summary>协作臂/机械臂抓取与放料</summary>
        public const string Arm = "Arm";

        /// <summary>视觉相机二次定位与拍照纠偏</summary>
        public const string Vision = "Vision";

        /// <summary>交管避让与死锁排队</summary>
        public const string TrafficControl = "TrafficControl";

        /// <summary>RCS 核心调度与寻路分配算法</summary>
        public const string Dispatcher = "Dispatcher";

        /// <summary>系统底层自愈与基础设施</summary>
        public const string System = "System";
    }
}
