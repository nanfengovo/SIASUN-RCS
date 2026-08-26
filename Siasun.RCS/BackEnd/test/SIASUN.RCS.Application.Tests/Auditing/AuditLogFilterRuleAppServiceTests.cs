using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace SIASUN.RCS.Auditing
{
    public abstract class AuditLogFilterRuleAppServiceTests<TStartupModule> : RCSApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IAuditLogFilterRuleAppService _appService;

        protected AuditLogFilterRuleAppServiceTests()
        {
            _appService = GetRequiredService<IAuditLogFilterRuleAppService>();
        }

        [Fact]
        public async Task Should_Create_Update_Toggle_And_Delete_FilterRule()
        {
            // 1. Create
            var createResult = await _appService.CreateAsync(new CreateAuditLogFilterRuleDto
            {
                Name = "测试 WMS 出入库接口",
                PathPattern = "/api/wms/**",
                RuleType = FilterRuleType.Whitelist,
                Direction = FilterDirection.Both,
                HttpMethod = "POST",
                IsEnabled = true,
                Description = "测试规则"
            });

            createResult.ShouldNotBeNull();
            createResult.Name.ShouldBe("测试 WMS 出入库接口");
            createResult.PathPattern.ShouldBe("/api/wms/**");
            createResult.IsEnabled.ShouldBeTrue();

            // 2. Query
            var listResult = await _appService.GetListAsync(new GetAuditLogFilterRulesInput
            {
                Filter = "WMS"
            });
            listResult.TotalCount.ShouldBeGreaterThanOrEqualTo(1);

            // 3. Toggle
            var toggleResult = await _appService.ToggleAsync(createResult.Id);
            toggleResult.IsEnabled.ShouldBeFalse();

            // 4. Update
            var updateResult = await _appService.UpdateAsync(createResult.Id, new UpdateAuditLogFilterRuleDto
            {
                Name = "已更新 WMS 接口",
                PathPattern = "/api/wms/v2/**",
                RuleType = FilterRuleType.Whitelist,
                Direction = FilterDirection.Inbound,
                HttpMethod = "*",
                IsEnabled = true,
                Description = "更新备注"
            });
            updateResult.Name.ShouldBe("已更新 WMS 接口");
            updateResult.PathPattern.ShouldBe("/api/wms/v2/**");

            // 5. Delete
            await _appService.DeleteAsync(createResult.Id);

            var afterDelete = await _appService.GetListAsync(new GetAuditLogFilterRulesInput
            {
                Filter = "已更新 WMS 接口"
            });
            afterDelete.TotalCount.ShouldBe(0);
        }
    }
}
