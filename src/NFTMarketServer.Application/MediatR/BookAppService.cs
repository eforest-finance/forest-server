using System.Threading.Tasks;

using MediatR;
using Volo.Abp.Application.Services;

namespace NFTMarketServer.Market;

public class BookAppService :  IBookAppService //ApplicationService
{
    private readonly IMediator _mediator;

    public BookAppService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<BookDto> CreateBookAsync(CreateBookCommand command)
    {
        return await _mediator.Send(command);
    }

    public override async Task<BookDto> CreateBookDtoAsync(CreateBookCommand command)
    {
        return await _mediator.Send(command);
    }

    public override async Task<BookDto> GetBookAsync(GetBookQuery query)
    {
        return await _mediator.Send(query);
    }
}