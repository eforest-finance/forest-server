using System;
using System.Collections.Generic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT;


public class InscriptionInfoDtoPageInfo : IndexerCommonResult<InscriptionInfoDtoPageInfo>
{
    public long TotalRecordCount { get; set; }
    public List<InscriptionInfoDto> InscriptionInfoDtoList { get; set; }
}

public class InscriptionInfoDtos
{
    public List<InscriptionInfoDto> Inscription { get; set; }
}

public class InscriptionInfoDto
{
    public string Tick { get; set; } = "";
    public string IssuedTransactionId { get; set; } = "";
    public DateTime? DeployTime { get; set; } = null;
    public long MintLimit { get; set; } = -1;
}