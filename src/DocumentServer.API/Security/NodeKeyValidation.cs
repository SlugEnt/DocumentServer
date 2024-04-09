namespace SlugEnt.DocumentServer.API.Security;

public class NodeKeyValidation : INodeKeyValidation
{
    private readonly IConfiguration _configuration;


    public NodeKeyValidation(IConfiguration configuration) { _configuration = configuration; }


    public bool IsValidNodeKey(string hostNodeKey)
    {
        if (string.IsNullOrWhiteSpace(hostNodeKey))
            return false;

        string? nodeKey = _configuration.GetValue<string>("DocumentServer:NodeKey");
        if (nodeKey == null || nodeKey != hostNodeKey)
            return false;

        return true;
    }
}