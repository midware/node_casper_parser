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
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Macs;
//using EnvisionStaking.Casper.SDK.Model.Common;

namespace NodeCasperParser.Controllers
{
    

      [Route("[controller]")]
  //  [ApiController]
 //   [EnableCors("AllowOrigin")]
    public class marketplaceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public marketplaceController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        /// <summary>
        /// Get Mystra NFT marketplace collections
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
        [HttpGet("collections")]
        public async Task<ActionResult> OffchainGetPaginationMarketplaceCollections(
        string collection_contract_hash = null, string collection_contract_package_hash = null, string collection_contract_deploy_hash = null,
        string collection_creator_public_key = null, string collection_creator_account_hash = null, string collection_contract_type = null,
        string collection_contract_name = null, string collection_contract_symbol = null, string collection_name = null, string collection_description = null,
        string collection_www = null, string collection_twitter = null, string collection_telegram = null, string collection_discord = null,
        bool? collection_verified = null, int? min_collection_total_items = null, int? max_collection_total_items = null,
        int? min_collection_unique_items = null, int? max_collection_unique_items = null,
        long? min_collection_total_volume = null, long? max_collection_total_volume = null, 
        long? min_collection_floor_price = null, long? max_collection_floor_price = null,
        long? min_collection_best_offer = null, long? max_collection_best_offer = null,
        int? min_collection_listed_items = null, int? max_collection_listed_items = null,
        int? min_collection_unique_owners = null, int? max_collection_unique_owners = null,
        long? min_collection_created = null, long? max_collection_created = null,/*
        DateTime? min_collection_registered = null, DateTime? max_collection_registered = null,*/

        int page_number = 1, int page_size = 10,
        string order_by = "collection_contract_name", string order_direction = "DESC")
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

            if (!string.IsNullOrEmpty(collection_contract_hash))
            {
                conditions.Add("collection_contract_hash = @collection_contract_hash");
                parameters.Add(new NpgsqlParameter("collection_contract_hash", collection_contract_hash));
            }

            if (!string.IsNullOrEmpty(collection_contract_package_hash))
            {
                conditions.Add("collection_contract_package_hash = @collection_contract_package_hash");
                parameters.Add(new NpgsqlParameter("collection_contract_package_hash", collection_contract_package_hash));
            }

            if (!string.IsNullOrEmpty(collection_contract_deploy_hash))
            {
                conditions.Add("collection_contract_deploy_hash = @collection_contract_deploy_hash");
                parameters.Add(new NpgsqlParameter("collection_contract_deploy_hash", collection_contract_deploy_hash));
            }

            if (!string.IsNullOrEmpty(collection_creator_public_key))
            {
                conditions.Add("collection_creator_public_key = @collection_creator_public_key");
                parameters.Add(new NpgsqlParameter("collection_creator_public_key", collection_creator_public_key));
            }

            if (!string.IsNullOrEmpty(collection_creator_account_hash))
            {
                conditions.Add("collection_creator_account_hash = @collection_creator_account_hash");
                parameters.Add(new NpgsqlParameter("collection_creator_account_hash", collection_creator_account_hash));
            }

            if (!string.IsNullOrEmpty(collection_contract_type))
            {
                conditions.Add("collection_contract_type = @collection_contract_type");
                parameters.Add(new NpgsqlParameter("collection_contract_type", collection_contract_type));
            }

            if (!string.IsNullOrEmpty(collection_contract_name))
            {
                conditions.Add("collection_contract_name LIKE @collection_contract_name");
                parameters.Add(new NpgsqlParameter("collection_contract_name", "%" + collection_contract_name + "%"));
            }

            if (!string.IsNullOrEmpty(collection_contract_symbol))
            {
                conditions.Add("collection_contract_symbol = @collection_contract_symbol");
                parameters.Add(new NpgsqlParameter("collection_contract_symbol", "%" + collection_contract_symbol + "%"));
            }

            if (!string.IsNullOrEmpty(collection_name))
            {
                conditions.Add("collection_name = @collection_name");
                parameters.Add(new NpgsqlParameter("collection_name", "%" + collection_name + "%"));
            }

            if (!string.IsNullOrEmpty(collection_description))
            {
                conditions.Add("collection_description = @collection_description");
                parameters.Add(new NpgsqlParameter("collection_description", "%" + collection_description + "%"));
            }

            if (!string.IsNullOrEmpty(collection_www))
            {
                conditions.Add("collection_www = @collection_www");
                parameters.Add(new NpgsqlParameter("collection_www", collection_www));
            }

            if (!string.IsNullOrEmpty(collection_twitter))
            {
                conditions.Add("collection_twitter = @collection_twitter");
                parameters.Add(new NpgsqlParameter("collection_twitter", collection_twitter));
            }

            if (!string.IsNullOrEmpty(collection_telegram))
            {
                conditions.Add("collection_telegram = @collection_telegram");
                parameters.Add(new NpgsqlParameter("collection_telegram", collection_telegram));
            }

            if (!string.IsNullOrEmpty(collection_discord))
            {
                conditions.Add("collection_discord = @collection_discord");
                parameters.Add(new NpgsqlParameter("collection_discord", collection_discord));
            }

            if (collection_verified != null & collection_verified.HasValue)
            {
                conditions.Add("collection_verified = @collection_verified");
                parameters.Add(new NpgsqlParameter("collection_verified", collection_verified.Value));
            }

            if (min_collection_total_items != null & min_collection_total_items.HasValue)
            {
                conditions.Add("collection_total_items >= @min_collection_total_items");
                parameters.Add(new NpgsqlParameter("min_collection_total_items", min_collection_total_items.Value));
            }

            if (max_collection_total_items != null & max_collection_total_items.HasValue)
            {
                conditions.Add("collection_total_items <= @max_collection_total_items");
                parameters.Add(new NpgsqlParameter("max_collection_total_items", max_collection_total_items.Value));
            }

            if (min_collection_unique_items != null & min_collection_unique_items.HasValue)
            {
                conditions.Add("collection_unique_items >= @min_collection_unique_items");
                parameters.Add(new NpgsqlParameter("min_collection_unique_items", min_collection_unique_items.Value));
            }

            if (max_collection_unique_items != null & max_collection_unique_items.HasValue)
            {
                conditions.Add("collection_unique_items <= @max_collection_unique_items");
                parameters.Add(new NpgsqlParameter("max_collection_unique_items", max_collection_unique_items.Value));
            }

            if (min_collection_total_volume != null & min_collection_total_volume.HasValue)
            {
                conditions.Add("collection_total_volume >= @min_collection_total_volume");
                parameters.Add(new NpgsqlParameter("min_collection_total_volume", min_collection_total_volume.Value));
            }

            if (max_collection_total_volume != null & max_collection_total_volume.HasValue)
            {
                conditions.Add("collection_unique_items <= @max_collection_total_volume");
                parameters.Add(new NpgsqlParameter("max_collection_total_volume", max_collection_total_volume.Value));
            }

            if (min_collection_floor_price != null & min_collection_floor_price.HasValue)
            {
                conditions.Add("collection_floor_price >= @min_collection_floor_price");
                parameters.Add(new NpgsqlParameter("min_collection_floor_price", min_collection_floor_price.Value));
            }

            if (max_collection_floor_price != null & max_collection_floor_price.HasValue)
            {
                conditions.Add("collection_floor_price <= @max_collection_floor_price");
                parameters.Add(new NpgsqlParameter("max_collection_floor_price", max_collection_floor_price.Value));
            }

            if (min_collection_best_offer != null & min_collection_best_offer.HasValue)
            {
                conditions.Add("collection_best_offer >= @min_collection_best_offer");
                parameters.Add(new NpgsqlParameter("min_collection_best_offer", min_collection_best_offer.Value));
            }

            if (max_collection_best_offer != null & max_collection_best_offer.HasValue)
            {
                conditions.Add("collection_best_offer <= @max_collection_best_offer");
                parameters.Add(new NpgsqlParameter("max_collection_best_offer", max_collection_best_offer.Value));
            }

            if (min_collection_listed_items != null & min_collection_listed_items.HasValue)
            {
                conditions.Add("collection_listed_items >= @min_collection_listed_items");
                parameters.Add(new NpgsqlParameter("min_collection_listed_items", min_collection_listed_items.Value));
            }

            if (max_collection_listed_items != null & max_collection_listed_items.HasValue)
            {
                conditions.Add("collection_listed_items <= @max_collection_listed_items");
                parameters.Add(new NpgsqlParameter("max_collection_listed_items", max_collection_listed_items.Value));
            }

            if (min_collection_unique_owners != null & min_collection_unique_owners.HasValue)
            {
                conditions.Add("collection_unique_owners >= @min_collection_unique_owners");
                parameters.Add(new NpgsqlParameter("min_collection_unique_owners", min_collection_unique_owners.Value));
            }

            if (max_collection_unique_owners != null & max_collection_unique_owners.HasValue)
            {
                conditions.Add("collection_unique_owners <= @max_collection_unique_owners");
                parameters.Add(new NpgsqlParameter("max_collection_unique_owners", max_collection_unique_owners.Value));
            }

            if (min_collection_created != null & min_collection_created.HasValue)
            {
                conditions.Add("collection_created >= @min_collection_created");
                parameters.Add(new NpgsqlParameter("min_collection_created", min_collection_created.Value));
            }

            if (max_collection_created != null & max_collection_created.HasValue)
            {
                conditions.Add("collection_created <= @max_collection_created");
                parameters.Add(new NpgsqlParameter("max_collection_created", max_collection_created.Value));
            }

            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            int skip = (page_number - 1) * page_size;

            try
            {
                using (var countCmd = new NpgsqlCommand($@"SELECT COUNT(*) FROM (
    SELECT 
    ncc.hash AS collection_contract_hash, 
    ncc.package AS collection_contract_package_hash, 
    ncc.deploy AS collection_contract_deploy_hash, 
    ncc.from AS collection_creator_public_key, 
	ncmnc.collection_creator_account_hash,
    ncc.type AS collection_contract_type, 
    ncc.name AS collection_contract_name, 
    ncc.symbol AS collection_contract_symbol,     
    ncmnc.collection_name, 
    ncmnc.collection_description, 
    ncmnc.collection_banner, 
    ncmnc.collection_square_pic, 
    ncmnc.collection_icon, 
    ncmnc.collection_www, 
    ncmnc.collection_twitter, 
    ncmnc.collection_telegram, 
    ncmnc.collection_discord, 
    ncmnc.collection_verified, 
    ncmnc.collection_total_items, 
    ncmnc.collection_unique_items, 
    ncmnc.collection_total_volume, 
    ncmnc.collection_floor_price, 
    ncmnc.collection_best_offer, 
    ncmnc.collection_listed_items, 
    ncmnc.collection_unique_owners, 
    ncmnc.collection_created, 
    ncmnc.collection_registered,
    ncmnc.collection_minted_nfts
    FROM  
    node_casper_contracts ncc 
    LEFT JOIN 
    node_casper_marketplace_nft_collections ncmnc 
    ON 
    ncc.hash = ncmnc.collection_contract_hash 
    WHERE 
    ncc.type = 'NFTCEP78' 
    OR ncc.type = 'NFTCEP47' 
	) collections {whereClause};", connection))
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
                    cmd.CommandText = $@"EXPLAIN ANALYZE SELECT * FROM (
    SELECT 
    ncc.hash AS collection_contract_hash, 
    ncc.package AS collection_contract_package_hash, 
    ncc.deploy AS collection_contract_deploy_hash, 
    ncc.from AS collection_creator_public_key, 
	ncmnc.collection_creator_account_hash,
    ncc.type AS collection_contract_type, 
    ncc.name AS collection_contract_name, 
    ncc.symbol AS collection_contract_symbol,     
    ncmnc.collection_name, 
    ncmnc.collection_description, 
    ncmnc.collection_banner, 
    ncmnc.collection_square_pic, 
    ncmnc.collection_icon, 
    ncmnc.collection_www, 
    ncmnc.collection_twitter, 
    ncmnc.collection_telegram, 
    ncmnc.collection_discord, 
    ncmnc.collection_verified, 
    ncmnc.collection_total_items, 
    ncmnc.collection_unique_items, 
    ncmnc.collection_total_volume, 
    ncmnc.collection_floor_price, 
    ncmnc.collection_best_offer, 
    ncmnc.collection_listed_items, 
    ncmnc.collection_unique_owners, 
    ncmnc.collection_created, 
    ncmnc.collection_registered,
    ncmnc.collection_minted_nfts
    FROM  
    node_casper_contracts ncc 
    LEFT JOIN 
    node_casper_marketplace_nft_collections ncmnc 
    ON 
    ncc.hash = ncmnc.collection_contract_hash 
    WHERE 
    ncc.type = 'NFTCEP78' 
    OR ncc.type = 'NFTCEP47' 
	) collections {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                    cmd.CommandText = $@"SELECT * FROM (
    SELECT 
    ncc.hash AS collection_contract_hash, 
    ncc.package AS collection_contract_package_hash, 
    ncc.deploy AS collection_contract_deploy_hash, 
    ncc.from AS collection_creator_public_key, 
	ncmnc.collection_creator_account_hash,
    ncc.type AS collection_contract_type, 
    ncc.name AS collection_contract_name, 
    ncc.symbol AS collection_contract_symbol,     
    ncmnc.collection_name, 
    ncmnc.collection_description, 
    ncmnc.collection_banner, 
    ncmnc.collection_square_pic, 
    ncmnc.collection_icon, 
    ncmnc.collection_www, 
    ncmnc.collection_twitter, 
    ncmnc.collection_telegram, 
    ncmnc.collection_discord, 
    ncmnc.collection_verified, 
    ncmnc.collection_total_items, 
    ncmnc.collection_unique_items, 
    ncmnc.collection_total_volume, 
    ncmnc.collection_floor_price, 
    ncmnc.collection_best_offer, 
    ncmnc.collection_listed_items, 
    ncmnc.collection_unique_owners, 
    ncmnc.collection_created, 
    ncmnc.collection_registered,
    ncmnc.collection_minted_nfts
    FROM  
    node_casper_contracts ncc 
    LEFT JOIN 
    node_casper_marketplace_nft_collections ncmnc 
    ON 
    ncc.hash = ncmnc.collection_contract_hash 
    WHERE 
    ncc.type = 'NFTCEP78' 
    OR ncc.type = 'NFTCEP47' 
	) collections {whereClause} ORDER BY {order_by} {order_direction} LIMIT @page_size OFFSET {skip}";

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
                      
            PostgresCasperNodeService pcns = new PostgresCasperNodeService();
            
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
                    collection_contract_hash = pcns.getRowValue<string>(row, "collection_contract_hash"),
                    collection_contract_package_hash = pcns.getRowValue<string>(row, "collection_contract_package_hash"),
                    collection_contract_deploy_hash = pcns.getRowValue<string>(row, "collection_contract_deploy_hash"),
                    collection_creator_public_key = pcns.getRowValue<string>(row, "collection_creator_public_key"),
                    collection_creator_account_hash = pcns.getRowValue<string>(row, "collection_creator_account_hash"),
                    collection_contract_type = pcns.getRowValue<string>(row, "collection_contract_type"),
                    collection_contract_name = pcns.getRowValue<string>(row, "collection_contract_name"),
                    collection_contract_symbol = pcns.getRowValue<string>(row, "collection_contract_symbol"),
                    collection_name = pcns.getRowValue<string>(row, "collection_name"),
                    collection_description = pcns.getRowValue<string>(row, "collection_description"),
                    collection_banner = pcns.getRowValue<string>(row, "collection_banner"),
                    collection_square_pic = pcns.getRowValue<string>(row, "collection_square_pic"),
                    collection_icon = pcns.getRowValue<string>(row, "collection_icon"),
                    collection_www = pcns.getRowValue<string>(row, "collection_www"),
                    collection_twitter = pcns.getRowValue<string>(row, "collection_twitter"),
                    collection_telegram = pcns.getRowValue<string>(row, "collection_telegram"),
                    collection_discord = pcns.getRowValue<string>(row, "collection_discord"),
                    collection_verified = pcns.getRowValue<bool>(row, "collection_verified"),
                    collection_total_items = pcns.getRowValue<int>(row, "collection_total_items"),
                    collection_unique_items = pcns.getRowValue<int>(row, "collection_unique_items"),
                    collection_total_volume = pcns.getRowValue<long>(row, "collection_total_volume"),
                    collection_floor_price = pcns.getRowValue<long>(row, "collection_floor_price"),
                    collection_best_offer = pcns.getRowValue<long>(row, "collection_best_offer"),
                    collection_listed_items = pcns.getRowValue<int>(row, "collection_listed_items"),
                    collection_unique_owners = pcns.getRowValue<int>(row, "collection_unique_owners"),
                    collection_minted_nfts = pcns.getRowValue<int>(row, "collection_minted_nfts"),
                    collection_created = pcns.getRowValue<long>(row, "collection_created"),
                    collection_registered = pcns.getRowValue<DateTime>(row, "collection_registered")
                }).ToList()
            };

            return Ok(result);
        }
    }
}
