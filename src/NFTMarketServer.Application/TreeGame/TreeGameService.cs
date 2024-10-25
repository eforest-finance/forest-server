using System;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chains;
using NFTMarketServer.File;
using NFTMarketServer.Users.Index;
using NFTMarketServer.Users.Provider;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Users
{
    public class TreeGameService : NFTMarketServerAppService, ITreeGameService
    {
        private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;
        private readonly INESTRepository<UserExtraIndex, Guid> _userExtraIndexRepository;
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<UserAppService> _logger;
        private readonly IObjectMapper _objectMapper;
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IChainAppService _chainAppService;
        private readonly ISymbolIconAppService _symbolIconAppService;
        private readonly INESTRepository<UserIndex, Guid> _userRepository;
        private const string FullAddressPrefix = "ELF";
        public const char FullAddressSeparator = '_';
        private const string AELF = "AELF";
        private readonly ITreeGameUserInfoProvider _treeGameUserInfoProvider;


        public TreeGameService(
            ITreeGameUserInfoProvider treeGameUserInfoProvider,
            IUserInformationProvider userInformationProvider,
            INESTRepository<UserIndex, Guid> userIndexRepository,
            INESTRepository<UserExtraIndex, Guid> userExtraIndexRepository,
            ILogger<UserAppService> logger,
            IClusterClient clusterClient,
            IChainAppService chainAppService,
            ISymbolIconAppService symbolIconAppService,
            INESTRepository<UserIndex, Guid> userRepository,
            IObjectMapper objectMapper)

        {
            _userInformationProvider = userInformationProvider;
            _userIndexRepository = userIndexRepository;
            _userExtraIndexRepository = userExtraIndexRepository;
            _clusterClient = clusterClient;
            _logger = logger;
            _chainAppService = chainAppService;
            _objectMapper = objectMapper;
            _symbolIconAppService = symbolIconAppService;
            _userRepository = userRepository;
            _treeGameUserInfoProvider = treeGameUserInfoProvider;
        }
        
    }
}