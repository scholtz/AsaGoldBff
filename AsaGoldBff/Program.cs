
using AlgorandAuthentication;
using AsaGoldBff.Controllers.Email;
using AsaGoldBff.UseCase;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System.Net;
using System.Reflection;

namespace AsaGoldBff
{
    public class Program
    {
        public static HashSet<string> Admins = new HashSet<string>();
        public static void Main(string[] args)
        {
            var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
            logger.Debug("init main");

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseNLog();

            // Add services to the container.
            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressMapClientErrors = true;
                });
            builder.Services.AddProblemDetails(options =>
                options.CustomizeProblemDetails = ctx =>
                        ctx.ProblemDetails.Extensions.Add("nodeId", Environment.MachineName));


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(
                c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Asa Gold BFF API",
                        Version = "v1",
                        Description = File.ReadAllText("doc/readme.md")
                    });
                    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Description = "ARC-0014 Algorand authentication transaction",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                    });

                    c.OperationFilter<Swashbuckle.AspNetCore.Filters.SecurityRequirementsOperationFilter>();
                    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                    //c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"doc/documentation.xml"));
                }
                );

            builder.Services.Configure<Model.Config.BFFOptions>(builder.Configuration.GetSection("BFF"));
            builder.Services.Configure<AlgorandAuthenticationOptions>(builder.Configuration.GetSection("AlgorandAuthentication"));
            var algorandAuthenticationOptions = new AlgorandAuthenticationOptions();
            builder.Configuration.GetSection("AlgorandAuthentication").Bind(algorandAuthenticationOptions);

            builder.Services
             .AddAuthentication(AlgorandAuthenticationHandler.ID)
             .AddAlgorand(o =>
             {
                 o.CheckExpiration = algorandAuthenticationOptions.CheckExpiration;
                 o.Debug = algorandAuthenticationOptions.Debug;
                 o.AlgodServer = algorandAuthenticationOptions.AlgodServer;
                 o.AlgodServerToken = algorandAuthenticationOptions.AlgodServerToken;
                 o.AlgodServerHeader = algorandAuthenticationOptions.AlgodServerHeader;
                 o.Realm = algorandAuthenticationOptions.Realm;
                 o.NetworkGenesisHash = algorandAuthenticationOptions.NetworkGenesisHash;
                 o.MsPerBlock = algorandAuthenticationOptions.MsPerBlock;
                 o.EmptySuccessOnFailure = algorandAuthenticationOptions.EmptySuccessOnFailure;
                 o.EmptySuccessOnFailure = algorandAuthenticationOptions.EmptySuccessOnFailure;
             });

            builder.Services.AddSingleton<EmailValidationUseCase>();
            builder.Services.AddSingleton<AccountUseCase>();
            builder.Services.AddMemoryCache();
            builder.Services.AddResponseCaching();

            var corsConfig = builder.Configuration.GetSection("Cors").AsEnumerable().Select(k => k.Value).Where(k => !string.IsNullOrEmpty(k)).ToArray();
            if (corsConfig?.Any() != true) throw new Exception("Cors not setup");
            logger.Info($"Cors: {string.Join(",", corsConfig)}");
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                builder =>
                {
                    builder.WithOrigins(corsConfig)
                                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                                        .AllowAnyMethod()
                                        .AllowAnyHeader()
                                        .AllowCredentials();
                });
            });

            var admins = builder.Configuration.GetSection("Admins").AsEnumerable().Select(k => k.Value).Where(k => !string.IsNullOrEmpty(k)).Select(k => k.ToString()).ToHashSet();
            if (admins?.Any() == true)
            {
                Admins = admins;
                logger.Info($"Admins: {string.Join(",", Admins)}");
            }

            #region Email
            var emailConfigured = false;
            if (builder.Configuration.GetSection("MailGun").Exists())
            {
                var config = builder.Configuration.GetSection("MailGun")?.Get<Model.Settings.MailGunConfiguration>();
                if (!string.IsNullOrEmpty(config?.ApiKey))
                {
                    logger.Info("MailGun configured");
                    Console.WriteLine("MailGun configured");
                    emailConfigured = true;
                    builder.Services.Configure<Model.Settings.MailGunConfiguration>(builder.Configuration.GetSection("MailGun"));
                    builder.Services.AddSingleton<IEmailSender, Controllers.Email.MailGunSender>();
                }
            }
            if (!emailConfigured && builder.Configuration.GetSection("Azure").Exists())
            {
                var config = builder.Configuration.GetSection("Azure")?.Get<Model.Settings.AzureConfiguration>();
                if (!string.IsNullOrEmpty(config?.EmailConnectionString))
                {
                    logger.Info("Azure email configured");
                    Console.WriteLine("Azure email configured");

                    emailConfigured = true;
                    builder.Services.Configure<Model.Settings.AzureConfiguration>(builder.Configuration.GetSection("Azure"));
                    builder.Services.AddSingleton<IEmailSender, Controllers.Email.AzureEmailSender>();
                }
            }

            if (!emailConfigured && builder.Configuration.GetSection("SendGrid").Exists())
            {
                var config = builder.Configuration.GetSection("SendGrid")?.Get<Model.Settings.SendGridConfiguration>();
                if (!string.IsNullOrEmpty(config?.MailerApiKey))
                {
                    logger.Info("SendGridEmail configured");
                    Console.WriteLine("SendGridEmail configured");

                    emailConfigured = true;
                    builder.Services.Configure<Model.Settings.SendGridConfiguration>(builder.Configuration.GetSection("SendGrid"));
                    builder.Services.AddSingleton<IEmailSender, Controllers.Email.SendGridController>();
                }
            }

            if (!emailConfigured && builder.Configuration.GetSection("RabbitMQEmail").Exists())
            {
                var config = builder.Configuration.GetSection("RabbitMQEmail")?.Get<Model.Settings.RabbitMQEmailQueueConfiguration>();
                if (!string.IsNullOrEmpty(config?.HostName))
                {
                    logger.Info("RabbitMQEmail configured " + JsonConvert.SerializeObject(config));
                    Console.WriteLine("RabbitMQEmail configured " + JsonConvert.SerializeObject(config));

                    emailConfigured = true;
                    builder.Services.Configure<Model.Settings.RabbitMQEmailQueueConfiguration>(builder.Configuration.GetSection("RabbitMQEmail"));
                    builder.Services.AddSingleton<IEmailSender, Controllers.Email.RabbitMQEmailSender>();
                }
            }

            if (!emailConfigured)
            {
                logger.Info("NoEmailSender configured");
                Console.WriteLine("NoEmailSender configured");
                builder.Services.AddSingleton<IEmailSender, Controllers.Email.NoEmailSender>();
            }
            #endregion

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseStatusCodePages();
            app.UseResponseCaching();

            app.UseExceptionHandler(exceptionHandlerApp
                => exceptionHandlerApp.Run(async context
                    =>
                {
                    var exception = context.Features.Get<IExceptionHandlerPathFeature>().Error;
                    await Results.Problem(exception.Message, null, 400, exception.Message).ExecuteAsync(context);
                }));
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();


            logger.Info("Initializing singletons");
            _ = app.Services.GetService<EmailValidationUseCase>();
            _ = app.Services.GetService<AccountUseCase>();

            logger.Info("Starting app");

            app.Run();
        }
    }
}