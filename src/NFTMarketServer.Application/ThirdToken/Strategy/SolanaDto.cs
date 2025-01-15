using System.Collections.Generic;

namespace NFTMarketServer.ThirdToken.Strategy;

public class SolanaRequest
{
    public string JsonRpc { get; set; }
    public int Id { get; set; }
    public string Method { get; set; }
    public List<string> Params { get; set; }
}

public class SolanaResponse
{
    public string JsonRpc { get; set; }
    public SolanaResult Result { get; set; }
    public SolanaError Error { get; set; }
}

public class SolanaResult
{
    public SolanaResultValue Value { get; set; }
}

public class SolanaError
{
    public int Code { get; set; }
    public string Message { get; set; }
}

public class SolanaResultValue
{
    public string Amount { get; set; }
}