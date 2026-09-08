using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// 多车同批次门禁过滤器与互锁协同步道网关接口
    /// 遵循《AGENTS.md》铁律 6：支持一单分拆多子任务、多车汇聚协同与洁净室/立库通道防冲撞安全互锁
    /// </summary>
    public interface IBatchHandshakeGate
    {
        /// <summary>
        /// 检查某 AGV 车辆是否允许进入同批次管制的共享受限区域（如立库口、风淋门、窄通道）
        /// </summary>
        /// <param name="batchCode">批次编号</param>
        /// <param name="vehicleCode">AGV 车辆唯一编码</param>
        /// <param name="zoneOrStation">受限区域或工位代码</param>
        /// <param name="maxConcurrentVehicles">该区域当前批次允许的最大并发车辆数（默认为 1，即互斥通行）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>若允许通行返回 true，否则返回 false</returns>
        Task<bool> CanVehicleEnterZoneAsync(
            string batchCode,
            string vehicleCode,
            string zoneOrStation,
            int maxConcurrentVehicles = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 请求获取区域进入通行许可（加锁），并登记当前车辆占位
        /// </summary>
        /// <param name="batchCode">批次编号</param>
        /// <param name="vehicleCode">AGV 车辆唯一编码</param>
        /// <param name="zoneOrStation">受限区域或工位代码</param>
        /// <param name="maxConcurrentVehicles">允许最大并发车辆数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>获取锁成功返回 true，若已被占满返回 false</returns>
        Task<bool> TryAcquireEntryAsync(
            string batchCode,
            string vehicleCode,
            string zoneOrStation,
            int maxConcurrentVehicles = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放区域通行占位（离开区域或动作完成后调用）
        /// </summary>
        /// <param name="batchCode">批次编号</param>
        /// <param name="vehicleCode">AGV 车辆唯一编码</param>
        /// <param name="zoneOrStation">受限区域或工位代码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作任务</returns>
        Task ReleaseExitAsync(
            string batchCode,
            string vehicleCode,
            string zoneOrStation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查同批次多车是否全部到达特定汇聚里程碑步骤（汇聚同步握手）
        /// </summary>
        /// <param name="batchCode">批次编号</param>
        /// <param name="targetStepIndex">目标汇聚步骤序号</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>若批次下所有未失败子任务均已推进至目标步骤或更晚，返回 true；若仍在等待其他车辆则返回 false</returns>
        Task<bool> CheckConvergenceAsync(
            string batchCode,
            int targetStepIndex,
            CancellationToken cancellationToken = default);
    }
}
