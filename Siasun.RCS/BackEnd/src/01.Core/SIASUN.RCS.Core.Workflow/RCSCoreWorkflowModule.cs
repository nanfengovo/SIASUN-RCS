using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Modularity;

namespace SIASUN.RCS
{
    /// <summary>
    /// SIASUN RCS 声明式通用步进工作流微内核模块 (SIASUN.RCS.Core.Workflow)
    /// 承载 10 步标准化搬运工序微内核、Activity 节点定义、Suspend/SignalAsync 异步唤醒引擎、SAGA 四级逆向事务补偿器
    /// </summary>
    [DependsOn(typeof(RCSDomainSharedModule))]
    [ExcludeFromCodeCoverage]
    public class RCSCoreWorkflowModule : AbpModule
    {
    }
}
