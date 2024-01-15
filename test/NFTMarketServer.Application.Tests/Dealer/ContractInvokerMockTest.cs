using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using Microsoft.Extensions.Options;
using Moq;
using NFTMarketServer.Dealer.Options;
using NFTMarketServer.Dealer.Provider;

namespace NFTMarketServer.Dealer;

public partial class ContractInvokerTest
{
    
    private static IOptionsMonitor<ChainOption> MockChainOptions()
    {
        var chainOption = new ChainOption
        {
            InvokeExpireSeconds = 2,
            QueryTransactionDelayMillis = 100,
            QueryPendingTxSecondsAgo = 0,
            ChainNode = new Dictionary<string, string>
            {
                ["AELF"] = "http://127.0.0.1:9200/aelfNode",
                ["tDVV"] = "http://127.0.0.1:9200/tdvvNode"
            },
            AccountOption = new Dictionary<string, AccountOptionItem>
            {
                ["AuctionAutoClaimAccount"] = new()
                {
                    PrivateKey = "5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"
                },
                ["SeedCreateAccount"] = new()
                {
                    PrivateKey = "5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"
                }
            },
            ContractAddress = new Dictionary<string, Dictionary<string, string>>
            {
                ["AELF"] = new()
                {
                    ["Forest.AuctionContract"] = "qHCtKiVbw5NwpQVWN1cx8uy97LUukMYMTb9Ga4QUHNfY219mS",
                    ["Forest.SymbolRegistrarContract"] = "qHCtKiVbw5NwpQVWN1cx8uy97LUukMYMTb9Ga4QUHNfY219mS"
                },
                ["tDVV"] = new()
                {
                    ["Forest.AuctionContract"] = "qHCtKiVbw5NwpQVWN1cx8uy97LUukMYMTb9Ga4QUHNfY219mS",
                    ["Forest.SymbolRegistrarContract"] = "qHCtKiVbw5NwpQVWN1cx8uy97LUukMYMTb9Ga4QUHNfY219mS"
                }
            }
        };
        var mockData = new Mock<IOptionsMonitor<ChainOption>>();
        mockData.Setup(o => o.CurrentValue).Returns(chainOption);
        return mockData.Object;
    }

    private static IAelfClientProvider MockSendTransaction(string queryStatus)
    {
        var aelfClientProvider = new Mock<IAelfClientProvider>();
        aelfClientProvider.Setup(ser => 
            ser.SendTransactionAsync(It.IsAny<string>(), It.IsAny<SendTransactionInput>()))
            .Returns(Task.FromResult(new SendTransactionOutput
            {
                
            }));
        
        aelfClientProvider.Setup(ser => 
            ser.GetChainStatusAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(new ChainStatusDto
            {
                ChainId = "AELF",
                BestChainHash = HashHelper.ComputeFrom("").ToHex(),
                BestChainHeight = 100
            }));
        
        aelfClientProvider.Setup(ser => 
            ser.GetTransactionResultAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((chainId,transactionId ) => Task.FromResult(new TransactionResultDto
            {
                Status = transactionId == HashHelper.ComputeFrom(Dtos.TransactionResultStatus.MINED.ToString()).ToHex() ? Dtos.TransactionResultStatus.MINED.ToString()
                    : transactionId == HashHelper.ComputeFrom(Dtos.TransactionResultStatus.PENDING.ToString()).ToHex() ? Dtos.TransactionResultStatus.PENDING.ToString() : queryStatus,
                TransactionId = transactionId,
                ReturnValue = "MockReturnValue"
            }));
        return aelfClientProvider.Object;
    }
}