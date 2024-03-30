using Microsoft.AspNetCore.Authorization;
using SlugEnt.DocumentServer.Core;


namespace SlugEnt.DocumentServer.API.Security;

public class NodeKeyHandler : AuthorizationHandler<NodeKeyRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INodeKeyValidation   _nodeKeyValidation;


    public NodeKeyHandler(IHttpContextAccessor httpContextAccessor,
                          INodeKeyValidation nodeKeyValidation)
    {
        _httpContextAccessor = httpContextAccessor;
        _nodeKeyValidation   = nodeKeyValidation;
    }


    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NodeKeyRequirement requirement)
    {
        string nodeKey = _httpContextAccessor?.HttpContext?.Request.Headers[ApiConstants.NodeKeyHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (!_nodeKeyValidation.IsValidNodeKey(nodeKey))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}