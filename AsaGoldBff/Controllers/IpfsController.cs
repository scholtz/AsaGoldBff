using AsaGoldBff.Model.Cache;
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
        private readonly ILogger<IpfsController> logger;
        private readonly IMemoryCache cache;
        private readonly TimeSpan CacheTime = TimeSpan.FromDays(30);
        public string[] IpfsGateways { get; set; } = new string[] { "gw3.io", "cloudflare-ipfs.com", "gateway.ipfs.io", "ipfs.io", "dweb.link" };
        public IpfsController(IMemoryCache cache, ILogger<IpfsController> logger)
        {
            this.logger = logger;
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
            try
            {
                if (!System.IO.Directory.Exists("ipfs"))
                {
                    System.IO.Directory.CreateDirectory("ipfs");
                }
                if (System.IO.File.Exists($"ipfs/{hash}.data"))
                {
                    if (System.IO.File.Exists($"ipfs/{hash}.content-type"))
                    {
                        var bytes = System.IO.File.ReadAllBytes($"ipfs/{hash}.data");
                        var content = System.IO.File.ReadAllText($"ipfs/{hash}.content-type");

                        cache.Set<Model.Cache.Ipfs>(hash, new Model.Cache.Ipfs() { ContentType = content, Data = bytes }, CacheTime);

                        var stream = new MemoryStream(bytes);
                        return new FileStreamResult(stream, content);

                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            foreach (var ipfs in IpfsGateways)
            {
                try
                {
                    logger.LogInformation($"Loading https://{ipfs}/ipfs/{hash}");
                    var client = new RestClient($"https://{ipfs}");
                    var request = new RestRequest($"/ipfs/{hash}", Method.GET);
                    request.AddHeader("User-Agent", "\r\nMozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    var response = await client.ExecuteAsync(request);
                    if (!response.IsSuccessful)
                    {
                        logger.LogInformation($"Request not successful {response.StatusCode} {response.ContentLength}");
                        continue;
                    }
                    var contentType = response.Headers.Where(h => h?.Name?.ToLower() == "content-type");

                    cache.Set<Model.Cache.Ipfs>(hash, new Model.Cache.Ipfs() { ContentType = response.ContentType, Data = response.RawBytes }, CacheTime);

                    try
                    {
                        if (!System.IO.Directory.Exists("ipfs"))
                        {
                            System.IO.Directory.CreateDirectory("ipfs");
                        }
                        System.IO.File.WriteAllBytes($"ipfs/{hash}.data", response.RawBytes);
                        System.IO.File.WriteAllText($"ipfs/{hash}.content-type", response.ContentType);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }

                    var stream = new MemoryStream(response.RawBytes);
                    return new FileStreamResult(stream, response.ContentType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }

            }
            throw new Exception("Not found");
        }
    }
}
