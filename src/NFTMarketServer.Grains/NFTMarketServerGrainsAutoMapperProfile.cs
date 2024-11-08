using AutoMapper;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Grains.Grain.Icon;
using NFTMarketServer.Grains.Grain.Inscription;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Grains.Grain.Order;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Grains.Grain.Verify;
using NFTMarketServer.Grains.State.Dealer;
using NFTMarketServer.Grains.State.Icon;
using NFTMarketServer.Grains.State.Inscription;
using NFTMarketServer.Grains.State.NFTInfo;
using NFTMarketServer.Grains.State.Order;
using NFTMarketServer.Grains.State.Synchronize;
using NFTMarketServer.Grains.State.Users;
using NFTMarketServer.Grains.State.Verify;
using NFTMarketServer.TreeGame;

namespace NFTMarketServer.Grains;

public class NFTMarketServerGrainsAutoMapperProfile : Profile
{
    public NFTMarketServerGrainsAutoMapperProfile()
    {
        // User AutoMap
        CreateMap<UserGrainDto, UserState>();
        CreateMap<UserState, UserGrainDto>();
        CreateMap<PlatformNFTTokenIdGrainDto, PlatformNFTTokenIdState>();
        CreateMap<PlatformNFTTokenIdState, PlatformNFTTokenIdGrainDto>();
        CreateMap<CreatePlatformNFTState, CreatePlatformNFTGrainDto>();
        CreateMap<CreatePlatformNFTGrainDto, CreatePlatformNFTState>();
        CreateMap<NftInfoExtensionState, NftInfoExtensionGrainDto>();
        CreateMap<NftInfoExtensionGrainDto, NftInfoExtensionState>();
        // verify
        CreateMap<OwnerShipVerifyOrderState, OwnerShipVerifyOrderGrainDto>().ReverseMap();
        // Synchronize
        CreateMap<CreateSynchronizeTransactionJobGrainDto, SynchronizeState>();
        CreateMap<SynchronizeState, SynchronizeTxJobGrainDto>();
        CreateMap<SynchronizeTxJobGrainDto, SynchronizeState>();
        CreateMap<CreateSeedJobGrainDto, SynchronizeState>();


        CreateMap<NftCollectionExtensionState, NftCollectionExtensionGrainDto>();
        CreateMap<NftCollectionExtensionGrainDto, NftCollectionExtensionState>();
        CreateMap<SymbolIconGrainState, SymbolIconGrainDto>().ReverseMap();
        
        //order
        CreateMap<NFTOrderGrainDto, NFTOrderState>().ReverseMap();
        
        CreateMap<ContractInvokeGrainDto, ContractInvokeState>().ReverseMap();
        CreateMap<InscriptionInscribeState, InscriptionInscribeGrainDto>().ReverseMap();
        CreateMap<InscriptionAmountState, InscriptionAmountGrainDto>().ReverseMap();
        CreateMap<InscriptionItemCrossChainState, InscriptionItemCrossChainGrainDto>().ReverseMap();
        CreateMap<TreeUserInfoState, TreeGameUserInfoDto>();
        CreateMap<TreeGameUserInfoDto, TreeUserInfoState>();
    }
}