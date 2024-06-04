
using System.Collections.Generic;

namespace NFTMarketServer.Common.AElfSdk.Dtos;

public static class SystemContractName
{
    public const string BasicContractZero = "";
    public const string CrossChainContract = "AElf.ContractNames.CrossChain";
    public const string TokenContract = "AElf.ContractNames.Token";
    public const string ParliamentContract = "AElf.ContractNames.Parliament";
    public const string ConsensusContract = "AElf.ContractNames.Consensus";
    public const string ReferendumContract = "AElf.ContractNames.Referendum";
    public const string TreasuryContract = "AElf.ContractNames.Treasury";
    public const string AssociationContract = "AElf.ContractNames.Association";
    public const string TokenConverterContract = "AElf.ContractNames.TokenConverter";
    public const string VoteContract = "AElf.ContractNames.Vote";
    public const string GenesisContract = "AElf.ContractNames.Genesis";
    public const string ProfitContract = "AElf.ContractNames.Profit";
    public const string ElectionContract = "AElf.ContractNames.Election";
    public const string ConfigurationContract = "AElf.ContractNames.Configuration";
    public const string TokenHolderContract = "AElf.ContractNames.TokenHolder";
    public const string EconomicContract = "AElf.ContractNames.Economic";

    public static readonly List<string> All = new()
    {
        BasicContractZero, CrossChainContract, TokenContract,
        ParliamentContract, ConsensusContract, ReferendumContract,
        TreasuryContract, AssociationContract, TokenConverterContract,
        VoteContract, GenesisContract, ProfitContract, 
        ElectionContract, ConfigurationContract, TokenHolderContract,
        EconomicContract
    };
}