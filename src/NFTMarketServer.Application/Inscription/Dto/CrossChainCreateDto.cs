using System.Collections.Generic;
using AElf.Client.Dto;
using Google.Protobuf;

namespace NFTMarketServer.Inscription;

public class CrossChainCreateDto
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string Symbol { get; set; }
    public long ParentChainHeight { get; set; }
    public MerklePathDto MerklePathDto { get; set; }
    public ByteString TransactionBytes { get; set; }
}