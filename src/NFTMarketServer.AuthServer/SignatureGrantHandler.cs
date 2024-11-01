using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Dto;
using NFTMarketServer.Model;
using NFTMarketServer.TreeGame;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Provider;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;

namespace NFTMarketServer;

public class SignatureGrantHandler: ITokenExtensionGrant
{
    private IUserInformationProvider _userInformationProvider;
    private ITreeGameService _treeGameService;
    private ILogger<SignatureGrantHandler> _logger;
    private IAbpDistributedLock _distributedLock;
    private readonly string _lockKeyPrefix = "NFTMarketServer:Auth:SignatureGrantHandler:";
    private readonly string _source_portkey = "portkey";
    private readonly string _source_nightaelf = "nightElf";
    private readonly string _V2 = "v2";

    public string Name { get; } = "signature";

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        _logger.LogInformation("create token start");
        var publicKeyVal = context.Request.GetParameter("pubkey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var timestampVal = context.Request.GetParameter("timestamp").ToString();
        var inviteFrom = context.Request.GetParameter("invite_from").ToString();
        var inviteType = context.Request.GetParameter("invite_type").ToString();
        var nickName = context.Request.GetParameter("nickname").ToString();

        // var caHash = context.Request.GetParameter("ca_hash").ToString();
        // var chainId = context.Request.GetParameter("chain_id").ToString();
        var accountInfo = context.Request.GetParameter("accountInfo").ToString();
        var source = context.Request.GetParameter("source").ToString();
        var signTip = context.Request.GetParameter("signTip").ToString();

        var invalidParamResult = CheckParams(publicKeyVal, signatureVal, timestampVal, accountInfo, source);
        if (invalidParamResult != null)
        {
            return invalidParamResult;
        }

        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var timestamp = long.Parse(timestampVal);
        var address = string.Empty;
        if (!string.IsNullOrWhiteSpace(publicKeyVal))
        {
            address = Address.FromPublicKey(publicKey).ToBase58();
        }

        var caHash = string.Empty;
        var caAddressMain = string.Empty;
        var caAddressSide = new Dictionary<string, string>();
        
        if (source == _source_portkey)
        {
            var accountInfoList = JsonConvert.DeserializeObject<List<GrantAccountInfo>>(accountInfo);
            if(accountInfoList == null || accountInfoList.Count == 0)
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"The accountInfo is invalid.");
            }
            
            //Find caHash by caAddress
            var graphqlConfig = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<GraphQLOption>>().Value;
            
            foreach (var account in accountInfoList)
            {
                var version = context.Request.GetParameter("version").ToString();
                var portkeyUrl = version == _V2 ? graphqlConfig.PortkeyV2Url : graphqlConfig.PortkeyUrl;
                var caHolderInfos = new IndexerCAHolderInfos();
                try
                {
                    caHolderInfos = await GetCAHolderInfo(portkeyUrl,
                        new List<string>(){ account.Address} , account.ChainId);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "create token exception portkeyUrl:{A} errMsg:{B}", JsonConvert.SerializeObject(portkeyUrl),JsonConvert.SerializeObject(ex.Message));
                    return null;
                }

                if (caHolderInfos == null || caHolderInfos.CaHolderManagerInfo==null || caHolderInfos.CaHolderManagerInfo.Count == 0)
                {
                    return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                        $"invalid caaddress {account.Address}.");
                }

                if (!string.IsNullOrEmpty(caHash) && caHash != caHolderInfos.CaHolderManagerInfo[0].CaHash)
                {
                    return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                        $"User identities are inconsistent.");
                }

                caHash = caHolderInfos.CaHolderManagerInfo[0].CaHash;
                if (account.ChainId.ToLower() == "aelf")
                {
                    caAddressMain = account.Address;
                }
                else
                {
                    caAddressSide.TryAdd(account.ChainId, account.Address);
                }
            }
        }
        else
        {
            var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
            var timeRangeConfig = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<TimeRangeOption>>()
                .Value;

            if (time < DateTime.UtcNow.AddMinutes(-timeRangeConfig.TimeRange) ||
                time > DateTime.UtcNow.AddMinutes(timeRangeConfig.TimeRange))
            {
                return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                    $"The time should be {timeRangeConfig.TimeRange} minutes before and after the current time.");
            }
        }
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        _userInformationProvider = context.HttpContext.RequestServices.GetRequiredService<IUserInformationProvider>();
        _treeGameService = context.HttpContext.RequestServices.GetRequiredService<ITreeGameService>();

        var userName = address;
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            userName = caHash;
        }

        var user = await userManager.FindByNameAsync(userName);
        _logger.LogInformation("create token user:{A} userName:{B} address:{C} caHash:{D}",JsonConvert.SerializeObject(user), userName, address, caHash);

        if (user == null)
        {
            var userId = Guid.NewGuid();

            var createUserResult = await CreateUserAsync(userManager, _userInformationProvider, userId, address, caHash,caAddressMain,caAddressSide);
            if (!createUserResult)
            {
                return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Create user failed.");
            }

            user = await userManager.GetByIdAsync(userId);

            if (TreeGameConstants.TreeGameInviteType.Equals(inviteType))
            {
                await _treeGameService.AcceptInvitationAsync(address, nickName, inviteFrom);
            }
        }
        else
        {
            UserSourceInput userSourceInput = new UserSourceInput
            {
                UserId = user.Id,
                AelfAddress = address,
                CaHash = caHash,
                CaAddressMain = caAddressMain,
                CaAddressSide = caAddressSide
            };
            await _userInformationProvider.SaveUserSourceAsync(userSourceInput);

        }

        try
        {
            var userClaimsPrincipalFactory = context.HttpContext.RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>();
            var signInManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<IdentityUser>>();

            var principal = await signInManager.CreateUserPrincipalAsync(user);
            var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
            _logger.LogInformation("create token scopes:{A}",JsonConvert.SerializeObject(principal.GetScopes()));

            claimsPrincipal.SetScopes("NFTMarketServer");
            claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
            claimsPrincipal.SetAudiences("NFTMarketServer");
        
            await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimDestinationsManager>()
                .SetAsync(principal);

            var token = new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "create token errMsg:{A}",ex.Message);
            throw  ex;
        }

        return null;
    }
    
    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }));
    }
    
    private ForbidResult CheckParams(string publicKeyVal, string signatureVal, string timestampVal, string accoutInfo,
        string source)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(source))
        {
            errors.Add("invalid parameter source.");
        }

        if (source != _source_portkey && string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter pubkey.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (source != _source_portkey && string.IsNullOrWhiteSpace(timestampVal) || !long.TryParse(timestampVal, out var time) || time <= 0)
        {
            errors.Add("invalid parameter timestamp.");
        }

        if (source == _source_portkey && string.IsNullOrWhiteSpace(accoutInfo))
        {
            errors.Add("invalid parameter account_info.");
        }

        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = GetErrorMessage(errors)
                }));
        }

        return null;
    }

    private async Task<IndexerCAHolderInfos> GetCAHolderInfo(string url,List<string> userCaAddresses,string chainId)
    {
        using var graphQLClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());

        // It should just one item
        var graphQlRequest = new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderManagerInfo(dto: {chainId:$chainId, caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        chainId,caHash,caAddress}
                }",
            Variables = new
            {
                chainId=chainId,caAddresses = userCaAddresses, skipCount = 0, maxResultCount = userCaAddresses.Count
            }
        };

        var graphQLResponse = await graphQLClient.SendQueryAsync<IndexerCAHolderInfos>(graphQlRequest);

        return graphQLResponse.Data;
    }

    private string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");
        if (message.Contains(','))
        {
            return message.TrimEnd().TrimEnd(',');
        }

        return message;
    }

    private async Task<bool> CreateUserAsync(IdentityUserManager userManager,IUserInformationProvider userInformationProvider, Guid userId, string address,
        string caHash,string caAddressMain,Dictionary<string, string> caAddressSide)
    {
        var result = false;
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + caHash + address);

        //get shared lock
        if (handle != null)
        {
            string userName = string.IsNullOrEmpty(caHash) ? address : caHash;
            var user = new IdentityUser(userId, userName: userName, email: Guid.NewGuid().ToString("N") + "@nft-market.io");
            var identityResult = await userManager.CreateAsync(user);

            if (identityResult.Succeeded)
            {
                _logger.LogDebug("Save user extend info...");
                UserSourceInput userSourceInput = new UserSourceInput
                {
                    UserId = userId,
                    AelfAddress = address,
                    CaHash = caHash,
                    CaAddressMain = caAddressMain,
                    CaAddressSide = caAddressSide
                };
                var userDto = await userInformationProvider.SaveUserSourceAsync(userSourceInput);
                await userInformationProvider.AddNewUserCountAsync(userSourceInput, userName);
                _logger.LogDebug($"create user success: {userId.ToString()}");
            }

            result = identityResult.Succeeded;
        }
        else
        {
            _logger.LogError($"do not get lock, keys already exits. userId: {userId.ToString()}");
        }

        return result;
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
        ImmutableArray<string> scopes)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>()
                           .ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }
}