using AutoMapper;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Synchronize.Eto;

namespace NFTMarketServer.ContractEventHandler
{
    public class ContractEventHandlerAutoMapperProfile : Profile
    {
        public ContractEventHandlerAutoMapperProfile()
        {
            // Synchronize
            CreateMap<SynchronizeTxJobGrainDto, SynchronizeTransactionInfoEto>();
            CreateMap<SynchronizeTransactionInfoEto, SynchronizeTxJobGrainDto>();
            CreateMap<SynchronizeTransactionInfoIndex, SynchronizeTransactionInfoEto>();
        }
    }
}