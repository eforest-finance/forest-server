namespace NFTMarketServer;

public class AwsS3Option
{
    public string AccessKeyID { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public string S3Key { get; set; }
    
    public string S3KeyForest { get; set; }
    
    public string ServiceURL { get; set; }
}