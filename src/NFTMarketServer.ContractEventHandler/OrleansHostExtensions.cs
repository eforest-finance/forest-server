using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Serialization;
using Serilog;

namespace NFTMarketServer.ContractEventHandler;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansClient(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var configSection = context.Configuration.GetSection("Orleans");
            try
            {
                Log.Information("Contract start OrleansConfigSection:{config}",JsonConvert.SerializeObject(configSection));
                var database = configSection.GetValue<string>("DataBase");
                Log.Information("Contract start database:{config}",JsonConvert.SerializeObject(database));

            }
            catch (Exception e)
            {
                Log.Error(e, "Auth start Error OrleansConfigSection:{config} ,error:{error}",JsonConvert.SerializeObject(configSection),e.Message);
            }
            if (configSection == null)
                throw new ArgumentNullException(nameof(configSection), "The Orleans config node is missing");
            clientBuilder.UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .Configure<ExceptionSerializationOptions>(options=>
                {
                    options.SupportedNamespacePrefixes.Add("Volo.Abp");
                    options.SupportedNamespacePrefixes.Add("Newtonsoft.Json");
                });
        });
    }
    
}