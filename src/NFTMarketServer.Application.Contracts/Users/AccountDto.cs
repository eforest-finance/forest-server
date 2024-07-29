using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Helper;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Users
{
    public class AccountDto : EntityDto<string>
    {
        public AccountDto()
        {
        }

        public AccountDto(string address)
        {
            Address = address;
        }

        public string Address { get; set; }
        public string AelfAddress { get; set; }
        public string CaHash { get; set; }
        public Dictionary<string, string> CaAddress { get; set; } = new();
        public string Name { get; set; }
        public string ProfileImage { get; set; }
        public string Email { get; set; }
        public string Twitter { get; set; }
        public string Instagram { get; set; }
        
        public AccountDto WithChainIdAddress(string chainId)
        {
            var cp = (AccountDto)MemberwiseClone();
            cp.CaAddress = new Dictionary<string, string>();
            var defaultName = cp.Name.IsNullOrEmpty() || cp.Name.Contains(cp.Address);
            if (!defaultName) return cp;
            cp.Name = FullAddressHelper.ToShortAddress(cp.Address);
            
            // caAddresses, with caChainId tail
            foreach (var (caChain, caAddr) in CaAddress)
            {
                if (!cp.Name.Equals(caAddr)) continue;
                cp.Name = FullAddressHelper.ToFullAddress(cp.Name, chainId);
                break;
            }
            if (cp.Name.Length <= cp.Address.Length)
            {
                cp.Name = FullAddressHelper.ToFullAddress(cp.Name, chainId);
            }
            return cp;
        }
    }
    
}