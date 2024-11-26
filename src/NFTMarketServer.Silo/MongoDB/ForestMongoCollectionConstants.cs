namespace NFTMarketServer.Silo.MongoDB;

public class ForestMongoCollectionConstants
{
    public static readonly Dictionary<string, string> GrainSpecificCollectionName = new Dictionary<string, string>()
    {
        {"NFTMarketServer.Grains.State.ApplicationHandler.GraphQlState", "ContractServiceGraphQLGrain"},
        {"NFTMarketServer.Grains.State.Dealer.ContractInvokeState", "ContractInvokeGrain"},
        {"NFTMarketServer.Grains.State.Icon.SymbolIconGrainState", "SymbolIconGrain"},
        {"NFTMarketServer.Grains.State.Inscription.InscriptionAmountState", "InscriptionAmountGrain"},
        //{"NFTMarketServer.Grains.State.Inscription.InscriptionInscribeState", "InscriptionInscribeGrain"},//guid 1
        {"NFTMarketServer.Grains.State.Inscription.InscriptionItemCrossChainState", "InscriptionItemCrossChainGrain"},
        {"NFTMarketServer.Grains.Grain.Users.CreatePlatformNFTState", "CreatePlatformNFTGrain"},
        {"NFTMarketServer.Grains.State.NFTInfo.NftCollectionExtensionState", "NftCollectionExtensionGrain"},
        {"NFTMarketServer.Grains.State.NFTInfo.NftInfoExtensionState", "NftInfoExtensionGrain"},
        {"NFTMarketServer.Grains.Grain.Users.PlatformNFTTokenIdState", "PlatformNFTTokenIdGrain"},
        {"NFTMarketServer.Grains.State.Synchronize.SynchronizeState", "SynchronizeTxJobGrain"},
        {"NFTMarketServer.Grains.State.NFTInfo.TreeUserActivityRecordState", "TreeUserActivityRecordGrain"},
        {"NFTMarketServer.Grains.State.NFTInfo.TreeUserInfoState", "TreeUserInfoGrain"},
        {"NFTMarketServer.Grains.State.NFTInfo.TreeUserPointsDetailState", "TreeUserPointsDetailGrain"},
        {"NFTMarketServer.Grains.State.Order.NFTOrderState", "NFTOrderGrain"},

        //not find code
        //{"", "SynchronizeTransactionJobGrain"},
        //{"", "VersionStoreGrain"},//guid 1

        //discard
        {"NFTMarketServer.Grains.State.Verify.OwnerShipVerifyOrderState", "OwnerShipVerifyOrderGrain"},
        {"NFTMarketServer.Grains.State.Order.NFTOrderState", "NFTOrderGrain"},

    };
}