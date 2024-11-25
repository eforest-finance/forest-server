using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using NFTMarketServer.Ai;

namespace NFTMarketServer.HandleException;

public class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleExceptionRetrun(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionSymbolPriceRetrun(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
    public static async Task<FlowBehavior> HandleExceptionFailRetrun(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionSeedSyncRetrun(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = -1
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionRetrunResultDto(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new ResultDto<string>() {Success = false, Message = ex.Message}
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionRetrunFlag(Exception ex, bool flag)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = flag
        };
    }

    public static async Task<FlowBehavior> HandleExceptionRethrow(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
    
    public async Task<FlowBehavior> HandleExceptionThrow(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new Exception("New Exception")
        };
    }
    public async Task<FlowBehavior> HandleExceptionContinue(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
    
    public static async Task<FlowBehavior> HandleExceptionRetrunCreateAiResult(Exception ex, bool isCanRetry, string transactionId)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new CreateAiResultDto()
            {
                CanRetry = isCanRetry,
                TransactionId = transactionId,
                Success = false, ErrorMsg = ex.Message,
            }
        };
    }

}