using Microsoft.AspNetCore.Authorization;
using SlugEnt.DocumentServer.Core;

namespace SlugEnt.DocumentServer.API.Security;

public class ApiKeyHandler : AuthorizationHandler<ApiKeyRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApiKeyValidation    _apiKeyValidation;


    public ApiKeyHandler(IHttpContextAccessor httpContextAccessor,
                         IApiKeyValidation apiKeyValidation)
    {
        _httpContextAccessor = httpContextAccessor;
        _apiKeyValidation    = apiKeyValidation;
    }


    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiKeyRequirement requirement)
    {
        string apiKey = _httpContextAccessor?.HttpContext?.Request.Headers[ApiKeyConstants.ApiKeyHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (!_apiKeyValidation.IsValidApiKey(apiKey))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}