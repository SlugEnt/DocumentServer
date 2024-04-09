namespace SlugEnt.DocumentServer.API.Security;

public class ApiKeyValidation : IApiKeyValidation
{
    private readonly IConfiguration _configuration;


    public ApiKeyValidation(IConfiguration configuration) { _configuration = configuration; }


    public bool IsValidApiKey(string userApiKey)
    {
        if (string.IsNullOrWhiteSpace(userApiKey))
            return false;

        string? apiKey = _configuration.GetValue<string>("DocumentServer:ApiKey");
        if (apiKey == null || apiKey != userApiKey)
            return false;

        return true;
    }
}