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
    public class deploysController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public deploysController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        /// <summary>
        /// Get deploys
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
        [HttpGet("deploys")]
        public async Task<ActionResult> OffchainGetPaginationDeploys(string deploy_hash = null, string from = null, string cost = null, bool? result_status = null, DateTime ? timestamp = null, string? block_hash = null, string? type = null, string? metadata_type = null, string? contract_hash = null, string? contract_name = null, string? entrypoint = null, int page_number = 1, int page_size = 10, string order_by = "timestamp", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(deploy_hash))
            {
                conditions.Add("hash = @hash");
                parameters.Add(new NpgsqlParameter("hash", deploy_hash));
            }

            if (!string.IsNullOrEmpty(from))
            {
                conditions.Add("\"from\" = @from");
                parameters.Add(new NpgsqlParameter("from", from));
            }

            if (!string.IsNullOrEmpty(cost))
            {
                conditions.Add("\"cost\" = @cost");
                parameters.Add(new NpgsqlParameter("cost", cost));
            }

            if (result_status != null && result_status.HasValue)
            {
                conditions.Add("\"result\" = @result");
                parameters.Add(new NpgsqlParameter("result", result_status));
            }

            if (timestamp != null && timestamp.HasValue)
            {
                conditions.Add("\"timestamp\" = @timestamp");
                parameters.Add(new NpgsqlParameter("timestamp", timestamp));
            }

            if (!string.IsNullOrEmpty(block_hash))
            {
                conditions.Add("block = @block");
                parameters.Add(new NpgsqlParameter("block", block_hash));
            }

            if (!string.IsNullOrEmpty(type))
            {
                conditions.Add("\"type\" = @type");
                parameters.Add(new NpgsqlParameter("type", type));
            }

            if (!string.IsNullOrEmpty(metadata_type))
            {
                conditions.Add("metadata_type = @metadata_type");
                parameters.Add(new NpgsqlParameter("metadata_type", metadata_type));
            }

            if (!string.IsNullOrEmpty(contract_hash))
            {
                conditions.Add("contract_hash = @contract_hash");
                parameters.Add(new NpgsqlParameter("contract_hash", contract_hash));
            }

            if (!string.IsNullOrEmpty(contract_name))
            {
                conditions.Add("contract_name = @contract_name");
                parameters.Add(new NpgsqlParameter("contract_name", contract_name));
            }

            if (!string.IsNullOrEmpty(entrypoint))
            {
                conditions.Add("entrypoint = @entrypoint");
                parameters.Add(new NpgsqlParameter("entrypoint", entrypoint));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(hash) FROM node_casper_deploys {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT hash, \"from\", \"cost\", \"result\", \"timestamp\", block, \"type\", metadata_type, contract_hash, contract_name, entrypoint, metadata, events FROM node_casper_deploys {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT hash, \"from\", \"cost\", \"result\", \"timestamp\", block, \"type\", metadata_type, contract_hash, contract_name, entrypoint, metadata, events FROM node_casper_deploys {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    deploy_hash = row.Field<string>("hash"),
                    from = row.Field<string>("from"),
                    deploy_cost = row.Field<string>("cost"),
                    result = row.Field<bool>("result"),
                    timestamp = row.Field<DateTime>("timestamp"),
                    block_hash = row.Field<string>("block"),
                    type = row.Field<string>("type"),
                    metadata_type = row.Field<string>("metadata_type"),
                    contract_hash = row.Field<string>("contract_hash"),
                    contract_name = row.Field<string>("contract_name"),
                    entrypoint = row.Field<string>("entrypoint"),
                    metadata = row.Field<string>("metadata"),
                    events = row.Field<string>("events")                    
                }).ToList()
            };

            return Ok(result);
        }
    }
}
