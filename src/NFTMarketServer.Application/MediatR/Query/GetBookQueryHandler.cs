using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace NFTMarketServer.Market;

public class GetBookQueryHandler : IRequestHandler<GetBookQuery, BookDto>
{
    public Task<BookDto> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        // get book info
        return Task.FromResult(new BookDto { Id = request.Id, Title = "Sample" });
    }
}