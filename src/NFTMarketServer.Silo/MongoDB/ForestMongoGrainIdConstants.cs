namespace NFTMarketServer.Silo.MongoDB;

public class ForestMongoGrainIdConstants
{
    public static readonly Dictionary<string, string> GrainSpecificIdPrefix = new Dictionary<string, string>()
    {
        {"ContractServiceGraphQL","GrainReference=00000000000000000000000000000000060000007b0691e0"},
        //{"ContractInvoke","GrainReference=a29db13d603fa7b16c171f67e052ae9403ffffffc9d19f6b"},
        {"SymbolIcon","GrainReference=0000000000000000000000000000000006ffffffb5a4dc5e"},
        {"InscriptionAmount","GrainReference=000000000000000000000000000000000600000068d4baf5"},
        {"InscriptionItemCrossChain","GrainReference=0000000000000000000000000000000006ffffffc31194a9"},
        {"CreatePlatformNFT","GrainReference=0000000000000000000000000000000006ffffffcc77546a"},
        {"NftCollectionExtension","GrainReference=00000000000000000000000000000000060000000c3ae72f"},
        {"NftInfoExtension","GrainReference=0000000000000000000000000000000006ffffffbb5fab22"},
        {"PlatformNFTTokenId","GrainReference=0000000000000000000000000000000006ffffff80495c7a"},
        {"SynchronizeTxJob","GrainReference=0000000000000000000000000000000006ffffffad61444b"},
        {"TreeUserActivityRecord","GrainReference=0000000000000000000000000000000006ffffffb6474e1a"},
        {"TreeUserInfo","GrainReference=00000000000000000000000000000000060000001521c856"},
        {"TreeUserPointsDetail","GrainReference=0000000000000000000000000000000006ffffff8d2c1ace"},
       
        // {"User",""},//guid
        // {"InscriptionInscribe","GrainReference=449fda8bab1c2a824c896d2425a6348203ffffffc15159ed"},//guid
        
        //discard
        //{"OwnerShipVerifyOrder",""}
        //{"NFTOrder",""},

    };

}