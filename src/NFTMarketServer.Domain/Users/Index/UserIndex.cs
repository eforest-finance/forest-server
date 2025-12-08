using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Users.Index
{
    public class UserIndex : NFTMarketEntity<Guid>, IIndexBuild
    {
        [Keyword] public string Name { get; set; }
        [Keyword] public string UserName { get; set; }
        [Keyword] public string AelfAddress { get; set; }
        [Keyword] public string CaHash { get; set; }
        [Keyword] public string CaAddressMain { get; set; }
        
        [Nested(Name = "CaAddressListSide",Enabled = true,IncludeInParent = true,IncludeInRoot = true)]
        public List<UserAddress> CaAddressListSide { get; set; }
        [Keyword] public  string Email { get; set; }

        public string Twitter { get; set; }
        public string Instagram { get; set; }
        public string ProfileImage { get; set; }
        public string ProfileImageOriginal { get; set; }
        public string BannerImage { get; set; }

        public UserIndex ShallowCopy()
        {
            return (UserIndex)MemberwiseClone();
        }
        
    }
}