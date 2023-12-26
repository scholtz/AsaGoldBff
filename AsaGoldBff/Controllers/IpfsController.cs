using AsaGoldBff.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RestSharp;

namespace AsaGoldBff.Controllers
{
    //[Authorize]
    [Route("ipfs")]
    public class IpfsController : ControllerBase
    {
        private readonly IMemoryCache cache;
        private readonly TimeSpan CacheTime = TimeSpan.FromDays(30);
        public string[] IpfsGateways { get; set; } = new string[] { "gw3.io", "cloudflare-ipfs.com", "gateway.ipfs.io", "ipfs.io", "dweb.link" };
        public IpfsController(IMemoryCache cache)
        {
            this.cache = cache;
        }
        /// <summary>
        /// Returns the IPFS file
        /// </summary>
        /// <returns></returns>
        [ResponseCache(Duration = 1000000)]
        [HttpGet("{hash}")]
        public async Task<FileResult> Get(string hash)
        {
            if (cache.TryGetValue(hash, out var file))
            {
                var typed = file as Model.Cache.Ipfs;
                if (typed != null)
                {
                    var stream = new MemoryStream(typed.Data);
                    return new FileStreamResult(stream, typed.ContentType);
                }
            }

            foreach (var ipfs in IpfsGateways)
            {
                var client = new RestClient($"https://{ipfs}");
                var request = new RestRequest($"/ipfs/{hash}", Method.GET);
                request.AddHeader("User-Agent", "\r\nMozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessful)
                {
                    continue;
                }
                var contentType = response.Headers.Where(h => h?.Name?.ToLower() == "content-type");

                cache.Set<Model.Cache.Ipfs>(hash, new Model.Cache.Ipfs() { ContentType = response.ContentType, Data = response.RawBytes }, CacheTime);

                var stream = new MemoryStream(response.RawBytes);
                return new FileStreamResult(stream, response.ContentType);

            }
            throw new Exception("Not found");
        }
    }
}
