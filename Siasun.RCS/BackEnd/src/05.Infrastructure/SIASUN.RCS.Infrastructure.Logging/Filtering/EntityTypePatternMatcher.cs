using System;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public static class EntityTypePatternMatcher
    {
        public static bool IsMatch(string pattern, string fullName, string shortName)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return false;
            if (pattern == "*") return true;

            // 前缀匹配 (例如 Volo.Abp.Identity.*)
            if (pattern.EndsWith(".*"))
            {
                var prefix = pattern.Substring(0, pattern.Length - 2);
                return fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            // 后缀匹配 (例如 *Mission)
            if (pattern.StartsWith("*"))
            {
                var suffix = pattern.Substring(1);
                return fullName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) || 
                       shortName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            // 精确匹配
            return string.Equals(pattern, fullName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(pattern, shortName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
