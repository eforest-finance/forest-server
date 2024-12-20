using AutoMapper;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Order.Etos;
using NFTMarketServer.Order.Index;
using NFTMarketServer.Symbol.Etos;
using NFTMarketServer.Symbol.Index;
using NFTMarketServer.Synchronize.Eto;
using NFTMarketServer.ThirdToken.Etos;
using NFTMarketServer.ThirdToken.Index;
using NFTMarketServer.Users.Eto;
using NFTMarketServer.Users.Index;
using SocialMedia = NFTMarketServer.NFT.SocialMedia;

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
        
        CreateMap<SynchronizeTransactionInfoEto, SynchronizeTransactionInfoIndex>();
        CreateMap<SocialMedia, NFT.Index.SocialMedia>();
        CreateMap<NFTDropExtraEto, NFTDropExtensionIndex>
            ().ForMember(destination => destination.Id, 
            opt => opt.MapFrom(source => source.DropId));
        CreateMap<ThirdTokenEto, ThirdTokenIndex>();
        CreateMap<TokenRelationEto, TokenRelationIndex>();
    }
}