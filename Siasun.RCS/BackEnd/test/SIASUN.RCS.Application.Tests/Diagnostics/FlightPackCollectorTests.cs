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
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Diagnostics
{
    public class FlightPackCollectorTests
    {
        private readonly IRepository<OperationLog, Guid> _opRepo;
        private readonly IRepository<SystemEventLog, Guid> _sysRepo;
        private readonly IApiAuditLogStore _apiStore;
        private readonly IIncidentNarrativeBuilder _narrativeBuilder;
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly FlightPackCollector _collector;

        public FlightPackCollectorTests()
        {
            _opRepo = Substitute.For<IRepository<OperationLog, Guid>>();
            _sysRepo = Substitute.For<IRepository<SystemEventLog, Guid>>();
            _apiStore = Substitute.For<IApiAuditLogStore>();
            _narrativeBuilder = Substitute.For<IIncidentNarrativeBuilder>();
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();

            _collector = new FlightPackCollector(
                _opRepo,
                _sysRepo,
                _apiStore,
                _narrativeBuilder,
                _opRecorder,
                _asyncExecuter);
        }

        [Fact]
        public async Task CollectAndPackAsync_With_ValidTask_Should_Produce_Valid_Zip_With_ExpectedEntries()
        {
            // Arrange
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var request = new FlightPackRequest
            {
                AnchorType = "Task",
                AnchorKey = "T-1001",
                StartTime = baseTime,
                EndTime = baseTime.AddMinutes(10),
                BufferBeforeMinutes = 2,
                BufferAfterMinutes = 2,
                ExportedByUserName = "Tester"
            };

            var opLogs = new List<OperationLog>
            {
                new(
                    id: Guid.NewGuid(),
                    operatorType: OperatorType.User,
                    userId: Guid.NewGuid(),
                    userName: "ZhangSan",
                    clientIp: "127.0.0.1",
                    correlationId: "corr-1",
                    module: "Dispatch",
                    action: "CancelTask",
                    targetType: "Task",
                    targetId: "T-1001",
                    status: OperationLogStatus.Success,
                    description: "User cancelled task",
                    errorMessage: string.Empty,
                    creationTime: baseTime.AddMinutes(5))
            };

            var apiLogs = new List<ApiAuditLogEntry>
            {
                new()
                {
                    Id = 1,
                    CreationTime = baseTime.AddMinutes(1),
                    HttpMethod = SIASUN.RCS.Auditing.HttpMethod.Post,
                    Path = "/api/rcs/tasks",
                    StatusCode = 200,
                    ElapsedMs = 50,
                    ClientIpAddress = "192.168.1.10",
                    TraceId = "trace-1"
                }
            };

            var sysEvents = new List<SystemEventLog>
            {
                new(
                    id: Guid.NewGuid(),
                    eventCategory: "Hardware",
                    level: "Warning",
                    message: "TwinArm timeout",
                    actionDetails: "Timeout waiting for slot free",
                    creationTime: baseTime.AddMinutes(3))
            };

            _opRepo.GetQueryableAsync().Returns(Task.FromResult(opLogs.AsQueryable()));
            _sysRepo.GetQueryableAsync().Returns(Task.FromResult(sysEvents.AsQueryable()));
            _apiStore.GetListAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ApiAuditLogEntry>>(apiLogs));

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<OperationLog>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(opLogs));

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<SystemEventLog>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(sysEvents));

            _narrativeBuilder.BuildMarkdownNarrative(Arg.Any<FlightPackMetadata>(), Arg.Any<IReadOnlyList<FlightPackTimelineEvent>>())
                .Returns("# Mock Narrative");

            // Act
            var zipBytes = await _collector.CollectAndPackAsync(request);

            // Assert
            zipBytes.ShouldNotBeNull();
            zipBytes.Length.ShouldBeGreaterThan(0);

            using var memStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

            archive.Entries.ShouldContain(e => e.FullName == "metadata.json");
            archive.Entries.ShouldContain(e => e.FullName == "timeline.json");
            archive.Entries.ShouldContain(e => e.FullName == "diagnostic_summary.md");
            archive.Entries.ShouldContain(e => e.FullName == "raw/api_logs.json");
            archive.Entries.ShouldContain(e => e.FullName == "raw/operator_logs.json");
            archive.Entries.ShouldContain(e => e.FullName == "raw/system_logs.json");

            // Verify metadata content
            var metadataEntry = archive.GetEntry("metadata.json");
            metadataEntry.ShouldNotBeNull();
            using (var reader = new StreamReader(metadataEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                var metadata = JsonSerializer.Deserialize<FlightPackMetadata>(content);
                metadata.ShouldNotBeNull();
                metadata.Anchor.Key.ShouldBe("T-1001");
                metadata.ExportContext.ExportedByUserName.ShouldBe("Tester");
            }

            // Verify timeline events content
            var timelineEntry = archive.GetEntry("timeline.json");
            timelineEntry.ShouldNotBeNull();
            using (var reader = new StreamReader(timelineEntry.Open()))
            {
                var content = await reader.ReadToEndAsync();
                var events = JsonSerializer.Deserialize<List<FlightPackTimelineEvent>>(content);
                events.ShouldNotBeNull();
                events.Count.ShouldBe(3); // 1 API + 1 Operator + 1 System
                events.ShouldContain(e => e.Track == "API");
                events.ShouldContain(e => e.Track == "Operator");
                events.ShouldContain(e => e.Track == "Exception");
            }

            // Verify self-audit was triggered
            _opRecorder.Received(1).RecordSuccess(
                "Diagnostics",
                "ExportFlightPack",
                "Task",
                "T-1001",
                Arg.Any<string>());
        }

        [Fact]
        public async Task CollectAndPackAsync_With_EnableAiAnalysis_Should_Include_AiReport_And_RawAiJson()
        {
            // Arrange
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var request = new FlightPackRequest
            {
                AnchorType = "Task",
                AnchorKey = "T-2002",
                StartTime = baseTime,
                EndTime = baseTime.AddMinutes(5),
                EnableAiAnalysis = true
            };

            _opRepo.GetQueryableAsync().Returns(Task.FromResult(new List<OperationLog>().AsQueryable()));
            _sysRepo.GetQueryableAsync().Returns(Task.FromResult(new List<SystemEventLog>().AsQueryable()));
            _apiStore.GetListAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ApiAuditLogEntry>>(new List<ApiAuditLogEntry>()));

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<OperationLog>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new List<OperationLog>()));
            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<SystemEventLog>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new List<SystemEventLog>()));

            _narrativeBuilder.BuildMarkdownNarrative(Arg.Any<FlightPackMetadata>(), Arg.Any<IReadOnlyList<FlightPackTimelineEvent>>())
                .Returns("# Base Narrative");

            var aiProvider = Substitute.For<SIASUN.RCS.Diagnostics.AI.IAiIncidentAnalysisProvider>();
            aiProvider.IsEnabled.Returns(true);
            aiProvider.AnalyzeIncidentAsync(Arg.Any<FlightPackMetadata>(), Arg.Any<IReadOnlyList<FlightPackTimelineEvent>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new SIASUN.RCS.Diagnostics.AI.AiAnalysisResultDto
                {
                    IsSuccess = true,
                    ModelUsed = "deepseek-r1:7b",
                    RootCauseSummary = "死锁故障",
                    ResponsibleParty = "调度算法",
                    ConfidenceLevel = "High",
                    RecommendedActions = new List<string> { "重启调度引擎" },
                    MarkdownReport = "AI 推理报告全文",
                    ElapsedMs = 1500
                }));

            var collector = new FlightPackCollector(
                _opRepo,
                _sysRepo,
                _apiStore,
                _narrativeBuilder,
                _opRecorder,
                _asyncExecuter,
                aiProvider);

            // Act
            var zipBytes = await collector.CollectAndPackAsync(request);

            // Assert
            zipBytes.ShouldNotBeNull();
            using var memStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

            archive.Entries.ShouldContain(e => e.FullName == "raw/ai_analysis.json");

            var summaryEntry = archive.GetEntry("diagnostic_summary.md");
            summaryEntry.ShouldNotBeNull();
            using (var reader = new StreamReader(summaryEntry.Open()))
            {
                var summaryText = await reader.ReadToEndAsync();
                summaryText.ShouldContain("AI 深度根因智能推理报告");
                summaryText.ShouldContain("死锁故障");
                summaryText.ShouldContain("deepseek-r1:7b");
            }
        }
    }
}
