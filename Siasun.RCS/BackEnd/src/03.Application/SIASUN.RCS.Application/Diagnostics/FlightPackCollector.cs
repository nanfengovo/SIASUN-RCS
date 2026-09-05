using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Diagnostics.AI;
using SIASUN.RCS.Diagnostics.FlightPack;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Monitor;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace SIASUN.RCS.Diagnostics
{
    public class FlightPackCollector : IFlightPackCollector, ITransientDependency
    {
        private readonly IRepository<OperationLog, Guid> _operationLogRepository;
        private readonly IRepository<SystemEventLog, Guid> _systemEventLogRepository;
        private readonly IApiAuditLogStore _apiAuditLogStore;
        private readonly IIncidentNarrativeBuilder _narrativeBuilder;
        private readonly IOperationLogRecorder _operationLogRecorder;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IAiIncidentAnalysisProvider? _aiAnalysisProvider;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public FlightPackCollector(
            IRepository<OperationLog, Guid> operationLogRepository,
            IRepository<SystemEventLog, Guid> systemEventLogRepository,
            IApiAuditLogStore apiAuditLogStore,
            IIncidentNarrativeBuilder narrativeBuilder,
            IOperationLogRecorder operationLogRecorder,
            IAsyncQueryableExecuter asyncExecuter,
            IAiIncidentAnalysisProvider? aiAnalysisProvider = null)
        {
            _operationLogRepository = operationLogRepository;
            _systemEventLogRepository = systemEventLogRepository;
            _apiAuditLogStore = apiAuditLogStore;
            _narrativeBuilder = narrativeBuilder;
            _operationLogRecorder = operationLogRecorder;
            _asyncExecuter = asyncExecuter;
            _aiAnalysisProvider = aiAnalysisProvider;
        }

        public async Task<byte[]> CollectAndPackAsync(FlightPackRequest request, CancellationToken cancellationToken = default)
        {
            // 1. 计算时间窗口
            DateTime queryStartTime;
            DateTime queryEndTime;
            string relatedVehicleId = "N/A";
            string finalStatus = "Unknown";
            DateTime? taskCreationTime = null;
            DateTime? taskEndTime = null;

            var opQuery = await _operationLogRepository.GetQueryableAsync();

            if (request.StartTime.HasValue && request.EndTime.HasValue)
            {
                queryStartTime = request.StartTime.Value.AddMinutes(-request.BufferBeforeMinutes);
                queryEndTime = request.EndTime.Value.AddMinutes(request.BufferAfterMinutes);
            }
            else if (string.Equals(request.AnchorType, "Task", StringComparison.OrdinalIgnoreCase))
            {
                // 以 Task 为核心锚点：从 OperationLog 捞取该任务的生命周期边界
                var taskQuery = opQuery
                    .Where(x => x.TargetType == "Task" && x.TargetId == request.AnchorKey)
                    .OrderBy(x => x.CreationTime);

                var taskLogs = await _asyncExecuter.ToListAsync(taskQuery, cancellationToken);

                if (taskLogs.Count > 0)
                {
                    taskCreationTime = taskLogs.First().CreationTime;
                    taskEndTime = taskLogs.Last().CreationTime;
                    finalStatus = taskLogs.Last().Status.ToString();

                    queryStartTime = taskCreationTime.Value.AddMinutes(-request.BufferBeforeMinutes);
                    queryEndTime = taskEndTime.Value.AddMinutes(request.BufferAfterMinutes);
                }
                else
                {
                    var now = DateTime.UtcNow;
                    queryStartTime = now.AddMinutes(-15);
                    queryEndTime = now;
                }
            }
            else
            {
                var now = DateTime.UtcNow;
                queryStartTime = now.AddMinutes(-15);
                queryEndTime = now;
            }

            // 2. 捞取底层证据 (API Logs, Operator Logs, System Events)
            var apiLogs = await _apiAuditLogStore.GetListAsync(queryStartTime, queryEndTime, ct: cancellationToken);

            var operatorQuery = opQuery
                .Where(x => (x.CreationTime >= queryStartTime && x.CreationTime <= queryEndTime)
                            || (x.TargetType == "Task" && x.TargetId == request.AnchorKey))
                .OrderBy(x => x.CreationTime);

            var operatorLogs = await _asyncExecuter.ToListAsync(operatorQuery, cancellationToken);

            var sysQuery = await _systemEventLogRepository.GetQueryableAsync();
            var systemEventQuery = sysQuery
                .Where(x => x.CreationTime >= queryStartTime && x.CreationTime <= queryEndTime)
                .OrderBy(x => x.CreationTime);

            var systemEvents = await _asyncExecuter.ToListAsync(systemEventQuery, cancellationToken);

            // 3. 统一投影打平到 Timeline (三轨)
            var timelineEvents = new List<FlightPackTimelineEvent>();

            // (1) API 轨
            foreach (var api in apiLogs)
            {
                var isError = api.StatusCode >= 500 || !string.IsNullOrEmpty(api.Exception);
                var isWarn = api.StatusCode >= 400 && api.StatusCode < 500;
                var level = isError ? "Error" : (isWarn ? "Warning" : "Information");

                timelineEvents.Add(new FlightPackTimelineEvent
                {
                    Id = $"api_{api.Id}",
                    Timestamp = api.CreationTime,
                    Track = "API",
                    Level = level,
                    Source = string.IsNullOrEmpty(api.Peer) ? "API" : api.Peer,
                    Title = $"{api.HttpMethod} {api.Path} (HTTP {api.StatusCode})",
                    Summary = $"耗时: {api.ElapsedMs}ms, 客户端: {api.ClientIpAddress}" + (string.IsNullOrEmpty(api.Exception) ? "" : $", 异常: {api.Exception}"),
                    TraceId = api.TraceId,
                    RawRef = new RawRefDto
                    {
                        File = "raw/api_logs.json",
                        Id = api.Id.ToString()
                    }
                });
            }

            // (2) Operator 轨
            foreach (var op in operatorLogs)
            {
                var isError = op.Status == OperationLogStatus.Failed;
                var level = isError ? "Warning" : "Information";

                timelineEvents.Add(new FlightPackTimelineEvent
                {
                    Id = $"op_{op.Id}",
                    Timestamp = op.CreationTime,
                    Track = "Operator",
                    Level = level,
                    Source = op.OperatorType.ToString(),
                    Title = $"{op.UserName} 执行了【{op.Action}】",
                    Summary = $"模块: {op.Module}, 目标: {op.TargetType}/{op.TargetId}, 详情: {op.Description}" + (string.IsNullOrEmpty(op.ErrorMessage) ? "" : $", 错误: {op.ErrorMessage}"),
                    TraceId = op.CorrelationId,
                    RawRef = new RawRefDto
                    {
                        File = "raw/operator_logs.json",
                        Id = op.Id.ToString()
                    }
                });
            }

            // (3) Exception 轨 (SystemEventLog)
            foreach (var sys in systemEvents)
            {
                var level = string.Equals(sys.Level, "Error", StringComparison.OrdinalIgnoreCase) || string.Equals(sys.Level, "Fatal", StringComparison.OrdinalIgnoreCase)
                    ? "Error"
                    : (string.Equals(sys.Level, "Warning", StringComparison.OrdinalIgnoreCase) ? "Warning" : "Information");

                timelineEvents.Add(new FlightPackTimelineEvent
                {
                    Id = $"sys_{sys.Id}",
                    Timestamp = sys.CreationTime,
                    Track = "Exception",
                    Level = level,
                    Source = sys.EventCategory,
                    Title = sys.Message,
                    Summary = sys.ActionDetails,
                    RawRef = new RawRefDto
                    {
                        File = "raw/system_logs.json",
                        Id = sys.Id.ToString()
                    }
                });
            }

            // 排序并计算相对时间偏移 relativeMs
            timelineEvents = timelineEvents.OrderBy(e => e.Timestamp).ToList();
            foreach (var evt in timelineEvents)
            {
                evt.RelativeMs = Math.Max(0, (long)(evt.Timestamp - queryStartTime).TotalMilliseconds);
            }

            // 4. 组装 Metadata
            var metadata = new FlightPackMetadata
            {
                PackVersion = "1.0.0",
                ExportContext = new ExportContextDto
                {
                    ExportTime = DateTime.UtcNow,
                    ExportedByUserId = request.ExportedByUserId,
                    ExportedByUserName = request.ExportedByUserName,
                    ClientIp = request.ClientIp
                },
                Anchor = new AnchorDto
                {
                    Type = request.AnchorType,
                    Key = request.AnchorKey,
                    RelatedVehicleId = relatedVehicleId,
                    TaskLifecycle = taskCreationTime.HasValue ? new TaskLifecycleDto
                    {
                        CreationTime = taskCreationTime.Value,
                        EndTime = taskEndTime,
                        FinalStatus = finalStatus
                    } : null
                },
                TimeWindow = new TimeWindowDto
                {
                    QueryStartTime = queryStartTime,
                    QueryEndTime = queryEndTime,
                    BufferBeforeMinutes = request.BufferBeforeMinutes,
                    BufferAfterMinutes = request.BufferAfterMinutes
                },
                Environment = new EnvironmentDto
                {
                    RcsVersion = "3.0.0",
                    GitCommit = "663fe73",
                    HostName = System.Environment.MachineName,
                    ActiveMapName = "Default"
                }
            };

            // 5. 规则引擎生成 Markdown 叙事报告
            var diagnosticSummary = _narrativeBuilder.BuildMarkdownNarrative(metadata, timelineEvents);

            AiAnalysisResultDto? aiResult = null;
            if (request.EnableAiAnalysis && _aiAnalysisProvider != null && _aiAnalysisProvider.IsEnabled)
            {
                aiResult = await _aiAnalysisProvider.AnalyzeIncidentAsync(metadata, timelineEvents, diagnosticSummary, cancellationToken);
                if (aiResult != null && aiResult.IsSuccess)
                {
                    var aiReportSb = new StringBuilder();
                    aiReportSb.AppendLine();
                    aiReportSb.AppendLine("---");
                    aiReportSb.AppendLine();
                    aiReportSb.AppendLine($"## 🤖 AI 深度根因智能推理报告 (由 {aiResult.ModelUsed} 生成，耗时 {aiResult.ElapsedMs}ms)");
                    aiReportSb.AppendLine($"> **置信度**: {aiResult.ConfidenceLevel} | **初步责任归属**: {aiResult.ResponsibleParty}");
                    aiReportSb.AppendLine();
                    aiReportSb.AppendLine("### 💡 根因总结");
                    aiReportSb.AppendLine(aiResult.RootCauseSummary);
                    aiReportSb.AppendLine();
                    if (aiResult.RecommendedActions != null && aiResult.RecommendedActions.Count > 0)
                    {
                        aiReportSb.AppendLine("### 🛠️ 建议排障处置方案");
                        foreach (var action in aiResult.RecommendedActions)
                        {
                            aiReportSb.AppendLine($"- {action}");
                        }
                        aiReportSb.AppendLine();
                    }
                    aiReportSb.AppendLine("### 📑 详细推理分析");
                    aiReportSb.AppendLine(aiResult.MarkdownReport);

                    diagnosticSummary += aiReportSb.ToString();
                }
                else if (aiResult != null && !string.IsNullOrEmpty(aiResult.ErrorMessage))
                {
                    diagnosticSummary += $"\n\n---\n\n## 🤖 AI 深度根因智能推理报告\n> ⚠️ AI 智能推理未成功（{aiResult.ErrorMessage}），已基于客观多源时序事件完成基础规则叙事。\n";
                }
            }
            else if (request.EnableAiAnalysis && (_aiAnalysisProvider == null || !_aiAnalysisProvider.IsEnabled))
            {
                diagnosticSummary += "\n\n---\n\n## 🤖 AI 深度根因智能推理报告\n> ℹ️ AI 智能诊断模块未启用或未就绪 (AiDiagnostics:IsEnabled = false)，已提供上述客观基础时序分析报告。\n";
            }

            // 6. 内存流打成 ZIP 压缩包 (.rcspack)
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // metadata.json
                AddZipEntry(archive, "metadata.json", JsonSerializer.Serialize(metadata, JsonOptions));

                // timeline.json
                AddZipEntry(archive, "timeline.json", JsonSerializer.Serialize(timelineEvents, JsonOptions));

                // diagnostic_summary.md
                AddZipEntry(archive, "diagnostic_summary.md", diagnosticSummary);

                // raw/api_logs.json
                AddZipEntry(archive, "raw/api_logs.json", JsonSerializer.Serialize(apiLogs, JsonOptions));

                // raw/operator_logs.json
                AddZipEntry(archive, "raw/operator_logs.json", JsonSerializer.Serialize(operatorLogs, JsonOptions));

                // raw/system_logs.json
                AddZipEntry(archive, "raw/system_logs.json", JsonSerializer.Serialize(systemEvents, JsonOptions));

                // raw/ai_analysis.json (如果存在 AI 诊断结果)
                if (aiResult != null)
                {
                    AddZipEntry(archive, "raw/ai_analysis.json", JsonSerializer.Serialize(aiResult, JsonOptions));
                }
            }

            var zipBytes = memoryStream.ToArray();

            // 7. 自审计：记录谁导出了事故排障包
            _operationLogRecorder.RecordSuccess(
                module: "Diagnostics",
                action: "ExportFlightPack",
                targetType: request.AnchorType,
                targetKey: request.AnchorKey,
                description: $"导出事故排障黑匣子 (.rcspack)，事件总数: {timelineEvents.Count}，包大小: {zipBytes.Length / 1024.0:F2} KB");

            return zipBytes;
        }

        private static void AddZipEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(content);
        }
    }
}
