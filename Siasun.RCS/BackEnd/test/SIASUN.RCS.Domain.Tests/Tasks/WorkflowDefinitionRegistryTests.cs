using System.Collections.Generic;
using Shouldly;
using SIASUN.RCS.Tasks.Workflow;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Tasks
{
    public class WorkflowDefinitionRegistryTests
    {
        [Fact]
        public void Should_Register_And_Retrieve_Workflow_Definition()
        {
            // Arrange
            var registry = new WorkflowDefinitionRegistry();
            var defV1 = new WorkflowDefinition("test_workflow", 1, "测试工作流 V1")
            {
                Steps = new List<WorkflowStepDefinition>
                {
                    new(0, "Step0", "Init")
                }
            };
            var defV2 = new WorkflowDefinition("test_workflow", 2, "测试工作流 V2")
            {
                Steps = new List<WorkflowStepDefinition>
                {
                    new(0, "Step0", "Init"),
                    new(1, "Step1", "Action")
                }
            };

            // Act
            registry.Register(defV1);
            registry.Register(defV2);

            // Assert
            var foundExact = registry.Find("test_workflow", 1);
            foundExact.ShouldNotBeNull();
            foundExact.Version.ShouldBe(1);

            // 查询未指定版本时应默认返回最新最高版本 V2
            var foundLatest = registry.Find("test_workflow");
            foundLatest.ShouldNotBeNull();
            foundLatest.Version.ShouldBe(2);

            // 支持带版本后缀查询
            var foundByFullKey = registry.Find("test_workflow.v1");
            foundByFullKey.ShouldNotBeNull();
            foundByFullKey.Version.ShouldBe(1);

            // 异常测试
            Should.Throw<BusinessException>(() => registry.Get("non_existent_flow"));
        }

        [Fact]
        public void Should_Return_All_Registered_Definitions()
        {
            // Arrange
            var registry = new WorkflowDefinitionRegistry();
            registry.Register(new WorkflowDefinition("flow_a", 1, "A1") { Steps = new() { new(0, "s0", "t") } });
            registry.Register(new WorkflowDefinition("flow_b", 1, "B1") { Steps = new() { new(0, "s0", "t") } });

            // Act
            var all = registry.GetAll();

            // Assert
            all.Count.ShouldBe(2);
        }
    }
}
