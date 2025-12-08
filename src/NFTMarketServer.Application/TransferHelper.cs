using AElf;
using AElf.Types;
using Forest;
using Portkey.Contracts.CA;
using Volo.Abp;

namespace NFTMarketServer;

public abstract class TransferHelper
{
    public static Transaction TransferToTransaction(string rawTransaction)
    {
        return Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction));
    }
}