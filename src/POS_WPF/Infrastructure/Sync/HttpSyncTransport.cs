using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace POS_WPF.Infrastructure.Sync;

public sealed class HttpSyncTransport(HttpClient httpClient, IConfiguration configuration) : ISyncTransport
{
    public async Task<IReadOnlyList<SyncConflict>> PushAsync(IReadOnlyList<SyncBatchItem> batch, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue<bool>("Synchronization:Enabled"); var endpoint = configuration["Synchronization:Endpoint"];
        if (!enabled || string.IsNullOrWhiteSpace(endpoint)) throw new InvalidOperationException("Synchronization is not enabled or its endpoint is not configured.");
        using var response = await httpClient.PostAsJsonAsync(endpoint, batch, cancellationToken); response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SyncConflict>>(cancellationToken: cancellationToken) ?? [];
    }
}
