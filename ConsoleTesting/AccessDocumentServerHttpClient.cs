using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DocumentServer.Models.DTOS;
using DocumentServer.Models.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace ConsoleTesting;

public class AccessDocumentServerHttpClient : IDisposable
{
    private HttpClient                              _httpClient;
    private ILogger<AccessDocumentServerHttpClient> _logger;
    private JsonSerializerOptions                   _options;


    public AccessDocumentServerHttpClient(HttpClient httpClient, ILogger<AccessDocumentServerHttpClient> logger)
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
    public async Task<bool> SaveDocumentAsync(string name, string extension, string fileBytesInBase64)
    {
        DocumentUploadDTO documentUploadDto = new()
        {
            Name          = name,
            FileExtension = extension,
            FileBytes     = fileBytesInBase64
        };

        return await SaveDocumentInternalAsync(documentUploadDto);
    }



    /// <summary>
    /// Saves the given document to storage.  Will Read the provided file and store into the storage library.
    /// </summary>
    /// <param name="fileToSave">The FileInfo of the file you wish to save into storage.</param>
    /// <returns></returns>
    public async Task<bool> SaveDocumentAsync(FileInfo fileToSave)
    {
        try
        {
            string file = Convert.ToBase64String(File.ReadAllBytes(fileToSave.FullName));

            DocumentUploadDTO documentUploadDto = new()
            {
                Name          = fileToSave.Name,
                FileExtension = fileToSave.Extension,
                FileBytes     = file
            };

            return await SaveDocumentInternalAsync(documentUploadDto);
        }
        catch (Exception exception)
        {
            _logger.LogError("Failed to save Document:  {FileToSave}", fileToSave.FullName);
            return false;
        }
    }



    /// <summary>
    /// Saves the given document to storage.  This is the internal method that does the actual saving.
    /// </summary>
    /// <param name="documentUploadDto"></param>
    /// <returns></returns>
    private async Task<bool> SaveDocumentInternalAsync(DocumentUploadDTO documentUploadDto)
    {
        HttpResponseMessage response = null;
        try
        {
            // Call API
            response = await _httpClient.PostAsJsonAsync("documents", documentUploadDto);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception exception)
        {
            string content = await response.Content.ReadAsStringAsync();

            dynamic json = JsonNode.Parse(content);
            string  msg  = (string)json["detail"];
            _logger.LogError("Failed to store document:   {Name} {Extension}  |  Error: {Error} - Detailed {Msg}", documentUploadDto.Name,
                             documentUploadDto.FileExtension,
                             exception, msg);

            return false;
        }
    }



    public void Dispose() => _httpClient?.Dispose();
}