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
using Microsoft.Extensions.Primitives;
//using EnvisionStaking.Casper.SDK.Model.Common;

namespace NodeCasperParser.Controllers
{
    

      [Route("[controller]")]
  //  [ApiController]
 //   [EnableCors("AllowOrigin")]
    public class contractsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public contractsController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

      
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("get_contract_events")]
        //   [Route("GetCallUpTickets/{contractHash}")]
        // GET /GetAllCallUpTickets?contractHash=hash-511ee128bb963ebea34fcfb789f36a6d61d8062307218c9c5e3e8d8bc7f595f8
        // 9ec6171a9d23d9130f425f6d5cb51f33e64c77d29753656e025bab5c5315bd2d
        public async Task<JsonResult> GetAllContractEvents(string contractHash)
        {
            var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
            //  var user1Key = KeyPair.FromPem("./cert/secret_key.pem");
            //
            // Create the CEP47 client and initialize it with a previously installed CEP47 contract
            //
            var erc20Client = new Casper.Network.SDK.Clients.ERC20Client(casperSdk, _configuration.GetConnectionString("CHAIN_NAME"));

            if (contractHash != null)
            {
                try
                {
                    erc20Client.SetContractHash(contractHash);

                    var tSupply = await erc20Client.GetTotalSupply();
                    Console.WriteLine("erc20 total supply: " + tSupply);

                    //erc20Client.
                    //   var count = await cep47Client.GetBalanceOf(new AccountHashKey((user1Key.PublicKey));
                    //  Console.WriteLine("User1 balance: " + count);
                    return new JsonResult(tSupply);
                }
                catch (ContractException e)
                {
                    Console.WriteLine(e.Message);
                    return new JsonResult(e.Message);
                }
            }
            else
            {
                return new JsonResult("null");
            }
        }

        /// <summary>
        /// Get contracts
        /// </summary>
        /// <param name="page_number"></param>
        /// <param name="page_size"></param>
        /// <returns></returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        [HttpGet("contracts")]
        public async Task<ActionResult> OffchainGetPaginationContracts(
        string contract_hash = null, string contract_package_hash = null, string deploy_hash = null,
        string from = null, string contract_type = null, string contract_name = null,
        string contract_symbol = null, int page_number = 1, int page_size = 10,
        string order_by = "type", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(contract_hash))
            {
                conditions.Add("hash = @hash");
                parameters.Add(new NpgsqlParameter("hash", contract_hash));
            }

            if (!string.IsNullOrEmpty(contract_package_hash))
            {
                conditions.Add("package = @package");
                parameters.Add(new NpgsqlParameter("package", contract_package_hash));
            }

            // Add other conditions similarly for deploy, from, type, name, and symbol

            if (!string.IsNullOrEmpty(deploy_hash))
            {
                conditions.Add("deploy = @deploy");
                parameters.Add(new NpgsqlParameter("deploy", deploy_hash));
            }

            if (!string.IsNullOrEmpty(from))
            {
                conditions.Add("\"from\" = @from");
                parameters.Add(new NpgsqlParameter("from", from));
            }

            if (!string.IsNullOrEmpty(contract_type))
            {
                conditions.Add("type = @type");
                parameters.Add(new NpgsqlParameter("type", contract_type));
            }

            if (!string.IsNullOrEmpty(contract_name))
            {
                conditions.Add("name = @name");
                parameters.Add(new NpgsqlParameter("name", contract_name));
            }

            if (!string.IsNullOrEmpty(contract_symbol))
            {
                conditions.Add("symbol = @symbol");
                parameters.Add(new NpgsqlParameter("symbol", contract_symbol));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(hash) FROM node_casper_contracts {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT hash, package, deploy, \"from\", type, score, data, name, symbol FROM public.node_casper_contracts {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = $"SELECT hash, package, deploy, \"from\", type, score, data, name, symbol FROM public.node_casper_contracts {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    contract_hash = row.Field<string>("hash"),
                    package_hash = row.Field<string>("package"),
                    deploy_hash = row.Field<string>("deploy"),
                    from = row.Field<string>("from"),
                    contract_type = row.Field<string>("type"),
                    contract_name = row.Field<string>("name"),
                    symbol_name = row.Field<string>("symbol"),
                }).ToList()
            };

            return Ok(result);
        }
    }
}
