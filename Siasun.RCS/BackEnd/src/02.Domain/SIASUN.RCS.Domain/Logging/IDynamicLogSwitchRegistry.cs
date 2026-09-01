using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SIASUN.RCS.Logging
{
    public interface IDynamicLogSwitchRegistry
    {
        Dictionary<string, string> GetLevels();
        bool TrySetLevel(string namespacePrefix, LogLevel level);
    }
}

