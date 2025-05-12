using RestSharp;
using System.Text.Json;

namespace OpenAIServices;

public class OpenAiAssistantHelper(string apiKey, string model)
{
    private string? _assistantId;
    private string? _vectorStoreId;
    private string? _fileBatchId;
    private readonly List<string> _fileIds = [];

    private const string BaseUrl = "https://api.openai.com";

    public async Task UploadFilesToVectorStoreAsync(string folderPath, bool forceReload = false)
    {
        if (forceReload)
        {
            await DeleteAllUploadedFilesAsync();
            await DeleteAllVectorStoreAsync();
            await DeleteAllAssistantsAsync();
        }

        // Check if vector store already exists
        var vectorStores = await ListVectorStoreAsync();
        foreach (var vectorStore in vectorStores.Where(vectorStore =>
                     vectorStore.name == "HL7 Mock Generator Specifications"))
        {
            _vectorStoreId = vectorStore.id;
            Console.WriteLine($"Vector store already exists: {_vectorStoreId}");
            return;
        }

        await UploadFilesFromFolderAsync(folderPath);

        if (_fileIds.Count == 0)
            throw new InvalidOperationException("No files uploaded.");

        var client = new RestClient(BaseUrl);

        // Create vector store
        var vsRequest = new RestRequest("/v1/vector_stores", Method.Post);
        vsRequest.AddHeader("Authorization", $"Bearer {apiKey}");
        vsRequest.AddHeader("OpenAI-Beta", "assistants=v2");
        vsRequest.AddJsonBody(new
        {
            name = "HL7 Mock Generator Specifications"
        });
        var vsResponse = await client.ExecuteAsync(vsRequest);
        if (vsResponse.Content != null)
        {
            var vsJson = JsonDocument.Parse(vsResponse.Content);
            _vectorStoreId = vsJson.RootElement.GetProperty("id").GetString();
        }

        if (string.IsNullOrEmpty(_vectorStoreId))
            throw new InvalidOperationException("Vector store not created.");

        Console.WriteLine($"Vector store created: {_vectorStoreId}");

        // Add files to vector store
        var addFilesRequest = new RestRequest($"/v1/vector_stores/{_vectorStoreId}/file_batches", Method.Post);
        addFilesRequest.AddHeader("Authorization", $"Bearer {apiKey}");
        addFilesRequest.AddHeader("OpenAI-Beta", "assistants=v2");
        addFilesRequest.AddJsonBody(new
        {
            file_ids = _fileIds
        });
        var vsFbResponse = await client.ExecuteAsync(addFilesRequest);
        if (vsFbResponse.Content != null)
        {
            var vsJson = JsonDocument.Parse(vsFbResponse.Content);
            _fileBatchId = vsJson.RootElement.GetProperty("id").GetString();
        }

        if (string.IsNullOrEmpty(_fileBatchId))
            throw new InvalidOperationException("File batch not created.");

        // Check status
        RestResponse checkStatusResponse;
        do
        {
            await Task.Delay(1000);
            var checkStatusRequest = new RestRequest($"/v1/vector_stores/{_vectorStoreId}/file_batches/{_fileBatchId}",
                Method.Get);
            checkStatusRequest.AddHeader("Authorization", $"Bearer {apiKey}");
            checkStatusRequest.AddHeader("OpenAI-Beta", "assistants=v2");
            checkStatusResponse = await client.ExecuteAsync(checkStatusRequest);
        } while (checkStatusResponse.Content == null || !checkStatusResponse.Content.Contains("completed"));

        Console.WriteLine($"File batch created and ready: {_fileBatchId}");
    }

    public async Task CreateAssistantAsync()
    {
        if (string.IsNullOrEmpty(_vectorStoreId))
            throw new InvalidOperationException("Vector store not initialized.");

        var client = new RestClient(BaseUrl);

        // Check if assistant already exists
        var assistants = await ListAssistantsAsync();
        foreach (var assistant in assistants.Where(assistant => assistant.name == "HL7 Mock Generator Assistant"))
        {
            _assistantId = assistant.id;
            Console.WriteLine($"Assistant already exists: {_assistantId}");
            return;
        }

        var request = new RestRequest("/v1/assistants", Method.Post);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        request.AddJsonBody(new
        {
            model,
            name = "HL7 Mock Generator Assistant",
            instructions =
                "You are a healthcare integration expert. Generate a compliant HL7 message using the uploaded documentation and examples." +
                " Return only the HL7 message with no extra text or explanation. Ensure each segment is on a new line.",
            temperature = 0.2,
            tools = new[] { new { type = "file_search" } },
            tool_resources = new
            {
                file_search = new
                {
                    vector_store_ids = new[] { _vectorStoreId }
                }
            }
        });

        var response = await client.ExecuteAsync(request);
        if (response.Content != null)
        {
            var json = JsonDocument.Parse(response.Content);
            _assistantId = json.RootElement.GetProperty("id").GetString();
        }

        if (string.IsNullOrEmpty(_assistantId))
            throw new InvalidOperationException("Assistant not created.");

        Console.WriteLine($"Assistant created: {_assistantId}");
    }

    public async Task<string> AskAsync(string prompt)
    {
        Console.WriteLine($"Starting prompt: {prompt}");

        var client = new RestClient(BaseUrl);

        var threadReq = new RestRequest("/v1/threads", Method.Post);
        threadReq.AddHeader("Authorization", $"Bearer {apiKey}");
        threadReq.AddHeader("OpenAI-Beta", "assistants=v2");
        var threadRes = await client.ExecuteAsync(threadReq);
        var threadJson = JsonDocument.Parse(threadRes.Content);
        var threadId = threadJson.RootElement.GetProperty("id").GetString();

        var msgReq = new RestRequest($"/v1/threads/{threadId}/messages", Method.Post);
        msgReq.AddHeader("Authorization", $"Bearer {apiKey}");
        msgReq.AddHeader("OpenAI-Beta", "assistants=v2");
        msgReq.AddJsonBody(new
        {
            role = "user",
            content = prompt
        });
        await client.ExecuteAsync(msgReq);

        var runReq = new RestRequest($"/v1/threads/{threadId}/runs", Method.Post);
        runReq.AddHeader("Authorization", $"Bearer {apiKey}");
        runReq.AddHeader("OpenAI-Beta", "assistants=v2");
        runReq.AddJsonBody(new { assistant_id = _assistantId });
        var runRes = await client.ExecuteAsync(runReq);
        var runJson = JsonDocument.Parse(runRes.Content);
        var runId = runJson.RootElement.GetProperty("id").GetString();

        string status;
        do
        {
            await Task.Delay(1000);
            var checkReq = new RestRequest($"/v1/threads/{threadId}/runs/{runId}", Method.Get);
            checkReq.AddHeader("Authorization", $"Bearer {apiKey}");
            checkReq.AddHeader("OpenAI-Beta", "assistants=v2");
            var checkRes = await client.ExecuteAsync(checkReq);
            var checkJson = JsonDocument.Parse(checkRes.Content);
            status = checkJson.RootElement.GetProperty("status").GetString();
        } while (status != "completed");

        var msgRes = new RestRequest($"/v1/threads/{threadId}/messages", Method.Get);
        msgRes.AddHeader("Authorization", $"Bearer {apiKey}");
        msgRes.AddHeader("OpenAI-Beta", "assistants=v2");
        var finalRes = await client.ExecuteAsync(msgRes);
        var msgJson = JsonDocument.Parse(finalRes.Content);

        return msgJson.RootElement
            .GetProperty("data")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetProperty("value")
            .GetString();
    }

    private async Task UploadFilesFromFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException("Folder not found: " + folderPath);

        var filePaths = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .ToList();

        var client = new RestClient(BaseUrl);

        foreach (var path in filePaths)
        {
            Console.WriteLine($"Uploading file: {Path.GetFileName(path)}");

            var request = new RestRequest("/v1/files", Method.Post);
            request.AddHeader("Authorization", $"Bearer {apiKey}");
            request.AddParameter("purpose", "assistants");
            request.AddFile("file", path);

            var response = await client.ExecuteAsync(request);
            var json = JsonDocument.Parse(response.Content);
            _fileIds.Add(json.RootElement.GetProperty("id").GetString());
        }
    }

    private async Task<List<(string id, string filename)>> ListUploadedFilesAsync()
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest("/v1/files", Method.Get);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        var response = await client.ExecuteAsync(request);
        var json = JsonDocument.Parse(response.Content);

        var list = new List<(string id, string filename)>();
        foreach (var file in json.RootElement.GetProperty("data").EnumerateArray())
        {
            var id = file.GetProperty("id").GetString();
            var name = file.GetProperty("filename").GetString();
            list.Add((id, name));
        }

        return list;
    }

    private async Task DeleteFileAsync(string fileId)
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest($"/v1/files/{fileId}", Method.Delete);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        await client.ExecuteAsync(request);
        Console.WriteLine($"File deleted: {fileId}");
    }

    private async Task DeleteAllUploadedFilesAsync()
    {
        var files = await ListUploadedFilesAsync();
        foreach (var file in files)
        {
            await DeleteFileAsync(file.id);
        }
    }

    private async Task DeleteVectorStoreAsync(string vectorStoreId)
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest($"/v1/vector_stores/{vectorStoreId}", Method.Delete);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        await client.ExecuteAsync(request);
        Console.WriteLine($"Vector store deleted: {vectorStoreId}");
    }

    private async Task<List<(string id, string name)>> ListVectorStoreAsync()
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest("/v1/vector_stores", Method.Get);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        var response = await client.ExecuteAsync(request);
        var json = JsonDocument.Parse(response.Content);

        var list = new List<(string id, string name)>();
        foreach (var vectorStore in json.RootElement.GetProperty("data").EnumerateArray())
        {
            var id = vectorStore.GetProperty("id").GetString();
            var name = vectorStore.GetProperty("name").GetString();
            list.Add((id, name));
        }

        return list;
    }

    private async Task DeleteAllVectorStoreAsync()
    {
        var vectorStores = await ListVectorStoreAsync();
        foreach (var vectorStore in vectorStores)
        {
            await DeleteVectorStoreAsync(vectorStore.id);
        }
    }

    private async Task<List<(string id, string name)>> ListAssistantsAsync()
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest("/v1/assistants", Method.Get);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        var response = await client.ExecuteAsync(request);
        var json = JsonDocument.Parse(response.Content);

        var list = new List<(string id, string name)>();
        foreach (var file in json.RootElement.GetProperty("data").EnumerateArray())
        {
            var id = file.GetProperty("id").GetString();
            var name = file.GetProperty("name").GetString();
            list.Add((id, name));
        }

        return list;
    }

    private async Task DeleteAllAssistantsAsync()
    {
        var assistants = await ListAssistantsAsync();
        foreach (var assistant in assistants)
        {
            await DeleteAssistantAsync(assistant.id);
        }
    }

    private async Task DeleteAssistantAsync(string assistantId)
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest($"/v1/assistants/{assistantId}", Method.Delete);
        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("OpenAI-Beta", "assistants=v2");
        await client.ExecuteAsync(request);
        Console.WriteLine($"Assistant deleted: {assistantId}");
    }
}