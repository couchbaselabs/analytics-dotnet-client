// filepath: /Users/jeffry.morris/Documents/source/couchbase-net-client/src/Couchbase.Analytics/Internal/AnalyticsServiceTest.cs

using System.Net;
using System.Text;
using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Couchbase.Analytics2.UnitTests.Internal
{
    public class AnalyticsServiceTest
    {
        private readonly Mock<ILogger<AnalyticsService>> _loggerMock;
        private readonly Mock<ICouchbaseHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<HttpClient> _httpClientMock;
        private readonly Uri _endPoint;

        public AnalyticsServiceTest()
        {
            _loggerMock = new Mock<ILogger<AnalyticsService>>();
            _httpClientFactoryMock = new Mock<ICouchbaseHttpClientFactory>();
            _httpClientMock = new Mock<HttpClient>();
            _endPoint = new Uri($"{IPAddress.Loopback.ToString()}:8095");

            _httpClientFactoryMock
                .Setup(factory => factory.Create())
                .Returns(_httpClientMock.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var options = new ClusterOptions();

            // Act
            var service = new AnalyticsService(options, _httpClientFactoryMock.Object, _endPoint, _loggerMock.Object, new DefaultSerializer());

            // Assert
            Assert.NotNull(service.Uri);
            Assert.Equal($"https://{_endPoint.Host}:{_endPoint.Port}/api/v1/request", service.Uri.ToString());
            Assert.Equal(_endPoint, service.Uri);
        }

        [Fact]
        public async Task SendAsync_ShouldSendCorrectRequest()
        {
            // Arrange
            var options = new ClusterOptions();
            var queryOptions = new QueryOptions { Timeout = TimeSpan.FromSeconds(30) };
            var service = new AnalyticsService(options, _httpClientFactoryMock.Object, _endPoint, _loggerMock.Object, new DefaultSerializer());
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            _httpClientMock
                .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead, It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMessage);

            // Act
            var result = await service.SendAsync<object>("SELECT * FROM dataset", queryOptions);

            // Assert
            _httpClientMock.Verify(client => client.SendAsync(
                It.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == service.Uri &&
                    req.Content.ReadAsStringAsync().Result == "SELECT * FROM dataset"),
                HttpCompletionOption.ResponseHeadersRead,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAsync_ShouldAddPriorityHeader_WhenPriorityIsTrue()
        {
            // Arrange
            var options = new ClusterOptions();
            var queryOptions = new QueryOptions { Priority = true, Timeout = TimeSpan.FromSeconds(30) };
            var service = new AnalyticsService(options, _httpClientFactoryMock.Object, _endPoint, _loggerMock.Object, new DefaultSerializer());
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            _httpClientFactoryMock.Setup(factory => factory.Create()).Returns(new HttpClient(mockHandler.Object));


           /* _httpClientMock
                .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead, It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMessage);*/

            // Act
            var result = await service.SendAsync<object>("SELECT * FROM dataset", queryOptions);

            // Assert
            _httpClientMock.Verify(client => client.SendAsync(
                It.Is<HttpRequestMessage>(req =>
                    req.Headers.Contains("Analytics-Priority") &&
                    req.Headers.GetValues("Analytics-Priority").Contains("true")),
                HttpCompletionOption.ResponseHeadersRead,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAsync_ShouldThrowException_OnHttpError()
        {
            // Arrange
            var options = new ClusterOptions();
            var queryOptions = new QueryOptions { Timeout = TimeSpan.FromSeconds(30) };
            var service = new AnalyticsService(options, _httpClientFactoryMock.Object, _endPoint, _loggerMock.Object, new DefaultSerializer());

            _httpClientMock
                .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseHeadersRead, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("HTTP error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.SendAsync<object>("SELECT * FROM dataset", queryOptions));
        }
    }
}