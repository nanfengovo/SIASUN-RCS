using System;
using System.Text.Json.Serialization;

namespace SIASUN.RCS.Diagnostics.FlightPack
{
    /// <summary>
    /// 事故排障黑匣子元数据契约定义
    /// </summary>
    public class FlightPackMetadata
    {
        /// <summary>
        /// 黑匣子数据包规范版本 (如 1.0.0)
        /// </summary>
        [JsonPropertyName("packVersion")]
        public string PackVersion { get; set; } = "1.0.0";

        /// <summary>
        /// 导出操作上下文（操作人、导出时间、IP）
        /// </summary>
        [JsonPropertyName("exportContext")]
        public ExportContextDto ExportContext { get; set; } = new();

        /// <summary>
        /// 排障目标锚点信息
        /// </summary>
        [JsonPropertyName("anchor")]
        public AnchorDto Anchor { get; set; } = new();

        /// <summary>
        /// 时序查询的时间窗口
        /// </summary>
        [JsonPropertyName("timeWindow")]
        public TimeWindowDto TimeWindow { get; set; } = new();

        /// <summary>
        /// 系统部署环境与地图上下文
        /// </summary>
        [JsonPropertyName("environment")]
        public EnvironmentDto Environment { get; set; } = new();
    }

    /// <summary>
    /// 导出上下文数据传输对象
    /// </summary>
    public class ExportContextDto
    {
        /// <summary>
        /// 导出生成时间 (UTC)
        /// </summary>
        [JsonPropertyName("exportTime")]
        public DateTime ExportTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 导出人用户标识
        /// </summary>
        [JsonPropertyName("exportedByUserId")]
        public Guid? ExportedByUserId { get; set; }

        /// <summary>
        /// 导出人账号名称
        /// </summary>
        [JsonPropertyName("exportedByUserName")]
        public string ExportedByUserName { get; set; } = "System";

        /// <summary>
        /// 导出客户端 IP 地址
        /// </summary>
        [JsonPropertyName("clientIp")]
        public string ClientIp { get; set; } = string.Empty;
    }

    /// <summary>
    /// 事故排障定位锚点信息
    /// </summary>
    public class AnchorDto
    {
        /// <summary>
        /// 锚点类型 (Task / Vehicle / TimeRange)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Task";

        /// <summary>
        /// 锚点标识 (如任务编号、车号)
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 关联执行车辆标识
        /// </summary>
        [JsonPropertyName("relatedVehicleId")]
        public string? RelatedVehicleId { get; set; }

        /// <summary>
        /// 任务生命周期快照
        /// </summary>
        [JsonPropertyName("taskLifecycle")]
        public TaskLifecycleDto? TaskLifecycle { get; set; }
    }

    /// <summary>
    /// 任务生命周期摘要
    /// </summary>
    public class TaskLifecycleDto
    {
        /// <summary>
        /// 任务创建时间
        /// </summary>
        [JsonPropertyName("creationTime")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 任务结束或中断时间
        /// </summary>
        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 任务最终终止状态
        /// </summary>
        [JsonPropertyName("finalStatus")]
        public string FinalStatus { get; set; } = "Unknown";
    }

    /// <summary>
    /// 时序时间范围与缓冲配置
    /// </summary>
    public class TimeWindowDto
    {
        /// <summary>
        /// 实际检索起始时间 (含前置缓冲)
        /// </summary>
        [JsonPropertyName("queryStartTime")]
        public DateTime QueryStartTime { get; set; }

        /// <summary>
        /// 实际检索结束时间 (含后置缓冲)
        /// </summary>
        [JsonPropertyName("queryEndTime")]
        public DateTime QueryEndTime { get; set; }

        /// <summary>
        /// 前置缓冲分钟数
        /// </summary>
        [JsonPropertyName("bufferBeforeMinutes")]
        public int BufferBeforeMinutes { get; set; } = 5;

        /// <summary>
        /// 后置缓冲分钟数
        /// </summary>
        [JsonPropertyName("bufferAfterMinutes")]
        public int BufferAfterMinutes { get; set; } = 5;
    }

    /// <summary>
    /// 运行宿主环境元数据
    /// </summary>
    public class EnvironmentDto
    {
        /// <summary>
        /// RCS 系统软件版本
        /// </summary>
        [JsonPropertyName("rcsVersion")]
        public string RcsVersion { get; set; } = "3.0.0";

        /// <summary>
        /// 代码 Git 提交散列值
        /// </summary>
        [JsonPropertyName("gitCommit")]
        public string GitCommit { get; set; } = string.Empty;

        /// <summary>
        /// 宿主机名
        /// </summary>
        [JsonPropertyName("hostName")]
        public string HostName { get; set; } = System.Environment.MachineName;

        /// <summary>
        /// 当前激活地图名称
        /// </summary>
        [JsonPropertyName("activeMapName")]
        public string ActiveMapName { get; set; } = string.Empty;
    }
}
