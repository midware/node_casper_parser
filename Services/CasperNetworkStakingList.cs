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
    public class CasperNetworkStakingList : ControllerBase
    {
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");

        bool debugMode = false;

        public CasperNetworkStakingList(/*IConfiguration configuration*/)
        {
        }        

        public CasperNetworkStakingList(string validator, string delegator, string staked_amount,  string timestamp)
           {
               this.validator = validator;
               this.delegator = delegator;
               this.staked_amount = staked_amount;
               this.timestamp = timestamp;
           }

       
        [JsonProperty("validator")]
        public string validator { get; set; }

        [JsonProperty("delegator")]
        public string delegator { get; set; }

        [JsonProperty("staked_amount")]
        public string staked_amount { get; set; }

        [JsonProperty("timestamp")]
        public string timestamp { get; set; }
    }

 //   [EnableCors("AllowOrigin")]
    public class CasperNetworkStaking : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public CasperNetworkStaking(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));
        }

        public List<CasperNetworkStakingList> AddCasperNodeStakingToList()
        {            
            blocksController blocks = new blocksController(_configuration);
            List<CasperNetworkStakingList> stakingList = new List<CasperNetworkStakingList>();

            int lastBlockInt = blocks.iiGetLastBlockHeigh().Result;
            // int lastBlockInt = Convert.ToInt32(lastBlockS);
            Console.WriteLine("Last Block Heigh: " + lastBlockInt);
            Console.WriteLine("Staking Table updating...");

            var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

            var auctionInfo = casperSdk.GetAuctionInfo(lastBlockInt).Result;
            // var getAuctionInfoResult = auctionInfo.Parse();
            var eraSummary = auctionInfo.Parse().AuctionState.Bids;

            foreach (var delegator in eraSummary)
            {
                // var groupDelegators = delegator.Delegators.GroupBy(d => d.PublicKey);
                int count = delegator.Delegators.Count;

                System.Numerics.BigInteger StakedAmount = 0;

                for (int delIndex = 0; delIndex < count; delIndex++)
                {
                    StakedAmount = delegator.Delegators[delIndex].StakedAmount;
                    StakedAmount /= 1_000_000_000;
                    
                    stakingList.Add(new CasperNetworkStakingList(delegator.PublicKey.ToString(), delegator.Delegators[delIndex].PublicKey.ToString(), StakedAmount.ToString(), "-"));
                }                
            }
            return stakingList;
        }

        public void GetAllStakingFromCasperNode()
        {
            NpgsqlConnection myConn = new NpgsqlConnection(_configuration.GetConnectionString("psqlServer"));

            List<CasperNetworkStakingList> lista =  AddCasperNodeStakingToList();
            List<CasperNetworkStakingList> fullListStaking = new List<CasperNetworkStakingList>();
            fullListStaking.AddRange(lista);
            lista.Clear();            

            string buildStringInsertQuery = string.Empty;

            buildStringInsertQuery = "INSERT INTO node_casper_delegators (public_key, delegatee, staked_amount, bonding_purse) VALUES ";

            foreach (var staking in fullListStaking)
            {
                buildStringInsertQuery += "('" + staking.validator.ToLower() + "','" + staking.delegator.ToLower() + "'," + staking.staked_amount.Replace(",", ".") + ", '-'),";
               
            }
            
            buildStringInsertQuery = buildStringInsertQuery.Remove(buildStringInsertQuery.Length - 1);
            //  Console.WriteLine(buildStringInsertQuery);
            Console.WriteLine("UPDATING STAKING TABLE...");

            string clearTable = "DELETE FROM node_casper_delegators";
            try
            {
                myConn.Open();

                using (NpgsqlCommand cmd = new NpgsqlCommand(clearTable, myConn))
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
            }

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
