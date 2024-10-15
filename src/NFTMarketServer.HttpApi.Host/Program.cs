using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using NFTMarketServer.Host;
namespace NFTMarketServer
{
    public class Program
    {
        public async static Task<int> Main(string[] args)
        {
            System.Threading.ThreadPool.SetMinThreads(300, 300);
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
#if DEBUG
                .WriteTo.Async(c => c.Console())
#endif
                .CreateLogger();

            try
            {
                Log.Information("Starting NFTMarketServer.HttpApi.Host.");

                var builder = WebApplication.CreateBuilder(args);
                //configure apollo
                builder.Configuration.AddJsonFile("apollo.appsettings.json");
                builder.Host.AddAppSettingsSecretsJson()
                    .UseAutofac()
                    .UseApollo()
                    .UseOrleansClient()
                    .UseSerilog();
                await builder.AddApplicationAsync<NFTMarketServerHttpApiHostModule>();
                var app = builder.Build();
                //CreateHostBuilder(args).Build().Run();
                await app.InitializeApplicationAsync();
                await app.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .UseOrleansClient()
                .UseAutofac()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseSerilog();
        }
    }
    
}