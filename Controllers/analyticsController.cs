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
    public class analyticsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public analyticsController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        /// <summary>
        /// Get Simple Deploys Stats
        /// </summary>        
        /// <param name="min_deploys_count">minimum number of deploys</param>
        /// <param name="max_deploys_count">maximum number of deploys</param>
        /// <param name="min_timestamp">minimum range date</param>
        /// <param name="max_timestamp">maximum range date</param>
        /// <param name="page_number">actual page number</param>
        /// <param name="page_size">number of rows/items on actual page</param>
        /// <param name="order_by">order_by timestamp and/or deploys_count</param>
        /// <param name="order_direction">order direction: ASC - first oldest/smallest, DESC - first newest/biggest</param>
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
        [HttpGet("deploys/simple_stats")]
        public async Task<ActionResult> GetPaginationSimpleDeploysStats(long? min_deploys_count = null, long? max_deploys_count = null, DateTime? min_timestamp = null, DateTime? max_timestamp = null, int page_number = 1, int page_size = 10, string order_by = "timestamp", string order_direction = "DESC")
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

            if (min_deploys_count.HasValue)
            {
                conditions.Add("deploys_count >= @min_deploys_count");
                parameters.Add(new NpgsqlParameter("min_deploys_count", min_deploys_count.Value));
            }

            if (max_deploys_count.HasValue)
            {
                conditions.Add("deploys_count <= @max_deploys_count");
                parameters.Add(new NpgsqlParameter("max_deploys_count", max_deploys_count.Value));
            }

            if (min_timestamp.HasValue)
            {
                conditions.Add("timestamp >= @min_timestamp");
                parameters.Add(new NpgsqlParameter("min_timestamp", min_timestamp.Value));
            }

            if (max_timestamp.HasValue)
            {
                conditions.Add("timestamp <= @max_timestamp");
                parameters.Add(new NpgsqlParameter("max_timestamp", max_timestamp.Value));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT (deploys_count) FROM node_casper_simple_stats {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT deploys_count, timestamp FROM node_casper_simple_stats {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT deploys_count, timestamp FROM node_casper_simple_stats {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    deploys_count = row.Field<long>("deploys_count"),
                    timestamp = row.Field<DateTime>("timestamp")                                       
                }).ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Get Full Deploys Stats
        /// </summary>
        /// <param name="type">type of deploys</param>
        /// <param name="min_deploys_count">minimum number of deploys</param>
        /// <param name="max_deploys_count">maximum number of deploys</param>
        /// <param name="min_timestamp">minimum range date</param>
        /// <param name="max_timestamp">maximum range date</param>
        /// <param name="page_number">actual page number</param>
        /// <param name="page_size">number of rows/items on actual page</param>
        /// <param name="order_by">order_by timestamp and/or deploys_count</param>
        /// <param name="order_direction">order direction: ASC - first oldest/smallest, DESC - first newest/biggest</param>
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
        [HttpGet("deploys/full_stats")]
        public async Task<ActionResult> GetPaginationFullDeploysStats(string? type = null, long? min_deploys_count = null, long? max_deploys_count = null, DateTime? min_timestamp = null, DateTime? max_timestamp = null, int page_number = 1, int page_size = 10, string order_by = "timestamp", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(type))
            {
                conditions.Add("type = @type");
                parameters.Add(new NpgsqlParameter("type", type));
            }

            if (min_deploys_count.HasValue)
            {
                conditions.Add("deploys_count >= @min_deploys_count");
                parameters.Add(new NpgsqlParameter("min_deploys_count", min_deploys_count.Value));
            }

            if (max_deploys_count.HasValue)
            {
                conditions.Add("deploys_count <= @max_deploys_count");
                parameters.Add(new NpgsqlParameter("max_deploys_count", max_deploys_count.Value));
            }

            if (min_timestamp.HasValue)
            {
                conditions.Add("timestamp >= @min_timestamp");
                parameters.Add(new NpgsqlParameter("min_timestamp", min_timestamp.Value));
            }

            if (max_timestamp.HasValue)
            {
                conditions.Add("timestamp <= @max_timestamp");
                parameters.Add(new NpgsqlParameter("max_timestamp", max_timestamp.Value));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT (deploys_count) FROM node_casper_full_stats {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT deploys_count, type, \"timestamp\" FROM node_casper_full_stats {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT deploys_count, type, \"timestamp\" FROM node_casper_full_stats {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    deploys_count = row.Field<long>("deploys_count"),
                    type = row.Field<string>("type"),
                    timestamp = row.Field<DateTime>("timestamp")
                }).ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Get Simple Staking Stats
        /// </summary>
        /// <param name="type">type of deploys</param>
        /// <param name="min_amount">minimum amount of delegate/undelegate operation</param>
        /// <param name="max_amount">maximum amount of delegate/undelegate operation</param>
        /// <param name="min_timestamp">minimum range date</param>
        /// <param name="max_timestamp">maximum range date</param>
        /// <param name="page_number">actual page number</param>
        /// <param name="page_size">number of rows/items on actual page</param>
        /// <param name="order_by">order_by timestamp and/or amount</param>
        /// <param name="order_direction">order direction: ASC - first oldest/smallest, DESC - first newest/biggest</param>
        /// <returns>
        /// endpoint:
        /// <param name="execution_time">xxxxxxxxxx</param>
        /// <param name="execution_cost">yyyyyyyyyy</param>
        /// <param name="type">type of deploys</param>
        /// pagination:
        /// <param name="page_number">zzzzzzz</param>
        /// <param name="page_size">cccccc</param>
        /// <param name="total_rows">ddddd</param> 
        /// <param name="total_pages">ddddd</param>
        /// </returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        //[Authorize]        
        [HttpGet("staking/simple_stats")]
        public async Task<ActionResult> GetPaginationStakingStats(string? type = null, long? min_amount = null, long? max_amount = null, DateTime? min_timestamp = null, DateTime? max_timestamp = null, int page_number = 1, int page_size = 10, string order_by = "timestamp", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(type))
            {
                conditions.Add("type = @type");
                parameters.Add(new NpgsqlParameter("type", type));
            }

            if (min_amount.HasValue)
            {
                conditions.Add("amount >= @min_amount");
                parameters.Add(new NpgsqlParameter("min_amount", min_amount.Value));
            }

            if (max_amount.HasValue)
            {
                conditions.Add("amount <= @max_amount");
                parameters.Add(new NpgsqlParameter("max_amount", max_amount.Value));
            }

            if (min_timestamp.HasValue)
            {
                conditions.Add("timestamp >= @min_timestamp");
                parameters.Add(new NpgsqlParameter("min_timestamp", min_timestamp.Value));
            }

            if (max_timestamp.HasValue)
            {
                conditions.Add("timestamp <= @max_timestamp");
                parameters.Add(new NpgsqlParameter("max_timestamp", max_timestamp.Value));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(type) FROM node_casper_staking_mouvements {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT type, amount, \"timestamp\" FROM node_casper_staking_mouvements {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT type, amount, \"timestamp\" FROM node_casper_staking_mouvements {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    type = row.Field<string>("type"),
                    amount = row.Field<decimal>("amount"),                    
                    timestamp = row.Field<DateTime>("timestamp")
                }).ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Get Validators Staking Stats
        /// </summary>
        /// <param name="validator_public_key">validator public key</param>
        /// <param name="min_staked_amount">minimum delegated amount of CSPR</param>
        /// <param name="max_staked_amount">maximum delegated amount of CSPR</param>
        /// <param name="page_number">actual page number</param>
        /// <param name="page_size">number of rows/items on actual page</param>
        /// <param name="order_by">order_by validator_public_key and/or staked_amount</param>
        /// <param name="order_direction">order direction: ASC - first oldest/smallest, DESC - first newest/biggest</param>
        /// <returns>
        /// endpoint:
        /// <param name="execution_time">xxxxxxxxxx</param>
        /// <param name="execution_cost">yyyyyyyyyy</param>
        /// pagination:
        /// <param name="page_number">zzzzzzz</param>
        /// <param name="page_size">cccccc</param>
        /// <param name="total_rows">ddddd</param> 
        /// <param name="total_pages">ddddd</param>
        /// </returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        //[Authorize]        
        [HttpGet("staking/validators_stats")]
        public async Task<ActionResult> GetPaginationValidatorStakingStats(string? validator_public_key = null, long? min_staked_amount = null, long? max_staked_amount = null, int page_number = 1, int page_size = 10, string order_by = "staked_amount", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(validator_public_key))
            {
                conditions.Add("validator_public_key = @validator_public_key");
                parameters.Add(new NpgsqlParameter("validator_public_key", validator_public_key));
            }

            if (min_staked_amount.HasValue)
            {
                conditions.Add("staked_amount >= @min_staked_amount");
                parameters.Add(new NpgsqlParameter("min_staked_amount", min_staked_amount.Value));
            }

            if (max_staked_amount.HasValue)
            {
                conditions.Add("staked_amount <= @max_staked_amount");
                parameters.Add(new NpgsqlParameter("max_staked_amount", max_staked_amount.Value));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(validator_public_key) FROM node_casper_total_staking {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT validator_public_key, staked_amount FROM node_casper_total_staking {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT validator_public_key, staked_amount FROM node_casper_total_staking {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    validator_public_key = row.Field<string>("validator_public_key"),
                    staked_amount = row.Field<decimal>("staked_amount")
                }).ToList()
            };

            return Ok(result);
        }
    }
}
