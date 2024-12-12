using System.Threading.Tasks;

using MediatR;
using Volo.Abp.Application.Services;

namespace NFTMarketServer.Market;

public abstract class IBookAppService
{
    public abstract Task< BookDto> CreateBookDtoAsync(CreateBookCommand command);
    public abstract Task<BookDto> GetBookAsync(GetBookQuery query);
    
    public abstract Task<BookDto> CreateBookAsync(CreateBookCommand command);

}