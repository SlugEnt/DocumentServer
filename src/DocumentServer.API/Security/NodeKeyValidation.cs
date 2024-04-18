using SlugEnt.DocumentServer.Core;

namespace SlugEnt.DocumentServer.API.Security;

public class NodeKeyValidation : INodeKeyValidation
{
    private readonly IConfiguration            _configuration;
    private readonly ILogger                   _logger;
    private readonly DocumentServerInformation _dsi;


    public NodeKeyValidation(IConfiguration configuration,
                             ILogger<NodeKeyValidation> logger,
                             DocumentServerInformation dsi)
    {
        _configuration = configuration;
        _logger        = logger;
        _dsi           = dsi;
    }



    public bool IsValidNodeKey(string hostNodeKey)
    {
        if (string.IsNullOrWhiteSpace(hostNodeKey))
        {
#if DEBUG
            _logger.LogInformation("Client did not provide a NodeKey value.");
#endif
            return false;
        }

        string nodeKey = _dsi.ServerHostInfo.NodeKey;

        //string? nodeKey = _configuration.GetValue<string>("DocumentServer:NodeKey");
        string logMsg = "Client Sent NodeKey: [ " + hostNodeKey + " ]";
#if DEBUG
        logMsg += "  |  Our NodeKey:  [ " + nodeKey + " ]";
#endif
        _logger.LogInformation(logMsg);
        if (nodeKey == null || nodeKey != hostNodeKey)
            return false;

        return true;
    }
}