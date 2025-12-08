using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using NFTMarketServer.Ai;

namespace NFTMarketServer.HandleException;

public class CrossChainExceptionHandlingService
{
    private const int CrossChainDelay = 1000;
    public static async Task<FlowBehavior> HandleExceptionDelayReturn(Exception ex, int delayTime)
    {
        await Task.Delay(delayTime);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionDelayDefaultReturn(Exception ex)
    {
        await Task.Delay(CrossChainDelay);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = 0
        };
    }
    
}