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
using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Newtonsoft.Json;
using Casper.Network.SDK.Clients.CEP78;
using System.Text;
using NodeCasperParser.Models;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;


using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.JsonRpc.ResultTypes;
using Casper.Network.SDK.Types;
using NodeCasperParser.Services;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using MailKit.Search;

namespace NodeCasperParser.Controllers
{
    

      [Route("[controller]")]
  //  [ApiController]
 //   [EnableCors("AllowOrigin")]
    public class offchainController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public offchainController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }


        // GET GetEraInfoByBlockHash/{blockHash}
        // Example: GetEraInfoByBlockHash/985a2191cc94e2abaaa88081d78dedb6fc1445043e44b97c280b529154ca4946
        // Input type: string
        // Input value: block hash
        // Return type: Json string
        // Return value: sum of validators rewards, -1 if error
        // Description: Function return Era info from block hash.
        //              Returning value only when era is ended.
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetEraInfoByBlockHash/{blockHash}")]
        public async Task<string> GetEraInfoByBlockHash(string blockHash) // DZIAŁA
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
              
                var error = (await casperSdk.GetEraInfoBySwitchBlock(blockHash)).Parse();
                // var getEraInfo = await casperSdk.GetEraInfoBySwitchBlock(blockHash);
                if (error.EraSummary.EraId != 0)
                {
                    var getEraInfo = await casperSdk.GetEraInfoBySwitchBlock(blockHash);
                    var ddd = getEraInfo.Result.GetRawText();
                    var eraId = getEraInfo.Parse().EraSummary.EraId;
                    var result = JsonConvert.DeserializeObject(ddd.ToString());

                    if (debugMode)
                    {
                        Console.WriteLine("ERA INFO 1: " + result.ToString());
                    }
                    return result.ToString();
                }
                else
                {
                    var getEraInfo2 = casperSdk.GetEraSummary(blockHash);
                    // var ddd = getEraInfo.Result.GetRawText();
                    var result = JsonConvert.DeserializeObject(getEraInfo2.ToString());

                    if (debugMode)
                    {
                        Console.WriteLine("ERA INFO Summary z Catch: " + result.ToString());
                    }

                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine("Error: " + ex.ToString());
                }

                return ex.ToString();
            }
        }

        // GET /GetLastEra
        // Example: /GetLastEra
        // Input type: -
        // Input value: -
        // Return type: integer
        // Return value: Era Id, -1 if error
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("last_era")]
        public int GetLastEra()
        {
            try
            {  
                Casper.Network.SDK.NetCasperClient client_test = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockResult = client_test.GetBlock().Result.Parse();


                int eraId = Convert.ToInt32(lastBlockResult.Block.Header.EraId);
                if (debugMode)
                    Console.WriteLine("Ended Era Id: " + eraId.ToString());

                return eraId;

            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex.ToString());
                return -1;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("contract/onchain/{contractHash}")]
        public async Task<ActionResult> GetOnchainContract(string contractHash)
        {
            NodeCasperParser.Services.Contracts cs = new NodeCasperParser.Services.Contracts();
            var result = await cs.GetContract(contractHash);
            return Ok(result);
        }



        // GET GetBlockHeight/{blockHash}
        // Example: /GetBlockHeight/b38489513eb12f6730c65806917a2ff55d1e027de50805783778eed81527d4d8
        // Input type: string
        // Input value: block hash
        // Return type: integer
        // Return value: block heigh, -1 if error
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetBlockHeight/{blockHash}")]
        public async Task<int> GetBlockHeight(string blockHash) // DZIAŁA
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                string beSureItsString = Convert.ToString(blockHash);
                var getBlock = await casperSdk.GetBlock(beSureItsString);

                var getRawBlock = getBlock.Parse().Block.Header.Height;

                int returnValue = Convert.ToInt32(getRawBlock);

                if (debugMode)
                {
                    Console.WriteLine(returnValue.ToString());
                }

                return returnValue;
            }
            catch(Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine(ex.ToString());
                }

                return -1;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public string InsertValue(string value)
        {
            try
            {
                if (value != null)
                {
                    return value;
                }
                else
                {
                    return "null";
                }
            }
            catch(Exception)
            {
                return "error";
            }
        }

        // GET GetBlock   - load newest block as default
        // GET GET GetBlock?block=
        // Example: GetBlock
        // Example: GetBlock?block=1176034
        // Example: GetBlock?block=93ae7184e53b3a7ee804baed517c9b3070a16fc7f882e2ede73c7a10eb71d9fd
        // block=blockHeigh or block=BlockHash
        // Input type: integer, string
        // Input value: block heigh, block hash
        // Return type: string
        // Return value: Json block data, "error type" if error
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetBlock")]
        public async Task<string> GetBlock(string block) // DZIAŁA
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                StringWriter sw = new StringWriter();
                JsonTextWriter writer = new JsonTextWriter(sw);

                if (block != null)
                {
                    int blockHeigh;

                    if (Int32.TryParse(block, out blockHeigh))
                    {
                        var getBlock = await casperSdk.GetBlock(blockHeigh);

                        writer.WriteStartArray();
                        writer.WriteStartObject();

                        string blockHash = getBlock.Parse().Block.Header.BodyHash;
                        string parentHash = getBlock.Parse().Block.Header.ParentHash;
                        string timestamp = getBlock.Parse().Block.Header.Timestamp;
                        string eraId = getBlock.Parse().Block.Header.EraId.ToString();
                           // string EraEnd = InsertValue(getBlock.Parse().Block.Header.EraEnd.ToString());
                        string proposer = getBlock.Parse().Block.Body.Proposer.ToString();
                        string state = getBlock.Parse().Block.Header.StateRootHash.ToLower();
                        string deployCount = getBlock.Parse().Block.Body.DeployHashes.Count.ToString();
                        string transferCount = getBlock.Parse().Block.Body.TransferHashes.Count.ToString();
                        string height = getBlock.Parse().Block.Header.Height.ToString();
                        string blockHeight = getBlock.Parse().Block.Header.Height.ToString();
                        //   string deploys = getBlock.Parse().Block.Body.DeployHashes.ToString();

                        writer.WritePropertyName("blockHash");
                        writer.WriteValue(blockHash.ToString());

                        writer.WritePropertyName("parentHash");
                        writer.WriteValue(parentHash.ToString());

                        writer.WritePropertyName("timestamp");
                        writer.WriteValue(timestamp.ToString());

                        writer.WritePropertyName("eraId");
                        writer.WriteValue(eraId.ToString());

                       //   writer.WritePropertyName("EraEnd");
                       //   writer.WriteValue(EraEnd.ToString());

                        writer.WritePropertyName("proposer");
                        writer.WriteValue(proposer.ToString());

                        writer.WritePropertyName("state");
                        writer.WriteValue(state.ToString());

                        writer.WritePropertyName("deployCount");
                        writer.WriteValue(deployCount.ToString());

                        writer.WritePropertyName("transferCount");
                        writer.WriteValue(transferCount.ToString());

                        writer.WritePropertyName("height");
                        writer.WriteValue(height.ToString());

                        writer.WritePropertyName("blockHeight");
                        writer.WriteValue(blockHeight.ToString());

                        //  writer.WritePropertyName("deploys");
                        //  writer.WriteValue(deploys.ToString());

                        writer.WriteEndObject();
                        writer.WriteEndArray();

                    }
                    else
                    {
                        var getBlock = await casperSdk.GetBlock(block.ToString());

                        writer.WriteStartArray();
                        writer.WriteStartObject();

                        string blockHash = getBlock.Parse().Block.Header.BodyHash;
                        string parentHash = getBlock.Parse().Block.Header.ParentHash;
                        string timestamp = getBlock.Parse().Block.Header.Timestamp;
                        string eraId = getBlock.Parse().Block.Header.EraId.ToString();
                     //   string EraEnd = InsertValue(getBlock.Parse().Block.Header.EraEnd.ToString());
                        string proposer = getBlock.Parse().Block.Body.Proposer.ToString();
                        string state = getBlock.Parse().Block.Header.StateRootHash.ToLower();
                        string deployCount = getBlock.Parse().Block.Body.DeployHashes.Count.ToString();
                        string transferCount = getBlock.Parse().Block.Body.TransferHashes.Count.ToString();
                        string height = getBlock.Parse().Block.Header.Height.ToString();
                        string blockHeight = getBlock.Parse().Block.Header.Height.ToString();

                        //  string RandomBit = getBlock.Parse().Block.Header.RandomBit.ToString();
                        //  string ProtocolVersion = getBlock.Parse().Block.Header.ProtocolVersion.ToString();

                        //   string deploys = getBlock.Parse().Block.Body.DeployHashes.ToString();

                        writer.WritePropertyName("blockHash");
                        writer.WriteValue(blockHash.ToString());

                        writer.WritePropertyName("parentHash");
                        writer.WriteValue(parentHash.ToString());

                        writer.WritePropertyName("timestamp");
                        writer.WriteValue(timestamp.ToString());

                        writer.WritePropertyName("eraId");
                        writer.WriteValue(eraId.ToString());

                        //       writer.WritePropertyName("EraEnd");
                        //       writer.WriteValue(EraEnd.ToString());

                        writer.WritePropertyName("proposer");
                        writer.WriteValue(proposer.ToString());

                        writer.WritePropertyName("state");
                        writer.WriteValue(state.ToString());

                        writer.WritePropertyName("deployCount");
                        writer.WriteValue(deployCount.ToString());

                        writer.WritePropertyName("transferCount");
                        writer.WriteValue(transferCount.ToString());

                        writer.WritePropertyName("height");
                        writer.WriteValue(height.ToString());

                        writer.WritePropertyName("blockHeight");
                        writer.WriteValue(blockHeight.ToString());

                        //  writer.WritePropertyName("deploys");
                        //  writer.WriteValue(deploys.ToString());

                        writer.WriteEndObject();
                        writer.WriteEndArray();
                    }

                    if (debugMode)
                        Console.WriteLine("lastBlockHeigh: " + sw.ToString());
                    return sw.ToString();
                }
                else
                {
                    // JEŻELI użytko GetBlock bez parametru bloku to załaduj najnowszy blok.
                   
                    Casper.Network.SDK.NetCasperClient client = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                    var lastBlock = client.GetBlock().Result.Parse();

                    writer.WriteStartArray();
                    writer.WriteStartObject();

                    string blockHash = lastBlock.Block.Header.BodyHash;
                    string parentHash = lastBlock.Block.Header.ParentHash;
                    string timestamp = lastBlock.Block.Header.Timestamp.ToString();
                    string eraId = lastBlock.Block.Header.EraId.ToString();
                     //      string EraEnd = InsertValue(lastBlock.result.block.header.era_end.ToString());
                    string proposer = lastBlock.Block.Body.Proposer.ToString();
                    string state = lastBlock.Block.Header.StateRootHash.ToLower();
                    string deployCount = lastBlock.Block.Body.DeployHashes.Count.ToString();
                    string transferCount = lastBlock.Block.Body.TransferHashes.Count.ToString();
                    string height = lastBlock.Block.Header.Height.ToString();
                    string blockHeight = lastBlock.Block.Header.Height.ToString();
                    //   string deploys = getBlock.Parse().Block.Body.DeployHashes.ToString();

                    writer.WritePropertyName("blockHash");
                    writer.WriteValue(blockHash.ToString());

                    writer.WritePropertyName("parentHash");
                    writer.WriteValue(parentHash.ToString());

                    writer.WritePropertyName("timestamp");
                    writer.WriteValue(timestamp.ToString());

                    writer.WritePropertyName("eraId");
                    writer.WriteValue(eraId.ToString());

                    //        writer.WritePropertyName("EraEnd");
                    //        writer.WriteValue(EraEnd.ToString());

                    writer.WritePropertyName("proposer");
                    writer.WriteValue(proposer.ToString());

                    writer.WritePropertyName("state");
                    writer.WriteValue(state.ToString());

                    writer.WritePropertyName("deployCount");
                    writer.WriteValue(deployCount.ToString());

                    writer.WritePropertyName("transferCount");
                    writer.WriteValue(transferCount.ToString());

                    writer.WritePropertyName("height");
                    writer.WriteValue(height.ToString());

                    writer.WritePropertyName("blockHeight");
                    writer.WriteValue(blockHeight.ToString());

                    //  writer.WritePropertyName("deploys");
                    //  writer.WriteValue(deploys.ToString());

                    writer.WriteEndObject();
                    writer.WriteEndArray();

                    if (debugMode)
                        Console.WriteLine("lastBlockHeigh: " + sw.ToString());
                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());
                return "Error: " + ex.ToString();

            }
        }
                
        
               
        

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<Casper.Network.SDK.Types.Account> GetAccountInfo2(Casper.Network.SDK.Types.PublicKey publicKey)
        {
            var client = new Casper.Network.SDK.NetCasperClient(_configuration.GetConnectionString("rpcUrl"));// CasperServiceByJsonRPC(nodeAddress);
            var stateRootHash = await client.GetStateRootHash();
            var accountHash = publicKey.ToAccountHex();
            var blockState = await client.QueryGlobalStateWithBlockHash(stateRootHash, accountHash);
            Console.WriteLine(blockState.Parse().StoredValue.Account);

            return blockState.Parse().StoredValue.Account;
        }

        // GET GetAccountInfo/{publicKey}
        // Example: GetAccountInfo/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed
        // Input type: string
        // Input value: public key
        // Return type: Json string
        // Return value: user account info, "error type" if error
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetAccountInfo/{publicKey}")] // GetAccountInfo/020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed
        public JsonResult GetAccountInfo(string publicKey) // Działa
        {
            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var getInfo = client.GetAccountInfo(publicKey);

                if (debugMode)
                {
                    Console.WriteLine(getInfo.ToString());
                }

                return new JsonResult(getInfo);
            }
            catch(Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());
                return new JsonResult("Error: " + ex.ToString());
            }
        }

        // GET GetValidatorChanges
        // Example: GetValidatorChanges
        // Input type: -
        // Input value: -
        // Return type: Json string
        // Return value: user account info, "error type" if error
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetValidatorChanges")]
        public async Task<JsonResult> GetValidatorChanges()// działa
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                var rpc = await casperSdk.GetValidatorChanges();
                var getValidatorChanges = rpc.Result.GetRawText();
                var result = JsonConvert.DeserializeObject(getValidatorChanges.ToString());

                if (debugMode)
                {
                    Console.WriteLine(result.ToString());
                }

                return new JsonResult(result);
            }
            catch(Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine(ex.ToString());
                }
                return new JsonResult("Error: " + ex.ToString());
            }
        }

        // GET GetAuctionInfo
        // Example: GetAuctionInfo
        // Input type: -
        // Input value: -
        // Return type: Json string
        // Return value: user account info, "error type" if error
        // Description: Function return all information about validators bid, staking etc.
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetAuctionInfo")]
        public async Task<string> GetAuctionInfo() // Działa
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                var getAuctionInfo = await casperSdk.GetAuctionInfo();
                                
                var auctionInfo = getAuctionInfo.Result.GetRawText();

                if (debugMode)
                {
                    Console.WriteLine(JsonConvert.DeserializeObject(auctionInfo).ToString());
                }
                var result = JsonConvert.DeserializeObject(auctionInfo);

                return result.ToString();
            }
            catch(Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine(ex.ToString());
                }
                return "Error: " + ex.ToString();

            }
        }








        // GET GetTotalStakedByDelegators/{blockHash}
        // Example: GetTotalStakedByDelegators/346cb02e0ac1d8a3f458943f0fa22d5387b040534ee9d656c08117b145e60245
        // Total stake pomniejszony o Total Validator Rewards (daje staked przez delegatorów?)
        // Input type: string
        // Input value: block hash
        // Return type: double
        // Return value: sum of self staked, -1 if error
        // Description: Function return total staked cspr of validators (excluded delegators).
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetTotalStakedByDelegators/{blockHash}")]
        public async Task<double> GetTotalStakedByDelegators(string blockHash) // NIE DZIAŁA
        {
            try
            {
                double sumStaking = 0;
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                var auctionInfo = await casperSdk.GetAuctionInfo(blockHash);

                var eraSummary = auctionInfo.Parse().AuctionState.Bids;

                foreach (var group in eraSummary)
                {
                    var eraStaked1 = group.Delegators.Sum(a => (double)a.StakedAmount); //.StakedAmount);

                    eraStaked1 /= 1_000_000_000;

                    sumStaking += eraStaked1;

                    Console.WriteLine(eraStaked1.ToString());

                }

                return sumStaking;
            }
            catch(Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());
                return -1;
            }
        }

        // GET GetValidatorRewardsByEraBlockHash/{blockHash}
        // Example: GetValidatorRewardsByEraBlockHash/985a2191cc94e2abaaa88081d78dedb6fc1445043e44b97c280b529154ca4946
        // Input type: string
        // Input value: block hash
        // Return type: Json string
        // Return value: sum of validators rewards, "error type" if error
        // Description: Function return total validator rewards in cspr.
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetValidatorRewardsByEraBlockHash/{blockHash}")]
        public async Task<string> GetValidatorRewardsByEraBlockHash(string blockHash) // DZIAŁA
        {
            try
            {   
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var getEraInfo = await casperSdk.GetEraInfoBySwitchBlock(blockHash);

                var eraSummary = getEraInfo.Parse().EraSummary;
                var eraInfo = eraSummary.StoredValue.EraInfo;

                if (debugMode)
                {
                    Console.WriteLine("Block Hash: " + eraSummary.BlockHash);
                    Console.WriteLine("Era Id    : " + eraSummary.EraId);
                
                    // print the era rewards per validator
                    //
                    Console.WriteLine("Validator                                                            Deleg.    Rewards");
                    Console.WriteLine("--------------------------------------------------------------------------------------------------------");
                }
                var groupedByValidator = eraInfo.SeigniorageAllocations
                    .GroupBy(allocation => allocation.ValidatorPublicKey);

                StringWriter sw = new StringWriter();
                JsonTextWriter writer = new JsonTextWriter(sw);
                writer.WriteStartArray();

                writer.WriteStartObject();
                writer.WritePropertyName("block_hash");
                writer.WriteValue(eraSummary.BlockHash);

                writer.WritePropertyName("era");
                writer.WriteValue(eraSummary.EraId);

                //   writer.WritePropertyName("stake");
                //   writer.WriteValue(eraSummary.StoredValue.Bid.StakedAmount);

                writer.WriteEndObject();

                //  var ddd = eraSummary.StoredValue.Bid.StakedAmount;

                foreach (var group in groupedByValidator)
                {
                    var eraRewards = group.Sum(a => (double)a.Amount);
                    eraRewards /= 1_000_000_000;

                    //    Console.WriteLine($"{group.Key} - {group.Count(),5} - " +
                    //                    $"{eraRewards.ToString("N9"),20} $CSPR");
                    writer.WriteStartObject();
                    writer.WritePropertyName("validator");
                    writer.WriteValue(group.Key.ToString());

                    writer.WritePropertyName("delegators");
                    writer.WriteValue(group.Count().ToString());

                    writer.WritePropertyName("reward");
                    writer.WriteValue($"{eraRewards.ToString("N9"),20} $CSPR");
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                //    var result = JsonConvert.DeserializeObject(ddd.ToString());

                if (debugMode)
                {
                    Console.WriteLine(sw.ToString());
                }

                return sw.ToString();
            }
            catch(Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine(ex.ToString());
                }
                return "Error: " + ex.ToString();
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private string SanitizeReceivedJson(string uglyJson)
        {
            var sb = new StringBuilder(uglyJson);
            sb.Replace("\\\t", "\t");
            sb.Replace("\\\n", "\n");
            sb.Replace("\\\r", "\r");
            return sb.ToString();
        }

        // GET GetStateRootHashFromBlockHash/{blockHash}
        // Example: GetStateRootHashFromBlockHash/985a2191cc94e2abaaa88081d78dedb6fc1445043e44b97c280b529154ca4946
        // Input type: string
        // Input value: block hash
        // Return type: Json string
        // Return value: state root hash, "error type" if error
        // Description: -
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetStateRootHashFromBlockHash/{blockHash}")]
        public async Task<JsonResult> GetStateRootHashFromBlockHash(string blockHash) // DZIAŁA
        {
            string resultNodeQuery = "";

            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));                
                var rpcResponse = await casperSdk.GetStateRootHash(blockHash);

                if (debugMode)
                {
                    Console.WriteLine(rpcResponse);
                }
                resultNodeQuery = rpcResponse.ToString();

                return new JsonResult(resultNodeQuery);
            }
            catch (RpcClientException ex)
            {
                if(debugMode)
                    Console.WriteLine("ERROR:\n" + ex.RpcError.Message);
                return new JsonResult("Error: " + ex.RpcError.Message.ToString());
            }
            catch (Exception ex)
            {
                if(debugMode)
                    Console.WriteLine(ex);
                return new JsonResult("Error: " + ex.ToString());
            }

            
        }

        // GET GetStateRootHashFromLastBlock
        // Example: GetStateRootHashFromLastBlock
        // Input type: -
        // Input value: -
        // Return type: Json string
        // Return value: state root hash, "error type" if error
        // Description: -
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetStateRootHashFromLastBlock")]
        public async Task<JsonResult> GetStateRootHashFromLastBlock() // DZIAŁA
        {
            string resultNodeQuery = "";

            try
            {
                var rpc = new RpcClient(_configuration.GetConnectionString("rpcUrl"));
                
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                var lastBlockResult = casperSdk.GetBlock().Result.Parse().Block.Hash;

                var rpcResponse = await casperSdk.GetStateRootHash(lastBlockResult.ToString());
                
                if (debugMode)
                {
                    Console.WriteLine(rpcResponse);
                }
                resultNodeQuery = rpcResponse.ToString();

                return new JsonResult(resultNodeQuery);
            }
            catch (RpcClientException ex)
            {
                if (debugMode)
                    Console.WriteLine("ERROR:\n" + ex.RpcError.Message);
                return new JsonResult("Error: " + ex.RpcError.Message.ToString());
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("ERROR:\n" + ex.ToString());
                return new JsonResult("Error: " + ex.ToString());
            }
        }

        // GET GetRpcSchema
        // Example: GetRpcSchema
        // Input type: -
        // Input value: -
        // Return type: Json string
        // Return value: RPC default shema, "error type" if error
        // Description: -
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetRpcSchema")]
        public async Task<string> GetRpcSchema()// DZIAŁA
        {
            string resultNodeQuery = "";
           
            try
            {               
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var rpcResponse = await casperSdk.GetRpcSchema();
                
                resultNodeQuery = rpcResponse.ToString();//.Result.GetRawText();

                var result = JsonConvert.DeserializeObject(resultNodeQuery.ToString());

                if (debugMode)
                {
                    Console.WriteLine(result.ToString());
                }
                return result.ToString();
            }
            catch (RpcClientException ex)
            {
                if(debugMode)
                    Console.WriteLine("ERROR:\n" + ex.RpcError.Message);
                return ex.RpcError.Message.ToString();
            }
            catch (Exception ex)
            {
                if(debugMode)
                    Console.WriteLine(ex);
                return ex.ToString();
            }
        }

        // GET GetCasperArmyNodeStatus
        // Example: GetCasperArmyNodeStatus
        // Input type: -
        // Input value: -
        // Return type: Json string
        // Return value: full status of CasperArmy node, "error type" if error
        // Description: -
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetCasperArmyNodeStatus")]
        public async Task<string> GetCasperArmyNodeStatus() // DZIAŁA
        {
            string resultNodeQuery = "";

            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var rpcResponse = await casperSdk.GetNodeStatus();

                resultNodeQuery = rpcResponse.Result.GetRawText();
                var result = JsonConvert.DeserializeObject(resultNodeQuery.ToString());

                if (debugMode)
                {
                    Console.WriteLine(result.ToString());
                }
                return result.ToString();
            }
            catch (RpcClientException ex)
            {
                if (debugMode)
                    Console.WriteLine("ERROR:\n" + ex.RpcError.Message);
                return ex.RpcError.Message.ToString();
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return ex.ToString();
            }
        }

        // GET GetNodePeers
        // Example: GetNodePeers
        // Input type: -
        // Input value: -
        // Return type: Json string
        // Return value: full status of CasperArmy node peers, "error type" if error
        // Description: -
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetNodePeers")]
        public async Task<string> GetNodePeers() // DZIAŁA
        {
            CasperClient casperClient = new CasperClient(_configuration.GetConnectionString("rpcUrl"));
            
            //  var keyPair = casperClient.SigningService.GetKeyPair

            string resultNodeQuery = "";

            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var rpcResponse = await casperSdk.GetNodePeers();
                
                resultNodeQuery = rpcResponse.Result.GetRawText();
                var result = JsonConvert.DeserializeObject(resultNodeQuery.ToString());

               // DataTable dt = (DataTable)JsonConvert.DeserializeObject(resultNodeQuery, (typeof(DataTable)));// Zapisz dane do Tablicy
                

                if (debugMode)
                {
                    Console.WriteLine(result.ToString());
                }
                return result.ToString();
            }
            catch (RpcClientException ex)
            {
                if(debugMode)
                    Console.WriteLine("ERROR:\n" + ex.RpcError.Message);
                return ex.RpcError.Message.ToString();
            }
            catch (Exception ex)
            {
                if(debugMode)
                    Console.WriteLine(ex);
                return ex.ToString();
            }
        }

        

        
    }
}
