namespace NFTMarketServer.Activity;

public enum SymbolMarketActivityType
{
    //Seed in the chain by the current user address of competitive bidding the Bid for auction after a successful transaction
    // SymbolMarkerContract.BidPlaced
    Bid,

    //In the chain by the current user address of Seed transactions
    // SymbolMarkerContract.Dealt
    Buy,

    //The current user address created in the chain by the FT NFT
    // TokenContract.create & SeedSymbolInfo TokenType in [FT,NFT]
    Create,

    //In the chain by the current user address issued token
    // TokenContract.Issue & SeedSymbolInfo TokenType in [FT]
    Issue
}