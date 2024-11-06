using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.TreeGame.Provider;

public interface ITreeGamePointsDetailProvider
{
    public Task BulkSaveOrUpdateTreePointsDetailsAsync(List<TreeGamePointsDetailInfoIndex> treeGameUserInfos);

    public Task<TreeGamePointsDetailInfoIndex> SaveOrUpdateTreePointsDetailAsync(TreeGamePointsDetailInfoIndex treeGameUserInfo);
    
    public Task<List<TreeGamePointsDetailInfoIndex>> GetTreePointsDetailsAsync(string address);


}