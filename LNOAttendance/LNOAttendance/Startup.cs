using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(LNOAttendance.Startup))]
namespace LNOAttendance
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
