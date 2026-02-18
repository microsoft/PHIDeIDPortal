using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.FeatureManagement;
using PhiDeidPortal.Ui.Services;
using System.Text.Json;
using PhiDeidPortal.Ui.Hubs;
using Microsoft.Extensions.Caching.Cosmos;

namespace PhiDeidPortal.Ui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.ConfigureKestrel(options =>
            {
                var configuration = builder.Configuration.GetSection("Kestrel");
                var maxRequestBodySize = configuration["MaxRequestBodySizeinMB"];
                maxRequestBodySize ??= "100";
                var size = int.Parse(maxRequestBodySize);
                options.Limits.MaxRequestBodySize = size * 1024 * 1024;
            });

            builder.Services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.WriteIndented = true;
                });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.HandleSameSiteCookieCompatibility();
            });

            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    var configuration = builder.Configuration.GetSection("AzureAd");
                    options.Instance = configuration["Instance"];
                    options.Domain = configuration["Domain"];
                    options.TenantId = configuration["TenantId"];
                    options.ClientId = configuration["ClientId"];
                    options.ClientSecret = configuration["ClientSecret"];
                    options.CallbackPath = configuration["CallbackPath"];
                });

            builder.Services.AddControllersWithViews(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                }).AddMicrosoftIdentityUI();

            builder.Services.AddTransient<IFeatureService, FeatureService>();
            builder.Services.AddSingleton<IBlobService, BlobService>(x =>
            {
                var blobService = new BlobService(builder.Configuration);
                return blobService;
            });

            builder.Services.AddSingleton<IAISearchService, AISearchService>(x =>
            {
                var indexQueryer = new AISearchService(builder.Configuration);
                return indexQueryer;
            });

            builder.Services.AddSingleton<Services.IAuthorizationService, AuthorizationService>(x =>
            {
                var authorizationService = new AuthorizationService(builder.Configuration);
                return authorizationService;
            });

            builder.Services.AddSingleton<IUserContextService, UserContextService>();

            SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler();
            socketsHttpHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(5);

            var connectionModeConfig = builder.Configuration.GetSection("CosmosDb")["ConnectionMode"];
            ConnectionMode connectionMode = ConnectionMode.Gateway;
            if (null != connectionModeConfig)
            {
                connectionMode = (ConnectionMode)Enum.Parse(typeof(ConnectionMode), connectionModeConfig, ignoreCase: true);
            }

            CosmosClientOptions cosmosClientOptions = new CosmosClientOptions()
            {
                ConnectionMode = connectionMode,
                HttpClientFactory = () => new HttpClient(socketsHttpHandler, disposeHandler: false)
            };

            var cosmosConnectionString = builder.Configuration.GetConnectionString("Cosmos");
            var cosmosEndpoint = builder.Configuration.GetSection("CosmosDb")["Endpoint"];
            var cosmosUseEntraAuth = bool.Parse(builder.Configuration.GetSection("CosmosDb")["UseEntraAuth"] ?? "false");
            
            var cosmosClient = cosmosUseEntraAuth ?     new CosmosClient(cosmosEndpoint, new DefaultAzureCredential(), cosmosClientOptions) :
                                                        new CosmosClient(cosmosConnectionString, cosmosClientOptions);

            builder.Services.AddSingleton(x =>
            {
                return cosmosClient;
            });

            builder.Services.AddSingleton<ICosmosService, CosmosService>(x =>
            {
                return new CosmosService(cosmosClient, builder.Configuration);
            });

            builder.Services.AddCosmosCache((CosmosCacheOptions cacheOptions) =>
            {
                cacheOptions.DatabaseName = builder.Configuration.GetSection("CosmosDb")["CacheProviderDatabaseId"];
                cacheOptions.ContainerName = builder.Configuration.GetSection("CosmosDb")["CacheProviderContainerId"];
                cacheOptions.CosmosClient = cosmosClient;
                cacheOptions.CreateIfNotExists = true;
            });

            builder.Services.AddSingleton<ICacheService, CosmosCacheService>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddFeatureManagement().AddFeatureFilter<Filters.EnvironmentFeatureFilter>();

            builder.Services.AddRazorPages();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();            

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<CosmosDocuments>("/cosmosdocuments");
                endpoints.MapControllers();
            });

            app.MapRazorPages();

            app.Run();
        }
    }
}
