using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Common;

public class PagedResultWrapper<T>
{
    public static PagedResultDto<T> Initialize()
    {
        return new PagedResultDto<T>
        {
            TotalCount = 0,
            Items = new List<T>()
        };
    }
}