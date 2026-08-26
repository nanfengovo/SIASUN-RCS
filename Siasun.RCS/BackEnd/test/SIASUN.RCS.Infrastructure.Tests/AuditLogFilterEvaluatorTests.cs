using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests
{
    public class AuditLogFilterEvaluatorTests
    {
        private readonly IRepository<AuditLogFilterRule, Guid> _ruleRepository;
        private readonly IServiceScopeFactory _scopeFactory;

        public AuditLogFilterEvaluatorTests()
        {
            _ruleRepository = Substitute.For<IRepository<AuditLogFilterRule, Guid>>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IRepository<AuditLogFilterRule, Guid>)).Returns(_ruleRepository);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(serviceProvider);

            _scopeFactory = Substitute.For<IServiceScopeFactory>();
            _scopeFactory.CreateScope().Returns(scope);
        }

        [Fact]
        public void Should_Fast_Reject_Builtin_Static_And_Health_Paths()
        {
            var evaluator = new AuditLogFilterEvaluator(_scopeFactory, NullLogger<AuditLogFilterEvaluator>.Instance);

            evaluator.ShouldAudit("/health", "GET", Direction.Inbound).ShouldBeFalse();
            evaluator.ShouldAudit("/swagger/index.html", "GET", Direction.Inbound).ShouldBeFalse();
            evaluator.ShouldAudit("/Themes/LeptonXLite/css/bundle.css", "GET", Direction.Inbound).ShouldBeFalse();
            evaluator.ShouldAudit("/libs/abp/core/abp.js", "GET", Direction.Inbound).ShouldBeFalse();
            evaluator.ShouldAudit("/favicon.ico", "GET", Direction.Inbound).ShouldBeFalse();
            evaluator.ShouldAudit("/", "GET", Direction.Inbound).ShouldBeFalse();
            evaluator.ShouldAudit("", "GET", Direction.Inbound).ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Audit_Whitelisted_Paths()
        {
            _ruleRepository.GetListAsync(Arg.Any<Expression<Func<AuditLogFilterRule, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new List<AuditLogFilterRule>
                {
                    new AuditLogFilterRule(Guid.NewGuid(), "业务接口", "/api/rcs/**", FilterRuleType.Whitelist, FilterDirection.Both, "*", true),
                    new AuditLogFilterRule(Guid.NewGuid(), "TM 适配器", "/api/adapters/tm/*", FilterRuleType.Whitelist, FilterDirection.Inbound, "POST", true)
                });

            var evaluator = new AuditLogFilterEvaluator(_scopeFactory, NullLogger<AuditLogFilterEvaluator>.Instance);
            await evaluator.InitializeAsync();

            evaluator.ShouldAudit("/api/rcs/tasks/create", "POST", Direction.Inbound).ShouldBeTrue();
            evaluator.ShouldAudit("/api/rcs/tasks/123", "GET", Direction.Inbound).ShouldBeTrue();
            evaluator.ShouldAudit("/api/adapters/tm/callback", "POST", Direction.Inbound).ShouldBeTrue();

            // GET 方法不符合 TM 适配器 POST 规则
            evaluator.ShouldAudit("/api/adapters/tm/callback", "GET", Direction.Inbound).ShouldBeFalse();

            // 不在白名单中的路径
            evaluator.ShouldAudit("/api/other/something", "POST", Direction.Inbound).ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Blacklist_Prioritize_Over_Whitelist()
        {
            _ruleRepository.GetListAsync(Arg.Any<Expression<Func<AuditLogFilterRule, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new List<AuditLogFilterRule>
                {
                    new AuditLogFilterRule(Guid.NewGuid(), "业务接口白名单", "/api/rcs/**", FilterRuleType.Whitelist, FilterDirection.Both, "*", true),
                    new AuditLogFilterRule(Guid.NewGuid(), "高频心跳黑名单", "/api/rcs/vehicles/*/heartbeat", FilterRuleType.Blacklist, FilterDirection.Both, "POST", true)
                });

            var evaluator = new AuditLogFilterEvaluator(_scopeFactory, NullLogger<AuditLogFilterEvaluator>.Instance);
            await evaluator.InitializeAsync();

            // 普通任务接口命中白名单且无黑名单拦截 -> True
            evaluator.ShouldAudit("/api/rcs/tasks/create", "POST", Direction.Inbound).ShouldBeTrue();

            // 高频心跳命中黑名单 -> False (黑名单优先)
            evaluator.ShouldAudit("/api/rcs/vehicles/AGV01/heartbeat", "POST", Direction.Inbound).ShouldBeFalse();
        }
    }
}
