namespace NFTMarketServer.Tokens
{
    public class TokenCreateInput:InputBase
    {
        public string Address { get; set; }
        public string Symbol { get; set; }
        public int Decimals { get; set; }
    }
}