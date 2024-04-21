using SlugEnt.DocumentServer.Core;
using SlugEnt.FluentResults;
using System.Text.Json;


namespace SlugEnt.DocumentServer.ClientLibrary;

/// <summary>
/// This class provides a standardized means of communicating to the Document Server.  It will handle automatically some of
/// the decisions of where to send documents to in a distributed system.
/// </summary>
public sealed class AccessDocumentServerHttpClient : IDisposable
{
    private readonly HttpClient                              _httpClient;
    private readonly ILogger<AccessDocumentServerHttpClient> _logger;
    private readonly JsonSerializerOptions                   _options;
    private          string                                  _apiKey;


    /// <summary>
    /// Constructs a Document Server Client Interface
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public AccessDocumentServerHttpClient(HttpClient httpClient,
                                          ILogger<AccessDocumentServerHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
        _httpClient.Timeout = new TimeSpan(0, 0, 1000);
        _httpClient.DefaultRequestHeaders.Clear();
    }


    // The API Key to access the API with
    public string ApiKey
    {
        get { return _apiKey; }
        set { _apiKey = value; }
    }


    public void Dispose() => _httpClient?.Dispose();


    /// <summary>
    /// Sets/Retreives the HttpClient Base Address
    /// </summary>
    public Uri BaseAddress
    {
        get { return _httpClient.BaseAddress; }
        set { _httpClient.BaseAddress = value; }
    }


    /// <summary>
    /// Retrieves just the document and saves it to the file name
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="saveFileName"></param>
    /// <returns>Result object with Success or Failure return code.</returns>
    public async Task<Result> GetDocumentAndSaveToFileSystem(long documentId,
                                                             string saveFileName,
                                                             string appToken)
    {
        try
        {
            string qry = "documents/" + documentId;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHeaderName, _apiKey);
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.AppTokenHeaderName, appToken);
            using (HttpResponseMessage httpResponse = await _httpClient.GetAsync(qry, HttpCompletionOption.ResponseHeadersRead))
            {
                httpResponse.EnsureSuccessStatusCode();
                Stream stream = await httpResponse.Content.ReadAsStreamAsync();

                using (FileStream outputStream = new(saveFileName, FileMode.CreateNew))
                {
                    await stream.CopyToAsync(outputStream);
                }

                Console.WriteLine("SUCCESS:  Saved File: {0}", saveFileName);
                return Result.Ok();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("Something wong:  {Error}", exception);
            return Result.Fail(new ExceptionalError(exception));
        }
    }



    /// <summary>
    /// Retrieves the document AND additional metadata about the document from the Document Server.  Use this if you want to
    /// figure out what to do with the file bytes (save them, send them somewhere else etc...)
    /// </summary>
    /// <param name="documentId">Id of document to retreive</param>
    /// <returns>Rresult with success or failure.  If Success the Value of the Result object is the ReturnedDocumentInfo class</returns>
    public async Task<Result<ReturnedDocumentInfo?>> GetDocumentAsync(long documentId,
                                                                      string appToken)
    {
        try
        {
            string action = "documents/" + documentId;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHeaderName, _apiKey);
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.AppTokenHeaderName, appToken);

            ReturnedDocumentInfo? returnedDocumentInfo = await _httpClient.GetFromJsonAsync<ReturnedDocumentInfo>(action);
            return Result.Ok(returnedDocumentInfo);
        }
        catch (Exception exception)
        {
            _logger.LogError("Error during GetDocumentAsync:  {Error}", exception);
            return Result.Fail(new ExceptionalError(exception));
        }
    }



    /// <summary>
    ///     Saves the given document to Document Server by reading it from the File System
    /// </summary>
    /// <param name="transferDocumentDto">Information required to save the document to the Document Server</param>
    /// <param name="fileName">The full path and file name of file/document to save</param>
    /// <returns>A Result with a value of the Long Id of the Document as stored in the Document Server</returns>
    public async Task<Result<long>> SaveDocumentFromFileAsync(TransferDocumentDto transferDocumentDto,
                                                              string fileName,
                                                              string appToken)
    {
        HttpResponseMessage response;
        string              responseContent = string.Empty;

        try
        {
            MultipartFormDataContent form = new();

            // Add File
            await using var stream = System.IO.File.OpenRead(fileName);
            form.Add(new StreamContent(stream), "File", fileName);

            return await SaveDocumentInternal(form, transferDocumentDto, appToken);
        }
        catch (Exception exception)
        {
            return Result.Fail(new Error("Failed to save document due to errors").CausedBy(exception));
        }
    }


    /// <summary>
    /// Saves a file into DocumentServer from a byte array.
    /// </summary>
    /// <param name="transferDocumentDto"></param>
    /// <param name="fileBytes"></param>
    /// <param name="appToken"></param>
    /// <returns></returns>
    public async Task<Result<long>> SaveDocumentFromBytesAsync(TransferDocumentDto transferDocumentDto,
                                                               byte[] fileBytes,
                                                               string appToken)
    {
        try
        {
            MultipartFormDataContent form = new();
            MemoryStream             ms   = new(fileBytes);
            ms.Seek(0, SeekOrigin.Begin);

            form.Add(new StreamContent(ms), "File", "somefile");

            return await SaveDocumentInternal(form, transferDocumentDto, appToken);
        }
        catch (Exception exception)
        {
            return Result.Fail(new Error("Failed to save document due to errors").CausedBy(exception));
        }
    }



    private async Task<Result<long>> SaveDocumentInternal(MultipartFormDataContent form,
                                                          TransferDocumentDto transferDocumentDto,
                                                          string appToken)
    {
        HttpResponseMessage response;
        string              responseContent = string.Empty;

        try
        {
            // Fill out the rest of data
            form.Add(new StringContent(transferDocumentDto.Description), "Description");
            form.Add(new StringContent(transferDocumentDto.DocumentTypeId.ToString()), "DocumentTypeId");
            form.Add(new StringContent(transferDocumentDto.FileExtension), "FileExtension");
            form.Add(new StringContent(transferDocumentDto.RootObjectId.ToString()), "RootObjectId");
            form.Add(new StringContent(transferDocumentDto.DocTypeExternalId), "DocTypeExternalId");
            form.Add(new StringContent(transferDocumentDto.CurrentStoredDocumentId.ToString()), "CurrentStoredDocumentId");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHeaderName, _apiKey);
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.AppTokenHeaderName, appToken);
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