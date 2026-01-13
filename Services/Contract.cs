
using System;
using System.Collections.Generic;
using System.Linq;
using Casper.Network.SDK.JsonRpc.ResultTypes;
using Casper.Network.SDK.Types;
using static NodeCasperParser.Services.CasperNodeDeployService;

//using CasperParser.Types.Config; // Make sure to include the correct namespace for the ConfigParsed class

using System;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Microsoft.AspNetCore.Mvc;
using Casper.Network.SDK.JsonRpc;
using NodeCasperParser.Helpers;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Newtonsoft.Json;
//using EnvisionStaking.Casper.SDK.Model.Common;

namespace NodeCasperParser.Services
{
    public class Contracts : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _configuration;

        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");
        private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");
        
        public class Contract
        {
            [JsonProperty("contract_package_hash")]
            public string ContractPackageHash { get; set; }
            [JsonProperty("contract_wasm_hash")]
            public string ContractWasmHash { get; set; }
            [JsonProperty("named_keys")]
            public List<NamedKey> NamedKeys { get; set; }
            [JsonProperty("entry_points")]
            public List<EntryPoint> EntryPoints { get; set; }
        }

        public class NamedKey
        {
            public string Name { get; set; }
            public string Key { get; set; }
            public bool IsPurse { get; set; }
            public object InitialValue { get; set; }
        }

        public class EntryPoint
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("args")]
            public List<Arg> Args { get; set; }
            public object Ret { get; set; }
            public object Access { get; set; }
            public string EntryPointType { get; set; }
        }

        public class Arg
        {
            public string Name { get; set; }
            public object ClType { get; set; }
        }

        public class MainPurse
        {
            public stored_value stored_value { get; set; }
        }

        public class stored_value
        {
            [JsonProperty("Contract")]
            public Contract Contract { get; set; }
            [JsonProperty("Account")]
            public Account Account { get; set; }
            [JsonProperty("CLValue")]
            public CLValue CLValue { get; set; }
        }

        public class Account
        {
            public string main_purse { get; set; }
        }

        public class PurseBalanceResponse
        {
            public string balance_value { get; set; }
        }

        public class CLValue
        {
            public object parsed { get; set; }
        }

        // test: account-hash-fa12d2dd5547714f8c2754d418aa8c9d59dc88780350cb4254d622e2d4ef7e69
        /// <summary>
        /// Get Main Purse
        /// </summary>
        /// <param name="client"></param>
        /// <param name="account_hash"></param>
        /// <returns>stored_value.Account.main_purse</returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> GetMainPurse(Casper.Network.SDK.NetCasperClient client, string account_hash)
        {
            try
            {
                if (!account_hash.Contains("account-hash-"))
                {
                    account_hash = "account-hash-" + account_hash;
                }

                var queryGlobalState = await client.QueryGlobalState(account_hash);

                string parsedQuery = queryGlobalState.Result.GetRawText();
                var result = JsonConvert.DeserializeObject<MainPurse>(parsedQuery);

                return result.stored_value.Account.main_purse;
            }
            catch (Exception ex)
            {
                return null;
                //throw new Exception($"failed to get result: {ex.Message}");
            }
        }


        // Test: uref-bb9f47c30ddbe192438fad10b7db8200247529d6592af7159d92c5f3aa7716a1-007
        public async Task<(string, bool)> GetPurseBalance(Casper.Network.SDK.NetCasperClient client, string uref_hash)
        {
            try
            {
                /* if (!uref_hash.Contains("uref-"))
                 {
                     uref_hash = "uref-" + uref_hash;
                 }*/

                var queryGlobalState = await client.GetAccountBalance(uref_hash);

                string parsedQuery = queryGlobalState.Result.GetRawText();
                var result = JsonConvert.DeserializeObject<PurseBalanceResponse>(parsedQuery);

                return (result.balance_value, true);
            }
            catch (Exception ex)
            {
                return (null, false);
            }
        }

        public async Task<(string?, bool)> GetUrefValue(Casper.Network.SDK.NetCasperClient client, string hash)
        {
            try
            {
                var queryGlobalState = await client.QueryGlobalState(hash);

                string parsedQuery = queryGlobalState.Result.GetRawText();
                var parsedUref = JsonConvert.DeserializeObject<MainPurse>(parsedQuery)?.stored_value?.CLValue?.parsed;

                if (parsedUref == null)
                {
                    var (balance, isBalanceSuccess) = await GetPurseBalance(client, hash);

                    if (isBalanceSuccess)
                    {
                        return (balance, true);
                    }
                }

                var parsedValueJson = JsonConvert.SerializeObject(parsedUref);

                return (parsedValueJson, false);
            }
            catch (Exception ex)
            {
                return (null, false);
            }
        }

        // example testnet contract: db3a41adea55e5ae65c8cba29d8e8527a16ac5fa998a76dfed553215e3254090
        public async Task<string> GetContract(string hash)
        {
            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
             
                var resp = await client.QueryGlobalState("hash-"+hash);
                               
                string contractParsed = resp.Result.GetRawText();
                
                var result = JsonConvert.DeserializeObject(contractParsed);
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"failed to get result: {ex.Message}", ex);
            }
        }

        public async Task<MainPurse> GetContractStoredValue(string hash)
        {
            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);

                var resp = await client.QueryGlobalState("hash-" + hash);

                string contractParsed = resp.Result.GetRawText();
                var result = JsonConvert.DeserializeObject<MainPurse>(contractParsed);

               // var result = JsonConvert.DeserializeObject<StoredValue>(contractParsed);
              //  var result = JsonConvert.DeserializeObject<stored_value>(contractParsed);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"failed to get result: {ex.Message}", ex);
            }
        }

        public async Task<(string, string)> GetNewestContractFromContractPackageHash(string hash)
        {
            string contractPackageHash = string.Empty;
            string contractHash = string.Empty;

            try
            {
                Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
                var contractQuery = GlobalStateKey.FromString("hash-" + hash);
                var contractResponse = await client.QueryGlobalState(contractQuery);
                var package = contractResponse.Parse().StoredValue.ContractPackage;


                string contractParsed = contractResponse.Result.GetRawText();

                var result = JsonConvert.DeserializeObject<MainPurse>(contractParsed);
                
                if (package != null)
                {
                    var contract = package?.Versions?.OrderByDescending(item => item.Version).FirstOrDefault();
                    //Console.WriteLine("Latest CasperPunks contract hash: " + contract.Hash);

                    contractPackageHash = hash;
                    contractHash = contract.Hash.Replace("contract-", "");

                }

                if (package == null)
                {
                    contractPackageHash = contractResponse.Parse().StoredValue.Contract.ContractPackageHash;
                    contractPackageHash = contractPackageHash.Replace("contract-package-", "");

                    // Get the contract package using the state root hash and contract package hash
                    var stateItem = await client.QueryGlobalState("hash-"+contractPackageHash);
                    var contractPackage = stateItem.Parse().StoredValue.ContractPackage;

                    // Print the contract hashes associated with the contract package
                    foreach (var contract in contractPackage.Versions)
                    {
                        contractHash = contract.Hash;
                        Console.WriteLine($"Contract Hash: {contract.Hash}");
                    }
                                        
                    
                }

                return (contractPackageHash, contractHash.Replace("contract-",""));

            }
            catch (Exception ex)
            {
                throw new Exception($"failed to get result: {ex.Message}", ex);
            }
        }
    }
}