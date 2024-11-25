using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Silo.MongoDB;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Statistics;
using Serilog;

namespace NFTMarketServer.Silo.Extensions;

public static class OrleansHostExtensions
{
     public static IHostBuilder UseOrleansSnapshot(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            //Configure OrleansSnapshot
            var configSection = context.Configuration.GetSection("Orleans");
            var IsRunningInKubernetes = configSection.GetValue<bool>("IsRunningInKubernetes");
            var advertisedIP = IsRunningInKubernetes ?  Environment.GetEnvironmentVariable("POD_IP") :configSection.GetValue<string>("AdvertisedIP");
            var clusterId = IsRunningInKubernetes ? Environment.GetEnvironmentVariable("ORLEANS_CLUSTER_ID") : configSection.GetValue<string>("ClusterId");
            var serviceId = IsRunningInKubernetes ? Environment.GetEnvironmentVariable("ORLEANS_SERVICE_ID") : configSection.GetValue<string>("ServiceId");

            siloBuilder
                .ConfigureEndpoints(advertisedIP: IPAddress.Parse(advertisedIP),siloPort: configSection.GetValue<int>("SiloPort"), gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<GrainCollectionNameOptions>(options =>
                {
                    /*var collectionName = configSection.GetSection(nameof(GrainCollectionNameOptions.GrainSpecificCollectionName)).GetChildren();
                    options.GrainSpecificCollectionName = collectionName.ToDictionary(o => o.Key, o => o.Value);*/
                    options.GrainSpecificCollectionName = ForestMongoCollectionConstants.GrainSpecificCollectionName;
                })
                /*.AddMongoDBGrainStorage("Default",(MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = configSection.GetValue<string>("DataBase");
                
                    /*op.ConfigureJsonSerializerSettings = jsonSettings =>
                    {
                        // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                        jsonSettings.NullValueHandling = NullValueHandling.Include;
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                        jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    };#1#
                    
                })*/
                .AddForestMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = configSection.GetValue<string>("DataBase");

                    /*var grainIdPrefix = configSection
                        .GetSection("GrainSpecificIdPrefix").GetChildren().ToDictionary(o => o.Key.ToLower(), o => o.Value);*/
                    var grainIdPrefix = ForestMongoGrainIdConstants.GrainSpecificIdPrefix;
                    foreach (var kv in grainIdPrefix)
                    {
                        Log.Information($"GrainSpecificIdPrefix, key: {kv.Key}, Value: {kv.Value}");
                    }

                    op.KeyGenerator = id =>
                    {
                        var grainType = id.Type.ToString();
                        if (grainIdPrefix.TryGetValue(grainType, out var prefix))
                        {
                            return $"{prefix}+{id.Key}";
                        }

                        return id.ToString();
                    };
                    op.CreateShardKeyForCosmos = configSection.GetValue<bool>("CreateShardKeyForMongoDB", false);
                })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.CreateShardKeyForCosmos = false;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterId;
                    options.ServiceId = serviceId;
                })
                .ConfigureServices(services =>
                    services.AddSingleton<IGrainStateSerializer, ForestJsonGrainStateSerializer>())
                 .AddMemoryGrainStorage("PubSubStore")
                //.ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(options =>
                {
                    options.Username = configSection.GetValue<string>("DashboardUserName");
                    options.Password = configSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = configSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                })
                // .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        });
    }
}