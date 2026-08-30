using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace SIASUN.RCS.Auditing
{
    public abstract class EntityAuditRuleAppServiceTests<TStartupModule> : RCSApplicationTestBase<TStartupModule>
        where TStartupModule : IAbpModule
    {
        private readonly IEntityAuditRuleAppService _appService;

        protected EntityAuditRuleAppServiceTests()
        {
            _appService = GetRequiredService<IEntityAuditRuleAppService>();
        }

        [Fact]
        public async Task Should_Create_Update_Toggle_And_Delete_EntityRule()
        {
            // 1. Create
            var createResult = await _appService.CreateAsync(new CreateUpdateEntityAuditRuleDto
            {
                Name = "测试 Task 变更",
                EntityTypePattern = "*Task",
                Mode = EntityAuditMode.Summary,
                SampleIntervalMs = 1000,
                IsEnabled = true
            });

            createResult.ShouldNotBeNull();
            createResult.Name.ShouldBe("测试 Task 变更");
            createResult.Mode.ShouldBe(EntityAuditMode.Summary);
            createResult.IsEnabled.ShouldBeTrue();

            // 2. Query
            var listResult = await _appService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto());
            listResult.TotalCount.ShouldBeGreaterThanOrEqualTo(1);

            // 3. Toggle
            await _appService.ToggleAsync(createResult.Id);
            var afterToggle = await _appService.GetAsync(createResult.Id);
            afterToggle.IsEnabled.ShouldBeFalse();

            // 4. Update
            var updateResult = await _appService.UpdateAsync(createResult.Id, new CreateUpdateEntityAuditRuleDto
            {
                Name = "已更新 Task 变更",
                EntityTypePattern = "*Task",
                Mode = EntityAuditMode.Full,
                SampleIntervalMs = 2000,
                IsEnabled = true
            });
            updateResult.Name.ShouldBe("已更新 Task 变更");
            updateResult.Mode.ShouldBe(EntityAuditMode.Full);

            // 5. Delete
            await _appService.DeleteAsync(createResult.Id);
        }
    }
}
