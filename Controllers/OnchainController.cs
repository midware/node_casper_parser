using Microsoft.AspNetCore.Mvc;
using System;
using NodeCasperParser;
using NodeCasperParser.Services;
using Casper.Network.SDK;
using Newtonsoft.Json;
using Casper.Network.SDK.JsonRpc;
using System.Diagnostics;
using NodeCasperParser.Controllers;

namespace NodeCasperParser
{
    [Route("[controller]")]
    public class onchainController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public onchainController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("GetLastEndedEra")]
        public int GetLastEndedEra()
        {
            // SELECT era, height, "timestamp", era_end FROM public.node_casper_blocks WHERE era_end = true ORDER BY height DESC LIMIT 1
            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var lastBlockResult = client.GetBlock().Result.Parse();

                int eraId = Convert.ToInt32(lastBlockResult.Block.Header.EraId);
                if (debugMode)
                    Console.WriteLine("Ended Era Id: " + (eraId - 1).ToString());

                //   var lastBlockResult2 = rpc.GetEraInfoLast();//.GetBlockLast();
                //   lastBlockResult2.result.era_summary.stored_value.Transfer.amount.
                return eraId - 1;

            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine(ex);
                return -1;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("StakingDataFromNode")]
        public async Task<string> StakingDataFromNode()
        {
            blocksController blocks = new blocksController(_configuration);

            try
            {
               // onchainController node = new onchainController(_configuration);
                int lastBlockInt = blocks.iGetLastBlockHeigh().Result.Value;
                //  string lastBlockHash = GetLastBlockHash();

                double sumValidatorsStaking = 0;
                double sumDelegatorsStaking = 0;
                double sumValidatorRewards = 0;

                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                int previewsBlock = lastBlockInt;// - 1;
                var auctionInfo = await casperSdk.GetAuctionInfo(previewsBlock);
                var getAuctionInfoResult = auctionInfo.Parse();

                int lastEra = GetLastEndedEra();
                var endedEraData = await casperSdk.QueryGlobalState("era-" + lastEra);
                var isEraEnded = endedEraData.Parse();

                var xxxxxx = isEraEnded.StoredValue.EraInfo;

                var groupedByValidator = getAuctionInfoResult.AuctionState.Bids
                    .GroupBy(allocation => allocation.PublicKey);

                foreach (var group in groupedByValidator)
                {
                    var eraValidatorsStaked = group.Sum(a => (double)a.StakedAmount);
                    // Console.WriteLine(eraValidatorsStaked); // DATA TEST
                    sumValidatorsStaking += eraValidatorsStaked;

                }

                var groupedByValidator2 = xxxxxx.SeigniorageAllocations
                    .GroupBy(allocation => allocation.ValidatorPublicKey);

                foreach (var group in groupedByValidator2)
                {
                    var eraRewards = group.Sum(a => (double)a.Amount);
                    // Console.WriteLine(eraRewards); // DATA TEST
                    sumValidatorRewards += eraRewards;

                }

                foreach (var group in getAuctionInfoResult.AuctionState.Bids)
                {
                    var eraDelegatorsStaked = group.Delegators.Sum(a => (double)a.StakedAmount);
                    // Console.WriteLine(eraDelegatorsStaked); // DATA TEST
                    sumDelegatorsStaking += eraDelegatorsStaked;
                }

                sumValidatorsStaking /= 1_000_000_000;
                sumDelegatorsStaking /= 1_000_000_000;
                sumValidatorRewards /= 1_000_000_000;

                // Sum of staking
                var sumStaking = sumValidatorsStaking + sumDelegatorsStaking + sumValidatorRewards;

                double apy = 0;
                double totalsupply = 11353204867; // static for testing
                                                  // circulating	10415477368
                                                  // APY = (Total Supply * 0.08) / Total Stake

                apy = ((totalsupply * 0.08) / sumStaking) * 100;
                // "apy": total_supply*.08*100000000000/era_stake,

                if (debugMode)
                {
                    Console.WriteLine("APY : " + apy.ToString("N2") + "%");
                    Console.WriteLine("sumValidatorsStaking : " + sumValidatorsStaking.ToString());
                    Console.WriteLine("sumDelegatorsStaking : " + sumDelegatorsStaking.ToString());
                    Console.WriteLine("sumValidatorRewards : " + sumValidatorRewards.ToString());
                    Console.WriteLine("TOTAL: " + sumStaking.ToString());
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return "";
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet] // DZIA£A
        [Route("Onchain_GetStakingAsValidator")]
        public async Task<ActionResult> GetStakingAsValidatorOnchain()
        {
            // offchainController platformNodeStats = new offchainController(_configuration);
            blocksController blocks = new blocksController(_configuration);

            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);

            var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
            string lastBlockHash = blocks.sGetLastBlockHash().Result;
            int lastBlockHeigh = blocks.iGetLastBlockHeigh().Result.Value;
            var rpcResponse1 = await casperSdk.GetAuctionInfo(lastBlockHeigh);
            var sss = rpcResponse1.Parse();

            var eraCount = sss.AuctionState.EraValidators.Count;

            writer.WriteRaw("{");
            writer.WriteRaw(@"""data""");
            writer.WriteRaw(":");
            writer.WriteStartArray();

            for (int era = 0; era < eraCount; era++)
            {
                var validatorCount = sss.AuctionState.EraValidators[era].ValidatorWeights.Count;

                for (int validatorIndex = 0; validatorIndex < validatorCount; validatorIndex++)
                {
                    var EraId = sss.AuctionState.EraValidators[era].EraId.ToString();
                    var publicKey = sss.AuctionState.EraValidators[era].ValidatorWeights[validatorIndex].PublicKey.ToString();
                    var weight = sss.AuctionState.EraValidators[era].ValidatorWeights[validatorIndex].Weight.ToString();

                    writer.WriteStartObject();

                    writer.WritePropertyName("EraId");
                    writer.WriteValue(EraId);

                    writer.WritePropertyName("publicKey");
                    writer.WriteValue(publicKey);

                    writer.WritePropertyName("weight");
                    writer.WriteValue(weight);

                    writer.WriteEndObject();

                    // Console.WriteLine(validatorIndex + ". EraID: " + EraId + " | " + publicKey.ToString() + " " + weight.ToString());
                }
            }
            writer.WriteEndArray();

            writer.WriteRaw("}");

            var result = JsonConvert.DeserializeObject(sw.ToString());

            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet] // DZIA£A
        [Route("Onchain_GetStakingAsDelegator")]
        public async Task<ActionResult> GetStakingAsDelegatorOnchain()
        {
            blocksController blocks = new blocksController(_configuration);

            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);

            var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
            string lastBlockHash = blocks.sGetLastBlockHash().Result;
            int lastBlockHeigh = blocks.iGetLastBlockHeigh().Result.Value;
            var rpcResponse1 = await casperSdk.GetAuctionInfo(lastBlockHeigh);
            var sss = rpcResponse1.Parse();

            var bids = sss.AuctionState.Bids;

            writer.WriteRaw("{");
            writer.WriteRaw(@"""data""");
            writer.WriteRaw(":");
            writer.WriteStartArray();
            int noCount = 0;

            var bidsCount = bids.Count;

            for (int bidIndex = 0; bidIndex < bidsCount; bidIndex++)
            {
                var delegatorIndex = bids[bidIndex].Delegators.Count;

                for (int delegator = 0; delegator < delegatorIndex; delegator++)
                {
                    noCount += 1;
                    // writer.WriteStartObject();
                    var Delegatee = bids[bidIndex].Delegators[delegator].Delegatee.ToString();
                    //    writer.WritePropertyName("Delegatee");
                    //    writer.WriteValue(Delegatee);

                    var PublicKey = bids[bidIndex].Delegators[delegator].PublicKey.ToString();
                    //    writer.WritePropertyName("publicKey");
                    //    writer.WriteValue(PublicKey);

                    var StakedAmount = bids[bidIndex].Delegators[delegator].StakedAmount.ToString();
                    //    writer.WritePropertyName("StakedAmount");
                    //    writer.WriteValue(StakedAmount);

                    //    writer.WriteEndObject();

                    Console.WriteLine(noCount + ". " + Delegatee + " " + PublicKey + " " + StakedAmount);
                }
            }

            writer.WriteEndArray();

            writer.WriteRaw("}");

            var result = JsonConvert.DeserializeObject(sw.ToString());

            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        // [Route("GetTotalStaking/{blockHash}")]
        [Route("GetTotalStaking")]
        public async Task<double> GetTotalStaking() // DZIA£A
        {
            blocksController blocks = new blocksController(_configuration);

            //    int lastBlockInt = node.GetLastBlockHeigh();
            string lastBlockHash = blocks.sGetLastBlockHash().Result;

            try
            {
                double sumValidatorsStaking = 0;
                double sumDelegatorsStaking = 0;
                double sumValidatorRewards = 0;

                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var auctionInfo = await casperSdk.GetAuctionInfo(lastBlockHash);
                var getAuctionInfoResult = auctionInfo.Parse();

                int lastEra = GetLastEndedEra();
                var endedEraData = await casperSdk.QueryGlobalState("era-" + lastEra);
                var isEraEnded = endedEraData.Parse();
                var eraInfo = isEraEnded.StoredValue.EraInfo;

                // Validators Rewards
                var groupedByValidator = getAuctionInfoResult.AuctionState.Bids
                    .GroupBy(allocation => allocation.PublicKey);

                double eraValidatorsStaked = 0;

                foreach (var group in groupedByValidator)
                {
                    eraValidatorsStaked = group.Sum(a => (double)a.StakedAmount);

                    sumValidatorsStaking += eraValidatorsStaked;

                    if (debugMode)
                    {
                        Console.WriteLine($"{group.Key} - {group.Count(),5} - " +
                                       $"{eraValidatorsStaked.ToString("N9"),20} $CSPR | " + eraValidatorsStaked.ToString());
                        Console.WriteLine(eraValidatorsStaked.ToString());
                    }
                }



                // Delegators staking - Total Stake)
                var eraSummary = auctionInfo.Parse().AuctionState.Bids;

                foreach (var group in eraSummary)
                {
                    var eraDelegatorsStaked = group.Delegators.Sum(a => (double)a.StakedAmount);

                    sumDelegatorsStaking += eraDelegatorsStaked;

                    if (debugMode)
                    {
                        Console.WriteLine($"{group.PublicKey} - " +
                                       $"{eraDelegatorsStaked.ToString("N9"),20} $CSPR | " + eraDelegatorsStaked.ToString());
                        Console.WriteLine(eraDelegatorsStaked.ToString());
                    }
                }

                // Validators Rewards            
                var groupedByValidator2 = eraInfo.SeigniorageAllocations
                    .GroupBy(allocation => allocation.ValidatorPublicKey);

                foreach (var group in groupedByValidator2)
                {
                    var eraRewards = group.Sum(a => (double)a.Amount);

                    sumValidatorRewards += eraRewards;
                    Console.WriteLine(eraRewards);

                    if (debugMode)
                    {
                        //     Console.WriteLine($" - " +
                        //                  $"{eraRewards.ToString("N9"),20} $CSPR | " + eraRewards.ToString());
                        Console.WriteLine(eraRewards.ToString());
                    }
                }

                sumValidatorsStaking /= 1_000_000_000;
                sumDelegatorsStaking /= 1_000_000_000;
                sumValidatorRewards /= 1_000_000_000;

                // Sum of staking
                var sumStaking = sumValidatorsStaking + sumDelegatorsStaking;// + sumValidatorRewards;

                double apy = 0;
                //double totalsupply = 11241328577; // static for testing
                // circulating	10415477368
                // APY = (Total Supply * 0.08) / Total Stake

                double totalSupply = await GetTotalSupply();

                apy = ((totalSupply * 0.08) / sumStaking) * 100;

                if (debugMode)
                {
                    //    Console.WriteLine("APY : " + apy.ToString("N2") + "%");
                    //    Console.WriteLine("sumValidatorsStaking : " + sumValidatorsStaking.ToString());
                    //    Console.WriteLine("sumDelegatorsStaking : " + sumDelegatorsStaking.ToString());
                    //    Console.WriteLine("sumValidatorRewards : " + sumValidatorRewards.ToString());
                    //    Console.WriteLine("TOTAL: " + sumStaking.ToString());
                }
                return sumStaking;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine("Error: " + ex.ToString());
                }
                return -1;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<double> GetTotalSupply()
        {
            var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
            string apiEnvironment = _configuration.GetConnectionString("ENVIRONMENT");
            double supplyT = 0;


            if (String.Equals(apiEnvironment, "Mainnet"))
            {
                var casperQuery = await casperSdk.QueryGlobalState("uref-8032100a1dcc56acf84d5fc9c968ce8caa5f2835ed665a2ae2186141e9946214-007");
                var totalSupply = Convert.ToDouble(casperQuery.Parse().StoredValue.CLValue.Parsed);
                supplyT = totalSupply /= 1_000_000_000;
            }
            else // TESTNET
            {
                var casperQuery = await casperSdk.QueryGlobalState("uref-5d7b1b23197cda53dec593caf30836a5740afa2279b356fae74bf1bdc2b2e725-007");
                var totalSupply = Convert.ToDouble(casperQuery.Parse().StoredValue.CLValue.Parsed);
                supplyT = totalSupply /= 1_000_000_000;
            }

            return supplyT;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("XXXXXGetSelfStakedByValidators")]
        public async Task<double> GetSelfStakedByValidators()
        {
            blocksController blocks = new blocksController(_configuration);

            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));

                string lastBlockHash = blocks.sGetLastBlockHash().Result;

                var auctionInfo = await casperSdk.GetAuctionInfo(/*lastBlockHash*/"fb2d932fb751bb7e3a173fee919f38fb29e7148e7523faa23b4f14cba08aaf58");
                var getAuctionInfoResult = auctionInfo.Parse();

                //   var groupedByValidator = getAuctionInfoResult.AuctionState.Bids
                //        .GroupBy(allocation => allocation.PublicKey);

                var groupedByValidator = getAuctionInfoResult.AuctionState.Bids
                    .Where(allocation => allocation.PublicKey.ToString() == "020377bc3ad54b5505971e001044ea822a3f6f307f8dc93fa45a05b7463c0a053bed");

                double sumStaking = 0;

                foreach (var group in groupedByValidator)
                {
                    var eraStaked = (double)group.StakedAmount;// group.Sum(a => (double)a.StakedAmount);

                    //     var eraStaked = group.Sum(a => (double)a.StakedAmount);

                    sumStaking /= 1_000_000_000;
                    eraStaked /= 1_000_000_000;

                    sumStaking += eraStaked;

                    //   if (debugMode)
                    //       Console.WriteLine($"{group.Key} - {group.Count(),5} - " +
                    //                        $"{eraStaked.ToString("N9"),20} $CSPR | " + eraStaked.ToString());

                }
                sumStaking /= 1_000_000_000;

                if (debugMode)
                    Console.WriteLine($"Total: " + sumStaking.ToString());

                return sumStaking;
            }
            catch (Exception ex)
            {
                if (debugMode)
                    Console.WriteLine("Error: " + ex.ToString());
                return -1;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("account/balance/{public_key}")]
        public async Task<double> GetAccountBalance(string public_key)
        {
            ulong csprUserBalance = 0;

            try
            {
                string sqlDataSource = _configuration.GetConnectionString("psqlServer");
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var accountBalance = await casperSdk.GetAccountBalance(public_key);
                string bal = accountBalance.Parse().BalanceValue.ToString();
                ulong asignelulong = Convert.ToUInt64(bal);
                csprUserBalance = (ulong)(asignelulong / 1000000000);

                return csprUserBalance;
            }
            catch
            {
                return csprUserBalance;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("blocks/proposer/{blockHash}")]
        public async Task<JsonResult> GetEraInfo(string blockHash)
        {
            blocksController blocks = new blocksController(_configuration);

            try
            {
                var casperSdk = new NetCasperClient(_configuration.GetConnectionString("rpcUrl"));
                var getEraInfo = await casperSdk.GetEraInfoBySwitchBlock(blockHash);

                int lastBlockResult = blocks.iGetLastBlockHeigh().Result.Value;
                var getBlock = casperSdk.GetBlock(lastBlockResult.ToString());


                if (debugMode)
                {
                    // Console.WriteLine(ddddd.ToString());
                }

                return new JsonResult("");// "";// result.ToString();
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine("Error: " + ex.ToString());
                }
                return new JsonResult("Error: " + ex.ToString());
            }
        }        
    }
}
