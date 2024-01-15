namespace NFTMarketServer.Grains.State.Users;

public class UserState
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string AelfAddress { get; set; }
    public string CaHash { get; set; }
    public string CaAddressMain { get; set; }
    public Dictionary<string, string> CaAddressSide { get; set; }
    public virtual string Email { get; set; }

    public string Twitter { get; set; }
    public string Instagram { get; set; }
    public string ProfileImage { get; set; }
    public string ProfileImageOriginal { get; set; }
    public string BannerImage { get; set; }
}