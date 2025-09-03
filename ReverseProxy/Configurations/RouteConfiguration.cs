using Shared.Common.Utils.Const;
using Yarp.ReverseProxy.Configuration;

namespace ReverseProxy.Configurations;

public static class RouteConfiguration
{
    public static IReadOnlyList<RouteConfig> GetRoutes()
    {
        return new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "authServiceRoute",
                ClusterId = ConstReverseProxy.AuthServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/auth/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string,string> { { "PathRemovePrefix", "/auth" } }
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", "Anonymous" },
                    { "Exceptions", "" }
                }
            },
            
            new RouteConfig
            {
                RouteId = "userServiceRoute",
                ClusterId = ConstReverseProxy.UserServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/user/{**catch-all}"
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "PathRemovePrefix", "/user" } }
                ],
                Metadata = new Dictionary<string,string>
                {
                    { "AllowedRoles", "Admin,User" },
                    { "Exceptions", "GET:/public-profile,POST:/notify" }
                }
            },
            
            new RouteConfig
            {
                RouteId = "utilityServiceRoute",
                ClusterId = ConstReverseProxy.UtilityServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/utility/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "PathRemovePrefix", "/utility" } }
                ]
            },
            
            new RouteConfig
            {
                RouteId = "studentServiceRoute",
                ClusterId = ConstReverseProxy.StudentServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/student/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "PathRemovePrefix", "/student" } }
                ]
            }
        };
    }
}