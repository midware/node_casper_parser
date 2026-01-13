using Casper.Network.SDK;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CasperParser
{   

    public class ContractParser
    {
        public async Task<(string, string)> GetContractNameAndSymbol(Casper.Network.SDK.NetCasperClient client, string jsonContractData)
        {
            JObject jsonObject = JObject.Parse(jsonContractData);
            string contractName = string.Empty;
            string contractSymbol = string.Empty;

            if (jsonObject.TryGetValue("stored_value", out JToken storedValueToken) && storedValueToken is JObject storedValueObject)
            {
                if (storedValueObject.TryGetValue("Contract", out JToken contractToken) && contractToken is JObject contractObject)
                {
                    if (contractObject.TryGetValue("named_keys", out JToken namedKeysToken))
                    {
                        foreach (JToken namedKey in namedKeysToken)
                        {
                            if (namedKey is JObject namedKeyObject)
                            {
                                if (namedKeyObject.TryGetValue("name", out JToken nameToken) && namedKeyObject.TryGetValue("key", out JToken keyToken))
                                {
                                    string name = nameToken.Value<string>();
                                    string key = keyToken.Value<string>();

                                    if (name == "name" || name == "contract_name" || name == "collection_name")
                                    {
                                      //  var resp1 = await client.QueryGlobalState(key);
                                      //  var dec = resp1.Result;
                                      //  var result = JsonConvert.DeserializeObject(dec.ToString());

                                        var resp = await client.GetDictionaryItem(key);
                                        var decodedUref = resp.Result;//.Parse().DictionaryKey.ToString();
                                        var result2 = JsonConvert.DeserializeObject(decodedUref.ToString());
                                        var xxx = (JObject)result2;
                                        JObject jObj = (JObject)xxx.SelectToken($"stored_value.CLValue");
                                        contractName = jObj.Property("parsed").Value.ToString();
                                    }

                                    if (name == "symbol" || name == "contract_symbol" || name == "collection_symbol")
                                    {
                                        //  var resp1 = await client.QueryGlobalState(key);
                                        //  var dec = resp1.Result;
                                        //  var result = JsonConvert.DeserializeObject(dec.ToString());

                                        var resp = await client.GetDictionaryItem(key);
                                        var decodedUref = resp.Result;//.Parse().DictionaryKey.ToString();
                                        var result2 = JsonConvert.DeserializeObject(decodedUref.ToString());
                                        var xxx = (JObject)result2;
                                        JObject jObj = (JObject)xxx.SelectToken($"stored_value.CLValue");
                                        contractSymbol = jObj.Property("parsed").Value.ToString();
                                    }

                                //    if (contractName != string.Empty && contractSymbol != string.Empty)
                                //        return (contractName, contractSymbol);
                                }
                            }
                        }
                    }
                }
                
            }
            return (contractName, contractSymbol);
        }

        public async Task<List<NamedKey>> RetrieveNamedKeyValues(Casper.Network.SDK.NetCasperClient client, string contractResult)
        {
            var contractFromJson = JsonConvert.DeserializeObject<JsonContractData>(contractResult);
            
            var namedKeys = new List<NamedKey>();

            foreach (var namedKey in contractFromJson.stored_value.Contract.named_keys)
            {
                if (namedKey.key.Contains("account-hash-"))
                {
                    namedKeys.Add(new NamedKey
                    {
                        key = namedKey.key,
                        name = namedKey.name,
                        is_purse = false,
                        initial_value = "null"
                    });
                }
                else
                {
                    NodeCasperParser.Services.Contracts contract = new NodeCasperParser.Services.Contracts();
                    var (value, isPurse) = await contract.GetUrefValue(client, namedKey.key);

                    namedKeys.Add(new NamedKey
                    {
                        key = namedKey.key,
                        name = namedKey.name,
                        is_purse = isPurse,
                        initial_value = value
                    });
                }
            }

            return namedKeys;
        }

        public (string, double) GetContractTypeAndScore(string jsonContract, string jsonContractTypesLayoutFilePath)
        {
            try
            {
                var contractFromJson = JsonConvert.DeserializeObject<JsonContractData>(jsonContract);
                var yamlContractTypes = File.ReadAllText(jsonContractTypesLayoutFilePath);
               
                var contractTypesDefinitions = JsonConvert.DeserializeObject<ContractTypesDefinition>(yamlContractTypes);

                string contractType = "unknown";
                int previousCount = 0;
                int contractPerfectScore = 0;

                if (contractTypesDefinitions != null)
                {
                    foreach (var contractTypeEntryDef in contractTypesDefinitions.contractTypes)
                    {
                        contractPerfectScore += contractTypeEntryDef.Value.entrypoints.Count;
                        contractPerfectScore += contractTypeEntryDef.Value.namedkeys.Count;

                        int count = CalculateScore(contractFromJson, contractTypeEntryDef.Value);
                        if (count > previousCount && count >= (contractPerfectScore / 4))
                        {
                            contractType = contractTypeEntryDef.Key;
                            previousCount = count;
                        }

                        contractPerfectScore = 0;
                    }
                }
                Console.WriteLine($"Result - Contract name: {contractType} Score: {previousCount} Perfect Score: {contractPerfectScore} Accuracy: {(double)previousCount / contractPerfectScore * 100}");

              //  Console.WriteLine($"Result - Contract name: {contractType} Score: {previousCount} Perfect Score: {contractPerfectScore} Accuracy: {score}");
                // 2115044
                double score = 0.0;
                if (contractPerfectScore > 0)
                {
                    score = ((double)previousCount / contractPerfectScore) * 100;
                }

                
                return (contractType, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing YAML: " + ex.Message);
                return (null, 0);

            }
        }

        private int CalculateScore(JsonContractData result, ContractDefinition contractType)
        {
            int count = 0;
             // coś źle się liczy w entrypoints - sprawdzić.
         /*   foreach (var entrypoint in result.stored_value.Contract.entry_points)
            {
                var flatArgs = entrypoint.args.Select(arg => arg.name).ToList();

                count += entrypoint.args.Count;                

                foreach (var contractDefEntrypoint in contractType.entrypoints)
                {
                    count += contractDefEntrypoint.args.Count;

                    if (contractDefEntrypoint.args.Contains(contractDefEntrypoint.name))
                    {
                        count++;
                    }
                }
            }*/

            count += GetNamedKeysScore(result.stored_value?.Contract?.named_keys, contractType.namedkeys);

            return count;
        }

        public int GetNamedKeysScore(List<NamedKey> storedNamedKeys, List<string> namedKeys)
        {
            int count = 0;
            if (!storedNamedKeys.IsNullOrEmpty())
            {
                foreach (var namedKey in storedNamedKeys)
                {
                    if (namedKeys.Contains(namedKey.name))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }

    public class JsonContractData
    {
        public StoredValue stored_value { get; set; }
    }

    public class StoredValue
    {
        public Contract Contract { get; set; }
    }

    public class Contract
    {
        public string contract_package_hash { get; set; }
        public string contract_wasm_hash { get; set; }
        public List<NamedKey> named_keys { get; set; }
        public List<Entrypoint> entry_points { get; set; }
        public string protocol_version { get; set; }
    }

    public class NamedKey
    {
        public string name { get; set; }
        public string key { get; set; }
        public bool is_purse { get; set; }
        public object initial_value { get; set; }
    }

    public class Entrypoint
    {
        public string name { get; set; }
        public List<Arg> args { get; set; }
        public object ret { get; set; }
        public object access { get; set; }
        public string entry_point_type { get; set; }
    }
    
    public class Arg
    {
        public string name { get; set; }
        public object cl_type { get; set; }
    }
    
    public class ModuleByte
    {
        public bool StrictArgs { get; set; }
        public List<string> Args { get; set; }
        public List<string> Events { get; set; }
    }
   
    public class ContractTypesDefinition
    {
        public Dictionary<string, ContractDefinition> contractTypes { get; set; }
      // public Dictionary<string, ModuleByte> moduleBytes { get; set; }
    }

    public class ContractDefinition
    {
        public List<EntrypointsDefinition> entrypoints { get; set; }
        public List<string> namedkeys { get; set; }
    }

    public class EntrypointsDefinition
    {
        public string name { get; set; }
        public List<string> args { get; set; }
    }
}
