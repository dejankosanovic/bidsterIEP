using Owin;
using Microsoft.Owin;
[assembly: OwinStartup(typeof(bidster.Startup))]
namespace bidster
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}