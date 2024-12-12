using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace NFTMarketServer.Market;

public class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, int>
{
    public Task<int> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        // add new book ....
        return Task.FromResult(1); 
    }
}