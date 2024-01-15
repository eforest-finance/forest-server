using System.Collections.Generic;

namespace NFTMarketServer.Dealer.Options;

public class WorkerOption
{
    public const string DefaultCron = "0 0/5 * * * ?";
    public Dictionary<string, Worker> Workers { get; set; } = new Dictionary<string, Worker>();
}


public class Worker
{
    public string Cron { get; set; } = WorkerOption.DefaultCron;
}