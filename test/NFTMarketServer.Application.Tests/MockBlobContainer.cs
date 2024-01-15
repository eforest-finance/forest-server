using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.BlobStoring;

namespace NFTMarketServer
{
    public class MockBlobContainer : IBlobContainer
    {
        public Task SaveAsync(string name, Stream stream, bool overrideExisting = false,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            return true;
        }

        public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetOrNullAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}