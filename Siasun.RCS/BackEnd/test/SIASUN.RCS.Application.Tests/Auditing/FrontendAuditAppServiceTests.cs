using System.Threading.Tasks;
using NSubstitute;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Interfaces.OperationLogs;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Auditing
{
    public class FrontendAuditAppServiceTests
    {
        [Fact]
        public async Task RecordFrontendActionAsync_Should_Call_OperationLogRecorder_RecordSuccess()
        {
            // Arrange
            var recorder = Substitute.For<IOperationLogRecorder>();
            var service = new FrontendAuditAppService(recorder);

            // Act
            await service.RecordFrontendActionAsync(
                module: "ReportModule",
                action: "ExportReport",
                targetType: "UI_Button",
                targetKey: "btn_export",
                description: "Exported yesterday report"
            );

            // Assert
            recorder.Received(1).RecordSuccess(
                module: "ReportModule",
                action: "ExportReport",
                targetType: "UI_Button",
                targetKey: "btn_export",
                description: "Exported yesterday report"
            );
        }
    }
}
