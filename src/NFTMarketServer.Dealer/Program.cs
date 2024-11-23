using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler.ABP;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NFTMarketServer.Dealer;
using Serilog;
using Serilog.Events;

namespace NFTMarketServer.ContractEventHandler
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
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
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting NFTMarketServer.Dealer.");
                var builder = WebApplication.CreateBuilder(args);
                builder.Configuration.AddJsonFile("apollo.appsettings.json");
                builder.Host.AddAppSettingsSecretsJson()
                    .UseAutofac()
                    //.UseAElfExceptionHandler()
                    .UseOrleansClient()
                    .UseApollo() 
                    .UseSerilog();
                await builder.AddApplicationAsync<NFTMarketServerDealerModule>();
                var app = builder.Build();
               // CreateHostBuilder(args).Build().Run();
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
                //.UseAutofac()
                //.ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseSerilog();
        }
    }
}