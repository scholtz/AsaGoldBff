using AsaGoldBff.Controllers;
using AsaGoldBff.Model.Cache;
using AsaGoldBff.Model.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace AsaGoldBffTests
{
    [TestFixture]
    public class IpfsControllerTests
    {
        private IpfsController _controller;
        private MemoryCache _cache;
        private Mock<ILogger<IpfsController>> _loggerMock;
        private IpfsOptions _ipfsOptions;

        [SetUp]
        public void Setup()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<IpfsController>>();
            _ipfsOptions = new IpfsOptions
            {
                IpfsUrl = "https://ipfs.biatec.io",
                ApiUrl = "https://ipfs-api.biatec.io",
                User = "ipfsuser",
                Password = ""
            };
            var optionsMock = new Mock<IOptions<IpfsOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_ipfsOptions);

            _controller = new IpfsController(_cache, _loggerMock.Object, optionsMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _cache.Dispose();
        }

        [Test]
        public async Task Get_ShouldReturnFileFromCache_WhenAvailable()
        {
            // Arrange
            var hash = "testHash";
            var cachedData = new Ipfs { ContentType = "application/json", Data = new byte[] { 1, 2, 3 } };
            _cache.Set(hash, cachedData);

            // Act
            var result = await _controller.Get(hash);

            // Assert
            Assert.That(result, Is.InstanceOf<FileStreamResult>());
            var fileResult = result as FileStreamResult;
            Assert.That(fileResult!.ContentType, Is.EqualTo("application/json"));
        }

        [Test]
        public async Task Get_ShouldReturnFileFromDisk_AndPopulateCache()
        {
            var hash = "diskHash" + Guid.NewGuid().ToString("N");
            var dir = Path.Combine(Path.GetTempPath(), "ipfs-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var previous = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(dir);
                Directory.CreateDirectory("ipfs");
                File.WriteAllBytes($"ipfs/{hash}.data", new byte[] { 4, 5, 6 });
                File.WriteAllText($"ipfs/{hash}.content-type", "image/png");

                var result = await _controller.Get(hash);

                Assert.That(result, Is.InstanceOf<FileStreamResult>());
                Assert.That(result.ContentType, Is.EqualTo("image/png"));
                Assert.That(_cache.TryGetValue(hash, out Ipfs? cached), Is.True);
                Assert.That(cached!.Data, Is.EqualTo(new byte[] { 4, 5, 6 }));
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                Directory.Delete(dir, true);
            }
        }

        // Additional tests can be added for other scenarios
    }
}