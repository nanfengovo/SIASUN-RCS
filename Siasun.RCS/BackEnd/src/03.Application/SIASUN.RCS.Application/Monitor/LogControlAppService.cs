using System.Collections.Generic;
using SIASUN.RCS.Logging;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    public class LogControlAppService : ApplicationService, ILogControlAppService
    {
        private readonly IDynamicLogSwitchRegistry _registry;

        public LogControlAppService(IDynamicLogSwitchRegistry registry)
        {
            _registry = registry;
        }

        public Dictionary<string, string> GetLevels()
        {
            return _registry.GetLevels();
        }

        public bool SetLevel(string namespaceName, string level)
        {
            if (System.Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(level, true, out var parsedLevel))
            {
                return _registry.TrySetLevel(namespaceName, parsedLevel);
            }
            return false;
        }
    }
}
