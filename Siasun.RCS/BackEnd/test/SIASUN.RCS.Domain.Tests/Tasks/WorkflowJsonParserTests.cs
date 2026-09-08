using System;
using System.Collections.Generic;
using Shouldly;
using SIASUN.RCS.Tasks.Workflow;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Tasks
{
    public class WorkflowJsonParserTests
    {
        [Fact]
        public void Should_Parse_Valid_Workflow_Json_Correctly()
        {
            // Arrange
            var json = """
            {
              "workflowId": "transfer_standard",
              "version": 1,
              "title": "半导体标准晶圆/载具搬运工作流",
              "description": "通用流程",
              "steps": [
                {
                  "stepIndex": 0,
                  "stepName": "LockPickupLocation",
                  "stepType": "LocationLock",
                  "isIdempotent": true,
                  "timeoutSeconds": 30,
                  "maxRetries": 3,
                  "parameters": {
                    "action": "Lock",
                    "target": "FromStation"
                  }
                },
                {
                  "stepIndex": 1,
                  "stepName": "DispatchFetchLeg",
                  "stepType": "DispatchTmLeg",
                  "activeLeg": "Fetch",
                  "isIdempotent": false,
                  "timeoutSeconds": 180,
                  "maxRetries": 0,
                  "waitingEvent": "TM_FETCH_DONE",
                  "rollbackStepIndex": 0
                }
              ]
            }
            """;

            // Act
            var definition = WorkflowJsonParser.Parse(json);

            // Assert
            definition.ShouldNotBeNull();
            definition.WorkflowId.ShouldBe("transfer_standard");
            definition.Version.ShouldBe(1);
            definition.Title.ShouldBe("半导体标准晶圆/载具搬运工作流");
            definition.FullKey.ShouldBe("transfer_standard.v1");
            definition.Steps.Count.ShouldBe(2);

            var step0 = definition.Steps[0];
            step0.StepIndex.ShouldBe(0);
            step0.StepName.ShouldBe("LockPickupLocation");
            step0.StepType.ShouldBe("LocationLock");
            step0.IsIdempotent.ShouldBeTrue();
            step0.Parameters["action"].ShouldBe("Lock");

            var step1 = definition.Steps[1];
            step1.StepIndex.ShouldBe(1);
            step1.ActiveLeg.ShouldBe("Fetch");
            step1.WaitingEvent.ShouldBe("TM_FETCH_DONE");
            step1.RollbackStepIndex.ShouldBe(0);
        }

        [Fact]
        public void Should_Serialize_And_Deserialize_Without_Loss()
        {
            // Arrange
            var original = new WorkflowDefinition("test_flow", 2, "测试工作流", "说明")
            {
                Steps = new List<WorkflowStepDefinition>
                {
                    new(0, "Step0", "Init", isIdempotent: true),
                    new(1, "Step1", "Action", activeLeg: "Fetch", isIdempotent: false, waitingEvent: "DONE", rollbackStepIndex: 0)
                }
            };

            // Act
            var json = WorkflowJsonParser.Serialize(original);
            var parsed = WorkflowJsonParser.Parse(json);

            // Assert
            parsed.WorkflowId.ShouldBe(original.WorkflowId);
            parsed.Version.ShouldBe(original.Version);
            parsed.Steps.Count.ShouldBe(2);
            parsed.Steps[1].ActiveLeg.ShouldBe("Fetch");
            parsed.Steps[1].WaitingEvent.ShouldBe("DONE");
        }

        [Fact]
        public void Should_Throw_When_WorkflowId_Is_Empty()
        {
            var json = """
            {
              "workflowId": "",
              "version": 1,
              "title": "Invalid",
              "steps": [
                { "stepIndex": 0, "stepName": "Step0", "stepType": "Init" }
              ]
            }
            """;

            var ex = Should.Throw<BusinessException>(() => WorkflowJsonParser.Parse(json));
            ex.Code.ShouldBe("RCS:WorkflowValidationFailed");
        }

        [Fact]
        public void Should_Throw_When_Steps_Are_Empty()
        {
            var json = """
            {
              "workflowId": "flow_empty",
              "version": 1,
              "title": "Empty Steps",
              "steps": []
            }
            """;

            var ex = Should.Throw<BusinessException>(() => WorkflowJsonParser.Parse(json));
            ex.Code.ShouldBe("RCS:WorkflowValidationFailed");
        }

        [Fact]
        public void Should_Throw_When_StepIndex_Is_Duplicate()
        {
            var json = """
            {
              "workflowId": "flow_dup",
              "version": 1,
              "title": "Duplicate Index",
              "steps": [
                { "stepIndex": 0, "stepName": "StepA", "stepType": "A" },
                { "stepIndex": 0, "stepName": "StepB", "stepType": "B" }
              ]
            }
            """;

            var ex = Should.Throw<BusinessException>(() => WorkflowJsonParser.Parse(json));
            ex.Code.ShouldBe("RCS:WorkflowValidationFailed");
        }

        [Fact]
        public void Should_Throw_When_StepIndex_Is_Not_Continuous()
        {
            var json = """
            {
              "workflowId": "flow_gap",
              "version": 1,
              "title": "Gap in Index",
              "steps": [
                { "stepIndex": 0, "stepName": "Step0", "stepType": "A" },
                { "stepIndex": 2, "stepName": "Step2", "stepType": "B" }
              ]
            }
            """;

            var ex = Should.Throw<BusinessException>(() => WorkflowJsonParser.Parse(json));
            ex.Code.ShouldBe("RCS:WorkflowValidationFailed");
        }
    }
}
