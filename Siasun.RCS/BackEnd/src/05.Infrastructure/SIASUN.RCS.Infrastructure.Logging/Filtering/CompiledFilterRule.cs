using System;
using System.Text.RegularExpressions;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public class CompiledFilterRule
    {
        public Guid Id { get; }
        public string Name { get; }
        public FilterRuleType RuleType { get; }
        public FilterDirection Direction { get; }
        public string HttpMethod { get; }
        public string PathPattern { get; }
        public Regex PathRegex { get; }

        public CompiledFilterRule(
            Guid id,
            string name,
            FilterRuleType ruleType,
            FilterDirection direction,
            string pathPattern,
            string httpMethod)
        {
            Id = id;
            Name = name;
            RuleType = ruleType;
            Direction = direction;
            PathPattern = pathPattern;
            HttpMethod = string.IsNullOrWhiteSpace(httpMethod) ? "*" : httpMethod.Trim().ToUpperInvariant();
            PathRegex = ConvertGlobToRegex(pathPattern);
        }

        public bool IsMatch(string path, string httpMethod, Direction requestDirection)
        {
            if (Direction != FilterDirection.Both)
            {
                if (Direction == FilterDirection.Inbound && requestDirection != SIASUN.RCS.Auditing.Direction.Inbound) return false;
                if (Direction == FilterDirection.Outbound && requestDirection != SIASUN.RCS.Auditing.Direction.Outbound) return false;
            }

            if (HttpMethod != "*" && !string.Equals(HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return PathRegex.IsMatch(path);
        }

        private static Regex ConvertGlobToRegex(string glob)
        {
            if (string.IsNullOrWhiteSpace(glob))
            {
                return new Regex("^$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            glob = glob.Trim();
            var escaped = Regex.Escape(glob)
                .Replace(@"\*\*", ".*")
                .Replace(@"\*", "[^/]*")
                .Replace(@"\?", ".");

            return new Regex($"^{escaped}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
