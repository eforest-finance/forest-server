using System.Collections.Generic;
using NFTMarketServer.Chain;

namespace NFTMarketServer.Worker;

public class WorkerOptions
{
    public const int TimePeriod = 3000;
     
    public Dictionary<string, WorkerSetting> WorkerSettings { get; set; }
    
    public WorkerSetting GetWorkerSettings(BusinessQueryChainType businessType)
    {
        return WorkerSettings?.GetValueOrDefault(businessType.ToString()) ?? 
               new WorkerSetting();
    }
}

public class WorkerSetting
{ 
    public int TimePeriod { get; set; } = 3000;

    public bool OpenSwitch { get; set; } = true;
    public bool ResetBlockHeightFlag { get; set; } = false;
    public long ResetBlockHeight { get; set; } = 0;
}