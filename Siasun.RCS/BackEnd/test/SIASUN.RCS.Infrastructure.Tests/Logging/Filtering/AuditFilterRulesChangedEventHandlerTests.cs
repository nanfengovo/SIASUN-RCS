using System.Threading.Tasks;
using NSubstitute;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering;

public class AuditFilterRulesChangedEventHandlerTests
{
    [Fact]
    public async Task HandleEventAsync_ShouldRefreshRules()
    {
        // Arrange
        var evaluator = Substitute.For<IAuditLogFilterEvaluator>();
        var handler = new AuditFilterRulesChangedEventHandler(evaluator);
        var eventData = new AuditFilterRulesChangedEvent();

        // Act
        await handler.HandleEventAsync(eventData);

        // Assert
        await evaluator.Received(1).RefreshRulesAsync();
    }
}
