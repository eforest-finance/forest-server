using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.HandleException;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.AwsS3;

public class AwsS3Client : ISingletonDependency
{
    private const string HttpSchema = "https";
    private const string HostS3 = ".s3.amazonaws.com";
    private readonly AwsS3Option _awsS3Option;
    private readonly ILogger<AwsS3Client> _logger;

    private AmazonS3Client _amazonS3Client;

    public AwsS3Client(IOptionsSnapshot<AwsS3Option> awsS3Option,ILogger<AwsS3Client> logger)
    {
        _logger = logger;
        _awsS3Option = awsS3Option.Value;
        InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        var accessKeyID = _awsS3Option.AccessKeyID;
        var secretKey = _awsS3Option.SecretKey;
        var ServiceURL = _awsS3Option.ServiceURL;
        var config = new AmazonS3Config()
        {
            ServiceURL = ServiceURL,
            RegionEndpoint = Amazon.RegionEndpoint.APNortheast1
        };
        _amazonS3Client = new AmazonS3Client(accessKeyID, secretKey, config);
    }


    public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3Key + "/" + fileName + ".svg",
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
            ? $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{_awsS3Option.S3Key}/{fileName}.svg"
            : string.Empty;
    }

    public async Task<string> UpLoadFileForNFTAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3KeyForest + "/" + fileName,
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        
        UriBuilder uriBuilder = new UriBuilder
        {
            Scheme = HttpSchema,
            Host = _awsS3Option.BucketName + HostS3,
            Path = "/" + _awsS3Option.S3KeyForest + "/" + fileName
        };

        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
            ? uriBuilder.ToString() : string.Empty;
    }
    
    public async Task<KeyValuePair<string,string>> UpLoadFileForNFTWithHashAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3KeyForest + "/" + fileName,
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse = new PutObjectResponse();

        var msg = "";
        for (var i = 0; i < CommonConstant.IntThree; i++)
        {
            putObjectResponse = await UploadS3Async(putObjectRequest);
            if (putObjectResponse != null && putObjectResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                break;
            }

            await Task.Delay(CommonConstant.IntOneThousand);
        }
        
        var uriBuilder = new UriBuilder
        {
            Scheme = HttpSchema,
            Host = _awsS3Option.BucketName + HostS3,
            Path = "/" + _awsS3Option.S3KeyForest + "/" + fileName
        };
        if (putObjectResponse.HttpStatusCode == HttpStatusCode.OK)
        {
            return new KeyValuePair<string, string>(uriBuilder.ToString(), putObjectResponse.ETag);
        }
        else
        {
            throw new SystemException(msg);
        }
    }

    [ExceptionHandler(typeof(Exception),
        Message = "AwsS3Client.UploadS3Async", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"putObjectRequest"})]
    public virtual async Task<PutObjectResponse> UploadS3Async(PutObjectRequest putObjectRequest)
    {
        return await _amazonS3Client.PutObjectAsync(putObjectRequest);
    }

    public async Task<string> GetSpecialSymbolUrl(string fileName)
    {
        return $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{_awsS3Option.S3Key}/{fileName}.svg";
    }


    public async Task<GetObjectResponse> GetObjectAsync(string fileName)
    {
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3Key + "/" + fileName + ".svg"
        };
        var getObjectResponse = await _amazonS3Client.GetObjectAsync(getObjectRequest);
        return getObjectResponse;
    }
}