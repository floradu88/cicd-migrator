using System.Web.Http;

namespace FileDownloadApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            // Note: Large file download support (>512MB) is configured in Web.config
            // - maxRequestLength and maxAllowedContentLength are set to 2GB
            // - File downloads use streaming to avoid memory issues

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}

