namespace NFTMarketServer.Basic;

public class BasicStatusCode
{
    private const int ForestStatusPreCode = 002;
    public static int Success = 20000;
    public static int ResourceDoesNotExist = 30001;
    public static int ResourceAlreadyExists = 30002;
    public static int FailedToCreateResource = 30003;
    public static int FailedToUpdateResource = 30004;
    public static int FailedToDeleteResource = 30005;
    public static int InputDataExeceedLimit = 60001;
    public static int IllegalInputData = 60002;
    public static int DataSubmitted = 60003;
    public static int VerificationFailed = 60004;
}

public class BasicStatusMessage
{
    public static string Success = "Success";
    public static string ResourceDoesNotExist = "ResourceDoesNotExist";
    public static string ResourceAlreadyExists = "ResourceAlreadyExists";
    public static string FailedToCreateResource = "FailedToCreateResource";
    public static string FailedToUpdateResource = "FailedToUpdateResource";
    public static string FailedToDeleteResource = "FailedToDeleteResource";
    public static string InputDataExeceedLimit = "Input Data exceeding limit";
    public static string IllegalInputData = "Illegal input data";
    public static string DataSubmitted = "Data submitted";
    public static string VerificationFailed = "Data submitted";
}