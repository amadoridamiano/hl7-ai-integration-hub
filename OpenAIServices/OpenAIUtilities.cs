using System.Net.Http.Headers;

namespace OpenAIServices;

public class OpenAiUtilities
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAiUtilities(string apiKey, string model)
    {
        _httpClient = new HttpClient();
        _model = model;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    /// <summary>
    /// Checks if the API key is valid by making a request to the OpenAI API.
    /// </summary>
    /// <returns>None if valid, error message if invalid.</returns>
    public async Task<string> CheckApiKeyAsync()
    {
        var response = await _httpClient.GetAsync("https://api.openai.com/v1/models");
        return !response.IsSuccessStatusCode ? $"Error: {response.StatusCode} - {response.ReasonPhrase}" : string.Empty;
    }
}