using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests
{
    public class AuditFilterRulesChangedEventHandlerTests
    {
        [Fact]
        public async Task HandleEventAsync_Should_RefreshEvaluator()
        {
            var evaluator = Substitute.For<IAuditLogFilterEvaluator>();
            var handler = new AuditFilterRulesChangedEventHandler(evaluator);

            await handler.HandleEventAsync(new AuditFilterRulesChangedEvent());

            await evaluator.Received(1).RefreshRulesAsync();
        }
    }
}
