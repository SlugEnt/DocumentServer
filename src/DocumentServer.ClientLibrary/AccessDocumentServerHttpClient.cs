using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using FileInfo = SlugEnt.DocumentServer.ClientLibrary.FileInfo;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace ConsoleTesting;

public sealed class AccessDocumentServerHttpClient : IDisposable
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


    public async Task DoDownload(long id)
    {
        string tmpFileName = Guid.NewGuid().ToString();
        string path        = Path.Join($"T:\\temp", tmpFileName);

        await GetDocumentFileAsStream(id, path);
    }


    public async Task GetDocumentFileAsStream(long documentId,
                                              string saveFileName)
    {
        try
        {
            string qry = "documents/" + documentId;
            using (HttpResponseMessage httpResponse = await _httpClient.GetAsync(qry, HttpCompletionOption.ResponseHeadersRead))
            {
                httpResponse.EnsureSuccessStatusCode();
                Stream stream = await httpResponse.Content.ReadAsStreamAsync();

                using (FileStream outputStream = new FileStream(saveFileName, FileMode.CreateNew))
                {
                    await stream.CopyToAsync(outputStream);
                }

                Console.WriteLine("SUCCESS:  Saved File: {0}", saveFileName);
                return;

                //StoredDocument storedDocument = await JsonSerializer.DeserializeAsync<StoredDocument>(stream, _options);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("Something wong:  {Error}", exception);
        }
    }


    public async Task<DocumentContainer> GetDocumentAndInfo(long documentId)
    {
        string content = string.Empty;

        try
        {
            string             action            = "documents/" + documentId + "/all";
            DocumentContainer? documentContainer = await _httpClient.GetFromJsonAsync<DocumentContainer>(action);
            return documentContainer;
        }
        catch (Exception exception)
        {
            _logger.LogError("Error during GetDocumentAndInfo:  {Error}", exception);
            return null;
        }
    }



    /// <summary>
    ///     Saves the given document to storage.  This is the internal method that does the actual saving.
    /// </summary>
    /// <param name="transferDocumentDto"></param>
    /// <param name="fileName">The full path and file name of file to send</param>
    /// <returns></returns>
    public async Task<Result<long>> SaveDocumentAsync(TransferDocumentDto transferDocumentDto,
                                                      string fileName)
    {
        HttpResponseMessage response        = null;
        string              content         = "";
        dynamic             json            = null;
        string              responseContent = string.Empty;

        try
        {
            DocumentContainer documentContainer = new DocumentContainer()
            {
                Info     = transferDocumentDto,
                FileInfo = new FileInfo(),
            };

            MultipartFormDataContent form = new MultipartFormDataContent();

            // Fill out the rest of data
            form.Add(new StringContent(transferDocumentDto.Description), "Info.Description");
            form.Add(new StringContent(transferDocumentDto.DocumentTypeId.ToString()), "Info.DocumentTypeId");
            form.Add(new StringContent(transferDocumentDto.FileExtension), "Info.FileExtension");
            form.Add(new StringContent(transferDocumentDto.RootObjectId.ToString()), "Info.RootObjectId");
            form.Add(new StringContent(transferDocumentDto.DocTypeExternalId), "Info.DocTypeExternalId");
            form.Add(new StringContent(transferDocumentDto.CurrentStoredDocumentId.ToString()), "Info.CurrentStoredDocumentId");

            // Add File
            await using var stream = System.IO.File.OpenRead(fileName);
            form.Add(new StreamContent(stream), "File", fileName);
            response = await _httpClient.PostAsync("documents", form);

            responseContent = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            if (!long.TryParse(responseContent, out long value))
                return Result.Ok(0L);
            else
                return Result.Ok(value);
        }
        catch (Exception exception)
        {
            string msg = "";
            if (json != null)
                msg = (string)json["detail"];
            if (msg == string.Empty)
                msg = exception.Message + " |  " + responseContent;


            _logger.LogError("Failed to store document:   {Description} {Extension}  |  Error: {Error} - Detailed {Msg}",
                             transferDocumentDto.Description,
                             transferDocumentDto.FileExtension,
                             exception,
                             msg);

            return Result.Fail(msg);
        }
    }
}