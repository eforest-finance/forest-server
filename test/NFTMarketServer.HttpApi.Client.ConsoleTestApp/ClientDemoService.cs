using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.HttpApi.Client.ConsoleTestApp
{
    public class ClientDemoService : ITransientDependency
    {
        private readonly IProfileAppService _profileAppService;

        public ClientDemoService(IProfileAppService profileAppService)
        {
            _profileAppService = profileAppService;
        }

        public async Task RunAsync()
        {
            Uri server = new Uri("http://localhost:8088/api/app/user/image/2");
            HttpClient httpClient = new HttpClient();

            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();

            StreamContent streamConent = new StreamContent(new FileStream("/Users/zx/Downloads/WX20220211-180024.png", FileMode.Open, FileAccess.Read, FileShare.Read));

            multipartFormDataContent.Add(streamConent, "pnj", "WX20220211-180024.png");

            HttpResponseMessage responseMessage = httpClient.PostAsync(server, multipartFormDataContent).Result;
            
            
            var output = await _profileAppService.GetAsync();
            Console.WriteLine($"UserName : {output.UserName}");
            Console.WriteLine($"Email    : {output.Email}");
            Console.WriteLine($"Name     : {output.Name}");
            Console.WriteLine($"Surname  : {output.Surname}");
        }
    }
}