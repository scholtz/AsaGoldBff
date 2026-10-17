using AsaGoldBff.Model.Cache;
using AsaGoldBff.Model.Config;
using AsaGoldBff.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;

namespace AsaGoldBff.Controllers
{
    //[Authorize]
    [Route("ipfs")]
    public class IpfsController : ControllerBase
    {
        private readonly ILogger<IpfsController> logger;
        private readonly IMemoryCache cache;
        private readonly IpfsOptions ipfsOptions;
        private readonly TimeSpan CacheTime = TimeSpan.FromDays(30);
        public string[] IpfsGateways { get; set; } = new string[] { "gateway.pinata.cloud", "gw3.io", "cloudflare-ipfs.com", "gateway.ipfs.io", "ipfs.io", "dweb.link" };
        public IpfsController(IMemoryCache cache, ILogger<IpfsController> logger, IOptions<IpfsOptions> ipfsOptions)
        {
            this.logger = logger;
            this.cache = cache;
            this.ipfsOptions = ipfsOptions.Value;
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

            // Try private IPFS server first
            if (!string.IsNullOrEmpty(ipfsOptions.IpfsUrl) && !string.IsNullOrEmpty(ipfsOptions.User) && !string.IsNullOrEmpty(ipfsOptions.Password))
            {
                try
                {
                    logger.LogInformation($"Loading from private IPFS {ipfsOptions.IpfsUrl}/ipfs/{hash}");
                    using var client = new RestClient(new RestClientOptions(ipfsOptions.IpfsUrl)
                    {
                        Timeout = TimeSpan.FromMilliseconds(1000),
                        Authenticator = new HttpBasicAuthenticator(ipfsOptions.User, ipfsOptions.Password)
                    });
                    var request = new RestRequest($"/ipfs/{hash}", Method.Get);
                    request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    var response = await client.ExecuteAsync(request);
                    if (response.IsSuccessful && response.RawBytes != null)
                    {
                        var contentType = response.ContentType ?? "application/octet-stream";

                        cache.Set<Model.Cache.Ipfs>(hash, new Model.Cache.Ipfs() { ContentType = contentType, Data = response.RawBytes }, CacheTime);

                        try
                        {
                            if (!System.IO.Directory.Exists("ipfs"))
                            {
                                System.IO.Directory.CreateDirectory("ipfs");
                            }
                            System.IO.File.WriteAllBytes($"ipfs/{hash}.data", response.RawBytes);
                            System.IO.File.WriteAllText($"ipfs/{hash}.content-type", contentType);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.Message);
                        }

                        var stream = new MemoryStream(response.RawBytes);
                        return new FileStreamResult(stream, contentType);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error loading from private IPFS: {ex.Message}");
                }
            }

            foreach (var ipfs in IpfsGateways)
            {
                try
                {
                    logger.LogInformation($"Loading https://{ipfs}/ipfs/{hash}");
                    using var client = new RestClient($"https://{ipfs}");
                    var request = new RestRequest($"/ipfs/{hash}", Method.Get);
                    request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
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

                    // Pin to private IPFS server
                    if (!string.IsNullOrEmpty(ipfsOptions.ApiUrl) && !string.IsNullOrEmpty(ipfsOptions.User) && !string.IsNullOrEmpty(ipfsOptions.Password))
                    {
                        try
                        {
                            logger.LogInformation($"Pinning to private IPFS {ipfsOptions.ApiUrl}/api/v0/add");
                            using var pinClient = new RestClient(new RestClientOptions(ipfsOptions.ApiUrl)
                            {
                                Authenticator = new HttpBasicAuthenticator(ipfsOptions.User, ipfsOptions.Password)
                            });
                            var pinRequest = new RestRequest("/api/v0/add", Method.Post);
                            pinRequest.AddFile("file", response.RawBytes, hash, response.ContentType ?? "application/octet-stream");
                            var pinResponse = await pinClient.ExecuteAsync(pinRequest);
                            if (pinResponse.IsSuccessful)
                            {
                                logger.LogInformation($"Successfully pinned {hash} to private IPFS");
                            }
                            else
                            {
                                logger.LogWarning($"Failed to pin {hash} to private IPFS: {pinResponse.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Error pinning to private IPFS: {ex.Message}");
                        }
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
