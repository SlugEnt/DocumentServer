using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocumentServer.ClientLibrary;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace ConsoleTesting;

public class AccessDocumentServerHttpClient : IDisposable
{
    private readonly HttpClient                              _httpClient;
    private readonly ILogger<AccessDocumentServerHttpClient> _logger;
    private readonly JsonSerializerOptions                   _options;


    public AccessDocumentServerHttpClient(HttpClient httpClient,
                                          ILogger<AccessDocumentServerHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        _httpClient.BaseAddress = new Uri("https://localhost:7223/api/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
        _httpClient.Timeout = new TimeSpan(0, 0, 1200);
        _httpClient.DefaultRequestHeaders.Clear();
    }



    public void Dispose() => _httpClient?.Dispose();


    public async Task<string> GetDocument()
    {
        try
        {
            //StoredDocument storedDocument = new StoredDocument { AppAreaId = "WC", AppName = "MDOS", CreatedAt = DateTime.Now, Id= new Guid(), StorageFolder = @"C:\temp"};
            /*
            using StringContent json = new(JsonSerializer.Serialize(storedDocument, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8,
                                           MediaTypeNames.Application.Json);
            */
            Guid id = Guid.NewGuid();

            string q = "documents/" + "12345" + "/scott";
            using (HttpResponseMessage httpResponse = await _httpClient.GetAsync(q, HttpCompletionOption.ResponseHeadersRead))
            {
                httpResponse.EnsureSuccessStatusCode();
                Stream         stream         = await httpResponse.Content.ReadAsStreamAsync();
                StoredDocument storedDocument = await JsonSerializer.DeserializeAsync<StoredDocument>(stream, _options);
            }

            return "yeep";
        }
        catch (Exception exception)
        {
            _logger.LogError("Something wong:  {Error}", exception);
            return "no";
        }
    }



    /// <summary>
    ///     Saves the given document to storage.  This is the internal method that does the actual saving.
    /// </summary>
    /// <param name="transferDocumentDto"></param>
    /// <returns></returns>
    public async Task<Result<int>> SaveDocumentAsync(TransferDocumentDto transferDocumentDto)
    {
        HttpResponseMessage response = null;
        string              content  = "";
        dynamic             json     = null;
        try
        {
            // Call API
            response = await _httpClient.PostAsJsonAsync("Documents", transferDocumentDto);
            content  = await response.Content.ReadAsStringAsync();
            json     = JsonNode.Parse(content);

            response.EnsureSuccessStatusCode();

            int id = json["id"];
            return Result.Ok(id);
        }
        catch (Exception exception)
        {
            string msg = "";
            if (json != null)
                msg = (string)json["detail"];
            if (msg == string.Empty)
                msg = exception.Message;


            _logger.LogError("Failed to store document:   {Description} {Extension}  |  Error: {Error} - Detailed {Msg}",
                             transferDocumentDto.Description,
                             transferDocumentDto.FileExtension,
                             exception,
                             msg);

            return Result.Fail(msg);
        }
    }
}