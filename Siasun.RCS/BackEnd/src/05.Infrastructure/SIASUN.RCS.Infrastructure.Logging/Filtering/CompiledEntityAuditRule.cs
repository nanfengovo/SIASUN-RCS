using System;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public class CompiledEntityAuditRule
    {
        public string Pattern { get; }
        public EntityAuditMode Mode { get; }
        public int Priority { get; }
        public int SampleIntervalMs { get; }
        public string? ExcludedProperties { get; }
        private readonly bool _isExact;
        private readonly bool _isPrefix;
        private readonly bool _isSuffix;
        private readonly bool _isWildcard;
        private readonly string _matchString;

        public CompiledEntityAuditRule(string pattern, EntityAuditMode mode, int priority, int sampleIntervalMs, string? excludedProperties)
        {
            Pattern = pattern;
            Mode = mode;
            Priority = priority;
            SampleIntervalMs = sampleIntervalMs;
            ExcludedProperties = excludedProperties;
            
            if (pattern == "*")
            {
                _isWildcard = true;
                _matchString = "";
            }
            else if (pattern.StartsWith("*"))
            {
                _isSuffix = true;
                _matchString = pattern.TrimStart('*');
            }
            else if (pattern.EndsWith("*"))
            {
                _isPrefix = true;
                _matchString = pattern.TrimEnd('*');
            }
            else
            {
                _isExact = true;
                _matchString = pattern;
            }
        }

        public bool IsMatch(string fullName, string shortName)
        {
            if (_isWildcard) return true;
            if (_isExact) return fullName == _matchString || shortName == _matchString;
            if (_isSuffix) return fullName.EndsWith(_matchString) || shortName.EndsWith(_matchString);
            if (_isPrefix) return fullName.StartsWith(_matchString) || shortName.StartsWith(_matchString);
            return false;
        }
    }
}
