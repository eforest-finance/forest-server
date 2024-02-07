using System.Collections.Concurrent;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using NFTMarketServer.Provider;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Common
{
    public class GraphQLClientFactory : IGraphQLClientFactory, ISingletonDependency
    {
        private readonly GraphQLOptions _graphQlClientOptions;
        private readonly ConcurrentDictionary<string, IGraphQLClient> _clientDic;
        private static readonly object lockObject = new object();
        public GraphQLClientFactory(IOptionsSnapshot<GraphQLOptions> graphQlClientOptions)
        {
            _graphQlClientOptions = graphQlClientOptions.Value;
            _clientDic = new ConcurrentDictionary<string, IGraphQLClient>();
        }

        public IGraphQLClient GetClient(GraphQLClientEnum clientEnum)
        {
            var clientName = clientEnum.ToString();
            
            if (_clientDic.TryGetValue(clientName, out var client))
            {
                return client;
            }

            lock (lockObject)
            {
                if (!_clientDic.TryGetValue(clientName, out client))
                {
                    client = new GraphQLHttpClient(_graphQlClientOptions.Configuration,
                        new NewtonsoftJsonSerializer());
                    switch (clientEnum)
                    {
                        case GraphQLClientEnum.InscriptionClient:
                            client = new GraphQLHttpClient(_graphQlClientOptions.InscriptionConfiguration,
                                new NewtonsoftJsonSerializer());
                            break;
                        case GraphQLClientEnum.DropClient:
                            client = new GraphQLHttpClient(_graphQlClientOptions.DropConfiguration,
                                new NewtonsoftJsonSerializer());
                            break;
                    }
                   
                    _clientDic[clientName] = client;
                }
            }
            return client;
        }
    }
}