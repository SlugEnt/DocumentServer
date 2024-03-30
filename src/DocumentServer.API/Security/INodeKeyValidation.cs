namespace SlugEnt.DocumentServer.API.Security;

public interface INodeKeyValidation
{
    bool IsValidNodeKey(string apiKey);
}