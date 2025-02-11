using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

public class GitHubService(HttpClient httpClient, ILogger<GitHubService> logger)
{
    private class GitHubApiResponse
    {
        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }
    }

    [RequiresUnreferencedCode("The following members are used by JsonSerializer.DeserializeAsync<TValue>")]
    public async Task<(bool Success, int StargazersCount)> GetStargazersCountAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{name}");

        // !!IMPORTANT!!: GitHub API requires a User-Agent header
        // When this code is running in a browser, the User-Agent header is automatically added by the browser.
        // However, when this code running in a dotnet-publish process, the User-Agent header is not added automatically.
        // So, we need to add it manually.
        request.Headers.Add("User-Agent", "BlazorWasmApp1");

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            using var streamContent = await response.Content.ReadAsStreamAsync(cancellationToken);
            var apiResponse = await JsonSerializer.DeserializeAsync<GitHubApiResponse>(streamContent, new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
            if (apiResponse is not null)
            {
                logger.LogInformation($"Repository {owner}/{name} has {apiResponse.StargazersCount} stargazers");
                return (true, apiResponse.StargazersCount);
            }
        }

        logger.LogWarning($"Failed to get stargazers count for repository {owner}/{name}");
        return (false, 0);
    }
}
