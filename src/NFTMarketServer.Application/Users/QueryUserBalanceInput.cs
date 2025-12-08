namespace NFTMarketServer.Users;

public class QueryUserBalanceInput : PagedAndSortedMaxCountResultRequestDto
{
    public long BlockHeight { get; set; }
    public string ChainId{ get; set; }

}