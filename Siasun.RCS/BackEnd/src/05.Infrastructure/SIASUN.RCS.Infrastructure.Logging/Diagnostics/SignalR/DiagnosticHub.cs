using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    public class DiagnosticHub : Hub
    {
        private readonly IDiagnosticLiveStreamBroker _broker;

        public DiagnosticHub(IDiagnosticLiveStreamBroker broker)
        {
            _broker = broker;
        }

        public async Task SubscribeAsync(string topic)
        {
            var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "all" : topic.Trim().ToLowerInvariant();
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedTopic);

            // 连入即推该 Topic 最近的环形滑窗历史
            var history = _broker.GetHistory(normalizedTopic);
            await Clients.Caller.SendAsync("ReceiveHistory", normalizedTopic, history);
        }

        public async Task UnsubscribeAsync(string topic)
        {
            var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "all" : topic.Trim().ToLowerInvariant();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, normalizedTopic);
        }

        public Task<IReadOnlyList<LiveEventDto>> GetHistoryAsync(string topic)
        {
            var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "all" : topic.Trim().ToLowerInvariant();
            return Task.FromResult(_broker.GetHistory(normalizedTopic));
        }

        public override async Task OnConnectedAsync()
        {
            // 客户端初次连入时，默认推送全量主题 recent 历史
            var defaultHistory = _broker.GetHistory("all");
            await Clients.Caller.SendAsync("ReceiveHistory", "all", defaultHistory);
            await base.OnConnectedAsync();
        }
    }
}
