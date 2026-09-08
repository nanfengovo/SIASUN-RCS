using System;
using SIASUN.RCS.Profiling;

namespace SIASUN.RCS.Tasks.Profiling
{
    /// <summary>
    /// 任务步骤剖析器领域接口
    /// 提供轻量级 using 作用域秒表语法糖与异步剖析记录能力
    /// </summary>
    public interface ITaskProfiler
    {
        /// <summary>
        /// 开启一个可自动计时的步骤作用域
        /// 离开 using 代码块时自动计算耗时并推入异步缓冲管道
        /// </summary>
        /// <param name="taskCode">关联任务唯一编号</param>
        /// <param name="subsystem">子系统类别 (常量参见 ProfilingSubsystem)</param>
        /// <param name="operationName">操作或方法名称</param>
        /// <param name="agvId">关联的 AGV 编号（可选）</param>
        /// <param name="stepIndex">任务步进索引</param>
        /// <param name="activeLeg">激活程段标识</param>
        /// <param name="summary">操作摘要</param>
        /// <param name="batchId">批次标识</param>
        /// <returns>计时作用域，Dispose 时自动记录</returns>
        IDisposable BeginStep(
            string taskCode,
            string subsystem,
            string operationName,
            string? agvId = null,
            int stepIndex = 0,
            string? activeLeg = null,
            string? summary = null,
            string? batchId = null);

        /// <summary>
        /// 直接记录已完成的步骤剖析数据
        /// </summary>
        /// <param name="record">步骤剖析记录</param>
        void RecordStep(TaskStepProfilingRecord record);

        /// <summary>
        /// 快速记录已知耗时的步骤操作
        /// </summary>
        void RecordStep(
            string taskCode,
            string subsystem,
            string operationName,
            long durationMs,
            string status = "Success",
            string? summary = null,
            string? agvId = null,
            int stepIndex = 0,
            string? activeLeg = null,
            string? batchId = null,
            string? details = null);
    }
}
