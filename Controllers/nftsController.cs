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
using NodeCasperParser.Cryptography.CasperNetwork;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;
using static NodeCasperParser.NftParser.NftParser;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Agreement;

namespace NodeCasperParser.Controllers
{
    

      [Route("[controller]")]
  //  [ApiController]
 //   [EnableCors("AllowOrigin")]
    public class nftsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public nftsController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        [HttpGet("nft")]
        public async Task<ActionResult> GetNFTPagination(
            string token_id = null,
            decimal? token_price = null,
            string last_bidder_hash = null,
            bool? is_on_sale = null,
            DateTime? listing_timestamp = null,
            DateTime? listing_expiration_date = null,
            bool? token_burned = null,
            string metadata_type = null,
            string token_contract_package_hash = null,
            string token_contract_hash = null,
            string contract_type = null,
            string contract_name = null,
            string contract_symbol = null,
            string token_owner_public_key = null,
            string token_owner_account_hash = null,
            int page_number = 1,
            int page_size = 10,
            string order_by = "listing_timestamp",
            string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(token_id))
            {
                conditions.Add("token_id = @token_id");
                parameters.Add(new NpgsqlParameter("@token_id", token_id));
            }

            if (token_price.HasValue)
            {
                conditions.Add("token_price = @token_price");
                parameters.Add(new NpgsqlParameter("@token_price", token_price));
            }

            if (!string.IsNullOrEmpty(last_bidder_hash))
            {
                conditions.Add("last_bidder_hash = @last_bidder_hash");
                parameters.Add(new NpgsqlParameter("@last_bidder_hash", last_bidder_hash));
            }

            if (is_on_sale.HasValue)
            {
                conditions.Add("isonsale = @isonsale");
                parameters.Add(new NpgsqlParameter("@isonsale", is_on_sale));
            }

            if (listing_timestamp.HasValue)
            {
                conditions.Add("listing_timestamp = @listing_timestamp");
                parameters.Add(new NpgsqlParameter("@listing_timestamp", listing_timestamp));
            }

            if (listing_expiration_date.HasValue)
            {
                conditions.Add("listing_expiration_date = @listing_expiration_date");
                parameters.Add(new NpgsqlParameter("@listing_expiration_date", listing_expiration_date));
            }

            if (token_burned.HasValue)
            {
                conditions.Add("token_burned = @token_burned");
                parameters.Add(new NpgsqlParameter("@token_burned", token_burned));
            }

            if (!string.IsNullOrEmpty(metadata_type))
            {
                conditions.Add("metadata_type = @metadata_type");
                parameters.Add(new NpgsqlParameter("@metadata_type", metadata_type));
            }

            if (!string.IsNullOrEmpty(token_contract_hash))
            {
                conditions.Add("token_contract_package_hash = @token_contract_package_hash");
                parameters.Add(new NpgsqlParameter("@token_contract_package_hash", token_contract_package_hash));
            }

            if (!string.IsNullOrEmpty(token_contract_hash))
            {
                conditions.Add("token_contract_hash = @token_contract_hash");
                parameters.Add(new NpgsqlParameter("@token_contract_hash", token_contract_hash));
            }

            if (!string.IsNullOrEmpty(contract_type))
            {
                conditions.Add("contract_type = @contract_type");
                parameters.Add(new NpgsqlParameter("@contract_type", contract_type));
            }

            if (!string.IsNullOrEmpty(contract_name))
            {
                conditions.Add("contract_name = @contract_name");
                parameters.Add(new NpgsqlParameter("@contract_name", contract_name));
            }

            if (!string.IsNullOrEmpty(contract_symbol))
            {
                conditions.Add("contract_symbol = @contract_symbol");
                parameters.Add(new NpgsqlParameter("@contract_symbol", contract_symbol));
            }

            if (!string.IsNullOrEmpty(token_owner_public_key))
            {
                conditions.Add("token_owner_public_key = @token_owner_public_key");
                parameters.Add(new NpgsqlParameter("@token_owner_public_key", token_owner_public_key));
            }

            if (!string.IsNullOrEmpty(token_owner_account_hash))
            {
                conditions.Add("token_owner_account_hash = @token_owner_account_hash");
                parameters.Add(new NpgsqlParameter("@token_owner_account_hash", token_owner_account_hash));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM (SELECT ncn.token_id, ncmns.token_price, ncmns.last_bidder_hash, ncmns.isonsale, ncmns.contract_hash AS marketplace_contract_hash, ncmns.timestamp AS listing_timestamp, ncmns.expiration_date AS listing_expiration_date, ncn.token_burned, ncn.metadata_type, ncn.metadata, ncn.contract_package_hash AS token_contract_package_hash, ncn.contract_hash AS token_contract_hash, ncn.contract_type, ncn.contract_name, ncn.contract_symbol, ncn.token_owner_account_hash, ncn.token_owner_public_key, ncn.timestamp AS nft_last_update_timestamp, ncn.metadata_parsed FROM public.node_casper_marketplace_nft_status ncmns RIGHT JOIN  public.node_casper_nfts ncn ON ncmns.token_id = ncn.token_id AND ncmns.contract_hash = ncn.contract_hash) AS tokens {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT * FROM (SELECT ncn.token_id, ncmns.token_price, ncmns.last_bidder_hash, ncmns.isonsale, ncmns.contract_hash AS marketplace_contract_hash, ncmns.timestamp AS listing_timestamp, ncmns.expiration_date AS listing_expiration_date, ncn.token_burned, ncn.metadata_type, ncn.metadata, ncn.contract_package_hash AS token_contract_package_hash, ncn.contract_hash AS token_contract_hash, ncn.contract_type, ncn.contract_name, ncn.contract_symbol, ncn.token_owner_account_hash, ncn.token_owner_public_key, ncn.timestamp AS nft_last_update_timestamp, ncn.metadata_parsed FROM public.node_casper_marketplace_nft_status ncmns RIGHT JOIN  public.node_casper_nfts ncn ON ncmns.token_id = ncn.token_id AND ncmns.contract_hash = ncn.contract_hash) AS tokens {whereClause} ORDER BY {order_by} {order_direction} LIMIT {page_size} OFFSET {skip}";

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
                    cmd.CommandText = $"SELECT * FROM (SELECT ncn.token_id, ncmns.token_price, ncmns.last_bidder_hash, ncmns.isonsale, ncmns.contract_hash AS marketplace_contract_hash, ncmns.timestamp AS listing_timestamp, ncmns.expiration_date AS listing_expiration_date, ncn.token_burned, ncn.metadata_type, ncn.metadata, ncn.contract_package_hash AS token_contract_package_hash, ncn.contract_hash AS token_contract_hash, ncn.contract_type, ncn.contract_name, ncn.contract_symbol, ncn.token_owner_account_hash, ncn.token_owner_public_key, ncn.timestamp AS nft_last_update_timestamp, ncn.metadata_parsed FROM public.node_casper_marketplace_nft_status ncmns RIGHT JOIN  public.node_casper_nfts ncn ON ncmns.token_id = ncn.token_id AND ncmns.contract_hash = ncn.contract_hash) AS tokens {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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

            DatabaseHelper dbh = new DatabaseHelper();

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
                    token_id = dbh.getRowValue<string>(row, "token_id"),
                    token_price = dbh.getRowValue<string>(row, "token_price"),
                    last_bidder_hash = dbh.getRowValue<string>(row, "last_bidder_hash"),
                    isonsale = dbh.getRowValue<bool>(row, "isonsale"),
                    marketplace_contract_hash =  dbh.getRowValue<string>(row, "marketplace_contract_hash"),
                    listing_timestamp = dbh.getRowValue<long>(row, "listing_timestamp"),
                    listing_expiration_date = dbh.getRowValue<long>(row, "listing_expiration_date"),
                    token_burned = dbh.getRowValue<bool>(row, "token_burned"),
                    metadata_type = dbh.getRowValue<string>(row, "metadata_type"),
                    metadata = dbh.getRowValue<string>(row, "metadata"),
                    token_contract_package_hash = dbh.getRowValue<string>(row, "token_contract_package_hash"),
                    token_contract_hash = dbh.getRowValue<string>(row, "token_contract_hash"),
                    contract_type = dbh.getRowValue<string>(row, "contract_type"),
                    contract_name = dbh.getRowValue<string>(row, "contract_name"),
                    contract_symbol = dbh.getRowValue<string>(row, "contract_symbol"),
                    token_owner_account_hash = dbh.getRowValue<string>(row, "token_owner_account_hash"),
                    token_owner_public_key = dbh.getRowValue<string>(row, "token_owner_public_key"),
                    nft_last_update_timestamp = dbh.getRowValue<DateTime>(row, "nft_last_update_timestamp"),
                    metadata_parsed = dbh.getRowValue<string>(row, "metadata_parsed")
                }).ToList()
            };

            return Ok(result);
            /*
            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            int totalRows;
            DataTable dataTable = new DataTable();
            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM (SELECT ncn.token_id, ncmns.token_price, ncmns.isonsale, ncmns.timestamp AS listing_timestamp, ncmns.expiration_date AS listing_expiration_date, ncn.token_burned, ncn.metadata_type, ncn.metadata, ncn.contract_package_hash AS token_contract_package_hash, ncn.contract_hash AS token_contract_hash, ncn.contract_type, ncn.contract_name, ncn.contract_symbol, ncn.token_owner_account_hash, ncn.token_owner_public_key, ncn.timestamp AS nft_last_update_timestamp, ncn.metadata_parsed FROM public.node_casper_marketplace_nft_status ncmns RIGHT JOIN  public.node_casper_nfts ncn ON ncmns.token_id = ncn.token_id AND ncmns.contract_hash = ncn.contract_hash) AS tokens {whereClause};", connection))
                {
                    countCmd.Parameters.AddRange(parameters.ToArray());
                    totalRows = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                string finalQuery = $"SELECT * FROM (SELECT ncn.token_id, ncmns.token_price, ncmns.isonsale, ncmns.timestamp AS listing_timestamp, ncmns.expiration_date AS listing_expiration_date, ncn.token_burned, ncn.metadata_type, ncn.metadata, ncn.contract_package_hash AS token_contract_package_hash, ncn.contract_hash AS token_contract_hash, ncn.contract_type, ncn.contract_name, ncn.contract_symbol, ncn.token_owner_account_hash, ncn.token_owner_public_key, ncn.timestamp AS nft_last_update_timestamp, ncn.metadata_parsed FROM public.node_casper_marketplace_nft_status ncmns RIGHT JOIN  public.node_casper_nfts ncn ON ncmns.token_id = ncn.token_id AND ncmns.contract_hash = ncn.contract_hash) AS tokens {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET @skip;";
                using (var cmd = new NpgsqlCommand(finalQuery, connection))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    cmd.Parameters.AddWithValue("@page_size", page_size);
                    cmd.Parameters.AddWithValue("@skip", skip);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        dataTable.Load(reader);
                    }
                }

                return Ok(new
                {
                    page_number,
                    page_size,
                    total_rows = totalRows,
                    total_pages = (int)Math.Ceiling((double)totalRows / page_size),
                    previous_page = page_number > 1 ? page_number - 1 : (int?)null,
                    next_page = page_number * page_size < totalRows ? page_number + 1 : (int?)null,
                    data = dataTable
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }*/
        }

        /// <summary>
        /// Get NFTs
        /// </summary>
        /// <param name="contract_package_hash"></param>
        /// <param name="contract_hash"></param>
        /// <param name="token_burned"></param>
        /// <param name="token_id"></param>
        /// <param name="contract_type"></param>
        /// <param name="contract_name"></param>
        /// <param name="contract_symbol"></param>
        /// <param name="account_hash"></param>
        /// <param name="public_key"></param>
        /// <param name="metadata_parsed"></param>
        /// <param name="timestamp"></param>
        /// <param name="page_number"></param>
        /// <param name="page_size"></param>
        /// <param name="order_by"></param>
        /// <param name="order_direction"></param>
        /// <returns></returns>
        /// <remarks>
        /// sample result:        
        ///
        /// <p>Read more about this endpoint in <a href="https://docs.mystra.io">documentation</a></p>
        /// </remarks>
        /// <response code="200">Ok</response> 
        /// <response code="401">Unauthorized: License key missing</response>
        /// <response code="403">Forbidden: Invalid or expired license key</response>
        [HttpGet("nfts")]
        public async Task<ActionResult> OffchainGetPaginationNFTs(
        string contract_package_hash = null, string contract_hash = null, bool? token_burned = false,
        string token_id = null, string contract_type = null, string contract_name = null,
        string contract_symbol = null, string account_hash = null, string? public_key = null, bool metadata_parsed = false, DateTime? timestamp = null, int page_number = 1, int page_size = 10,
        string order_by = "timestamp", string order_direction = "DESC")
        {
            ParserConfig parserConfig = new ParserConfig();
            string publicKey = public_key;

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

            if (!string.IsNullOrEmpty(contract_package_hash))
            {
                conditions.Add("contract_package_hash = @contract_package_hash");
                parameters.Add(new NpgsqlParameter("contract_package_hash", contract_package_hash));
            }

            if (!string.IsNullOrEmpty(contract_hash))
            {
                conditions.Add("contract_hash = @contract_hash");
                parameters.Add(new NpgsqlParameter("contract_hash", contract_hash));
            }
                       

            // Add other conditions similarly for deploy, from, type, name, and symbol

            if (token_burned != null && token_burned.HasValue)
            {
                conditions.Add("token_burned = @token_burned");
                parameters.Add(new NpgsqlParameter("token_burned", token_burned));
            }

            if (!string.IsNullOrEmpty(token_id))
            {
                conditions.Add("token_id = @token_id");
                parameters.Add(new NpgsqlParameter("token_id", token_id));
            }

            if (!string.IsNullOrEmpty(contract_type))
            {
                conditions.Add("contract_type = @contract_type");
                parameters.Add(new NpgsqlParameter("contract_type", contract_type));
            }

            if (!string.IsNullOrEmpty(contract_name))
            {
                conditions.Add("contract_name = @contract_name");
                parameters.Add(new NpgsqlParameter("contract_name", contract_name));
            }

            if (!string.IsNullOrEmpty(contract_symbol))
            {
                conditions.Add("contract_symbol = @contract_symbol");
                parameters.Add(new NpgsqlParameter("contract_symbol", contract_symbol));
            }

            if (!string.IsNullOrEmpty(account_hash))
            {
                conditions.Add("token_owner_account_hash = @token_owner_account_hash");
                parameters.Add(new NpgsqlParameter("token_owner_account_hash", account_hash));
            }

            if (!string.IsNullOrEmpty(public_key))
            {
                var userPublicKey = Casper.Network.SDK.Types.PublicKey.FromHexString(public_key);
                string getAccountHashFromPublicKey = userPublicKey.GetAccountHash().ToString();
                var accountHashHeyFromPublicKey = new AccountHashKey(getAccountHashFromPublicKey);
                publicKey = accountHashHeyFromPublicKey.ToString().ToLower().Replace("account-hash-", "");
                conditions.Add("token_owner_account_hash = @token_owner_account_hash");
                parameters.Add(new NpgsqlParameter("token_owner_account_hash", publicKey));
            }

            if (timestamp != null && timestamp.HasValue)
            {
                conditions.Add("timestamp = @timestamp");
                parameters.Add(new NpgsqlParameter("timestamp", timestamp));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(contract_hash) FROM node_casper_nfts {whereClause};", connection))
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
                    cmd.CommandText = $"EXPLAIN ANALYZE SELECT token_id, token_burned, metadata_type, metadata, metadata_parsed, contract_package_hash, contract_hash, contract_type, contract_name, contract_symbol, token_owner_account_hash, token_owner_public_key, \"timestamp\" FROM node_casper_nfts {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param.Clone());
                    }

                    cmd.Parameters.AddWithValue("page_size", page_size);
                    cmd.Parameters.AddWithValue("skip", skip);

                    var planResult = await cmd.ExecuteScalarAsync();
                    var values = parserConfig.ExtractQueryCostAndTime(planResult.ToString());

                    if (metadata_parsed)
                    {
                        countQueryCost = values.Item1 * 2; // Dane parsowane więc przemnóż koszt razy dwa
                        countQueryTime = values.Item2;
                    }
                    else
                    {
                        countQueryCost = values.Item1;
                        countQueryTime = values.Item2;
                    }
                }

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = $"SELECT token_id, token_burned, metadata_type, metadata, metadata_parsed, contract_package_hash, contract_hash, contract_type, contract_name, contract_symbol, token_owner_account_hash, token_owner_public_key, \"timestamp\" FROM node_casper_nfts {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param.Clone());
                    }

                    cmd.Parameters.AddWithValue("page_size", page_size);
                    cmd.Parameters.AddWithValue("skip", skip);

                    var myReader = await cmd.ExecuteReaderAsync();

                    msgTable.Load(myReader);

                    if (metadata_parsed)
                    {
                        

                        // Modify data in the 'metadata' column
                        foreach (DataRow row in msgTable.Rows)
                        {
                            StringWriter sw = new StringWriter();
                            JsonTextWriter writer = new JsonTextWriter(sw);

                            var dataToParsing = row["metadata"];

                            dynamic metadata = JsonConvert.DeserializeObject(dataToParsing.ToString(), typeof(object));

                            foreach (var data in metadata)
                            {
                                string metaName = data.Name;
                                string metaValue = data.Value;

                                if (!metaValue.IsNullOrEmpty() && metaValue.ToLower().Contains("ipfs"))
                                {
                                    using (HttpClient client = new HttpClient())
                                    {
                                        string filename = metaValue;
                                        var updatedIpfsLink = filename.Remove(0, 7);
                                        var ddd = "https://ipfs.io/" + filename;

                                        //  var finalIpfsHttpLink = "https://" + updatedIpfsLink + ".ipfs.dweb.link/";
                                        var finalIpfsHttpLink = "https://ipfs.io/ipfs/" + updatedIpfsLink;
                                        using (HttpResponseMessage res = await client.GetAsync(finalIpfsHttpLink))
                                        {
                                            using (HttpContent content = res.Content)
                                            {
                                                var dataInIpfs = await content.ReadAsStringAsync();

                                                if (dataInIpfs != null)
                                                {
                                                    try
                                                    {
                                                        JObject dataObj = JObject.Parse(dataInIpfs);

                                                        var jsonString = dataObj.ToString();

                                                        // dynamic wyciagnieteMetadane_z_ipfs_nftka = JsonConvert.DeserializeObject(jsonString);
                                                        var wyciagnieteMetadane_z_ipfs_nftka2 = JsonConvert.DeserializeObject(jsonString);

                                                        writer.WriteRaw(wyciagnieteMetadane_z_ipfs_nftka2.ToString());

                                                        string parsedData = sw.ToString();
                                                        parsedData = parsedData.Trim().Replace("\r", string.Empty);
                                                        parsedData = parsedData.Trim().Replace("\n", string.Empty);
                                                        parsedData = parsedData.Replace(Environment.NewLine, string.Empty);

                                                        if (parsedData.Length > 0)
                                                            row["metadata"] = parsedData;

                                                    }
                                                    catch
                                                    {
                                                        row["metadata"] = row["metadata"];
                                                    }
                                                }
                                            }
                                        }
                                    }                                   
                                }
                                else
                                {
                                    row["metadata"] = row["metadata"];
                                }

                                msgTable.AcceptChanges();
                            }
                            
                        }
                        
                    }
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
                    token_id = row.Field<string>("token_id"),
                    token_burned = row.Field<bool>("token_burned"),
                   // metadata_type = row.Field<string>("metadata_type"),
                    token_metadata = row.Field<string>("metadata"),
                    contract_package_hash = row.Field<string>("contract_package_hash"),
                    contract_hash = row.Field<string>("contract_hash"),
                    contract_type = row.Field<string>("contract_type"),
                    contract_name = row.Field<string>("contract_name"),
                    token_owner_account_hash = row.IsNull("token_owner_account_hash") ? null : row.Field<string>("token_owner_account_hash"),//row.Field<string>("token_owner_account_hash"),
                    token_owner_public_key = row.IsNull("token_owner_public_key") ? null : row.Field<string>("token_owner_public_key"),
                    //publicKey,//row.Field<string>("token_owner_public_key"),
                    timestamp = row.Field<DateTime>("timestamp"),                    
                }).ToList()
            };

            return Ok(result);
        }
    }
}
