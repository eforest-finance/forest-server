using AutoMapper;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Order.Etos;
using NFTMarketServer.Order.Index;
using NFTMarketServer.Symbol.Etos;
using NFTMarketServer.Symbol.Index;
using NFTMarketServer.Synchronize.Eto;
using NFTMarketServer.Users.Eto;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTMarketServerEventHandlerAutoMapperProfile : Profile
{
    public NFTMarketServerEventHandlerAutoMapperProfile()
    {
        CreateMap<UserInformationEto, UserIndex>();
        CreateMap<NFTInfoExtraEto, NFTInfoExtensionIndex>();
        CreateMap<NFTCollectionExtraEto, NFTCollectionExtensionIndex>();
        CreateMap<OwnerShipVerifyOrderEto, OwnerShipVerifyOrderIndex>();
        CreateMap<NFTOrderEto, NFTOrderIndex>();
        // synchronization
        CreateMap<SynchronizeTransactionInfoEto, SynchronizeTransactionInfoIndex>();
        
        
        CreateMap<UserInformationEto, UserIndex>();
        CreateMap<SynchronizeTransactionInfoEto, SynchronizeTransactionInfoIndex>();
    }
}