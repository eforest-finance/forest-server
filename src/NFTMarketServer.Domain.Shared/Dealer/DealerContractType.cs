namespace NFTMarketServer.Dealer;

public class DealerContractType
{
   public static string MainChainId = "AELF";
      
   public static string AuctionContractName = "Forest.AuctionContract";
   
   public static string AuctionContractMethod = "Claim";

   public static string AuctionAutoClaimAccount = "AuctionAutoClaimAccount";
   
   public static string RegistrarContractName = "Forest.SymbolRegistrarContract";
   
   public static string RegistrarCreateSeedMethod = "CreateSeed";

   public static string RegistrarCreateSeedAccount = "RegistrarCreateSeedAccount";

   public static string TokenContractName = "AElf.Contracts.MutiToken";
   public static string ValidateTokenInfoExists = "ValidateTokenInfoExists";
   public static string CrossChainCreateToken = "CrossChainCreateToken";
   public static string TokenContractAccount = "TokenContractAccount";
   
   public static string CrossChainContractName = "AElf.Contracts.CrossChain";
   public static string GetParentChainHeight = "GetParentChainHeight";

   public static string InscriptionContractName = "Inscription";
   public static string IssueInscription = "IssueInscription";
   
   public static string DropContractName = "Drop";
   public static string DropFinishMethod = "FinishDrop";
   public static string DropFinishAccount = "DropFinishAccount";
   
}