using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Vehicles
{
    /// <summary>
    /// AGV 移动机器人车辆聚合根
    /// </summary>
    public class AgvVehicle : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 车辆唯一业务编码（例如 "AGV-01"）
        /// </summary>
        public string VehicleCode { get; private set; } = string.Empty;

        /// <summary>
        /// 车辆运行状态
        /// </summary>
        public VehicleStatus Status { get; private set; } = VehicleStatus.Idle;

        /// <summary>
        /// 当前所在物理工位或二维码坐标
        /// </summary>
        public string? CurrentStation { get; private set; }

        /// <summary>
        /// 当前电量百分比（0.0 ~ 100.0）
        /// </summary>
        public double BatteryPercentage { get; private set; } = 100.0;

        /// <summary>
        /// 车载工控机 IP 通信地址
        /// </summary>
        public string? IpAddress { get; private set; }

        /// <summary>
        /// 故障报警描述信息
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// 当前绑定的调度任务 ID
        /// </summary>
        public Guid? CurrentTaskId { get; private set; }

        /// <summary>
        /// 当前绑定的业务任务编号
        /// </summary>
        public string? CurrentTaskCode { get; private set; }

        /// <summary>
        /// EF Core 内部反序列化受保护无参构造函数
        /// </summary>
        protected AgvVehicle()
        {
        }

        /// <summary>
        /// 创建新的 AGV 车辆实例
        /// </summary>
        /// <param name="id">车辆主键</param>
        /// <param name="vehicleCode">车辆编码</param>
        /// <param name="ipAddress">通信 IP</param>
        /// <param name="initialStation">初始停靠工位</param>
        public AgvVehicle(
            Guid id,
            string vehicleCode,
            string? ipAddress = null,
            string? initialStation = null) : base(id)
        {
            VehicleCode = Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode), maxLength: 64);
            IpAddress = ipAddress;
            CurrentStation = initialStation;
            Status = VehicleStatus.Idle;
            BatteryPercentage = 100.0;
        }

        /// <summary>
        /// 调度员人工复位车辆（清除报警状态，重置为 Idle）
        /// </summary>
        /// <param name="reason">复位原因</param>
        public void Reset(string reason)
        {
            Status = VehicleStatus.Idle;
            ErrorMessage = null;
            CurrentTaskId = null;
            CurrentTaskCode = null;
        }

        /// <summary>
        /// 记录车辆硬件报警或通信异常停机
        /// </summary>
        /// <param name="error">故障原因描述</param>
        public void ReportError(string error)
        {
            Status = VehicleStatus.Error;
            ErrorMessage = Check.NotNullOrWhiteSpace(error, nameof(error));
        }

        /// <summary>
        /// 绑定并指派执行新任务
        /// </summary>
        /// <param name="taskId">调度任务 ID</param>
        /// <param name="taskCode">业务任务编号</param>
        public void AssignTask(Guid taskId, string taskCode)
        {
            if (Status == VehicleStatus.Error || Status == VehicleStatus.Offline)
            {
                throw new BusinessException("RCS:VehicleNotAvailableForTask")
                    .WithData("VehicleCode", VehicleCode)
                    .WithData("Status", Status.ToString());
            }

            CurrentTaskId = taskId;
            CurrentTaskCode = Check.NotNullOrWhiteSpace(taskCode, nameof(taskCode));
            Status = VehicleStatus.Running;
        }

        /// <summary>
        /// 解绑任务，恢复为空闲待命状态
        /// </summary>
        public void ReleaseTask()
        {
            CurrentTaskId = null;
            CurrentTaskCode = null;
            if (Status != VehicleStatus.Error && Status != VehicleStatus.Offline)
            {
                Status = VehicleStatus.Idle;
            }
        }

        /// <summary>
        /// 更新底层底盘遥测数据
        /// </summary>
        /// <param name="station">当前工位</param>
        /// <param name="batteryPercentage">电量</param>
        /// <param name="ipAddress">IP 地址</param>
        public void UpdateTelemetry(string? station, double batteryPercentage, string? ipAddress = null)
        {
            if (!string.IsNullOrWhiteSpace(station))
            {
                CurrentStation = station;
            }

            BatteryPercentage = Math.Clamp(batteryPercentage, 0.0, 100.0);
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                IpAddress = ipAddress;
            }
        }

        /// <summary>
        /// 切换车辆状态
        /// </summary>
        /// <param name="status">新状态</param>
        /// <param name="reason">变更原因说明</param>
        public void SetStatus(VehicleStatus status, string? reason = null)
        {
            Status = status;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = reason;
            }
        }
    }
}
