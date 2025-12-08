using GraphQL.Client.Abstractions;

namespace NFTMarketServer.Common;

public interface IGraphQLClientFactory
{
    IGraphQLClient GetClient(GraphQLClientEnum clientEnum);
}