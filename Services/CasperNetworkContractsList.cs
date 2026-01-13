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

    public class CasperNetworkContractsList
    {
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");
        
        bool debugMode = false;


    //    private static string psqlServer { get; } = Config.getToken("psqlServer");
    //    private static string debugModes { get; } = Config.getToken("debugMode");

        public CasperNetworkContractsList(/*IConfiguration configuration*/)
        {
        }
               
        public CasperNetworkContractsList(string contract_hash, string contract_package_hash, string deploy_hash, string contract_type_id,
               string contract_version, string is_disabled, string protocol_version, string timestamp)
           {
               this.contract_hash = contract_hash;
               this.contract_package_hash = contract_package_hash;
               this.deploy_hash = deploy_hash;
               this.contract_type_id = contract_type_id;
               this.contract_version = contract_version;
               this.is_disabled = is_disabled;
               this.protocol_version = protocol_version;
               this.timestamp = timestamp;
           }

       
        [JsonProperty("contract_hash")]
        public string contract_hash { get; set; }

        [JsonProperty("contract_package_hash")]
        public string contract_package_hash { get; set; }

        [JsonProperty("deploy_hash")]
        public string deploy_hash { get; set; }

        [JsonProperty("contract_type_id")]
        public string? contract_type_id { get; set; }

        [JsonProperty("contract_version")]
        public string? contract_version { get; set; }

        [JsonProperty("is_disabled")]
        public string is_disabled { get; set; }

        [JsonProperty("protocol_version")]
        public string protocol_version { get; set; }

        [JsonProperty("timestamp")]
        public string timestamp { get; set; }

        [JsonProperty("pageCount")]
        public string pageCount { get; set; }

        [JsonProperty("itemCount")]
        public string itemCount { get; set; }

        [JsonProperty("pages")]
        public string pages { get; set; }

        [JsonProperty("number")]
        public string number { get; set; }

        [JsonProperty("url")]
        public string url { get; set; }
    }

    
    public class CasperArmyApi : ControllerBase
    {
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");
        private static string rpcUrl { get; } = ParserConfig.getToken("rpcUrl");

        public async Task<int> GetCountContractPages()
        {           
            string baseUrl = "https://api.cspr.live/contracts/?page=1&limit=100";
            int pagesCount = 0;

            CasperNetworkContractsList contractsList = new CasperNetworkContractsList();
            List<CasperNetworkContractsList> listContracts = new List<CasperNetworkContractsList>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JsonDocument doc = JsonDocument.Parse(data);
                                JsonElement root = doc.RootElement;

                                //   JsonArray contractsArray = root2["data"]!.AsArray();
                                //   int count = contractsArray.Count;
                                                             
                                if (root.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var item in root.EnumerateObject())
                                    {
                                        if (item.Name == "pageCount")
                                        {
                                            pagesCount = Convert.ToInt32(item.Value.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                pagesCount = 0;
            }
            return pagesCount;
        }

        public async Task<List<CasperNetworkContractsList>> GetContractsList(int pageNumber, int limit)
        {
            //bool debugMode = Convert.ToBoolean(debugMode);
        
        string baseUrl = "https://api.cspr.live/contracts/?page=" + pageNumber + "&limit=" + limit + "";

            CasperNetworkContractsList contractsList = new CasperNetworkContractsList();
            List<CasperNetworkContractsList> listContracts = new List<CasperNetworkContractsList>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = await client.GetAsync(baseUrl))
                    {
                        using (HttpContent content = res.Content)
                        {
                            var data = await content.ReadAsStringAsync();

                            if (data != null)
                            {
                                JsonDocument doc = JsonDocument.Parse(data);
                                JsonElement root = doc.RootElement;

                                //   JsonArray contractsArray = root2["data"]!.AsArray();
                                //   int count = contractsArray.Count;

                                using (JsonDocument docs = JsonDocument.Parse(data))
                                {
                                    JsonElement roots = docs.RootElement;
                                    JsonElement contractData = roots.GetProperty("data");
                                    foreach (JsonElement contract in contractData.EnumerateArray())
                                    {
                                        if (contract.TryGetProperty("contract_hash", out JsonElement contract_hash))
                                        {
                                            contractsList.contract_hash = contract_hash.GetString();

                                        //    var casperSdk = new NetCasperClient(rpcUrl);
                                        //    var casperQuery = await casperSdk.QueryGlobalState("contract-" + contractsList.contract_hash);

                                        }
                                        if (contract.TryGetProperty("contract_package_hash", out JsonElement contract_package_hash))
                                        {
                                            contractsList.contract_package_hash = contract_package_hash.GetString();
                                        }
                                        if (contract.TryGetProperty("deploy_hash", out JsonElement deploy_hash))
                                        {
                                            contractsList.deploy_hash = deploy_hash.GetString();
                                        }
                                        if (contract.TryGetProperty("contract_type_id", out JsonElement contract_type_id))
                                        {
                                            try
                                            {
                                                contractsList.contract_type_id = contract_type_id.GetInt32().ToString();//.GetString();
                                            }
                                            catch
                                            {
                                                contractsList.contract_type_id = "null";
                                            }
                                        }
                                        if (contract.TryGetProperty("contract_version", out JsonElement contract_version))
                                        {
                                            contractsList.contract_version = contract_version.GetInt32().ToString();
                                        }
                                        if (contract.TryGetProperty("is_disabled", out JsonElement is_disabled))
                                        {
                                            contractsList.is_disabled = is_disabled.GetBoolean().ToString();
                                        }
                                        if (contract.TryGetProperty("protocol_version", out JsonElement protocol_version))
                                        {
                                            contractsList.protocol_version = protocol_version.GetString();
                                        }
                                        if (contract.TryGetProperty("timestamp", out JsonElement timestamp))
                                        {
                                            contractsList.timestamp = timestamp.GetString();
                                        }

                                        listContracts.Add(new CasperNetworkContractsList(contractsList.contract_hash, contractsList.contract_package_hash, contractsList.deploy_hash, contractsList.contract_type_id, contractsList.contract_version, contractsList.is_disabled, contractsList.protocol_version, contractsList.timestamp));
                                    }                                   
                                }                                

                                if (root.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var item in root.EnumerateObject())
                                    {                                        
                                        if (item.Name == "itemCount")
                                        {
                                            contractsList.itemCount = item.Value.ToString();
                                        }

                                        if (item.Name == "pageCount")
                                        {
                                            contractsList.pageCount = item.Value.ToString();
                                        }
                                    }
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
             //   if (debugMode)
             //       Console.WriteLine(ex);
                return listContracts;
            }
            return listContracts;
        }

        public void GetAllContractsFromCasperNode()
        {
            NpgsqlConnection myConn = new NpgsqlConnection(psqlServer);

            CasperArmyApi casperApi = new CasperArmyApi();

            var pages = casperApi.GetCountContractPages().Result;

            CasperNetworkContractsList contractsList = new CasperNetworkContractsList();

            List<CasperNetworkContractsList> fullListContracts = new List<CasperNetworkContractsList>();

            for (int countPage = 1; countPage <= pages; countPage++)
            {
                List<CasperNetworkContractsList> listContracts = new List<CasperNetworkContractsList>();
                listContracts = casperApi.GetContractsList(countPage, 100).Result;

                fullListContracts.AddRange(listContracts);
                listContracts.Clear();
            }

            string buildStringInsertQuery = string.Empty;

            buildStringInsertQuery = "INSERT INTO casper_node_contracts (contract_hash, contract_package_hash, deploy_hash, contract_type_id, contract_version, is_disabled, protocol_version, creation_date) VALUES ";

            foreach (var contract in fullListContracts) //.ToList())
            {
                buildStringInsertQuery += "('" + contract.contract_hash + "','" + contract.contract_package_hash + "','" + contract.deploy_hash + "','" + contract.contract_type_id  + "','" + contract.contract_version + "','" + contract.is_disabled + "','" + contract.protocol_version + "','" + contract.timestamp + "'),";
               
            }

            buildStringInsertQuery = buildStringInsertQuery.Remove(buildStringInsertQuery.Length - 1);

            string clearTable = "DELETE FROM casper_node_contracts";
            try
            {
                myConn.Open();

                using (NpgsqlCommand cmd = new NpgsqlCommand(clearTable, myConn))
                {
                    cmd.ExecuteReader();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (myConn.State == ConnectionState.Open)
                    myConn.Close();
            }

            try
            {
                myConn.Open();

                using (NpgsqlCommand cmd = new NpgsqlCommand(buildStringInsertQuery, myConn))
                {
                    cmd.ExecuteReader();
                }
            }
            catch (Exception)
            {
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
