using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Casper.Network.SDK.Clients.CEP78;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cors;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using static Org.BouncyCastle.Math.EC.ECCurve;
using System.Xml.Linq;
using Casper.Network.SDK.JsonRpc.ResultTypes;
using Casper.Network.SDK.JsonRpc;
using NodeCasperParser.Models;
using MailKit.Search;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NodeCasperParser.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace NodeCasperParser.Controllers
{
    /*  public static void Add(this List<CasperNetworkContractsList> list, string Contract_hash, string Contract_package_hash, string Deploy_hash, string Contract_type_id, string Contract_version, string Is_disabled, string Protocol_version, string Timestamp)
      {
          if (null == list)
              throw new NullReferenceException();

          var contractList = new CasperNetworkContractsList
          {
              contract_hash = Contract_hash,
              contract_package_hash = Contract_package_hash,
              deploy_hash = Deploy_hash,
              contract_type_id = Contract_type_id,
              contract_version = Contract_version,
              is_disabled = Is_disabled,
              protocol_version = Protocol_version,
              timestamp = Timestamp
          };
          list.Add(contractList);
      }*/
 //   [EnableCors("AllowOrigin")]
    public class CasperNetworkMarketplaceCollectionList : ControllerBase
    {
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");

        bool debugMode = false;


        public CasperNetworkMarketplaceCollectionList(string collection_contract_hash, string collection_creator_public_key)
        {
            this.collection_contract_hash = collection_contract_hash;
          //  this.collection_contract_package_hash = collection_contract_package_hash;
            this.collection_creator_public_key = collection_creator_public_key;
          /*  this.collection_creator_account_hash = collection_creator_account_hash;
            this.collection_total_items = collection_total_items;
            this.collection_unique_items = collection_unique_items;
            this.collection_total_volume = collection_total_volume;
            this.collection_floor_price = collection_floor_price;
            this.collection_best_offer = collection_best_offer;
            this.collection_listed_items = collection_listed_items;
            this.collection_unique_owners = collection_unique_owners;*/
        }

       
        [JsonProperty("collection_contract_hash")]
        public string collection_contract_hash { get; set; }

        [JsonProperty("collection_contract_package_hash")]
        public string collection_contract_package_hash { get; set; }

        [JsonProperty("collection_creator_public_key")]
        public string collection_creator_public_key { get; set; }

        [JsonProperty("collection_creator_account_hash")]
        public string collection_creator_account_hash { get; set; }

        [JsonProperty("collection_total_items")]
        public int collection_total_items { get; set; }

        [JsonProperty("collection_unique_items")]
        public int collection_unique_items { get; set; }

        [JsonProperty("collection_total_volume")]
        public long collection_total_volume { get; set; }

        [JsonProperty("collection_floor_price")]
        public long collection_floor_price { get; set; }

        [JsonProperty("collection_best_offer")]
        public long collection_best_offer { get; set; }

        [JsonProperty("collection_listed_items")]
        public long collection_listed_items { get; set; }

        [JsonProperty("collection_unique_owners")]
        public long collection_unique_owners { get; set; }
    }

 //   [EnableCors("AllowOrigin")]
    public class CasperNetworkMarketplaceBatch : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public CasperNetworkMarketplaceBatch(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        public async Task<List<CasperNetworkMarketplaceCollectionList>> AddExistingCollectionsToList()
        {
            NpgsqlConnection connection = new NpgsqlConnection(_configuration.GetConnectionString("psqlServer"));
            DataTable table = new DataTable();

            blocksController blocks = new blocksController(_configuration);
            List<CasperNetworkMarketplaceCollectionList> marketplaceCollectionsList = new List<CasperNetworkMarketplaceCollectionList>();

            Console.WriteLine("Adding existing collections to table for updating...");

            var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync().ConfigureAwait(false);

                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandText = $@"SELECT collection_contract_hash, collection_contract_package_hash, collection_creator_public_key, collection_creator_account_hash, collection_total_items, collection_unique_items, collection_total_volume, collection_floor_price, collection_best_offer, collection_listed_items, collection_unique_owners
	                FROM public.node_casper_marketplace_nft_collections;";


                    var myReader2 = await cmd.ExecuteReaderAsync();
                    table.Load(myReader2);
                }
            }
            catch (NpgsqlException npgEx)
            {
                // return BadRequest($"PostgreSQL Error: {npgEx.Message}");
            }
            catch (InvalidOperationException ioe)
            {
                // return BadRequest(ioe.Message);
            }
            catch (Exception ex)
            {
                // return BadRequest($"Error occurred: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();

                if (table.Rows.Count != 0)
                {
                    PostgresCasperNodeService pcns = new PostgresCasperNodeService();

                    foreach (DataRow row in table.Rows)
                    {
                        string collection_contract_hash = pcns.getRowValue<string>(row, "collection_contract_hash");
                        string collection_creator_public_key = pcns.getRowValue<string>(row, "collection_creator_public_key");

                        marketplaceCollectionsList.Add(new CasperNetworkMarketplaceCollectionList(collection_contract_hash, collection_creator_public_key));
                    }
                }                
            }

            return marketplaceCollectionsList;
        }

        public async Task<(long, double, double, double)> CalculateCollectionData(string collectionContractHash)
        {
            // Initialize database connection
            string sqlDataSource = _configuration.GetConnectionString("psqlServer");

            using var connection = new NpgsqlConnection(sqlDataSource);
            await connection.OpenAsync().ConfigureAwait(false);

            // Build dynamic query conditions
            List<string> conditions = new List<string>();
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrEmpty(collectionContractHash))
            {
                conditions.Add("contract_hash = @contract_hash");
                parameters.Add(new NpgsqlParameter("contract_hash", collectionContractHash));
            }

            // Construct the final WHERE clause
            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

            long totalMintedNfts = 0;
            double totalVolumeInCspr = 0;
            double floorPriceInCspr = 0;
            double bestOfferInCspr = 0;

            try
            {
                // Total volume
                using (var countCmd = new NpgsqlCommand($"SELECT COUNT(id) FROM public.node_casper_nfts {whereClause};", connection))
                {
                    foreach (var param in parameters)
                    {
                        countCmd.Parameters.Add((NpgsqlParameter)((ICloneable)param).Clone());
                    }

                    var result = await countCmd.ExecuteScalarAsync();
                    totalMintedNfts = result == DBNull.Value ? 0 : Convert.ToInt64(result);

                }
                // Total volume
                using (var countCmd = new NpgsqlCommand($"SELECT SUM(CAST(price AS numeric)) AS total_price FROM public.node_casper_marketplace_event_listing_bought {whereClause};", connection))
                {
                    foreach (var param in parameters)
                    {
                        countCmd.Parameters.Add((NpgsqlParameter)((ICloneable)param).Clone());
                    }

                    var result = await countCmd.ExecuteScalarAsync();
                    totalVolumeInCspr = result == DBNull.Value ? 0 : Convert.ToDouble(result);
                }

                // Floor price
                using (var floorCmd = new NpgsqlCommand(
                    $@"
WITH min_auction_bid AS (
    SELECT MIN(bid_price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_auction_bid {whereClause} 
),
min_auction_ended AS (
    SELECT MIN(ending_price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_auction_ended {whereClause} 
),
min_auction_started AS (
    SELECT MIN(price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_auction_started {whereClause} 
),
min_create_listing AS (
    SELECT MIN(price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_create_listing {whereClause} 
),
min_listing_bought AS (
    SELECT MIN(price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_listing_bought {whereClause} 
),
min_new_offer AS (
    SELECT MIN(price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_new_offer {whereClause} 
),
min_offer_accepted AS (
    SELECT MIN(price::numeric) AS min_price
    FROM public.node_casper_marketplace_event_offer_accepted {whereClause}
)
SELECT MIN(min_price) AS lowest_price
FROM (
    SELECT min_price FROM min_auction_bid
    UNION ALL
    SELECT min_price FROM min_auction_ended
    UNION ALL
    SELECT min_price FROM min_auction_started
    UNION ALL
    SELECT min_price FROM min_create_listing
    UNION ALL
    SELECT min_price FROM min_listing_bought
    UNION ALL
    SELECT min_price FROM min_new_offer
    UNION ALL
    SELECT min_price FROM min_offer_accepted
) AS combined_prices;", connection))
                {
                    foreach (var param in parameters)
                    {
                        floorCmd.Parameters.Add((NpgsqlParameter)((ICloneable)param).Clone());
                    }

                    var result = await floorCmd.ExecuteScalarAsync();
                    floorPriceInCspr = result == DBNull.Value ? 0 : Convert.ToDouble(result);
                }

                // Best offer
                using (var offerCmd = new NpgsqlCommand(
                    $@"
WITH highest_new_offer AS (
    SELECT price::numeric AS offer_price
    FROM public.node_casper_marketplace_event_new_offer {whereClause}
    ORDER BY offer_price DESC
    LIMIT 1
),
highest_auction_bid AS (
    SELECT bid_price::numeric AS offer_price
    FROM public.node_casper_marketplace_event_auction_bid {whereClause}
    ORDER BY offer_price DESC
    LIMIT 1
)
SELECT MAX(offer_price) AS highest_offer_price
FROM (
    SELECT offer_price FROM highest_new_offer
    UNION ALL
    SELECT offer_price FROM highest_auction_bid
) AS combined_offers;", connection))
                {
                    foreach (var param in parameters)
                    {
                        offerCmd.Parameters.Add((NpgsqlParameter)((ICloneable)param).Clone());
                    }

                    var result = await offerCmd.ExecuteScalarAsync();
                    bestOfferInCspr = result == DBNull.Value ? 0 : Convert.ToDouble(result);
                }
            }
            catch (Exception ex)
            {
                // Log the exception (not shown here for brevity)
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }

            return (totalMintedNfts, totalVolumeInCspr, floorPriceInCspr, bestOfferInCspr);
        }

        public void UpdateMarketplaceCollectionsData()
        {
            NpgsqlConnection myConn = new NpgsqlConnection(_configuration.GetConnectionString("psqlServer"));
            long total_minted_nfts = 0;
            double total_volume_in_cspr = 0;
            double floor_price_in_cspr = 0;
            double best_offer_in_cspr = 0;

            List<CasperNetworkMarketplaceCollectionList> lista = AddExistingCollectionsToList().Result;
            List<CasperNetworkMarketplaceCollectionList> fullCollectionList = new List<CasperNetworkMarketplaceCollectionList>();
            fullCollectionList.AddRange(lista);
            lista.Clear();            

            string buildStringInsertQuery = string.Empty;

            foreach (var collections in fullCollectionList)
            {
                (total_minted_nfts, total_volume_in_cspr, floor_price_in_cspr, best_offer_in_cspr) = CalculateCollectionData(collections.collection_contract_hash).Result;

                //    buildStringInsertQuery += @"UPDATE node_casper_marketplace_nft_collections SET collection_total_items=?, collection_unique_items=?, collection_total_volume='" + best_offer_in_cspr.ToString() + "', collection_floor_price=" + floor_price_in_cspr.ToString() + ", collection_best_offer=" + best_offer_in_cspr + ", collection_listed_items=?, collection_unique_owners=? WHERE collection_contract_hash = '" + collections.collection_contract_hash + "' AND collection_creator_public_key = '" + collections.collection_creator_public_key + "';";

                long collection_minted_nfts = total_minted_nfts;
                long totalVolume = Convert.ToInt64(total_volume_in_cspr.ToString().Replace(",","."));
                long floorPrice = Convert.ToInt64(floor_price_in_cspr.ToString().Replace(",", "."));
                long bestOffer = Convert.ToInt64(best_offer_in_cspr.ToString().Replace(",", "."));

                buildStringInsertQuery += @"UPDATE node_casper_marketplace_nft_collections SET collection_minted_nfts = " + collection_minted_nfts + " , collection_total_volume=" + totalVolume + ", collection_floor_price=" + floorPrice + ", collection_best_offer=" + bestOffer + " WHERE collection_contract_hash = '" + collections.collection_contract_hash + "' AND collection_creator_public_key = '" + collections.collection_creator_public_key + "';";
            }
            
            buildStringInsertQuery = buildStringInsertQuery.Remove(buildStringInsertQuery.Length - 1);
            //  Console.WriteLine(buildStringInsertQuery);
            Console.WriteLine("UPDATING COLLECTIONS TABLE...");

            
            try
            {
                myConn.Open();

                using (NpgsqlCommand cmd = new NpgsqlCommand(buildStringInsertQuery, myConn))
                {
                    cmd.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (myConn.State == ConnectionState.Open)
                    myConn.Close();
                Console.WriteLine();
            }
        }
    }
}
