using MediatR;

namespace NFTMarketServer.Market;

public class GetBookQuery : IRequest<BookDto>
{
    public int Id { get; set; }
}