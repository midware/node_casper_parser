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
    public class stakingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public stakingController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        /// <summary>
        /// Get Staking
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="validator_public_key"></param>
        /// <param name="delegator_public_key"></param>
        /// <param name="min_staked_amount"></param>
        /// <param name="max_staked_amount"></param>
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
        [HttpGet("staking")]
        public async Task<ActionResult> GetPaginationStakings(string validator_public_key = null, string delegator_public_key = null, decimal? min_staked_amount = null, decimal? max_staked_amount = null, int page_number = 1, int page_size = 10, string order_by = "validator_public_key", string order_direction = "DESC")
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

            if (order_by == "validator_public_key")
                order_by = "public_key";

            if (order_by == "delegator_public_key")
                order_by = "delegatee";

            if (!string.IsNullOrEmpty(validator_public_key))
            {
                conditions.Add("public_key = @validator_public_key");
                parameters.Add(new NpgsqlParameter("validator_public_key", validator_public_key));
            }

            if (!string.IsNullOrEmpty(delegator_public_key))
            {
                conditions.Add("delegatee = @delegator_public_key");
                parameters.Add(new NpgsqlParameter("delegator_public_key", delegator_public_key));
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
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(public_key) FROM node_casper_delegators {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT public_key, delegatee, staked_amount FROM node_casper_delegators {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT public_key, delegatee, staked_amount FROM node_casper_delegators {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    validator_public_key = row.Field<string>("public_key"),
                    delegatee_public_key = row.Field<string>("delegatee"),
                    staked_amount = row.Field<decimal>("staked_amount")                                     
                }).ToList()
            };

            return Ok(result);
        }
    }
}
