namespace SlugEnt.DocumentServer.API.Security;

public class ApiKeyValidation : IApiKeyValidation
{
    private readonly IConfiguration            _configuration;
    private readonly ILogger<ApiKeyValidation> _logger;


    public ApiKeyValidation(IConfiguration configuration,
                            ILogger<ApiKeyValidation> logger)
    {
        _logger        = logger;
        _configuration = configuration;
    }


    public bool IsValidApiKey(string userApiKey)
    {
        if (string.IsNullOrWhiteSpace(userApiKey))
        {
            _logger.LogWarning("Client did not provide an ApiKey value.");
            return false;
        }

        string? apiKey = _configuration.GetValue<string>("DocumentServer:ApiKey");
        if (apiKey == null || apiKey != userApiKey)
        {
            string logMsg = "Client Sent ApiKey: [ " + userApiKey + " ]";
#if DEBUG
            if (apiKey != null)
                logMsg += "  |  Our ApiKey:  [ " + apiKey + " ]";
#endif
            _logger.LogWarning(logMsg);

            return false;
        }

        return true;
    }
}