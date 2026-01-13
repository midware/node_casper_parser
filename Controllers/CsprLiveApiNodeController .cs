using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NodeCasperParser.Models;
using Microsoft.AspNetCore.Cors;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Newtonsoft.Json;
using Casper.Network.SDK.Clients.CEP78;
using System.Text;
using Casper.Network.SDK.Types;
using System.Collections;
using Org.BouncyCastle.Math;
using System.Net.Http;
using System.Web; // for HttpUtility
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlTypes;

namespace NodeCasperParser.Controllers
{
    //  [Route("[controller]")]
    //  [ApiController]
   // [EnableCors("AllowOrigin")]
    public class CsprLiveController : ControllerBase
    {
        
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        string baseUrl = "", finalUrl = "";
        string publicKey, accountHash, transferId, account, blockHash, deployHash, blockHeight, eraId, proposer, order_by, order_direction, fields, page, limit, with_amounts_in_currency_id, deployCount, transferCount, timestamp;

        private readonly string expectedLicenseHash; // Replace with your actual license hash

        public CsprLiveController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }
        /*
        [HttpGet]
      //  [LicenseValidation("your-expected-license-hash")]
        public IActionResult Get()
        {
            // Your secure endpoint logic here
            return Ok("This endpoint is secured by license hash.");
        }
        */

        [BindProperties]
        public class GetRequest
        {
            public string publicKey { get; set; }
            public string blockHash { get; set; }
            public string deployHash { get; set; }
            public int? blockHeight { get; set; }
            public int? eraId { get; set; }
            public string proposer { get; set; }

            public int? page { get; set; }            
            public int? limit { get; set; }

            public string order_by { get; set; }
            public string order_direction { get; set; }
            public string fields { get; set; }
            public int? with_amounts_in_currency_id { get; set; }

            public string account { get; set; }
            public string transferId { get; set; }
            public string accountHash { get; set; }

            public string deployCount { get; set; }
            public string transferCount { get; set; }
            public string timestamp { get; set; }

            
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public GetRequest SetRequestData(GetRequest urlString)
        {
            if (urlString.publicKey != null)
                publicKey = "&publicKey=" + urlString.publicKey;
            if (urlString.blockHash != null)
                blockHash = "&blockHash=" + urlString.blockHash;
            if (urlString.blockHeight != null)
                blockHeight = "&blockHeight=" + urlString.blockHeight.ToString();
            if (urlString.eraId != null)
                eraId = "&eraId=" + urlString.eraId.ToString();
            if (urlString.proposer != null)
                proposer = urlString.proposer;
            if (urlString.page != null)
                page = "&page=" + urlString.page.ToString();
            if (urlString.limit != null)
                limit = "&limit=" + urlString.limit.ToString();
            if (urlString.order_by != null)
                order_by = "&order_by=" + urlString.order_by;
            if (urlString.order_direction != null)
                order_direction = "&order_direction=" + urlString.order_direction;
            if (urlString.fields != null)
                fields = "&fields=" + urlString.fields;
            if (urlString.with_amounts_in_currency_id != null)
                with_amounts_in_currency_id = "&with_amounts_in_currency_id=" + urlString.with_amounts_in_currency_id;
            if (urlString.account != null)
                account = "&account=" + urlString.account;
            if (urlString.accountHash != null)
                accountHash = "&accountHash=" + urlString.accountHash;
            if (urlString.transferId != null)
                transferId = "&transferId=" + urlString.transferId;
            if (urlString.deployHash != null)
                deployHash = "&deployHash=" + urlString.deployHash;
            if (urlString.deployCount != null)
                deployCount = "&deployCount=" + urlString.deployCount;
            if (urlString.transferCount != null)
                transferCount = "&transferCount=" + urlString.transferCount;
            if (urlString.timestamp != null)
                timestamp = "&timestamp=" + urlString.timestamp;

            return urlString;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("blocks")]
        // blocks?page=1&limit=10
        public async Task<IActionResult> GetBlocksFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);
                        
            baseUrl = "https://api.cspr.live/blocks?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                           //     logBugs lB = new();
                           //     lB.logBug(result.ToString());
                                //logBugs.logBug(result.ToString());
                                // return new JsonResult(baseUrls);
                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
             //   logBugs lB = new();
             //   lB.logBug(ex.Message);
                // Program.logBug(ex.ToString());
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("blocks/{urlString.blockHash}")]
        // blocks/e67f45ada02ffd2659b32ecbc46e2de09f3bd04dd2c9c8b5103e274585c2f064&page=1&limit=10
        public async Task<IActionResult> GetBlockFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/blocks/" + urlString.blockHash + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("blocks/{blockHash}/deploys")]
        // blocks/815c5b25fd59fc9e5f97da9496cb08f3246bfe79d47b307872c1cf8fcd1aa430/deploys
        // blocks/8d95101d79c1716f9c2b1b67f735c841fe3a044a1566268c1ff35dc9d22c0362/deploys?page=1&limit=10
 
        // /blocks/8d95101d79c1716f9c2b1b67f735c841fe3a044a1566268c1ff35dc9d22c0362/deploys?page=1&limit=10
        public async Task<IActionResult> GetBlocksDeploysFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/blocks/" + urlString.blockHash + "/deploys?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");
            
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("blocks/{blockHash}/transfers")]
        // blocks/815c5b25fd59fc9e5f97da9496cb08f3246bfe79d47b307872c1cf8fcd1aa430/transfers
        // blocks/8d95101d79c1716f9c2b1b67f735c841fe3a044a1566268c1ff35dc9d22c0362/transfers?page=1&limit=10
        public async Task<IActionResult> GetBlocksTransfersFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/blocks/" + urlString.blockHash + "/transfers?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("deploys/{deployHash}/transfers")]
        // deploys/5581a63bb15a980d5f5f167046d9916b4eda5203f1aa16b500f28dd6688dc5bf/transfers
        // deploys/5581a63bb15a980d5f5f167046d9916b4eda5203f1aa16b500f28dd6688dc5bf/transfers?page=1&limit=10
        public async Task<IActionResult> GetDeploysTransfersFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/deploys?" + urlString.deployHash + "/transfers?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("transfers")]
        // transfers?page=1&limit=10
        public async Task<IActionResult> GetTransfersFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/transfers?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                // return new JsonResult(baseUrls);
                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("deploys")]
        // transfers?page=1&limit=10
        public async Task<IActionResult> GetDeploysFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/deploys?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                // return new JsonResult(baseUrls);
                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        /// <summary>
        /// NAPISAĆ OBŁUGĘ API DLA: GET /accounts/:public_key/extended-deploys
        /// </summary>
        /// <param name="accountHash"></param>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        /// 

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("accounts/{publicKey}/deploys")]
        // accounts/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/deploys
        // accounts/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/deploys?page=1&limit=10
        public async Task<IActionResult> GetAccountDeploysFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/accounts/" + urlString.publicKey + "/deploys?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("accounts/{accountHash}/transfers")]
        // accounts/82dee24436178b6d45ba241743f44e456ba64af0abc53f35f7cc3ab39e25fd7c/transfers
        // accounts/82dee24436178b6d45ba241743f44e456ba64af0abc53f35f7cc3ab39e25fd7c/transfers?page=1&limit=10
        public async Task<IActionResult> GetAccountTransfersFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/accounts/" + urlString.accountHash + "/transfers?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("validators/{publicKey}/blocks")]
        // validators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/blocks
        // validators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/blocks?page=1&limit=10
        public async Task<IActionResult> GetValidatorBlocksFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/validators/" + urlString.publicKey + "/blocks?" + publicKey + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("validators/{publicKey}/total-rewards")]
        // validators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/total-rewards
        public async Task<JsonResult> GetValidatorTotalRewardsFromCsprLive(string publicKey)
        {
            string baseUrl = "";

            if (publicKey != null)
            {
                baseUrl = "https://api.cspr.live/validators/" + publicKey + "/total-rewards";
            }
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("validators/{publicKey}/rewards")]
        // validators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/rewards?with_amounts_in_currency_id=1
        // validators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/rewards?with_amounts_in_currency_id=1&page=1&limit=10
        public async Task<IActionResult> GetValidatorRewardsFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/validators/" + urlString.publicKey + "/rewards?" + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");
                       
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("delegators/{publicKey}/total-rewards")]
        // delegators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/total-rewards
               public async Task<JsonResult> GetDelegatorTotalRewardsRewardsFromCsprLive(string publicKey)
        {
            string baseUrl = "";

            if (publicKey != null)
            {
                baseUrl = "https://api.cspr.live/delegators/" + publicKey + "/total-rewards";
            }
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("delegators/{publicKey}/rewards")]
        // delegators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/rewards?with_amounts_in_currency_id=1
        // delegators/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed/rewards?with_amounts_in_currency_id=1&page=1&limit=10
        public async Task<IActionResult> GetDelegatorRewardsFromCsprLive(GetRequest urlString)
        {
            SetRequestData(urlString);

            baseUrl = "https://api.cspr.live/delegators/" + urlString.publicKey + "/rewards?" + blockHash + blockHeight + eraId + proposer + page + limit + order_by +
                order_direction + fields + with_amounts_in_currency_id + account + accountHash + transferId + deployHash + deployCount + transferCount + timestamp;
            finalUrl = baseUrl.Replace("?&", "?");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(finalUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);

                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return new JsonResult(ex);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public JObject SetPropertyContent(JObject source, string name, object content)
        {
            var prop = source.Property(name);
            if (prop == null)
            {
                prop = new JProperty(name, content);
                source.Add(prop);
            }
            else
            {
                prop.Value = JContainer.FromObject(content);
            }
            return source;
        }

        // GET GetValidatorRewards/{publicKey}&{with_amounts_in_currency_id}
        // Example: GET GetValidatorRewards/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed&with_amounts_in_currency_id=2
        // Input type: string
        // Input value: public key, currency id
        // Return type: Json string
        // Return value: validator rewards in selected currency, "error type" if error, "null" if no data
        // Description: - Return walidator rewards calculated in selected currency
        // Currency value: USD=1, EUR=2, JPY=3, GBP=4, CAD=5, CHF=6, CNY=7, HKD=8
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetValidatorRewards/{publicKey}&{with_amounts_in_currency_id}")]
        public async Task<JsonResult> GetValidatorRewards(string publicKey, string with_amounts_in_currency_id)
        {
            // dugi wariant // baseUrl = "https://api.cspr.live/validators/" + publicKey + "/total-delegator-rewards";

            string baseUrl = "https://api.cspr.live/validators/" + publicKey + "/rewards?" + with_amounts_in_currency_id;
            Uri url = new Uri(baseUrl);

            string paramCurrency = HttpUtility.ParseQueryString(url.Query).Get("with_amounts_in_currency_id");
            string valueCurrency = string.Empty;

            if (paramCurrency=="1")
                valueCurrency = "USD";
            if (paramCurrency == "2")
                valueCurrency = "EUR";
            if (paramCurrency == "3")
                valueCurrency = "JPY";
            if (paramCurrency == "4")
                valueCurrency = "GBP";
            if (paramCurrency == "5")
                valueCurrency = "CAD";
            if (paramCurrency == "6")
                valueCurrency = "CHF";
            if (paramCurrency == "7")
                valueCurrency = "CNY";
            if (paramCurrency == "8")
                valueCurrency = "HKD";
            
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);
                             
                                var jsonString = dataObj.ToString();

                                return new JsonResult(jsonString);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if(debugMode)
                    Console.WriteLine(ex);

                return new JsonResult(ex.ToString());
                
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<double> GetCsprApy()
        {
            string baseUrl = "https://api.cspr.live/supply";

            string sqlDataSource = _configuration.GetConnectionString("psqlServer");
            string rpcCasperNode = _configuration.GetConnectionString("rpcUrl");

            DataTable StakingSumTable = new DataTable();

            double result = 8;
            double returnApy = 0;
            double sumStaking = 0;
            double totalSupply = 0;

            string query = @"SELECT SUM(staked_amount) AS staked_amount FROM casper_node_staking";

            NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource);

            try
            {
                NpgsqlDataReader myReader;

                await myCon.OpenAsync().ConfigureAwait(false);
                using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                {
                    myReader = await myCommand.ExecuteReaderAsync();
                    StakingSumTable.Load(myReader);

                    myReader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (myCon.State == ConnectionState.Open)
                    await myCon.CloseAsync();
            }

            try
            {
                foreach (var row in StakingSumTable.AsEnumerable())
                {
                    sumStaking = Convert.ToDouble(row.Field<decimal>("staked_amount"));
                }
            }
            catch
            {
                sumStaking = 0;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);
                                JObject addValue = (JObject)dataObj["data"];
                                ;
                                JObject channel = (JObject)dataObj;
                                totalSupply = Convert.ToDouble(addValue.Property("total").Value);

                                // apy = ((totalsupply * 0.08) / sumStaking) * 100;
                                returnApy = ((totalSupply * 0.08) / sumStaking) * 100;

                                addValue["apy"] = returnApy;
                                var jsonString = dataObj.ToString();

                                result = returnApy;

                                return result;
                            }
                            else
                            {
                                return 8;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex.ToString());
                return 8;
            }
        }

        // GET /supply
        // Example: GET /supply
        // Input type: -
        // Input value: -
        // Return type: Json string, "error type" if error, "null" if no data
        // Return value: total supply CSPR in json data
        // Description: - Return total supply CSPR
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("supply")]
        public async Task<JsonResult> GetCsprTotalSupply()
        {
            string baseUrl = "https://api.cspr.live/supply";
            
            string sqlDataSource = _configuration.GetConnectionString("psqlServer");
            string rpcCasperNode = _configuration.GetConnectionString("rpcUrl");

            DataTable StakingSumTable = new DataTable();

            double returnApy = 0;
            double sumStaking = 0;
            double totalSupply = 0;

            string query = @"SELECT SUM(staked_amount) AS staked_amount FROM casper_node_staking";

            NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource);

            try
            {
                NpgsqlDataReader myReader;

                await myCon.OpenAsync().ConfigureAwait(false);
                using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                {
                    myReader = await myCommand.ExecuteReaderAsync();
                    StakingSumTable.Load(myReader);

                    myReader.Close();
                }
            }
            catch (Exception ex)
            {
                returnApy = 0;
                Console.WriteLine(ex);
            }
            finally
            {
                if (myCon.State == ConnectionState.Open)
                    await myCon.CloseAsync();
            }

            try
            {
                foreach (var row in StakingSumTable.AsEnumerable())
                {
                    sumStaking = Convert.ToDouble(row.Field<decimal>("staked_amount"));
                }
            }
            catch
            {
                sumStaking = 0;
            }
            
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();
                            
                            if (data != null)
                            {
                                JObject dataObj = JObject.Parse(data);
                                JObject addValue = (JObject)dataObj["data"];
                                                                                                ;
                                JObject channel = (JObject)dataObj;
                                totalSupply = Convert.ToDouble(addValue.Property("total").Value);

                                // apy = ((totalsupply * 0.08) / sumStaking) * 100;
                                returnApy = ((totalSupply * 0.08) / sumStaking) *100;

                                addValue["apy"] = returnApy;
                                var jsonString = dataObj.ToString();

                                var result = JsonConvert.DeserializeObject(jsonString);

                                return new JsonResult(result);
                            }
                            else
                            {
                                return new JsonResult("null");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if(debugMode)
                    Console.WriteLine(ex.ToString());
                return new JsonResult(ex);
            }
        }
    }
}
