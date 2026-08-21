using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace POS_WPF.Infrastructure.Sync;

public sealed class HttpSyncTransport(HttpClient httpClient, IConfiguration configuration) : ISyncTransport
{
    public async Task<IReadOnlyList<SyncConflict>> PushAsync(IReadOnlyList<SyncBatchItem> batch, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["Synchronization:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) return [];
        using var response = await httpClient.PostAsJsonAsync(endpoint, batch, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<SyncConflict>>(cancellationToken: cancellationToken) ?? [];
    }
}
