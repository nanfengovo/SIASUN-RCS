using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Auditing;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public class AuditLogFilterEvaluator : IAuditLogFilterEvaluator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogFilterEvaluator> _logger;

        private IReadOnlyList<CompiledFilterRule> _rules = Array.Empty<CompiledFilterRule>();

        private static readonly string[] IgnoredPrefixes = new[]
        {
            "/health",
            "/swagger",
            "/Abp/",
            "/images/",
            "/Themes/",
            "/libs/",
            "/Account/"
        };

        private static readonly string[] IgnoredExtensions = new[]
        {
            ".js",
            ".css",
            ".ico",
            ".svg",
            ".png",
            ".jpg",
            ".jpeg",
            ".woff",
            ".woff2",
            ".ttf",
            ".map",
            ".html"
        };

        public AuditLogFilterEvaluator(
            IServiceScopeFactory scopeFactory,
            ILogger<AuditLogFilterEvaluator> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await RefreshRulesAsync();
        }

        public bool ShouldAudit(string path, string httpMethod, Direction direction)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return false;
            }

            for (var i = 0; i < IgnoredPrefixes.Length; i++)
            {
                if (path.StartsWith(IgnoredPrefixes[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            for (var i = 0; i < IgnoredExtensions.Length; i++)
            {
                if (path.EndsWith(IgnoredExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var snapshot = _rules;
            if (snapshot.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < snapshot.Count; i++)
            {
                var rule = snapshot[i];
                if (rule.RuleType == FilterRuleType.Blacklist && rule.IsMatch(path, httpMethod, direction))
                {
                    return false;
                }
            }

            for (var i = 0; i < snapshot.Count; i++)
            {
                var rule = snapshot[i];
                if (rule.RuleType == FilterRuleType.Whitelist && rule.IsMatch(path, httpMethod, direction))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task RefreshRulesAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetService<IRepository<AuditLogFilterRule, Guid>>();
                if (repository == null)
                {
                    return;
                }

                var activeRules = await repository.GetListAsync(r => r.IsEnabled);

                var compiled = activeRules.Select(r => new CompiledFilterRule(
                    r.Id,
                    r.Name,
                    r.RuleType,
                    r.Direction,
                    r.PathPattern,
                    r.HttpMethod
                )).ToList();

                Interlocked.Exchange(ref _rules, compiled);

                var whitelist = compiled.Where(r => r.RuleType == FilterRuleType.Whitelist).ToList();
                var blacklist = compiled.Where(r => r.RuleType == FilterRuleType.Blacklist).ToList();

                var whitelistStr = string.Join(", ", whitelist.Select(r => $"{r.Direction} {r.HttpMethod ?? "*"} {r.PathPattern}"));
                var blacklistStr = string.Join(", ", blacklist.Select(r => $"{r.Direction} {r.HttpMethod ?? "*"} {r.PathPattern}"));

                _logger.LogInformation(
                    "API 审计日志过滤规则已热刷新，当前已加载 {Count} 条生效规则。白名单: [{Whitelist}]，黑名单: [{Blacklist}]",
                    compiled.Count,
                    whitelistStr,
                    blacklistStr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新 API 审计日志过滤规则失败！");
            }
        }
    }
}
