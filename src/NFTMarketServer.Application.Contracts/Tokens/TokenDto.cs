using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Tokens
{
    public class TokenDto : EntityDto<string>
    {
        public string ChainId { get; set; }
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
    }
}