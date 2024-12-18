﻿using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler.ABP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace NFTMarketServer.EntityEventHandler
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
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
#if DEBUG
                .WriteTo.Async(c => c.Console())
#endif
                .CreateLogger();

            try
            {
                Log.Information("Starting NFTMarket.EntityEventHandler.");
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
                .ConfigureAppConfiguration(build => { build.AddJsonFile("appsettings.secrets.json", optional: true); })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplication<NFTMarketServerEntityEventHandlerModule>();
                })
                .ConfigureAppConfiguration((h, c) => c.AddJsonFile("apollo.appsettings.json"))
                .UseApollo() 
                .UseAutofac()
                .UseAElfExceptionHandler()
                .UseSerilog()
                .UseOrleansClient();
    }
}