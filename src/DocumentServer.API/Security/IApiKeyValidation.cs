namespace SlugEnt.DocumentServer.API.Security
{
    public interface IApiKeyValidation
    {
        bool IsValidApiKey(string apiKey);
    }
}