using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace AsaGoldBffTests
{
    /// <summary>
    /// Boots the real app in-process (no external services) to catch DI, configuration and Swagger regressions.
    /// </summary>
    public class StartupSmokeTests
    {
        private WebApplicationFactory<AsaGoldBff.Program> factory = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            factory = new WebApplicationFactory<AsaGoldBff.Program>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            factory.Dispose();
        }

        [Test]
        public async Task Swagger_ShouldBeGenerated()
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("/swagger/v1/swagger.json");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            Assert.That(body, Does.Contain("/api/v1/account"));
            Assert.That(body, Does.Contain("/ipfs/{hash}"));
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var scheme = doc.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("oauth2");
            Assert.That(scheme.GetProperty("name").GetString(), Is.EqualTo("Authorization"));
            var account = doc.RootElement.GetProperty("paths").GetProperty("/api/v1/account").GetProperty("get");
            Assert.That(account.GetProperty("security").GetRawText(), Does.Contain("oauth2"));
        }

        [Test]
        public async Task ProtectedEndpoint_ShouldRejectAnonymous()
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("/api/v1/account");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
