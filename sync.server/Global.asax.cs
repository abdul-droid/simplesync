using sync.server.Configuration;
using System.Web;
using System.Web.Http;

namespace sync.server
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
