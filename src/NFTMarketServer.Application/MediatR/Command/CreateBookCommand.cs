using MediatR;

namespace NFTMarketServer.Market;

public class CreateBookCommand : IRequest<BookDto>
{
    public string Title { get; set; }
    public string Author { get; set; }
}