using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Auditing
{
    [ExcludeFromCodeCoverage]
public class AuditLogFilterRuleDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGuidGenerator _guidGenerator;

        public AuditLogFilterRuleDataSeedContributor(
            IServiceProvider serviceProvider,
            IGuidGenerator guidGenerator)
        {
            _serviceProvider = serviceProvider;
            _guidGenerator = guidGenerator;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            var ruleRepository = _serviceProvider.GetService<IRepository<AuditLogFilterRule, Guid>>();
            if (ruleRepository == null)
            {
                return;
            }

            if (await ruleRepository.GetCountAsync() > 0)
            {
                return;
            }

            await ruleRepository.InsertAsync(new AuditLogFilterRule(
                _guidGenerator.Create(),
                "RCS 核心业务接口",
                "/api/rcs/**",
                FilterRuleType.Whitelist,
                FilterDirection.Both,
                "*",
                true,
                "记录 RCS 所有业务控制与状态流转报文"
            ));

            await ruleRepository.InsertAsync(new AuditLogFilterRule(
                _guidGenerator.Create(),
                "硬件/上游适配器接口",
                "/api/adapters/**",
                FilterRuleType.Whitelist,
                FilterDirection.Both,
                "*",
                true,
                "记录与车队调度系统(TM)、MES 及 WMS 等外部系统的交互报文"
            ));
        }
    }
}
