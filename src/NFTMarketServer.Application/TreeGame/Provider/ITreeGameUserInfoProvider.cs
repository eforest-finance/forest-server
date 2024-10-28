using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.TreeGame.Provider;

public interface ITreeGameUserInfoProvider
{
    public Task<TreeGameUserInfoIndex> SaveOrUpdateTreeUserBalanceAsync(TreeGameUserInfoDto treeGameUserInfoDto);
    
    public Task<TreeGameUserInfoIndex> GetTreeUserInfoAsync(string address);

    public Task<Tuple<long, List<TreeGameUserInfoIndex>>> GetTreeUsersByParentUserAsync(string address);

}