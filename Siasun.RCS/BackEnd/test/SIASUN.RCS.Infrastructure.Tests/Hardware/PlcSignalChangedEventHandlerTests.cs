using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Hardware;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using SIASUN.RCS.Infrastructure.Logging.Hardware;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Hardware
{
    /// <summary>
    /// PLC 信号跳变变位领域事件处理器单元测试
    /// </summary>
    public class PlcSignalChangedEventHandlerTests
    {
        [Fact]
        public async Task HandleEventAsync_NormalSignal_ShouldPublishToHardwareGateTrack()
        {
            // Arrange
            var mockBroker = Substitute.For<IDiagnosticLiveStreamBroker>();
            mockBroker.IsEnabled.Returns(true);

            var handler = new PlcSignalChangedEventHandler(
                NullLogger<PlcSignalChangedEventHandler>.Instance,
                mockBroker);

            var evt = new PlcSignalChangedEvent(
                deviceId: "PLC-01",
                tagName: "TwinArm_Position_Arrived",
                oldValue: false,
                newValue: true,
                dataType: "Boolean",
                traceId: "TRACE-PLC-100",
                vehicleCode: "AGV-03",
                taskCode: "TASK-888");

            // Act
            await handler.HandleEventAsync(evt);

            // Assert
            mockBroker.Received(1).Publish(Arg.Is<LiveEventDto>(dto =>
                dto.Track == DiagnosticTracks.HardwareGate &&
                dto.Level == DiagnosticLevels.Information &&
                dto.Source == "PLC-01" &&
                dto.Title.Contains("PLC 信号变位: PLC-01.TwinArm_Position_Arrived") &&
                dto.Summary.Contains("值变化: [False] -> [True]") &&
                dto.TraceId == "TRACE-PLC-100" &&
                dto.VehicleId == "AGV-03" &&
                dto.TargetId == "TASK-888"));
        }

        [Fact]
        public async Task HandleEventAsync_AlarmSignal_ShouldPublishWithWarningLevel()
        {
            // Arrange
            var mockBroker = Substitute.For<IDiagnosticLiveStreamBroker>();
            mockBroker.IsEnabled.Returns(true);

            var handler = new PlcSignalChangedEventHandler(
                NullLogger<PlcSignalChangedEventHandler>.Instance,
                mockBroker);

            var evt = new PlcSignalChangedEvent(
                deviceId: "PLC-02",
                tagName: "Safety_Door_Estop_Alarm",
                oldValue: false,
                newValue: true,
                dataType: "Boolean");

            // Act
            await handler.HandleEventAsync(evt);

            // Assert
            mockBroker.Received(1).Publish(Arg.Is<LiveEventDto>(dto =>
                dto.Track == DiagnosticTracks.HardwareGate &&
                dto.Level == DiagnosticLevels.Warning &&
                dto.Source == "PLC-02" &&
                dto.Title.Contains("Safety_Door_Estop_Alarm")));
        }
    }
}

