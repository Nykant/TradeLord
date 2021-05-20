using KiteConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Server.DataHelpers;
using TradeMaster6000.Server.Helpers;
using TradeMaster6000.Server.Hubs;
using TradeMaster6000.Server.Models;
using TradeMaster6000.Server.Services;
using TradeMaster6000.Server.Tasks;

namespace TradeMaster6000.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            Environment = webHostEnvironment;
        }

        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            string keyConnection = Configuration.GetConnectionString("KeyConnection");
            string tradeConnection = Configuration.GetConnectionString("TradeConnection");

            services.AddDbContext<MyKeysContext>(options =>
                options.UseMySql(keyConnection, ServerVersion.AutoDetect(keyConnection)));

            if (Environment.IsDevelopment())
            {
                services.AddDataProtection()
                    .PersistKeysToDbContext<MyKeysContext>()
                    .SetApplicationName("TradeMaster6000")
                    .ProtectKeysWithCertificate(new X509Certificate2("certificate.pfx", Configuration["Thumbprint"]));
            }
            else
            {
                services.AddDataProtection()
                    .PersistKeysToDbContext<MyKeysContext>()
                    .SetApplicationName("TradeMaster6000")
                    .ProtectKeysWithCertificate(new X509Certificate2("/etc/ssl/letsencrypt/certificate.pfx", Configuration["Thumbprint"]));
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            services.AddDbContextFactory<TradeDbContext>(options =>
                options.UseMySql(tradeConnection, ServerVersion.AutoDetect(tradeConnection)));

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var builder = services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            services.AddAuthentication()
                .AddIdentityServerJwt();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(10);
                options.Cookie.Name = "SessionCookie";
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            //-------------------
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<IRunningOrderService, RunningOrderService>();
            services.TryAddSingleton<IInstrumentService, InstrumentService>();
            services.TryAddSingleton<ITradeOrderHelper, TradeOrderHelper>();
            services.TryAddSingleton<ITradeLogHelper, TradeLogHelper>();
            services.TryAddSingleton<IKiteService, KiteService>();
            //-------------------
            services.TryAddSingleton<IInstrumentHelper, InstrumentHelper>();
            services.TryAddSingleton<ITradeHelper, TradeHelper>();
            services.TryAddSingleton<ITickerService, TickerService>();
            services.TryAddSingleton<ITimeHelper, TimeHelper>();
            services.TryAddSingleton<ITargetHelper, TargetHelper>();
            //-------------------
            services.TryAddSingleton<ISLMHelper, SLMHelper>();
            services.TryAddSingleton<IWatchingTargetHelper, WatchingTargetHelper>();
            //-------------------

            services.AddSignalR();
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<OrderHub>("/orderhub");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
