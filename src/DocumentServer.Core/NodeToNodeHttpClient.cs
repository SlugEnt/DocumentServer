using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Db.Migrations;
using SlugEnt.FluentResults;

namespace SlugEnt.DocumentServer.Core;

public sealed class NodeToNodeHttpClient : IDisposable
{
    private readonly HttpClient                    _httpClient;
    private readonly ILogger<NodeToNodeHttpClient> _logger;
    private          string                        _NodeKey;


    /// <summary>
    /// Constructs a Node to Node Http Client Interface
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public NodeToNodeHttpClient(HttpClient httpClient,
                                ILogger<NodeToNodeHttpClient> logger = null)

        //,ILogger<NodeToNodeHttpClient> logger)
    {
        _httpClient = httpClient;

        _logger = logger;


        //        _apiKey = configuration.GetValue<string>("DocumentServer:ApiKey");

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
        _httpClient.Timeout = new TimeSpan(0, 0, 10);
        _httpClient.DefaultRequestHeaders.Clear();
    }



    // The API Key to access the API with
    public string NodeKey
    {
        get { return _NodeKey; }
        set { _NodeKey = value; }
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
    /// Determines if the remote node is alive.
    /// </summary>
    /// <param name="nodeAddress"></param>
    /// <returns></returns>
    public async Task<Result> AskIfAlive(string nodeAddress)
    {
        try
        {
            // TODO fix this to be set via config http or https
            string query = "http://" + nodeAddress + "/api/node/alive";

//            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(ApiConstants.NodeKeyHeaderName, NodeKey);
            using (HttpResponseMessage httpResponse = await _httpClient.GetAsync(query, HttpCompletionOption.ResponseHeadersRead))
            {
                httpResponse.EnsureSuccessStatusCode();

                //_logger.LogInformation("Alive Message Received!");
                return Result.Ok();
            }
        }
        catch (Exception e)
        {
            return Result.Fail(new Error("Error asking remote if they are alive").CausedBy(e));
        }
    }


    public async Task<Result> SendDocument(string nodeAddress,
                                           RemoteDocumentStorageDto remoteDocumentStorageDto)
    {
        HttpResponseMessage? response;
        string               responseContent = string.Empty;

        _httpClient.DefaultRequestHeaders.Clear();

        try
        {
            // TODO fix this to be set via config http or https
            string query = "http://" + nodeAddress + "/api/node/storedocument";

            // Load the File data to form.
            MultipartFormDataContent form = new();
            MemoryStream             ms   = new();
            ms.Seek(0, SeekOrigin.Begin);
            await remoteDocumentStorageDto.File.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Fill out the rest of data
            form.Add(new StringContent(remoteDocumentStorageDto.StorageNodeId.ToString()), "StorageNodeId");
            form.Add(new StringContent(remoteDocumentStorageDto.StoragePath), "StoragePath");
            form.Add(new StringContent(remoteDocumentStorageDto.FileName), "FileName");
            form.Add(new StreamContent(ms), "File", remoteDocumentStorageDto.FileName);


            _httpClient.DefaultRequestHeaders.Add(ApiConstants.NodeKeyHeaderName, _NodeKey);


            using (HttpResponseMessage httpResponse = await _httpClient.PostAsync(query, form))
            {
                responseContent = await httpResponse.Content.ReadAsStringAsync();
                httpResponse.EnsureSuccessStatusCode();
                return Result.Ok();
            }
        }
        catch (Exception e)
        {
            string msg = "NodeController:  SendDocument -->  Error sending douxment to remote server.  Server Returned [ " + responseContent + " ]";
            if (_logger != null)
                _logger.LogError(msg + e.ToString());

            return Result.Fail(new Error(msg).CausedBy(e));
        }
    }
}