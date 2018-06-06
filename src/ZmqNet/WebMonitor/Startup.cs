using Agebull.ZeroNet.Core;
using Agebull.ZeroNet.ZeroApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebMonitor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ApiContext.SetConfiguration(configuration);
            ZeroApplication.AppName = "ZeroMonitor";
            ZeroApplication.Initialize();
            ZeroApplication.Discove();
            ZeroApplication.Run();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            app.Map("/ws", WebNotify.Map);
            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}