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
                }
            },
            new RouteConfig
            {
                RouteId = "userServiceRoute",
                ClusterId = ConstReverseProxy.UserServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/user/{**catch-all}"
                }
            }
        };
    }
}