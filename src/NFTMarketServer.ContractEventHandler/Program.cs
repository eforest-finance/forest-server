using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler.ABP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
#if DEBUG
                .WriteTo.Async(c => c.Console())
#endif
                .CreateLogger();

            try
            {
                Log.Information("Starting NFTMarketServer.Dealer.");
                await CreateHostBuilder(args).RunConsoleAsync();
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

        internal static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplication<NFTMarketServerContractEventHandlerModule>();
                })
                .ConfigureAppConfiguration((h, c) => c.AddJsonFile("apollo.appsettings.json"))
                .UseApollo() 
                .UseAutofac()
                .UseAElfExceptionHandler()
                .UseSerilog()
                .UseOrleansClient();
    }
    
}