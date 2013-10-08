using Owin;
using System.Web.Http;

namespace AzureCrawlerRole
{
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                "Default",
                "api/{controller}/{id}",
                defaults : new { id = RouteParameter.Optional });

            app.UseWebApi(config);
        }
    }
}
