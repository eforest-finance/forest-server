using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace NFTMarketServer.Market;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, BookDto>
{
    public Task<BookDto> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        // add new book ....
        return Task.FromResult(new BookDto()
        {
            Title = request.Title,
            Author = request.Author
        }); 
    }
}