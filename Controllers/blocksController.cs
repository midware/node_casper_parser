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
using Ipfs;

namespace NodeCasperParser.Controllers
{
    

      [Route("[controller]")]
  //  [ApiController]
 //   [EnableCors("AllowOrigin")]
    public class blocksController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public blocksController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        /// <summary>
        /// Get hash of last block
        /// </summary>
        /// <param name=""></param>
        /// <returns>last_block_hash</returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        [HttpGet]
        [Route("rpc/last_block_hash")]
        public async Task<ActionResult<string>> OnchainGetLastBlockHash()
        {
            NodeCasperParser.DatabaseHelper dh = new DatabaseHelper();
            HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey);

            bool isLicenseExpired = await dh.IsLicenseKeysExpired(licenseKey);

            if (isLicenseExpired)
            {
                return BadRequest("License Key Expired");
            }

            // Define your cost per millisecond
            const double costPerMillisecond = 0.01; // Example value: $0.01/ms
            double cost = 0;
            int roundQueryCost = 0;
            int roundQueryTime = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                Casper.Network.SDK.NetCasperClient client_test = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockHash = client_test.GetBlock().Result.Parse().Block.Hash;

                if (debugMode)
                    Console.WriteLine("lastBlockHash: " + lastBlockHash);

                stopwatch.Stop();

                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;
                roundQueryTime = (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds);
                roundQueryCost = (int)Math.Round(cost);

                var result = new
                {
                    endpoint = new
                    {
                        execution_time = roundQueryTime,
                        execution_cost = roundQueryCost
                    },
                    pagination = new
                    {
                        page_number = (int?)null,
                        page_size = (int?)null,
                        total_rows = (int?)null,
                        total_pages = (int?)null,
                        previous_page = (int?)null,
                        next_page = (int?)null
                    },
                    data = new
                    {
                        last_block_hash = lastBlockHash
                    }
                };

                return Ok(result);
            }
            catch (RpcClientException rpcError)
            {
                stopwatch.Stop();
                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;

                if (debugMode)
                    Console.WriteLine("Rpc client error: " + rpcError.ToString());

                return BadRequest($"Rpc client error occurred:  {rpcError.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());

                return BadRequest($"Error occurred:  {ex.Message}");
            }
        }
                

        [HttpGet]
        [Route("rpc/last_block_heigh")]
        public async Task<ActionResult> OnchainGetLastBlockHeigh()
        {
            NodeCasperParser.DatabaseHelper dh = new DatabaseHelper();
            HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey);

            bool isLicenseExpired = await dh.IsLicenseKeysExpired(licenseKey);

            if (isLicenseExpired)
            {
                return BadRequest("License Key Expired");
            }

            // Define your cost per millisecond
            const double costPerMillisecond = 0.01; // Example value: $0.01/ms
            double cost = 0;
            int roundQueryCost = 0;
            int roundQueryTime = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                Casper.Network.SDK.NetCasperClient client_test = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockHeigh = client_test.GetBlock().Result.Parse().Block.Header.Height;
                
                if (debugMode)
                    Console.WriteLine("lastBlockHeight: " + lastBlockHeigh);

                stopwatch.Stop();

                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;
                roundQueryTime = (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds);
                roundQueryCost = (int)Math.Round(cost);

                var result = new
                {
                    endpoint = new
                    {
                        execution_time = roundQueryTime,
                        execution_cost = roundQueryCost
                    },
                    pagination = new
                    {
                        page_number = (int?)null,
                        page_size = (int?)null,
                        total_rows = (int?)null,
                        total_pages = (int?)null,
                        previous_page = (int?)null,
                        next_page = (int?)null
                    },
                    data = new
                    {
                        last_block_height = lastBlockHeigh
                    }
                };

                return Ok(result);
            }
            catch (RpcClientException rpcError)
            {
                stopwatch.Stop();
                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;

                if (debugMode)
                    Console.WriteLine("Rpc client error: " + rpcError.ToString());

                return BadRequest($"Rpc client error occurred:  {rpcError.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());

                return BadRequest($"Error occurred:  {ex.Message}");
            }
        }


        [HttpGet]
        [Route("rpc/block")]
        public async Task<ActionResult> OnchainGetBlockHash(int? block_heigh, string? block_hash = null)
        {
            NodeCasperParser.DatabaseHelper dh = new DatabaseHelper();
            HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey);

            bool isLicenseExpired = await dh.IsLicenseKeysExpired(licenseKey);

            if (isLicenseExpired)
            {
                return BadRequest("License Key Expired");
            }

            RpcResponse<GetBlockResult> getBlock = null;

            // Define your cost per millisecond
            const double costPerMillisecond = 0.01; // Example value: $0.01/ms
            double cost = 0;
            int roundQueryCost = 0;
            int roundQueryTime = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                if (block_heigh != null)
                {
                    getBlock = await casperSdk.GetBlock((int)block_heigh);
                }
                else if (block_hash != null)
                {
                    getBlock = await casperSdk.GetBlock(block_hash);
                }
                else if (block_heigh != null && block_hash != null)
                {
                    return BadRequest("Only one input value can be use.");
                }
                else if (block_heigh == null && block_hash == null)
                {
                    return BadRequest("At least one input value must be filled");
                }

                var hash = getBlock.Parse().Block.Hash;
                var era = getBlock.Parse().Block.Header.EraId;
                var timestamp = getBlock.Parse().Block.Header.Timestamp;
                var block_height = getBlock.Parse().Block.Header.Height;
                var deploy_count = getBlock.Parse().Block.Body.DeployHashes.Count;
                string validator = getBlock.Parse().Block.Body.Proposer.PublicKey.ToString();
                var eraEnd = getBlock.Parse().Block.Header.EraEnd != null;

                if (debugMode)
                {
                    Console.WriteLine("Block Hash: " + hash.ToString());
                }

                stopwatch.Stop();

                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;
                roundQueryTime = (int)Math.Round(stopwatch.Elapsed.TotalMilliseconds);
                roundQueryCost = (int)Math.Round(cost);

                var result = new
                {
                    endpoint = new
                    {
                        execution_time = roundQueryTime,
                        execution_cost = roundQueryCost
                    },
                    pagination = new
                    {
                        page_number = (int?)null,
                        page_size = (int?)null,
                        total_rows = (int?)null,
                        total_pages = (int?)null,
                        previous_page = (int?)null,
                        next_page = (int?)null
                    },
                    data = new
                    {
                        block_hash = hash,
                        era = era,
                        timestamp = timestamp,
                        block_height = block_height,
                        deploys_count = deploy_count,
                        validator = validator,
                        era_end = eraEnd
                    }
                };

                return Ok(result);
            }
            catch (RpcClientException rpcError)
            {
                stopwatch.Stop();
                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;

                if (debugMode)
                    Console.WriteLine("Rpc client error: " + rpcError.ToString());

                return BadRequest($"Rpc client error occurred:  {rpcError.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Calculate cost based on the elapsed time
                cost = stopwatch.Elapsed.TotalMilliseconds * costPerMillisecond;
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());

                return BadRequest($"Error occurred:  {ex.Message}");
            }
        }

        /// <summary>
        /// Get blocks
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        //[Authorize]        
        [HttpGet("blocks")]
        public async Task<ActionResult> OffchainGetPaginationBlocks(string block_hash = null,string state_root_hash = null, long? block_height = null, long? era_id = null, DateTime? timestamp = null, int? deploys_count = null, string validator = null, bool? era_end = null, int page_number = 1, int page_size = 10, string order_by = "height", string order_direction = "DESC")
        {
            ParserConfig parserConfig = new ParserConfig();

            NodeCasperParser.DatabaseHelper dh = new DatabaseHelper();
            HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey);

            bool isLicenseExpired = await dh.IsLicenseKeysExpired(licenseKey);

            if (isLicenseExpired)
            {
                return BadRequest("License Key Expired");
            }

            if (page_size <= 0 || page_number <= 0)
            {
                return BadRequest("Invalid pageNumber or pageSize");
            }

            string sqlDataSource = _configuration.GetConnectionString("psqlServer");
            NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource);
            await connection.OpenAsync().ConfigureAwait(false);

            int total_rows;
            DataTable msgTable = new DataTable();
            int countQueryTime = 0;
            int countQueryCost = 0;

            List<string> conditions = new List<string>();
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrEmpty(block_hash))
            {
                conditions.Add("hash = @hash");
                parameters.Add(new NpgsqlParameter("hash", block_hash));
            }

            if (!string.IsNullOrEmpty(state_root_hash))
            {
                conditions.Add("state_root_hash = @state_root_hash");
                parameters.Add(new NpgsqlParameter("state_root_hash", state_root_hash));
            }

            if (block_height != null && block_height.HasValue)
            {
                conditions.Add("height = @height");
                parameters.Add(new NpgsqlParameter("height", block_height));
            }

            if (era_id != null && era_id.HasValue)
            {
                conditions.Add("era = @era");
                parameters.Add(new NpgsqlParameter("era", era_id));
            }

            if (timestamp != null && timestamp.HasValue)
            {
                conditions.Add("timestamp = @timestamp");
                parameters.Add(new NpgsqlParameter("timestamp", timestamp));
            }

            if (deploys_count != null && deploys_count.HasValue)
            {
                conditions.Add("deploys_count = @deploys_count");
                parameters.Add(new NpgsqlParameter("deploys_count", deploys_count));
            }

            if (!string.IsNullOrEmpty(validator))
            {
                conditions.Add("validator = @validator");
                parameters.Add(new NpgsqlParameter("validator", validator));
            }

            if (era_end != null && era_end.HasValue)
            {
                conditions.Add("era_end = @era_end");
                parameters.Add(new NpgsqlParameter("era_end", era_end));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(height) FROM node_casper_blocks {whereClause};", connection))
                {
                    foreach (var param in parameters)
                    {
                        countCmd.Parameters.Add(param);
                    }

                    total_rows = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT hash, state_root_hash, era, timestamp, height, era_end, deploys_count, validator FROM node_casper_blocks {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";
                    
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param.Clone());
                    }

                    cmd.Parameters.AddWithValue("page_size", page_size);
                    cmd.Parameters.AddWithValue("skip", skip);

                    var planResult = await cmd.ExecuteScalarAsync();
                    var values = parserConfig.ExtractQueryCostAndTime(planResult.ToString());

                    countQueryCost = values.Item1;
                    countQueryTime = values.Item2;
                }

                // Actual paginated data query
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = $"SELECT hash, state_root_hash, era, timestamp, height, era_end, deploys_count, validator FROM node_casper_blocks {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param.Clone());
                    }
                    
                    cmd.Parameters.AddWithValue("page_size", page_size);
                    cmd.Parameters.AddWithValue("skip", skip);

                    var myReader = await cmd.ExecuteReaderAsync();
                    msgTable.Load(myReader);
                }
            }
            catch (NpgsqlException npgEx)
            {
                // Handle PostgreSQL specific exceptions here
                Console.WriteLine(npgEx);
                // You can evaluate the ErrorCode or other properties of npgEx to return more specific error messages
                return BadRequest($"PostgreSQL Error: {npgEx.Message}");
            }
            catch (InvalidOperationException ioe)
            {
                // Handle known exceptions first
                Console.WriteLine(ioe);
                return BadRequest(ioe.Message);
            }
            catch (Exception ex)
            {
                // Handle other general exceptions
                Console.WriteLine(ex);
                return BadRequest($"Error occurred:  {ex.Message}");
            }
            finally
            {
                await dh.SubtractCompoundUnits(licenseKey, countQueryCost);

                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }

            var result = new
            {
                endpoint = new
                {
                    execution_time = countQueryTime,
                    execution_cost = countQueryCost
                },
                pagination = new
                {
                    page_number,
                    page_size,
                    total_rows,
                    total_pages = (int)Math.Ceiling((double)total_rows / page_size),
                    previous_page = page_number > 1 ? page_number - 1 : (int?)null,
                    next_page = page_number * page_size < total_rows ? page_number + 1 : (int?)null
                },
                data = msgTable.AsEnumerable().Select(row => new
                {
                    block_hash = row.Field<string>("hash"),
                    state_root_hash = row.Field<string>("state_root_hash"),                    
                    era = row.Field<long>("era"),
                    timestamp = row.Field<DateTime>("timestamp"),
                    block_height = row.Field<long>("height"),
                    deploys_count = row.Field<int>("deploys_count"),
                    validator = row.Field<string>("validator"),
                    era_end = row.Field<bool>("era_end")
                }).ToList()
            };

            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<string> sGetLastBlockHash()
        {
            try
            {
                Casper.Network.SDK.NetCasperClient client_test = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockHash = client_test.GetBlock().Result.Parse().Block.Hash;

                if (debugMode)
                    Console.WriteLine("lastBlockHash: " + lastBlockHash);

                return lastBlockHash;
            }
            catch (RpcClientException rpcError)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + rpcError.ToString());
                return "null";
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());

                return "null";
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<int>> iGetLastBlockHeigh()
        {
            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockHeigh = client.GetBlock().Result.Parse().Block.Header.Height;

                if (debugMode)
                    Console.WriteLine("lastBlockHash: " + lastBlockHeigh);

                return Ok(lastBlockHeigh);
            }
            catch (RpcClientException rpcError)
            {
                if (debugMode)
                    Console.WriteLine("Rpc client error: " + rpcError.ToString());

                return BadRequest($"Rpc client error occurred:  {rpcError.Message}");
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());

                return BadRequest($"Error occurred:  {ex.Message}");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<int> iiGetLastBlockHeigh()
        {
            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockHeigh = client.GetBlock().Result.Parse().Block.Header.Height;

                if (debugMode)
                    Console.WriteLine("lastBlockHash: " + lastBlockHeigh);

                return Convert.ToInt32(lastBlockHeigh);
            }
            catch (RpcClientException rpcError)
            {
                if (debugMode)
                    Console.WriteLine("Rpc client error: " + rpcError.ToString());

                return 0;
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());

                return 0;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<string> sGetBlockHash(int block_heigh)
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                int beSureItsINT = Convert.ToInt32(block_heigh);
                var getBlock = await casperSdk.GetBlock(beSureItsINT);

                var getRawBlock = getBlock.Parse().Block.Hash;


                if (debugMode)
                {
                    Console.WriteLine("Block Hash: " + getRawBlock.ToString());
                }

                return getRawBlock;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine(ex.ToString());
                }
                return "Error: " + ex.ToString();
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<string> iGetBlockHash(int block_heigh)
        {
            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                int beSureItsINT = Convert.ToInt32(block_heigh);
                var getBlock = await casperSdk.GetBlock(beSureItsINT);

                var getRawBlock = getBlock.Parse().Block.Hash;


                if (debugMode)
                {
                    Console.WriteLine("Block Hash: " + getRawBlock.ToString());
                }

                return getRawBlock;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine(ex.ToString());
                }
                return "Error: " + ex.ToString();
            }
        }
    }
}
