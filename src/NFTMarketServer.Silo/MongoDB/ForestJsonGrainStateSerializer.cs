
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Orleans.Serialization;
using Serilog;

namespace NFTMarketServer.Silo.MongoDB;

public class ForestJsonGrainStateSerializer: IGrainStateSerializer
{
    private readonly JsonSerializerSettings jsonSettings;

    public ForestJsonGrainStateSerializer(IOptions<JsonGrainStateSerializerOptions> options, IServiceProvider serviceProvider)
    {
        jsonSettings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(serviceProvider);
        options.Value.ConfigureJsonSerializerSettings(jsonSettings);
    }

    public T Deserialize<T>(BsonValue value)
    {
        using var jsonReader = new JTokenReader(value.ToJToken());
        var localSerializer = JsonSerializer.CreateDefault(jsonSettings);
        return localSerializer.Deserialize<T>(jsonReader);
    }

    public BsonValue Serialize<T>(T state)
    {
        var localSerializer = JsonSerializer.CreateDefault(jsonSettings);
        return JObject.FromObject(state, localSerializer).ToBson();
    }
}