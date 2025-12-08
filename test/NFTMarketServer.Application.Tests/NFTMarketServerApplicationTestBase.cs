using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NFTMarketServer.Orleans.TestBase;
using NSubstitute;
using Volo.Abp.DistributedLocking;
using Xunit.Abstractions;

namespace NFTMarketServer
{
    public abstract class
        NFTMarketServerApplicationTestBase : NFTMarketServerOrleansTestBase<NFTMarketServerApplicationTestModule>
    {
        protected int AELFChainId;
        protected Guid ELFTokenId;
        protected Guid USDTTokenId;
        protected readonly ITestOutputHelper TestOutputHelper;


        protected override void AfterAddApplication(IServiceCollection services)
        {
            services.AddSingleton(GetMockAbpDistributedLockAlwaysSuccess());
            services.AddSingleton(MockInMemoryHarness());
        }
        protected NFTMarketServerApplicationTestBase(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
            var environmentProvider = GetRequiredService<TestEnvironmentProvider>();
            AELFChainId = environmentProvider.AELFChainId;
            ELFTokenId = environmentProvider.ELFTokenId;
            USDTTokenId = environmentProvider.USDTTokenId;
        }
        protected IBus MockInMemoryHarness(params IConsumer[] consumers)
        {
            var busMock = new Mock<IBus>();

            busMock.Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Callback<object, CancellationToken>((message, token) =>
                {
                    foreach (var consumer in consumers)
                    {
                        var consumeMethod = consumer.GetType().GetMethod("Consume");
                        if (consumeMethod == null) continue;

                        var consumeContextType = typeof(ConsumeContext<>).MakeGenericType(message.GetType());

                        dynamic contextMock = Activator.CreateInstance(typeof(Mock<>).MakeGenericType(consumeContextType));
                        contextMock.SetupGet("Message").Returns(message);

                        consumeMethod.Invoke(consumer, new[] { ((Mock)contextMock).Object });
                    }
                })
                .Returns(Task.CompletedTask);

            return busMock.Object;
        }
        
        
        protected IAbpDistributedLock GetMockAbpDistributedLockAlwaysSuccess()
        {
            var mockLockProvider = new Mock<IAbpDistributedLock>();
            mockLockProvider
                .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) => 
                    Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
            return mockLockProvider.Object;
        }

        
        
        public static IHttpClientFactory MockHttpFactory(ITestOutputHelper testOutputHelper,
            params Action<Mock<HttpMessageHandler>, ITestOutputHelper>[] mockActions)
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            foreach (var mockFunc in mockActions)
                mockFunc.Invoke(mockHandler, testOutputHelper);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });

            return httpClientFactoryMock.Object;
        }

        public static Action<Mock<HttpMessageHandler>, ITestOutputHelper> PathMatcher(HttpMethod method, string path,
            string respData)
        {
        
            return (mockHandler, testOutputHelper) =>
            {
                DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(respData, Encoding.UTF8, "application/json")
                };

                mockHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == method && req.RequestUri.ToString().Contains(path)),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns(() =>
                    {
                        testOutputHelper?.WriteLine($"Mock Http {method} to {path}, resp={response}");
                        return Task.FromResult(response);
                    });
            };
        }

        public static Action<Mock<HttpMessageHandler>, ITestOutputHelper> PathMatcher(HttpMethod method, string path, object response)
        {
            return PathMatcher(method, path, JsonConvert.SerializeObject(response));
        }
    }
}
