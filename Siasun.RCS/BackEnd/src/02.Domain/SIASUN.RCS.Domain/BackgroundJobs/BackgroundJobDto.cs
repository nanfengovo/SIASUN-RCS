using System;

namespace SIASUN.RCS.BackgroundJobs
{
    public class BackgroundJobDto
    {
        /// <summary>Job 名称（与注册时的类名一致）</summary>
        public string JobName { get; set; } = string.Empty;

        /// <summary>Job 所属分组</summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Job 描述（来自 [JobDescription] Attribute 或配置）</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 调度状态：Normal / Paused / Complete / Error / Blocked / None
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>下次执行时间（UTC）</summary>
        public DateTime? NextFireTime { get; set; }

        /// <summary>上次执行时间（UTC）</summary>
        public DateTime? PreviousFireTime { get; set; }

        /// <summary>Cron 表达式（仅 Cron Trigger有效）</summary>
        public string? CronExpression { get; set; }
    }
}

