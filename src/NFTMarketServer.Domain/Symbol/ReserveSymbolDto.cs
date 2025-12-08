namespace NFTMarketServer.Symbol;

public class ReserveSymbolDto
{
    public string Symbol { get; set; }
    public string IssueChain { get; set; }
    public string TokenContract { get; set; }
    public long Price { get; set; }
}