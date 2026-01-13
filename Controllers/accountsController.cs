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
using NodeCasperParser.Helpers;

namespace NodeCasperParser.Controllers
{
    

      [Route("[controller]")]
  //  [ApiController]
 //   [EnableCors("AllowOrigin")]
    public class accountsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public accountsController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        /// <summary>
        /// Get accounts
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="account_hash"></param>
        /// <param name="public_key"></param>
        /// <param name="min_balance">Actual account balance of liquid CSPR</param>
        /// <param name="timestamp">Date of last activity</param>
        /// <returns></returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="400">License key expired</response>
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        //[Authorize]        
        [HttpGet("accounts")]
        public async Task<ActionResult> GetPaginationAccounts(string account_hash = null, string public_key = null, decimal? min_balance = null, decimal? max_balance = null, DateTime ? timestamp = null, int page_number = 1, int page_size = 10, string order_by = "timestamp", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(account_hash))
            {
                conditions.Add("account_hash = @account_hash");
                parameters.Add(new NpgsqlParameter("account_hash", account_hash));
            }

            if (!string.IsNullOrEmpty(public_key))
            {
                conditions.Add("public_key = @public_key");
                parameters.Add(new NpgsqlParameter("public_key", public_key));
            }
           
            if (min_balance.HasValue)
            {
                conditions.Add("balance >= @min_balance");
                parameters.Add(new NpgsqlParameter("min_balance", min_balance.Value));
            }

            if (max_balance.HasValue)
            {
                conditions.Add("balance <= @max_balance");
                parameters.Add(new NpgsqlParameter("max_balance", max_balance.Value));
            }

            if (timestamp != null && timestamp.HasValue)
            {
                conditions.Add("\"timestamp\" = @timestamp");
                parameters.Add(new NpgsqlParameter("timestamp", timestamp));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(account_hash) FROM node_casper_accounts {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT account_hash, public_key, main_purse, balance, \"timestamp\" FROM node_casper_accounts {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT account_hash, public_key, main_purse, balance, \"timestamp\" FROM node_casper_accounts {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
            /*
            var timestamp2 = Convert.ToInt64(timestamp);
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp2);
            var dateTimeFromTimestamp = dt.ToString("yyyy-MM-dd HH:mm:ss+02");
            */
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
                    account_hash = row.Field<string>("account_hash"),
                    public_key = row.Field<string>("public_key"),
                    balance = row.Field<decimal>("balance"),
                    balance_in_cspr = row.Field<decimal>("balance")/1000000000,
                    timestamp = row.Field<DateTime>("timestamp")                                       
                }).ToList()
            };

            return Ok(result);
        }
    }
}
