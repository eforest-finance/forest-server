using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT;

public class NFTOwnerDto
{ 
    public long TotalCount { get; set; }
    public long Supply { get; set; }
    public string ChainId { get; set; }
    public List<NFTOwnerInfo> Items { get; set; }
}


public class NFTOwnerInfo
{
    public UserInfo Owner { get; set; }
    public long ItemsNumber { get; set; }
}

public class UserInfo
{
    public string Address { get; set; }
    public string FullAddress { get; set; }
    public string Name { get; set; }
    public string ProfileImage { get; set; }
    public virtual string Email { get; set; }
    public string Twitter { get; set; }
    public string Instagram { get; set; }
}