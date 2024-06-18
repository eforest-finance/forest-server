using System.Collections.Generic;
using System.Linq;
using AElf;
using AutoMapper;
using NFTMarketServer.Activity;
using NFTMarketServer.Activity.Index;
using NFTMarketServer.Ai;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.Basic;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.BId.Index;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Index;
using NFTMarketServer.Entities;
using NFTMarketServer.Grains.Grain.Inscription;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Grains.Grain.Order;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Grains.Grain.Verify;
using NFTMarketServer.Grains.State.Dealer;
using NFTMarketServer.Grains.State.Inscription;
using NFTMarketServer.Inscription;
using NFTMarketServer.Market;
using NFTMarketServer.Message;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Dto;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Order;
using NFTMarketServer.Order.Dto;
using NFTMarketServer.Order.Etos;
using NFTMarketServer.Order.Handler;
using NFTMarketServer.Order.Index;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.OwnerShip.Dto;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Symbol;
using NFTMarketServer.Symbol.Etos;
using NFTMarketServer.Symbol.Index;
using NFTMarketServer.SymbolMarketToken;
using NFTMarketServer.SymbolMarketToken.Index;
using NFTMarketServer.Synchronize.Dto;
using NFTMarketServer.Synchronize.Eto;
using NFTMarketServer.Tokens;
using NFTMarketServer.Trait;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Eto;
using NFTMarketServer.Users.Index;
using Volo.Abp.AutoMapper;
using ExternalInfoDictionary = NFTMarketServer.Entities.ExternalInfoDictionary;

namespace NFTMarketServer;

public class NFTMarketServerApplicationAutoMapperProfile : Profile
{
    public NFTMarketServerApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        CreateMap<IndexerNFTInfo, HotNFTInfoDto>()
            .ForMember(des => des.CollectionImage, opt =>
                opt.MapFrom(source => source.ImageUrl
                ))
            .ForMember(des => des.OfferPrice, opt =>
                opt.MapFrom(source => source.MaxOfferPrice
                ))
            .ForMember(des => des.PreviewImage, opt =>
                opt.MapFrom(source => source.PreviewImage
                ))
            .ForMember(des => des.NFTId, opt =>
                opt.MapFrom(source => source.Id
                ))
            .ForMember(des => des.NFTName, opt =>
                opt.MapFrom(source => source.TokenName
                ))
            .ForMember(des => des.NFTSymbol, opt =>
                opt.MapFrom(source => source.Symbol
                ));
        CreateMap<NFTActivityItem, NFTMessageActivityDto>();
        CreateMap<IndexerNFTBriefInfo, CompositeNFTInfoIndexDto>();
        CreateMap<NFTActivityDto, CollectionActivitiesDto>();
        CreateMap<NFTInfoIndex, NFTInfoNewIndex>();
        CreateMap<AiCreateIndex, AiArtFailDto>().ForMember(des => des.AiPaintingStyleType,
                opt => opt.MapFrom(source => source.PaintingStyle.ToEnumString()))
            .ForMember(des => des.NegativePrompt, opt => opt.MapFrom(source => source.NegativePrompt))
            .ForMember(des => des.Number, opt => opt.MapFrom(source => source.Number))
            .ForMember(des => des.Prompt, opt => opt.MapFrom(source => source.Promt))
            .ForMember(des => des.Quality, opt => opt.MapFrom(source => source.Quality.ToEnumString()))
            .ForMember(des => des.TransactionId, opt => opt.MapFrom(source => source.TransactionId))
            .ForMember(des => des.Size, opt => opt.MapFrom(source => source.Size.ToEnumString()));
        CreateMap<AttributeDictionary, ExternalInfoDictionary>()
            .ForMember(des => des.Key, opt => opt.MapFrom(source => source.TraitType));
        CreateMap<NFTInfoNewIndex, NFTInfoIndex>();
        CreateMap<IndexerNFTInfo, NFTTraitsInfoDto>().Ignore(o => o.Id);
        CreateMap<IndexerNFTInfo, CollectionActivityBasicDto>()
            .Ignore(o => o.Image)
            .ForMember(des => des.NFTInfoId, opt => opt.MapFrom(source => source.Id))
            .ForMember(des => des.NFTTokenName, opt => opt.MapFrom(source => source.TokenName));
        CreateMap<CollectionActivityBasicDto, CollectionActivitiesDto>()
            .ForMember(des => des.NFTName, opt => opt.MapFrom(source => source.NFTTokenName))
            .ForMember(des => des.PreviewImage, opt => opt.MapFrom(source => source.Image));
        CreateMap<IndexerTokenInfo, TokenInfoIndex>();
        CreateMap<IndexerNFTListingChange, NFTListingChangeEto>();
        CreateMap<IndexerActivity, SymbolMarketActivityDto>();
        CreateMap<NFTInfoExtensionIndex,CompositeNFTInfoIndexDto>();
        CreateMap<IndexerSeedBriefInfo, CompositeNFTInfoIndexDto>();
        CreateMap<IndexerTokenInfo, TokenDto>();
        CreateMap<IndexerSymbolMarketToken, SymbolMarketTokenDto>()
            .ForMember(des => des.TokenImage, opt => opt.MapFrom(source => source.SymbolMarketTokenLogoImage
            ))
            .ForMember(des => des.CurrentSupply, opt => opt.MapFrom(source => source.Issued
            ))
            .ForMember(des => des.IssueChain, opt => opt.MapFrom(source => ChainIdHelper.MaskChainId(source.IssueChainId)
            ))
            .ForMember(des => des.IssueChainId, opt => opt.MapFrom(source => source.IssueChainId))
            .ForMember(des => des.OriginIssueChain, opt => opt.MapFrom(source => ChainHelper.ConvertChainIdToBase58((int)source.IssueChainId)
            ));

        CreateMap<IndexerNFTOffer, NFTOfferDto>()
            .ForMember(des => des.FromAddress, opt => opt.MapFrom(source => source.From))
            .ForMember(des => des.ToAddress, opt => opt.MapFrom(source => source.To))
            .ForMember(des => des.ExpireTime, opt
                => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.ExpireTime)))
            .ForMember(des => des.Quantity, opt => opt.MapFrom(source => source.RealQuantity));

        CreateMap<NFTInfoExtensionIndex, IndexerNFTInfo>();
        CreateMap<IndexerListingWhitelistPrice, NFTInfoIndexDto>()
            .Ignore(o => o.Id)
            .Ignore(o => o.Owner)
            .Ignore(o => o.OwnerCount)
            .Ignore(o => o.WhitelistId)
            .ForMember(des => des.ListingId, opt
                => opt.MapFrom(source => source.ListingId))
            .ForMember(des => des.ListingAddress, opt
                => opt.MapFrom(source => source.Owner));

        CreateMap<UserIndex, UserDto>();
        CreateMap<UserIndex, AccountDto>().ForMember(
            destination => destination.Address,
            opt => opt.MapFrom(source => source.UserName));

        CreateMap<TokenMarketData, TokenMarketDataDto>().ForMember(
            destination => destination.Timestamp,
            opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));

        CreateMap<NFTActivityItem, NFTActivityDto>()
            .Ignore(o => o.From)
            .Ignore(o => o.To)
            .ForMember(destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));

        CreateMap<NFTCollectionExtensionIndex, NftCollectionExtensionGrainDto>();

        // User AutoMap
        CreateMap<UserGrainDto, UserInformationEto>();
        CreateMap<UserUpdateDto, UserGrainDto>();
        CreateMap<UserDto, UserGrainDto>();
        CreateMap<UserGrainDto, UserDto>();

        CreateMap<NftInfoExtensionGrainDto, NFTInfoExtraEto>();
        CreateMap<IndexerNFTCollection, NFTCollectionIndexDto>()
            .ForMember(des => des.IssueChainId, opt
                => opt.MapFrom(source => ChainHelper.ConvertChainIdToBase58(source.IssueChainId)));
        CreateMap<NFTCollectionExtensionIndex, SearchNFTCollectionsDto>()
            .ForMember(des => des.Symbol, opt
                => opt.MapFrom(source => source.NFTSymbol));
        CreateMap<NFTCollectionExtensionIndex, SearchCollectionsFloorPriceDto>()
            .ForMember(des => des.Symbol, opt
                => opt.MapFrom(source => source.NFTSymbol));
        CreateMap<IndexerNFTCollection, RecommendedNFTCollectionsDto>();
        CreateMap<IndexerSeedInfo, IndexerNFTInfo>();
        CreateMap<IndexerNFTInfo, NFTInfoIndexDto>().ForMember(
            destination => destination.TraitPairsDictionary, opt => opt.MapFrom(
                source => source.TraitPairsDictionary.IsNullOrEmpty()
                    ? null
                    : source.TraitPairsDictionary
                        .Select(item => new MetadataDto { Key = item.Key, Value = item.Value }).ToList())
        ).ForMember(destination => destination.OwnerCount,
            opt => opt.MapFrom(source
                => source.AllOwnerCount));
        CreateMap<NFTInfoIndexDto, UserProfileNFTInfoIndexDto>().ForMember(
            destination => destination.TraitPairsDictionary, opt => opt.MapFrom(
                source => source.TraitPairsDictionary.IsNullOrEmpty()
                    ? null
                    : source.TraitPairsDictionary
                        .Select(item => new MetadataDto { Key = item.Key, Value = item.Value }).ToList())
        );
        CreateMap<TokenDto, UserProfileTokenDto>();
        CreateMap<NFTCollectionIndexDto, UserProfileNFTCollectionIndexDto>();
        CreateMap<NftCollectionExtensionGrainDto, NFTCollectionExtraEto>();
        CreateMap<IndexerNFTInfo, GetIssuedCountResponse>();
        CreateMap<IndexerSeedOwnedSymbol, SeedSymbolIndexDto>()
            .ForMember(destination => destination.CreateTimeStamp,
                opt => opt.MapFrom(source
                    => DateTimeHelper.ToUnixTimeMilliseconds(source.CreateTime)));
        CreateMap<NFTCollectionExtensionIndex, IndexerNFTCollection>();
        CreateMap<IndexerNFTInfoMarketData, NFTInfoMarketDataDto>();
        CreateMap<AccountDto, UserInfo>();

        // listing
        CreateMap<IndexerNFTInfo, NFTImmutableInfoDto>();
        CreateMap<IndexerTokenInfo, TokenDto>();
        CreateMap<IndexerNFTListingInfo, NFTListingIndexDto>()
            .Ignore(o => o.Owner)
            .ForMember(destination => destination.OwnerAddress,
                opt => opt.MapFrom(source => source.Owner))
            .ForMember(destination => destination.StartTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.StartTime)))
            .ForMember(destination => destination.PublicTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.PublicTime)))
            .ForMember(destination => destination.EndTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.ExpireTime)))
            .ForMember(destination=>destination.Quantity,
            opt => opt.MapFrom(source => source.RealQuantity));
        
        CreateMap<NFTCollectionExtensionIndex, NFTCollectionIndexDto>();
        CreateMap<NFTInfoExtensionIndex, NFTInfoExtensionIndex>();
        CreateMap<NFTInfoExtensionIndex, NFTInfoIndexDto>();
        CreateMap<IndexerNFTCollectionExtension, NftCollectionExtensionGrainDto>();
        CreateMap<IndexerNFTCollection, NftCollectionExtensionGrainDto>()
            .ForMember(destination => destination.NFTSymbol, opt 
                => opt.MapFrom(source => source.Symbol));

        // synchronize transaction
        CreateMap<SynchronizeTransactionDto, SyncResultDto>();
        CreateMap<SendNFTSyncDto, CreateSynchronizeTransactionJobGrainDto>();
        CreateMap<IndexerSeedMainChainChange, CreateSynchronizeTransactionJobGrainDto>
            ().ForMember(destination => destination.TxHash, opt => opt.MapFrom(source => source.TransactionId))
            .ForMember(destination => destination.FromChainId, opt => opt.MapFrom(source => source.ChainId));
        CreateMap<SynchronizeTxJobGrainDto, SynchronizeTransactionInfoEto>();
        CreateMap<SynchronizeTransactionInfoIndex, SynchronizeTransactionDto>();
        // verify
        CreateMap<OwnerShipVerifyOrder, OwnerShipVerifyOrderIndex>().ReverseMap();
        CreateMap<OwnerShipVerifyOrder, OwnerShipVerifyOrderGrainDto>().ReverseMap();
        CreateMap<OwnerShipVerifyOrderGrainDto, OwnerShipVerifyOrderEto>().ReverseMap();
        CreateMap<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrderSummaryDto>().ReverseMap();
        CreateMap<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrderDetailDto>().ReverseMap();
        // order
        CreateMap<NFTOrder, PortkeyCreateOrderParam>();
        CreateMap<NFTOrder, NFTOrderGrainDto>();
        CreateMap<NFTOrder, NFTOrderEto>();
        CreateMap<NFTOrderIndex, NFTOrderDto>();
        CreateMap<NFTOrderIndex, CommitCreateSeedEvent>();
        // dealer
        CreateMap<ContractParamDto, ContractInvokeGrainDto>().Ignore(
                destination => destination.ExecutionCount)
            .ReverseMap();
        CreateMap<ContractInvokeGrainDto, ContractInvokeIndex>().ReverseMap();
        CreateMap<SynchronizeTxJobGrainDto, TokenStatusDto>();
        CreateMap<CreateSeedDto, CreateSeedJobGrainDto>();
        CreateMap<ContractInvokeGrainDto, ContractInvokeState>().ReverseMap();

        CreateMap<IndexerNFTInfo, CreateTokenInformation>()
            .ForMember(destination => destination.Category, opt => opt.MapFrom(source => source.TokenType))
            .ForMember(destination => destination.TokenSymbol, opt => opt.MapFrom(source => source.SeedOwnedSymbol))
            .ForMember(destination => destination.Registered, opt => opt.MapFrom(source => source.RegisterTimeSecond))
            .ForMember(destination => destination.Expires, opt => opt.MapFrom(source => source.SeedExpTimeSecond));

        CreateMap<SeedInfoDto, SeedDto>().ForMember(destination => destination.ChainId,
            opt => opt.MapFrom(source => source.CurrentChainId));
        CreateMap<PriceInfo, TokenPriceDto>();
        CreateMap<SpecialSeedItem, SpecialSeedDto>();
        CreateMap<TsmSeedSymbolIndex, BiddingSeedDto>();
        
        CreateMap<AuctionInfoDto, SymbolAuctionInfoIndex>()
            .ForMember(destination => destination.Symbol, opt 
                => opt.MapFrom(source => source.SeedSymbol))
            .ReverseMap()
            .ForMember(destination => destination.SeedSymbol, opt 
                => opt.MapFrom(source => source.Symbol));
        
        CreateMap<BidInfoDto, SymbolBidInfoIndex>().ForMember(destination => destination.Symbol, opt 
                => opt.MapFrom(source => source.SeedSymbol))
            .ReverseMap()
            .ForMember(destination => destination.SeedSymbol, opt 
                => opt.MapFrom(source => source.Symbol));
        CreateMap<SeedDto, TsmSeedSymbolIndex>();

        CreateMap<SeedPriceIndex, SeedPriceDto>().ReverseMap();
        CreateMap<UniqueSeedPriceIndex, UniqueSeedPriceDto>().ReverseMap();

        CreateMap<TsmSeedSymbolIndex, SeedDto>().ReverseMap();
        CreateMap<TsmSeedSymbolIndex, SpecialSeedItem>();
        CreateMap<NFTInfoIndex, IndexerNFTInfo>()
            .ForMember(
                destination => destination.CreatorAddress,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.Issuer,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.ProxyIssuerAddress,
                opt => opt.MapFrom(source => source.Issuer));
        CreateMap<NFTInfoIndex, IndexerNFTInfo>()
            .ForMember(
                destination => destination.CreatorAddress,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.Issuer,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.ProxyIssuerAddress,
                opt => opt.MapFrom(source => source.Issuer));
        CreateMap<NFTInfoNewIndex, IndexerNFTInfo>()
            .ForMember(
                destination => destination.CreatorAddress,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.Issuer,
                opt => opt.MapFrom(source => source.RandomIssueManager))
            .ForMember(
                destination => destination.ProxyIssuerAddress,
                opt => opt.MapFrom(source => source.Issuer));
        CreateMap<SeedSymbolIndex, IndexerNFTInfo>()
            .ForMember(d => d.CollectionId,
                opt => opt.MapFrom(d => IdGenerateHelper.GetNFTCollectionId(d.ChainId, NFTSymbolBasicConstants.SeedCollectionSymbol)))
            .ForMember(d => d.ImageUrl,
                opt => opt.MapFrom(source => source.SeedImage))
            .ForMember(d => d.CollectionSymbol, opt 
                => opt.MapFrom(d => NFTSymbolBasicConstants.SeedCollectionSymbol))
            .ForMember(d => d.CollectionName, opt 
                => opt.MapFrom(d => NFTSymbolBasicConstants.SeedCollectionSymbol));
        CreateMap<SeedSymbolIndex, IndexerSeedInfo>();
        CreateMap<TokenInfoIndex, IndexerTokenInfo>();
        CreateMap<ExternalInfoDictionary, ExternalInfoDictionaryDto>();
        CreateMap<GetNFTListingsInput, GetNFTListingsDto>();
        CreateMap<NFTCollectionExtensionIndex, NFTForSaleDto>();
        CreateMap<TsmSeedSymbolIndex, SeedRankingWeightDto>();

        CreateMap<InscriptionAmountGrainDto, InscriptionAmountDto>();
        CreateMap<CreateNFTDropInput, NFTDropExtraEto>();
        CreateMap<NFTDropExtensionIndex, NFTDropIndexDto>()
            .ForMember(destination => destination.DropId,
                opt => opt.MapFrom(source => source.Id));
        CreateMap<NFTDropExtensionIndex, RecommendedNFTDropIndexDto>()
            .ForMember(destination => destination.DropId, 
                opt => opt.MapFrom(source => source.Id));
        CreateMap<NFTDropInfoIndex, NFTDropDetailDto>()
            .ForMember(destination => destination.AddressClaimLimit,
                opt => opt.MapFrom(source => source.ClaimMax)).
            ForMember(destination => destination.StartTime,
                opt => opt.MapFrom(source => TimeHelper.ToUtcMilliSeconds(source.StartTime))).
            ForMember(destination => destination.ExpireTime,
                opt => opt.MapFrom(source => TimeHelper.ToUtcMilliSeconds(source.ExpireTime))).
            ForMember(destination => destination.Burn,
                opt => opt.MapFrom(source => source.IsBurn)).
            ForMember(destination => destination.MintPrice,
                opt => opt.MapFrom(source => source.ClaimPrice));
        CreateMap<NFTDropExtensionIndex, NFTDropDetailDto>();
        CreateMap<NFTDropInfoIndex, NFTDropQuotaDto>()
            .ForMember(destination => destination.AddressClaimLimit,
                opt => opt.MapFrom(source => source.ClaimMax));
        CreateMap<MessageInfoIndex, MessageInfoDto>();
    }
}
