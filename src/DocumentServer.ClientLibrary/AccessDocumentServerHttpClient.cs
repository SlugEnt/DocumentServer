using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocumentServer.ClientLibrary;
using DocumentServer.Models.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SlugEnt.FluentResults;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace ConsoleTesting;

public class AccessDocumentServerHttpClient : IDisposable
{
    private HttpClient                              _httpClient;
    private ILogger<AccessDocumentServerHttpClient> _logger;
    private JsonSerializerOptions                   _options;


    public AccessDocumentServerHttpClient(HttpClient httpClient,
                                          ILogger<AccessDocumentServerHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri("https://localhost:7223/api/");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
        _httpClient.Timeout = new TimeSpan(0, 0, 1200);
        _httpClient.DefaultRequestHeaders.Clear();
    }


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
    /// Saves the given document to storage.
    /// </summary>
    /// <param name="name">The name of the file</param>
    /// <param name="extension">The extension the file has.</param>
    /// <param name="fileBytesInBase64">String of bytes.  MUST BE IN BASE64 format</param>
    /// <returns></returns>
    public async Task<Result<string>> SaveDocumentAsync(string name,
                                                        string extension,
                                                        string fileBytesInBase64)
    {
        TransferDocumentDto transferDocumentDto = new()
        {
            Description        = name,
            FileExtension      = extension,
            FileInBase64Format = fileBytesInBase64
        };

        return await SaveDocumentInternalAsync(transferDocumentDto);
    }



    /// <summary>
    /// Saves the given document to storage.  Will Read the provided file and store into the storage library.
    /// </summary>
    /// <param name="fileToSave">The FileInfo of the file you wish to save into storage.</param>
    /// <returns></returns>
    public async Task<Result<string>> SaveDocumentAsync(FileInfo fileToSave)
    {
        try
        {
            string file = Convert.ToBase64String(File.ReadAllBytes(fileToSave.FullName));

            TransferDocumentDto transferDocumentDto = new()
            {
                DocumentTypeId     = 1,
                Description        = fileToSave.Name,
                FileExtension      = fileToSave.Extension,
                FileInBase64Format = file
            };

            return await SaveDocumentInternalAsync(transferDocumentDto);
        }
        catch (Exception exception)
        {
            _logger.LogError("Failed to save Document:  {FileToSave}", fileToSave.FullName);
            return Result.Fail(exception.ToString());
        }
    }



    /// <summary>
    /// Saves the given document to storage.  This is the internal method that does the actual saving.
    /// </summary>
    /// <param name="transferDocumentDto"></param>
    /// <returns></returns>
    private async Task<Result<string>> SaveDocumentInternalAsync(TransferDocumentDto transferDocumentDto)
    {
        HttpResponseMessage response = null;
        string              content  = "";
        dynamic             json     = null;
        try
        {
            // Call API
            response = await _httpClient.PostAsJsonAsync("documents", transferDocumentDto);
            content  = await response.Content.ReadAsStringAsync();
            json     = JsonNode.Parse(content);

            response.EnsureSuccessStatusCode();

            string id = (string)json["id"];
            return Result.Ok(id);
        }
        catch (Exception exception)
        {
            string msg = (string)json["detail"];
            _logger.LogError("Failed to store document:   {Description} {Extension}  |  Error: {Error} - Detailed {Msg}",
                             transferDocumentDto.Description,
                             transferDocumentDto.FileExtension,
                             exception,
                             msg);

            return Result.Fail(msg);
        }
    }



    public void Dispose() => _httpClient?.Dispose();
}