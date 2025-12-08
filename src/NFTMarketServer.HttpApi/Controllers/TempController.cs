using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

public class TempController : AbpController
{
    [HttpPost]
    [Route("nftInfoIndex")]
    public Task GetListAsync1(NFTInfoIndex input)
    {
        return null;
    }

    [HttpPost]
    [Route("seedSymbolIndex")]
    public Task GetListAsync2(SeedSymbolIndex input)
    {
        return null;
    }
}