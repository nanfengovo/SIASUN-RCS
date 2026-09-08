using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Diagnostics.FlightPack;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Monitor;
using SIASUN.RCS.Tasks.Profiling;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Diagnostics
{
    /// <summary>
    /// 黑匣子排障包 Workflow 第五轨（工作流耗时剖析）单元测试
    /// 验证 stepProfiling 数据正确投影至 timeline.json 与 raw/workflow_profiling.json
    /// </summary>
    public class FlightPackCollectorWorkflowRailTests
    {
        private readonly IRepository<OperationLog, Guid> _opRepo;
        private readonly IRepository<SystemEventLog, Guid> _sysRepo;
        private readonly IApiAuditLogStore _apiStore;
        private readonly IIncidentNarrativeBuilder _narrativeBuilder;
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IRepository<TaskStepProfiling, Guid> _profilingRepo;
        private readonly FlightPackCollector _collector;

        public FlightPackCollectorWorkflowRailTests()
        {
            _opRepo = Substitute.For<IRepository<OperationLog, Guid>>();
            _sysRepo = Substitute.For<IRepository<SystemEventLog, Guid>>();
            _apiStore = Substitute.For<IApiAuditLogStore>();
            _narrativeBuilder = Substitute.For<IIncidentNarrativeBuilder>();
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();
            _profilingRepo = Substitute.For<IRepository<TaskStepProfiling, Guid>>();

            _collector = new FlightPackCollector(
                _opRepo,
                _sysRepo,
                _apiStore,
                _narrativeBuilder,
                _opRecorder,
                _asyncExecuter,
                aiAnalysisProvider: null,
                entityAuditLogStore: null,
                configuration: null,
                stepProfilingRepository: _profilingRepo);
        }

        [Fact]
        public async Task CollectAndPackAsync_Should_Include_Workflow_Rail_And_Raw_File()
        {
            var baseTime = new DateTime(2026, 9, 8, 10, 0, 0, DateTimeKind.Utc);
            var request = new FlightPackRequest
            {
                AnchorType = "Task",
                AnchorKey = "TASK_WF_001",
                StartTime = baseTime,
                EndTime = baseTime.AddMinutes(5),
                BufferBeforeMinutes = 1,
                BufferAfterMinutes = 1,
                ExportedByUserName = "DispatchAdmin"
            };

            var profilings = new List<TaskStepProfiling>
            {
                new TaskStepProfiling(
                    id: Guid.NewGuid(),
                    taskCode: "TASK_WF_001",
                    subsystem: "TM",
                    operationName: "DispatchLeg",
                    startTime: baseTime.AddMinutes(1),
                    endTime: baseTime.AddMinutes(1).AddMilliseconds(120),
                    durationMs: 120,
                    status: "Success",
                    summary: "下发执行程段 Fetch 到 TM",
                    batchId: "BATCH_01",
                    agvId: "AGV_01",
                    stepIndex: 2,
                    activeLeg: "Fetch",
                    traceId: "TRC_WF_01")
            };

            _opRepo.GetQueryableAsync().Returns(Task.FromResult(new List<OperationLog>().AsQueryable()));
            _sysRepo.GetQueryableAsync().Returns(Task.FromResult(new List<SystemEventLog>().AsQueryable()));
            _profilingRepo.GetQueryableAsync().Returns(Task.FromResult(profilings.AsQueryable()));

            _apiStore.GetListAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ApiAuditLogEntry>>(Array.Empty<ApiAuditLogEntry>()));

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<OperationLog>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new List<OperationLog>()));

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<SystemEventLog>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new List<SystemEventLog>()));

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<TaskStepProfiling>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(profilings));

            _narrativeBuilder.BuildMarkdownNarrative(Arg.Any<FlightPackMetadata>(), Arg.Any<IReadOnlyList<FlightPackTimelineEvent>>())
                .Returns("# Narrative Report");

            var zipBytes = await _collector.CollectAndPackAsync(request);

            zipBytes.ShouldNotBeNull();
            zipBytes.Length.ShouldBeGreaterThan(0);

            using var memStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

            // 1. 验证 raw/workflow_profiling.json 存在且包含数据
            var workflowEntry = archive.GetEntry("raw/workflow_profiling.json");
            workflowEntry.ShouldNotBeNull();
            using (var reader = new StreamReader(workflowEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                content.ShouldContain("TASK_WF_001");
                content.ShouldContain("DispatchLeg");
            }

            // 2. 验证 timeline.json 中存在 Track 为 Workflow 的事件
            var timelineEntry = archive.GetEntry("timeline.json");
            timelineEntry.ShouldNotBeNull();
            using (var reader = new StreamReader(timelineEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                var events = JsonSerializer.Deserialize<List<FlightPackTimelineEvent>>(content);
                events.ShouldNotBeNull();
                var wfEvent = events.FirstOrDefault(e => e.Track == "Workflow");
                wfEvent.ShouldNotBeNull();
                wfEvent.Source.ShouldBe("TM");
                wfEvent.Title.ShouldContain("DispatchLeg");
                wfEvent.Summary.ShouldContain("Fetch");
                wfEvent.RawRef.ShouldNotBeNull();
                wfEvent.RawRef.File.ShouldBe("raw/workflow_profiling.json");
            }
        }
    }
}

