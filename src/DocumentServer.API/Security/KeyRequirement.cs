﻿using Microsoft.AspNetCore.Authorization;

namespace SlugEnt.DocumentServer.API.Security
{
    public class ApiKeyRequirement : IAuthorizationRequirement { }

    public class NodeKeyRequirement : IAuthorizationRequirement { }
}