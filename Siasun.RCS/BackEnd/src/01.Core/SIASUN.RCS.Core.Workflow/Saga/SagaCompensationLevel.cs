namespace SIASUN.RCS.Tasks.Workflow.Saga
{
    /// <summary>
    /// SAGA 四级逆向事务补偿级别
    /// 遵循工控现场安全铁律：从轻量原地重试到物理安全回退，再到资源释放与故障隔离
    /// </summary>
    public enum SagaCompensationLevel
    {
        /// <summary>
        /// 1 级补偿：原步骤瞬态原地重试（适用于网络抖动、PLC 查询超时等幂等步骤）
        /// </summary>
        Level1_RetryCurrentStep = 1,

        /// <summary>
        /// 2 级补偿：回退至安全停靠/对位基准点（撤销未决物理动作，恢复车体初始姿态）
        /// </summary>
        Level2_RollbackToSafePoint = 2,

        /// <summary>
        /// 3 级补偿：跨系统资源逆向回滚与释放（释放库位原子锁、机台设备联锁、风淋门安全锁）
        /// </summary>
        Level3_ReleaseResources = 3,

        /// <summary>
        /// 4 级补偿：不可逆故障熔断挂起（固化黑匣子排障快照，任务标记 Failed 并告警呼叫调度员）
        /// </summary>
        Level4_NotifyOperatorAndFail = 4
    }
}
