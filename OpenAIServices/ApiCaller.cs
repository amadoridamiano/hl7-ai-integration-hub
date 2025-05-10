using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OpenAIServices;

public class ApiCaller
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public ApiCaller(string apiKey, string model)
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

    /// <summary>
    /// Generates an HL7 message using OpenAI's model.
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    public async Task<string> GenerateMessageFromAiAsync(string prompt)
    {
        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        try
        {
            return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }
        catch
        {
            return "";
        }
    }
}