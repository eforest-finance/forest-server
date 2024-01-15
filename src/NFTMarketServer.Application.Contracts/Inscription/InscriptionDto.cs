using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.Inscription;

public class InscriptionCreate : IndexerCommonResult<InscriptionCreate>
{
    public long BlockHeight { get; set; }
    public string CollectionSymbol { get; set; }

    public string ItemSymbol { get; set; }

    public string Tick { get; set; }

    public string TotalSupply { get; set; }

    public int Decimals { get; set; }

    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }


    public string IssueChainId { get; set; }
    public string CollectionExternalInfo { get; set; }
    public Dictionary<string, string> ExternalInfo { get; set; }
    public string Owner { get; set; }
    public int Limit { get; set; }
    public bool Deployer { get; set; }
}

public class GetInscriptionInput
{
    public string ChainId { get; set; }
    public string Tick { get; set; }
    public long BeginBlockHeight { get; set; } = 0;
    public long EndBlockHeight { get; set; } = 0;
}

public class InscriptionDto : GraphQLDto
{
    public string Tick { get; set; }
    public long TotalSupply { get; set; }
    public string Issuer { get; set; }
    public int IssueChainId { get; set; }
    public List<ExternalInfoDto> CollectionExternalInfo { get; set; } = new();
    public List<ExternalInfoDto> ItemExternalInfo { get; set; } = new();
    public string Owner { get; set; }
    public long Limit { get; set; }
    public string Deployer { get; set; }
    public string TransactionId { get; set; }
}

public class InscriptionResultDto
{
    [CanBeNull] public List<InscriptionDto> Inscription { get; set; }
}

public class GraphQLDto
{
    public string Id { get; set; }

    public string ChainId { get; set; }

    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public DateTime BlockTime { get; set; }
}

public class ExternalInfoDto
{
    public string Key { get; set; }
    public string Value { get; set; }
}

public class InscriptionCreateResultDto
{
    public InscriptionCreateResultDto InscriptionCreate { get; set; }
}