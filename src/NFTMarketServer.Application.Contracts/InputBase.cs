using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer
{
    public class InputBase
    {
        [Range(1, int.MaxValue)]
        public int ChainId { get; set; }
    }
}