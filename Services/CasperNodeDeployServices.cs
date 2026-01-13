namespace NodeCasperParser.Services;

using System.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Npgsql;
using Casper.Network.SDK;
using NodeCasperParser.Helpers;

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Casper.Network.SDK.SSE;
using Org.BouncyCastle.Math;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
//using EnvisionStaking.Casper.SDK.Model.Base;
using System.Numerics;
using System.Collections.Generic;
using Casper.Network.SDK.JsonRpc.ResultTypes;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.JsonRpc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Google.Protobuf.WellKnownTypes;
using System.Reflection;
using System.Text;
//using EnvisionStaking.Casper.SDK.Model.Common;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Tokens;
using Casper.Network.SDK.Clients;
using Org.BouncyCastle.Utilities;
//using EnvisionStaking.Casper.SDK.Model.DeployObject;
using System.Text.RegularExpressions;
using System.Linq;
using static NodeCasperParser.Services.CasperNodeDeployService;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq.Expressions;
using CasperParser;
using SixLabors.ImageSharp.Drawing.Processing;
using static System.Net.Mime.MediaTypeNames;
using Google.Protobuf;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Routing;
using YamlDotNet.Core;
using NodeCasperParser.Cryptography.CasperNetwork;
using NodeCasperParser.Controllers;
using System.Globalization;

public interface ICasperNodeDeployService
{
   
}


public class CasperNodeDeployService : ICasperNodeDeployService
{
    private readonly AppSettings _appSettings;
    private readonly IConfiguration _configuration;

    private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
    private static string debugModes { get; } = ParserConfig.getToken("debugMode");
    private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");

    bool debugMode = false;

    public class Result
    {
        [JsonPropertyName("deploy")]
        public JsonDeploy Deploy { get; set; }

        [JsonPropertyName("execution_results")]
        public List<ExecutionResult> ExecutionResults { get; set; }
    }

    public class JsonDeploy
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("header")]
        public Header Header { get; set; }

        [JsonPropertyName("payment")]
        public Payment Payment { get; set; }

        [JsonPropertyName("session")]
        public Session Session { get; set; }

        [JsonPropertyName("approvals")]
        public List<Approval> Approvals { get; set; }
    }

    public class Header
    {
        [JsonPropertyName("account")]
        public string Account { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("ttl")]
        public string Ttl { get; set; }

        [JsonPropertyName("gas_price")]
        public int GasPrice { get; set; }

        [JsonPropertyName("body_hash")]
        public string BodyHash { get; set; }

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; }

        [JsonPropertyName("chain_name")]
        public string ChainName { get; set; }
    }

    public class Payment
    {
        [JsonPropertyName("ModuleBytes")]
        public ModuleBytes ModuleBytes { get; set; }
    }

    public class Session
    {
        [JsonPropertyName("Transfer")]
        public Transfer Transfer { get; set; }

        [JsonPropertyName("StoredContractByHash")]
        public StoredContractByHash StoredContractByHash { get; set; }

        [JsonPropertyName("StoredContractByName")]
        public StoredContractByName StoredContractByName { get; set; }

        [JsonPropertyName("StoredVersionedContractByHash")]
        public StoredVersionedContractByHash StoredVersionedContractByHash { get; set; }

        [JsonPropertyName("StoredVersionedContractByName")]
        public StoredVersionedContractByName StoredVersionedContractByName { get; set; }

        [JsonPropertyName("ModuleBytes")]
        public ModuleBytes ModuleBytes { get; set; }
    }

    public class StoredVersionedContractByName
    {
        public string Name { get; set; }
        public int Version { get; set; }
        public string EntryPoint { get; set; }
        public List<List<object>> Args { get; set; }
    }

    public class ModuleBytes
    {
        public List<List<object>> Args { get; set; }
    }


    public class Approval
    {
        [JsonPropertyName("signer")]
        public string Signer { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

    public class ExecutionResult
    {
        public string BlockHash { get; set; }
        public ResultData Result { get; set; }
    }

    public class ResultData
    {
        public SuccessData Success { get; set; }
        public FailureData Failure { get; set; }
    }

    public class SuccessData
    {
        public object Effect { get; set; }
        public List<string> Transfers { get; set; }
        public string Cost { get; set; }
    }

    public class FailureData
    {
        public object Effect { get; set; }
        public List<string> Transfers { get; set; }
        public string Cost { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Transfer
    {
        [JsonPropertyName("args")]
        public List<List<object>> Args { get; set; }
    }

    public class StoredContractByHash
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("entry_point")]
        public string EntryPoint { get; set; }

        [JsonPropertyName("args")]
        public List<List<object>> Args { get; set; }
    }

    public class StoredContractByName
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("entry_point")]
        public string EntryPoint { get; set; }

        [JsonPropertyName("args")]
        public List<List<object>> Args { get; set; }
    }

    public class StoredVersionedContractByHash
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("entry_point")]
        public string EntryPoint { get; set; }

        [JsonPropertyName("args")]
        public List<List<object>> Args { get; set; }
    }

    public struct Effect
    {
        public Operation[] Operations { get; set; }
        public Dictionary<string, object>[] Transforms { get; set; }
    }

    public struct Operation
    {
        public string Key { get; set; }
        public string Kind { get; set; }
    }

    public struct TransferMetadata
    {
        public string ID { get; set; }
        public string From { get; set; }
        public string Hash { get; set; }
        public string Amount { get; set; }
        public string Target { get; set; }
    }

    public struct StoredContract
    {
        public string DeployName { get; set; }
        public bool HasName { get; set; }
        public string[] Args { get; set; }
        public string ContractName { get; set; }
        public string[] Events { get; set; }
    }
    public CasperNodeDeployService(/*IConfiguration configuration*/)
    {
        // _configuration = configuration;
    }

    public class PurseBalanceResponse
    {
        public string balance { get; set; }
    }

    public void GetDeploy(NetCasperClient client, List<string> deploysList)
    {
        HashService hs = new HashService();
        CasperNodeDeployService deploys = new CasperNodeDeployService();
        PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

        CasperNodeDeployService cnds = new CasperNodeDeployService();
        PostgresCasperNodeService pcns = new PostgresCasperNodeService();
        NodeCasperParser.Services.Contracts contract = new NodeCasperParser.Services.Contracts();

      //  var deploy = DeployTemplates.
      //  var dd = deploy.
        
        foreach (var deploy in deploysList)
        {
          //  Thread.Sleep(5);

            string deployHash = deploy.ToLower();

            var rpcDeploy = client.GetDeploy(deployHash);
                       
            var (result, cost, CostException) = deploys.GetResultAndCostFromNode(rpcDeploy.Result.Parse());

           // if (result == false)
             //   Console.WriteLine("Execution Result False for deploy: " + deployHash);

            var blockHash = rpcDeploy.Result.Parse().ExecutionResults[0].BlockHash.ToLower();

            var resultDeployParse = rpcDeploy.Result.Parse();

            Console.WriteLine("Deploy/Transfer to add from Block: " + blockHash + " | Hash: " + deploy.ToLower());
                        
            var (metadataType, metadata) = deploys.GetDeployMetadataFromNode(resultDeployParse);
            var events = cnds.GetEventsFromNode(client, resultDeployParse); // DZIA£A - TEGO U¯YWAÆ 

            var from = rpcDeploy.Result.Parse().Deploy.Header.Account.ToString().ToLower();

            var jsonDeploy = rpcDeploy.Result.Result.GetRawText().ToString();
            var timestampDeploy = rpcDeploy.Result.Parse().Deploy.Header.Timestamp.ToString();

            var deployType = rpcDeploy.Result.Parse().Deploy.Session.GetType().Name.Replace("DeployItem", "");
                       
            //   var (contractName, contractNameException) = GetNameFromNode(contractHash, resultDeployParse);
            var (contractName, contractNameException) = deploys.GetNameFromNode(resultDeployParse);
            var (contractSymbol, contractSymbolException) = deploys.GetContractSymbolFromNode(resultDeployParse);

            var (entrypoint, EntryPointException) = deploys.GetEntrypointFromNode(resultDeployParse);

            // piszê teraz GetModuleByteMetadataFromNode
            var (contractHash, contractHashException) = deploys.GetStoredContractHashFromNode(resultDeployParse);

            if (contractHash == "unknown")
            {
                // cnds.GetWriteContractPackageFromJson(json);                
            }

            string contract_hash = string.Empty;
            string contract_package_hash = string.Empty;

            contract_hash = contractHash;

            try
            {
                if (from.Length > 0 && jsonDeploy.Length > 0)
                {
                    var accountHash = hs.GetAccountHash(from);
                    // var balance = client.GetAccountBalance("account-hash-" + accountHash);
                    var queryGetBalance = client.GetAccountBalance("account-hash-" + accountHash);
                    string parsedQueryBalance = queryGetBalance.Result.Result.GetRawText();
                    var balance = JsonConvert.DeserializeObject<PurseBalanceResponse>(parsedQueryBalance);
                    //  ulong asignelulong = Convert.ToUInt64(bal);
                    //   var csprUserBalance = (ulong)(asignelulong / 1000000000);
                    var bal = Convert.ToDecimal(balance.balance);/// 1000000000;
                    var insertAccount = pcns.InsertAccount(from, accountHash.ToLower(), "-", bal, timestampDeploy);

                    var writeContractPackage = cnds.GetWriteContractPackageFromJson(jsonDeploy);
                                      
                    for (int countContractPackage = 0; countContractPackage < writeContractPackage.Length; countContractPackage++)
                    {
                        var insertContractPackage = pcns.InsertContractPackage(writeContractPackage[countContractPackage], deployHash, from, jsonDeploy);

                        var writeContract = cnds.GetWriteContractFromJson(jsonDeploy);

                        for (int countContract = 0; countContract < writeContract.Length; countContract++)
                        {
                            var jsonContractData = contract.GetContract(writeContract[countContract]).Result;

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var parser = new ContractParser();
                            var (contractTypeName, score) = parser.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName2, contractSymbol2) = parser.GetContractNameAndSymbol(client, jsonContractData).Result;
                            contractName = contractName2;
                            contractSymbol = contractSymbol2;

                            var namedKeys = parser.RetrieveNamedKeyValues(client, jsonContractData).Result;
                            /*    foreach (var namedKey in namedKeys)
                                {
                                    var insertNamedKey = pcns.InsertNamedKey(namedKey.key, namedKey.name, namedKey.is_purse, namedKey.initial_value?.ToString(), writeContract[countContract]);
                                }
                            */
                            var insertContract = pcns.InsertContract(writeContract[countContract], writeContractPackage[countContractPackage], deployHash, from, contractName2, contractSymbol2, contractTypeName, score, jsonContractData.ToString());
                        }
                    }
                }

            }
            catch (Exception ex)
            {                
                   Console.WriteLine(ex.ToString());
            }

            try
            {
                //  var insertDeploy = postgresCasperNodeServices.InsertDeploy(deployHash, from, cost, result, timestampDeploy, blockHash, deployType, jsonDeploy, metadataType, contractHash, contractName, contractSymbol, entrypoint, metadata, events);
                var insertDeploy = postgresCasperNodeServices.InsertDeploy(deployHash, from, cost, result, timestampDeploy, blockHash, deployType, jsonDeploy, metadataType, contractHash, contractName, contractSymbol, entrypoint, metadata, events);
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertDeploy ERROR: " + ex.ToString());
            }

            try
            {
                NftParser.NftParser nftp = new NftParser.NftParser();

                var nft = nftp.GetNftFromEvent(client, result, from, contract_hash, contract_package_hash, events, metadataType, metadata, timestampDeploy);
            }
            catch(Exception ex)
            {
                Console.WriteLine("GetNftFromEvent ERROR: "+ ex.ToString());
            }
            CasperNetworkContractsEvents cnce = new CasperNetworkContractsEvents();

         //   string[] contract_hashes = { "14b81d7630eb37c88bb3e92368c6d8db2258bb47e0b09d5b0e22e6a35f6bbfab" };
         //   var ddd = cnce.GetContractsMetadata(contract_hashes, "ae9385bb9132efb2408e08ad9b021c74d3835f0041247370e95afcde08dd2e99").Result;
        }
    }

    public JObject SetPropertyContent(JObject source, string name, object content)
    {
        var prop = source.Property(name);
        if (prop == null)
        {
            prop = new JProperty(name, content);
            source.Add(prop);
        }
        else
        {
            prop.Value = JContainer.FromObject(content);
        }
        return source;
    }


    /// <summary>
    /// Get contract entrypoint
    /// </summary>
    /// <param name="d">GetDeployResult</param>
    /// <returns>string, Exception</returns>
    public (string, Exception) GetEntrypointFromNode(GetDeployResult d)
    {
        var deployResult = d.Deploy.SerializeToJson();

        var jsonObject = JObject.Parse(deployResult);

        if (jsonObject.TryGetValue("session", out JToken sessionToken) && sessionToken is JObject sessionObject)
        {
            if (sessionObject.TryGetValue("StoredContractByHash", out JToken storedContractByHashToken) && storedContractByHashToken is JObject storedContractByHashObject)
            {
                if (storedContractByHashObject.TryGetValue("entry_point", out JToken entryPointToken))
                {
                    string entryPoint = (string)entryPointToken;
                    return (entryPoint, null);
                }
            }

            if (sessionObject.TryGetValue("StoredContractByName", out JToken storedContractByNameToken) && storedContractByNameToken is JObject storedContractByNameObject)
            {
                if (storedContractByNameObject.TryGetValue("entry_point", out JToken entryPointToken))
                {
                    string entryPoint = (string)entryPointToken;
                    return (entryPoint, null);
                }
            }

            if (sessionObject.TryGetValue("StoredVersionedContractByHash", out JToken storedVersionedContractByHashToken) && storedVersionedContractByHashToken is JObject storedVersionedContractByHashObject)
            {
                if (storedVersionedContractByHashObject.TryGetValue("entry_point", out JToken entryPointToken))
                {
                    string entryPoint = (string)entryPointToken;
                    return (entryPoint, null);
                }
            }

            if (sessionObject.TryGetValue("StoredVersionedContractByName", out JToken storedVersionedContractByNameToken) && storedVersionedContractByNameToken is JObject storedVersionedContractByNameObject)
            {
                if (storedVersionedContractByNameObject.TryGetValue("entry_point", out JToken entryPointToken))
                {
                    string entryPoint = (string)entryPointToken;
                    return (entryPoint, null);
                }
            }
        }

        return ("unknown", new Exception($"deploy {d.Deploy.Hash} doesn't have a entrypoint"));
    }

    public List<List<object>> GetArgsFromNode(GetDeployResult d)
    {
        if (d.Deploy.Session.RuntimeArgs.Count > 0)
        {
            List<List<object>> listOfLists = new List<List<object>>();

            for (int i = 0; i < d.Deploy.Session.RuntimeArgs.Count; i++)
            {
                // add items to the list of lists
                List<object> items = new List<object> { d.Deploy.Session.RuntimeArgs[i].Name?.ToString(), d.Deploy.Session.RuntimeArgs[i].Value.Parsed?.ToString() };

                //  var dyn = JsonConvert.DeserializeObject<JObject>(items.ToList());

                listOfLists.Add(items);
            }
            // return the list
            return listOfLists;

            // return d.Deploy.Session.RuntimeArgs.ToString();
        }


        /*   if (d.Deploy.Session.StoredContractByHash != null)
           {
               return d.Deploy.Session.StoredContractByHash.Args;
           }
           if (d.Deploy.Session.StoredContractByName != null)
           {
               return d.Deploy.Session.StoredContractByName.Args;
           }
           if (d.Deploy.Session.StoredVersionedContractByHash != null)
           {
               return d.Deploy.Session.StoredVersionedContractByHash.Args;
           }
           if (d.Deploy.Session.StoredVersionedContractByName != null)
           {
               return d.Deploy.Session.StoredVersionedContractByName.Args;
           }
           if (d.Deploy.Session.ModuleBytes != null)
           {
               return d.Deploy.Session.ModuleBytes.Args;
           }*/
        return null;
    }

    public List<List<object>> GetArgsFromDatabase(Result d)
    {
        if (d.Deploy.Session.Transfer != null)
        {
            return d.Deploy.Session.Transfer.Args;
        }
        if (d.Deploy.Session.StoredContractByHash != null)
        {
            return d.Deploy.Session.StoredContractByHash.Args;
        }
        if (d.Deploy.Session.StoredContractByName != null)
        {
            return d.Deploy.Session.StoredContractByName.Args;
        }
        if (d.Deploy.Session.StoredVersionedContractByHash != null)
        {
            return d.Deploy.Session.StoredVersionedContractByHash.Args;
        }
        if (d.Deploy.Session.StoredVersionedContractByName != null)
        {
            return d.Deploy.Session.StoredVersionedContractByName.Args;
        }
        if (d.Deploy.Session.ModuleBytes != null)
        {
            return d.Deploy.Session.ModuleBytes.Args;
        }
        return null;
    }

    public Dictionary<string, object> MapArgsFromDatabase(Result d)
    {
        var args = GetArgsFromDatabase(d);
        var values = new Dictionary<string, object>();
        foreach (var t in args)
        {
            string name;
            object value;
            if (t[0] is string s)
            {
                name = s;
                value = getValue(((Dictionary<string, object>)t[1])["parsed"]);
            }
            else
            {
                name = ((Dictionary<string, object>)t[1])["parsed"].ToString();
                value = getValue(t[0]);
            }
            values.Add(name, value);
        }
        return values;
    }

    // dzia³a
    public Dictionary<string, object> MapArgsFromNode(GetDeployResult d)
    {
        var args = GetArgsFromNode(d);

        var values = new Dictionary<string, object>();

        if (args != null)
        {

            foreach (var t in args)
            {
                string name;
                object value;
                if (t[0] is string s)
                {
                    name = s; // 2102387
                    value = getValue(t[1]);
                    //value = getValue(((Dictionary<string, object>)t[1])["parsed"]);
                }
                else
                {
                    name = ((Dictionary<string, object>)t[1])["parsed"].ToString();
                    value = getValue(t[0]);
                }

                if(!values.ContainsKey(name))
                {
                    values.Add(name, value);                    
                }
                //if(!values.ContainsKey(name))
                
            }
        }
        return values;
        /*var values = new Dictionary<string, object>();
        foreach (var t in args)
        {
            string name;
            object value;
            object value_test;
            if (t[0] is string s)
            {
                name = s;
                // value = getValue(t[1]["parsed"]);
               //   value = getValue(((Dictionary<string, string>)t[0])["parsed"]);
                value_test = getValue(t[0]);
                value = getValue(t[1]);
            }
            else
            {
                name = ((Dictionary<string, object>)t[1])["parsed"].ToString();
                value = getValue(t[0]);
            }
            values.Add(name, value);
        }

        return values;*/
    }

    public static object getValue(object v)
    {
        if (v is Dictionary<string, string> unboxedDictionary)
        {
            var datas = new Dictionary<string, string>();

            foreach (var kvp in unboxedDictionary)
            {
                datas[kvp.Key.ToString()] = getValue(kvp.Value).ToString();
            }
            return datas;
        }
        if (v is Dictionary<string, object> unboxed)
        {
            var datas = new Dictionary<string, object>();

            foreach (var kvp in unboxed)
            {
                datas[kvp.Key] = getValue(kvp.Value);
            }
            return datas;
        }
        if (v is List<object> unboxedList)
        {
            return unboxedList;
        }
        if (v is Dictionary<int, object> unboxedDict)
        {
            var datas = new Dictionary<string, object>();

            foreach (var kvp in unboxedDict)
            {
                datas[kvp.Key.ToString()] = getValue(kvp.Value);
            }
            return datas;
        }
        switch (v)
        {
            case null:
                return "";
            case bool boolValue:
                return boolValue.ToString();
            case double doubleValue:
                return ((int)doubleValue).ToString();
            case int intValue:
                return intValue.ToString();
            case string stringValue:
                return stringValue;
            default:
                return string.Format("{0}", v);
        }
    }


    public (string, string) ParseStoredContractFromNode(GetDeployResult d)
    {
        (string entrypoint, Exception e) = GetEntrypointFromNode(d);
        if (e != null)
        {
            Console.WriteLine(e);
            return ("unknown", "");
        }

        Dictionary<string, object> deployArgs = MapArgsFromNode(d);
        var metadataString = System.Text.Json.JsonSerializer.Serialize(deployArgs);
        return (entrypoint, metadataString);
    }

    /// <summary>
    /// Get Result of deploy and deploy cost
    /// </summary>
    /// <param name="d"></param>
    /// <returns>result - success or failure of deploy, cost, error exception</returns>
    public (bool, string, Exception) GetResultAndCostFromNode(GetDeployResult d)
    {
        bool result = false;
        var cost = "0";

        if (d.ExecutionResults.Count > 0)
        {
            if (d.ExecutionResults[0].IsSuccess)
            {
                cost = d.ExecutionResults[0].Cost.ToString();
                result = true;
            }
            else
            {
                cost = d.ExecutionResults[0].Cost.ToString();
                result = false;
            }
            return (result, cost, null);
        }

        return (false, cost, new Exception($"no result found for deploy : {d.Deploy.Hash}"));
    }

    /// <summary>
    /// Check / validate args
    /// </summary>
    /// <param name="strict"></param>
    /// <param name="args"></param>
    /// <param name="deployArgs"></param>
    /// <returns>bool: true or false validation status</returns>
    public bool CheckArgs(bool strict, string[] args, Dictionary<string, object> deployArgs)
    {
        if (strict && deployArgs.Count != args.Length)
        {
            return false;
        }
        foreach (string arg in args)
        {
            if (!deployArgs.ContainsKey(arg) && strict)
            {
                return false;
            }
        }
        return true;
    }

    /*
    public (string, string) GetModuleByteMetadata(Result d)
    {
        var deployArgs = MapArgs(d);
        var metadataString = JsonConvert.SerializeObject(deployArgs);
        
        foreach (var argConf in d.Deploy.Session.ModuleBytes)
        {
            var ok = CheckArgs(argConf.StrictArgs, argConf.Args, deployArgs);
            if (ok)
            {
                var resolvedDeployType = argConf.DeployType;
                if (resolvedDeployType == "stackingOperation")
                {
                    resolvedDeployType = "undelegate";
                    var (_, cost, _) = GetResultAndCost(d);
                    var bigCost = BigInteger.Parse(cost);
                    if (bigCost > 1000000000)
                    {
                        resolvedDeployType = "delegate";
                    }
                }
                return (resolvedDeployType, metadataString);
            }
        }

        return ("moduleBytes", metadataString);
    }
    */

    static string[] ConvertDeployArgsToStringArray(Dictionary<string, object> deployArgs)
    {
        string[] result = new string[deployArgs.Count];
        int index = 0;
        foreach (var kvp in deployArgs)
        {
            result[index++] = $"{kvp.Key}: {kvp.Value}";
        }
        return result;
    }

    // Nie jestem pewny czy dobrze dzia³a - sprawdziæ
    public (string, string) GetModuleByteMetadataFromNode(GetDeployResult d)
    {
        var deployResult = d.Deploy.SerializeToJson();

        var jsonObject = JObject.Parse(deployResult);

        if (jsonObject.TryGetValue("payment", out JToken paymentToken) && paymentToken is JObject paymentObject)
        {
            if (paymentObject.TryGetValue("ModuleBytes", out JToken moduleBytesToken) && moduleBytesToken is JObject moduleBytesObject)
            {
                if (moduleBytesObject.TryGetValue("module_bytes", out JToken moduleByteToken))
                {
                    string moduleButes = (string)moduleByteToken;
                    //     return (moduleButes, null);
                }
            }
        }

        Dictionary<string, object> deployArgs = MapArgsFromNode(d);
        string metadataString = JsonConvert.SerializeObject(deployArgs);

        var deployAruments = new Dictionary<string, object>();

      /*  foreach(var kvp in deployArgs)
        {
            deployAruments.Add(kvp.Key, kvp.Value);
        }
      */
        

        bool isStrict = true; // or false, depending on your requirement



        foreach (var kvp in deployArgs)//d.Deploy.Session.ModuleBytes.Args)
        {
            // bool ok = true;// CheckArgs(kvp.Value, kvp.Args, deployArgs);

            //  deployAruments.Add(kvp.Key, kvp.Value);
            //string[] argsToCheck = ConvertDeployArgsToStringArray(deployArgs);
          //  bool ok = CheckArgs(isStrict, argsToCheck, deployArgs);

            //bool ok = CheckArgs(kvp.Value, kvp.Args, deployArgs); 
            // if (ok)
            //{
            string resolvedDeployType = kvp.Key;
            string resolvedDeployValue = kvp.Value.ToString();

            if (resolvedDeployType == "deposit_entry_point_name")
            {
                resolvedDeployType = kvp.Value.ToString(); // "mint";
                return (resolvedDeployType, metadataString);
            }
            else if(resolvedDeployType == "entrypoint")
            {
                resolvedDeployType = kvp.Value.ToString();
                return (resolvedDeployType, metadataString);
            }
            else if (resolvedDeployType == "subscription_contract_hash")
            {
                resolvedDeployType = "subscription";//kvp.Value.ToString();
                return (resolvedDeployType, metadataString);
            }
            else if (resolvedDeployType == "stackingOperation")
            {
                resolvedDeployType = kvp.Value.ToString();
                return (resolvedDeployType, metadataString);

            }
            else if(resolvedDeployType == "buy_amount")
            {
                resolvedDeployType = "buy_amount";
                return (resolvedDeployType, metadataString);

            }
            else if (resolvedDeployType == "delegation_rate")
            {
                resolvedDeployType = "delegation_rate";
                return (resolvedDeployType, metadataString);

            }            

        // }
        }
        //   return ("{}", new Exception($"deploy {d.Deploy.Hash} doesn't have a module_bytes"));
        return ("unknown", metadataString);
    }

    /// <summary>
    /// Get Metadata from deploy
    /// </summary>
    /// <param name="d"></param>
    /// <returns>string, metadata type</returns>
    public (string, string) GetDeployMetadataFromNode(GetDeployResult d)
    {
        if (d.Deploy.Session.GetType().Name.Contains("Transfer"))//if (d.Deploy.Session.Transfer != null)
        {
            return GetTransferMetadataFromNode(d);
        }

        // For StoredContractByHash, StoredContractByName, StoredVersionedContractByHash, StoredVersionedContractByName
        if (d.Deploy.Session.GetType().Name.Contains("StoredContractByHash") || d.Deploy.Session.GetType().Name.Contains("StoredContractByName") ||
        d.Deploy.Session.GetType().Name.Contains("StoredVersionedContractByHash") || d.Deploy.Session.GetType().Name.Contains("StoredVersionedContractByName"))
        {
            return ParseStoredContractFromNode(d);
        }

        if (d.Deploy.Session.GetType().Name.Contains("ModuleBytes"))
        {
            return GetModuleByteMetadataFromNode(d);
        }

        return ("unknown", "");
    }

    /// <summary>
    /// Get transfer metadata
    /// </summary>
    /// <param name="d">GetDeployResult</param>
    /// <returns>transfer metadata in json format</returns>
    public (string, string) GetTransferMetadataFromNode(GetDeployResult d)
    {
        var values = MapArgsFromNode(d);
        var metadata = new TransferMetadata
        {
            Hash = d.Deploy.Hash,
            From = d.Deploy.Header.Account.ToString().ToLower(),
            ID = "", // Initialize with default value
            Amount = "", // Initialize with default value
            Target = "" // Initialize with default value
        };

        // Safely attempt to retrieve 'id' from the dictionary
        if (values.TryGetValue("id", out var idValue) && idValue != null)
        {
            metadata.ID = (string)idValue;
        }

        // Safely attempt to retrieve 'amount' from the dictionary
        if (values.TryGetValue("amount", out var amountValue) && amountValue != null)
        {
            metadata.Amount = (string)amountValue;
        }

        // Safely attempt to retrieve 'target' from the dictionary
        if (values.TryGetValue("target", out var targetValue) && targetValue != null)
        {
            metadata.Target = (string)targetValue;
        }

        // Serialize the metadata object to a JSON string
        var metadataString = System.Text.Json.JsonSerializer.Serialize(metadata);

        return ("transfer", metadataString);
    }
    /*public (string, string) GetTransferMetadataFromNode(GetDeployResult d)
    {
        var values = MapArgsFromNode(d);
        var metadata = new TransferMetadata
        {
            Hash = d.Deploy.Hash,
            From = d.Deploy.Header.Account.ToString().ToLower(),
            ID = ""
        };

        if (values["id"] != null)
        {
            metadata.ID = (string)values["id"];
        }
        metadata.Amount = "";
        if (values["amount"] != null)
        {
            metadata.Amount = (string)values["amount"];
        }
        metadata.Target = "";
        if (values["target"] != null)
        {
            metadata.Target = (string)values["target"];
        }
        var metadataString = System.Text.Json.JsonSerializer.Serialize(metadata);

        return ("transfer", metadataString);
    }*/

    public string GetType(Result d)
    {
        if (d.Deploy.Session.Transfer != null)
        {
            return "transfer";
        }
        if (d.Deploy.Session.StoredContractByHash != null)
        {
            return "storedContractByHash";
        }
        if (d.Deploy.Session.StoredContractByName != null)
        {
            return "storedContractByName";
        }
        if (d.Deploy.Session.StoredVersionedContractByHash != null)
        {
            return "storedVersionedContractByHash";
        }
        if (d.Deploy.Session.StoredVersionedContractByName != null)
        {
            return "storedVersionedContractByName";
        }
        if (d.Deploy.Session.ModuleBytes != null)
        {
            return "moduleBytes";
        }
        return "unknown";
    }


    // DZIA£A!! NIE ZMIENIAÆ
    public string GetEventsFromNode(Casper.Network.SDK.NetCasperClient client, GetDeployResult d)
    {
        try
        {
            var retrievedEvents = new List<Dictionary<string, string>>();

            var isEvent = false;
            

            //  if (d.ExecutionResults.Count > 0)
            //  {
            //  List<List<object>> listOfLists = new List<List<object>>();

            //   for (int i = 0; i < d.ExecutionResults.Count; i++)
            //  {
            var deployResult = d.ExecutionResults[0].Effect.Transforms;

            foreach (var item in deployResult)
            {
                var key1 = item.Key;
                if (key1.ToString().Contains("uref"))
                {
                    var queryGlobalState = client.QueryGlobalState(key1.ToString()).Result;
                    string parsedQuery = queryGlobalState.Result.GetRawText();

                    var parsedUref = JsonConvert.DeserializeObject(parsedQuery);
                    var parsedValueJson = JsonConvert.SerializeObject(parsedUref);

                    if (parsedValueJson.Contains("event_type"))
                    {
                        isEvent = true;
                    }

                    if (isEvent)
                    {
                        JsonElement transformsgg = JsonDocument.Parse(parsedValueJson.ToString()).RootElement;

                        JsonElement stored_Value = transformsgg.GetProperty("stored_value");

                        if (stored_Value.TryGetProperty("CLValue", out JsonElement clValue))
                        {
                            var tempMap = new Dictionary<string, string>();

                            try
                            {                                
                                foreach (var child in clValue.GetProperty("parsed").EnumerateArray())
                                {
                                    // var child1 = child.ValueKind.ToString();
                                    if (child.TryGetProperty("key", out var key))
                                    {
                                        var keyStr = key.ToString();//.GetString();
                                        if (child.TryGetProperty("value", out var value))
                                        {

                                            tempMap[keyStr] = value.ToString();

                                        }
                                        else
                                        {
                                            tempMap[keyStr] = "";
                                        }
                                    }
                                    
                                }
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine("GetEventsFromNode Error: " + ex.ToString());
                            }
                            retrievedEvents.Add(tempMap);
                            isEvent = false;
                        }
                        
                    }
                }
                
            }
            // }
            if (retrievedEvents.Count > 0)
            {
                var metadataString = System.Text.Json.JsonSerializer.Serialize(retrievedEvents);
                return metadataString;
            }
            //  }
            return "{}";
        }
        catch (Exception ex)
        {
           // Console.WriteLine("ERROR IN FUNCTION GetEventsFromNode: " + ex.ToString());
            return "{}";
        }
    }


    /* dzia³aj aca kopia powy¿szej
     public string GetEventsFromNode(Casper.Network.SDK.NetCasperClient client, GetDeployResult d)
    {
        var retrievedEvents = new List<Dictionary<string, string>>();

        var isEvent = false;
        var tempMap = new Dictionary<string, string>();

        if (d.ExecutionResults.Count > 0)
        {
          //  List<List<object>> listOfLists = new List<List<object>>();

            for (int i = 0; i < d.ExecutionResults.Count; i++)
            {
                var deployResult = d.ExecutionResults[i].Effect.Transforms;

                foreach(var item in deployResult)
                {
                    var key1 = item.Key;
                    if(key1.ToString().Contains("uref"))
                    {
                        var queryGlobalState = client.QueryGlobalState(key1.ToString()).Result;
                        string parsedQuery = queryGlobalState.Result.GetRawText();

                        var parsedUref = JsonConvert.DeserializeObject(parsedQuery);
                        var parsedValueJson = JsonConvert.SerializeObject(parsedUref);

                        if (parsedValueJson.Contains("event_type"))
                        {
                            isEvent = true;
                        }

                        if (isEvent)
                        {
                            JsonElement transformsgg = JsonDocument.Parse(parsedValueJson.ToString()).RootElement;

                            JsonElement stored_Value = transformsgg.GetProperty("stored_value");

                            if (stored_Value.TryGetProperty("CLValue", out JsonElement clValue))
                            {
                                foreach (var child in clValue.GetProperty("parsed").EnumerateArray())
                                {
                                    // var child1 = child.ValueKind.ToString();
                                    if (child.TryGetProperty("key", out var key) && key.ValueKind == JsonValueKind.String)
                                    {
                                        var keyStr = key.GetString();
                                        if (child.TryGetProperty("value", out var value))
                                        {
                                            if (value.ValueKind == JsonValueKind.String)
                                            {
                                                tempMap[keyStr] = value.GetString();
                                            }
                                            else
                                            {
                                                tempMap[keyStr] = "";
                                            }                                           
                                        }
                                        else
                                        {
                                            tempMap[keyStr] = "";
                                        }
                                    }                                    
                                }
                                retrievedEvents.Add(tempMap);
                            }
                        }

                    }
                }
            }
            if (retrievedEvents.Count > 0)
            {
                var metadataString = System.Text.Json.JsonSerializer.Serialize(retrievedEvents);
                return metadataString;
            }
        }        
        return "{}";
    }
     */

    public string GetEvents6666(Result d)
    {
        var retrievedEvents = new List<Dictionary<string, string>>();
        JsonElement transforms;
        if (d.ExecutionResults[0].Result.Success != null)
        {
            transforms = JsonDocument.Parse(d.ExecutionResults[0].Result.Success.Effect.ToString()).RootElement;
        }
        else
        {
            transforms = JsonDocument.Parse(d.ExecutionResults[0].Result.Failure.Effect.ToString()).RootElement;
        }

        foreach (var child in transforms.GetProperty("transforms").EnumerateArray())
        {
            if (child.TryGetProperty("transform", out var transform) &&
                transform.TryGetProperty("WriteCLValue", out var writeCLValue) &&
                writeCLValue.TryGetProperty("parsed", out var parsed))
            {
                var tempMap = new Dictionary<string, string>();
                var isEvent = false;
                foreach (var mapCLValue in parsed.EnumerateArray())
                {
                    if (mapCLValue.TryGetProperty("key", out var key) &&
                        key.ValueKind == JsonValueKind.String)
                    {
                        var keyStr = key.GetString();
                        if (mapCLValue.TryGetProperty("value", out var value))
                        {
                            if (value.ValueKind == JsonValueKind.String)
                            {
                                tempMap[keyStr] = value.GetString();
                            }
                            else
                            {
                                tempMap[keyStr] = "";
                            }
                            if (keyStr == "event_type")
                            {
                                isEvent = true;
                            }
                        }
                        else
                        {
                            tempMap[keyStr] = "";
                        }
                    }
                }
                if (isEvent)
                {
                    retrievedEvents.Add(tempMap);
                }
            }
        }

        if (retrievedEvents.Count > 0)
        {
            var metadataString = System.Text.Json.JsonSerializer.Serialize(retrievedEvents);
            return metadataString;
        }
        return "";
    }

    // DZIA£A
    public string GetEventsFromJson(string d)
    {
        try
        {
            var retrievedEvents = new List<Dictionary<string, string>>();

            var jsonObject = JObject.Parse(d);

            // parse the JSON string into a JObject
            JObject json = JObject.Parse(d);

            // get the execution_results array
            JArray executionResults = (JArray)json["execution_results"];

            // iterate over the items in the execution_results array
            foreach (JObject result in executionResults.Children<JObject>())
            {
                // get the result object for this result
                if (result.TryGetValue("result", out JToken resultVal) && resultVal is JObject resultObj)
                {
                    // check if the result object is a Success object
                    if (resultVal["Success"] != null || resultVal["Failure"] != null)
                    {
                        string status = "";

                        var success = resultObj.ContainsKey("Success");
                        var failure = resultObj.ContainsKey("Failure");
                        if (success)
                        {
                            status = "Success";
                        }
                        else
                        {
                            status = "Failure";
                        }

                        // get the effect object from the Success object
                        JObject effect = (JObject)resultVal[status]["effect"];

                        // iterate over the transforms array in the effect object
                        foreach (JObject transform in effect["transforms"].Children<JObject>())
                        {
                            // get the key and transform values for this transform
                            if (transform.TryGetValue("transform", out JToken transformValue) && transformValue is JObject transformValueObject)
                            {
                                if (transformValueObject.TryGetValue("WriteCLValue", out JToken wCLValue) && wCLValue is JObject writeCLValueObject)
                                {
                                    var tempMap = new Dictionary<string, string>();
                                    var isEvent = false;

                                    foreach (var mapCLValue in writeCLValueObject)
                                    {
                                        var key1 = mapCLValue.Key;
                                        var value = mapCLValue.Value;

                                        if (key1 != null)
                                        {
                                            if (value == null)
                                            {
                                                tempMap[key1] = "";
                                            }
                                            else
                                            {
                                                tempMap[key1] = value.ToString();
                                            }

                                            if (key1 == "event_type")
                                            {
                                                isEvent = true;
                                            }
                                        }
                                    }

                                    if (isEvent)
                                    {
                                        retrievedEvents.Add(tempMap);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (retrievedEvents.Count > 0)
            {
                return JArray.FromObject(retrievedEvents).ToString();
            }
            else
                return "{}";
        }
        catch(Exception ex)
        {
            Console.WriteLine("ERROR IN FUNCTION GetEventsFromJson: " + ex.ToString());
            return "{}";
        }
    }

    // nie dzia³a w pe³ni - testowe
    public string GetEventsFromDeploy6666(string d)
    {
        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);

        var retrievedEvents = new List<Dictionary<string, string>>();

        var deployResult = client.GetDeploy(d).Result.Parse();

        JObject json = (JObject)JsonConvert.SerializeObject(deployResult);

       // var jsonObject = JObject.Parse(deployResult);

        // parse the JSON string into a JObject
       // JObject json = JObject.Parse(deployResult);

        // get the execution_results array
        JArray executionResults = (JArray)json["execution_results"];

        // iterate over the items in the execution_results array
        foreach (JObject result in executionResults.Children<JObject>())
        {
            // get the result object for this result
            if (result.TryGetValue("result", out JToken resultVal) && resultVal is JObject resultObj)
            {
                // check if the result object is a Success object
                if (resultObj["Success"] != null || resultObj["Failure"] != null)
                {
                    string status = "";

                    var success = resultObj.ContainsKey("Success");
                    var failure = resultObj.ContainsKey("Failure");
                    if (success)
                    {
                        status = "Success";
                    }
                    else
                    {
                        status = "Failure";
                    }

                    // get the effect object from the Success object
                    JObject effect = (JObject)resultObj[success]["effect"];

                    // iterate over the transforms array in the effect object
                    foreach (JObject transform in effect["transforms"].Children<JObject>())
                    {
                        // get the key and transform values for this transform
                        if (transform.TryGetValue("transform", out JToken transformValue) && transformValue is JObject transformValueObject)
                        {
                            if (transformValueObject.TryGetValue("WriteCLValue", out JToken wCLValue) && wCLValue is JObject writeCLValueObject)
                            {
                                var tempMap = new Dictionary<string, string>();
                                var isEvent = false;

                                foreach (var mapCLValue in writeCLValueObject)
                                {
                                    var key1 = mapCLValue.Key;
                                    var value = mapCLValue.Value;

                                    if (key1 != null)
                                    {
                                        if (value == null)
                                        {
                                            tempMap[key1] = "";
                                        }
                                        else
                                        {
                                            tempMap[key1] = value.ToString();
                                        }

                                        if (key1 == "event_type")
                                        {
                                            isEvent = true;
                                        }
                                    }
                                }

                                if (isEvent)
                                {
                                    retrievedEvents.Add(tempMap);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (retrievedEvents.Count > 0)
        {
            return JArray.FromObject(retrievedEvents).ToString();
        }

        return "{}";
    }

    public object MapUrefsFromJson(string d)
    {
        var accessRights = new Regex(@"-\d{3}$");
        var values = new Dictionary<string, object>();
        //var tempMap = new Dictionary<string, string>();
        var retrievedEvents = new List<Dictionary<string, object>>();

        var jsonObject = JObject.Parse(d);

        // parse the JSON string into a JObject
        JObject json = JObject.Parse(d);

        // get the execution_results array
        JArray executionResults = (JArray)json["execution_results"];

        // iterate over the items in the execution_results array
        foreach (JObject result in executionResults.Children<JObject>())
        {
            if (result.TryGetValue("result", out JToken resultVal) && resultVal is JObject resultObj)
            {
                // check if the result object is a Success object
                if (resultObj["Success"] != null || resultObj["Failure"] != null)
                {
                    string status = "";

                    var success = resultObj.ContainsKey("Success");
                    var failure = resultObj.ContainsKey("Failure");
                    if (success)
                    {
                        status = "Success";
                    }
                    else
                    {
                        status = "Failure";
                    }

                    // get the effect object from the Success object
                    JObject effect = (JObject)resultObj[success]["effect"];

                    // iterate over the transforms array in the effect object
                    foreach (JObject transform in effect["transforms"].Children<JObject>())
                    {
                        // get the key and transform values for this transform
                        if (transform.TryGetValue("transform", out JToken transformValue) && transformValue is JObject transformValueObject)
                        {
                            if (transformValueObject.TryGetValue("WriteCLValue", out JToken wCLValue) && wCLValue is JObject writeCLValueObject)
                            {
                               // var tempMap = new Dictionary<string, string>();

                                foreach (var mapCLValue in writeCLValueObject)
                                {
                                    var key1 = mapCLValue.Key;
                                    var value = mapCLValue.Value;

                                    if (key1 != null && key1.Contains("uref-"))
                                    {

                                        JObject parsed = (JObject)resultObj["transform"]["parsed"];
                                        if (parsed != null && accessRights.IsMatch(key1))
                                        {
                                            var urefHash = accessRights.Replace(key1, "");
                                            values[urefHash] = getValue(parsed);

                                          //  retrievedEvents.Add(values);
                                        }
                                    }

                                    if (key1 != null && key1.Contains("balance-"))
                                    {
                                        JObject parsed = (JObject)resultObj["transform"]["parsed"];
                                        if (parsed != null)
                                        {
                                            values[key1] = getValue(parsed);

                                          //  retrievedEvents.Add(values);
                                        }
                                    }                                    
                                }

                            }
                        }
                    }
                }
            }
        }
        /*
        if (retrievedEvents.Count > 0)
        {
            return JArray.FromObject(retrievedEvents).ToString();
        }*/

        return values;// JArray.FromObject(retrievedEvents).ToString();//values;//"{}";
    }

    public string[] GetWriteContract(GetDeployResult d)
    {
        var transforms = GetTransforms(d);
        var contracts = new List<string>();

        foreach (var child in transforms.Children())
        {
            if (child["transform"].Value<string>() == "WriteContract")
            {
                if (child["key"] != null)
                {
                    contracts.Add(child["key"].Value<string>());
                }
            }
        }

        return contracts.ToArray();
    }

    // u¿ywane
    public string[] GetWriteContractFromJson(string d)
    {
        var transforms = GetTransformsFromJson(d);
        var contract = new List<string>();

        for (int i = 0; i < transforms.Count; i++)
        {
            var trans = transforms[i];

            if (trans.HasValues)
            {
                var objectTrans = transforms[i] as JObject;

                if (objectTrans.ContainsKey("key") && objectTrans.ContainsKey("transform"))
                {
                    var key = trans["key"].Value<string>();
                    if (!trans["transform"].HasValues)
                    {
                        var transAA = trans["transform"].Value<string>();

                        if (transAA == "WriteContract")
                        {
                            contract.Add(trans["key"].Value<string>().ToLower().Replace("hash-", ""));
                        }
                    }
                }
            }
        }

        return contract.ToArray();
    }

    private JArray GetTransforms(GetDeployResult d)
    {
        JObject json = (JObject)JsonConvert.SerializeObject(d);

        if (d.ExecutionResults.Count > 0)
        {
            var jsonObject = JObject.Parse(json.ToString());

            // parse the JSON string into a JObject
            //  JObject json = JObject.Parse(d);

            // get the execution_results array
            JArray executionResults = (JArray)json["execution_results"];

            List<List<string>> listOfLists = new List<List<string>>();

            // var tempMap = new Dictionary<string, string>();
            var retrievedEvents = new List<Dictionary<string, string>>();

            // iterate over the items in the execution_results array
            foreach (JObject result in executionResults.Children<JObject>())
            {
                // get the result object for this result
                if (result.TryGetValue("result", out JToken resultVal) && resultVal is JObject resultObj)
                {
                    // check if the result object is a Success object
                    if (resultObj["Success"] != null || resultObj["Failure"] != null)
                    {
                        string status = "";

                        var success = resultObj.ContainsKey("Success");
                        var failure = resultObj.ContainsKey("Failure");
                        if (success)
                        {
                            status = "Success";
                        }
                        else
                        {
                            status = "Failure";
                        }
                        // get the effect object from the Success object
                        JObject effect = (JObject)resultObj[success]["effect"];

                        // iterate over the transforms array in the effect object
                        foreach (JObject transform in effect["transforms"].Children<JObject>())
                        {
                            // get the key and transform values for this transform
                            if (transform.TryGetValue("transform", out JToken transformValue) && transformValue is JObject transformValueObject)
                            {
                                //   if (transformValueObject.TryGetValue("WriteCLValue", out JToken wCLValue) && wCLValue is JObject writeCLValueObject)
                                //   {
                                var tempMap = new Dictionary<string, string>();

                                foreach (var mapCLValue in transformValueObject)
                                {
                                    var key1 = mapCLValue.Key;
                                    var value = mapCLValue.Value;

                                    if (key1 != null)
                                    {
                                        if (value == null)
                                        {
                                            tempMap[key1] = "";
                                        }
                                        else
                                        {
                                            tempMap[key1] = value.ToString();
                                        }

                                    }
                                }

                                retrievedEvents.Add(tempMap);
                            }
                        }
                    }
                }
            }

            if (retrievedEvents.Count > 0)
            {
                return JArray.FromObject(retrievedEvents);
            }
        }

        return new JArray();
    }

    public string[] GetWriteContractPackageFromJson(string d)
    {
        var transforms = GetTransformsFromJson(d);
        var contractPackages = new List<string>();

        for (int i = 0; i < transforms.Count; i++)
        {
            var trans = transforms[i];

            if (trans.HasValues)
            {
                var objectTrans = transforms[i] as JObject;

                if (objectTrans.ContainsKey("key") && objectTrans.ContainsKey("transform"))
                {
                    var key = trans["key"].Value<string>();
                    if (!trans["transform"].HasValues)
                    {
                        var transAA = trans["transform"].Value<string>();

                        if (transAA == "WriteContractPackage")
                        {
                            contractPackages.Add(trans["key"].Value<string>().ToLower().Replace("hash-", ""));
                        }
                    }
                }
            }
        }
        
        return contractPackages.ToArray();
    }

    public JArray GetTransformsFromJson(string d)
    {
        //   JObject json = (JObject)JsonConvert.SerializeObject(d); // dla GetDeployResult

        // parse the JSON string into a JObject
        JObject json = JObject.Parse(d);

        // get the execution_results array
        JArray executionResults = (JArray)json["execution_results"];

        List<List<string>> listOfLists = new List<List<string>>();

        // var tempMap = new Dictionary<string, string>();
        var retrievedEvents = new List<Dictionary<object, object>>();

        // iterate over the items in the execution_results array
        foreach (JObject result in executionResults.Children<JObject>())
        {
            // get the result object for this result
            if (result.TryGetValue("result", out JToken resultVal) && resultVal is JObject resultObj)
            {
                // check if the result object is a Success object
                if (resultObj["Success"] != null || resultObj["Failure"] != null)
                {
                    string status = "";

                    var success = resultObj.ContainsKey("Success");
                    var failure = resultObj.ContainsKey("Failure");
                    if (success)
                    {
                        status = "Success";
                    }
                    else
                    {
                        status = "Failure";
                    }
                    // get the effect object from the Success object
                    JObject effect = (JObject)resultObj[status]["effect"];

                    // iterate over the transforms array in the effect object
                    foreach (JObject transform in effect["transforms"].Children<JObject>())
                    {
                        var tempMap = new Dictionary<object, object>();

                        foreach (var mapCLValue in transform)
                        {
                            var key1 = mapCLValue.Key;
                            var value = mapCLValue.Value;

                            if (key1 != null)
                            {
                                if (value == null)
                                {
                                    tempMap[key1] = "";
                                }
                                else
                                {
                                    tempMap[key1] = value;
                                }

                            }
                        }

                        retrievedEvents.Add(tempMap);
                    }
                }
            }

            if (retrievedEvents.Count > 0)
            {
                //  return transforms as Array ?? new Array();
                var transformsSerialized = System.Text.Json.JsonSerializer.Serialize(retrievedEvents);
                //     var deserial = System.Text.Json.JsonSerializer.Deserialize(retrievedEvents.ToString());

                //      return JToken.Parse(transformsSerialized).SelectToken("transforms") as JArray ?? new JArray();

                //return JArray.FromObject(transformsSerialized);

                return JArray.FromObject(retrievedEvents);
            }
        }

        return new JArray();
    }

    public (string, Exception) GetNameFromJson(string ddupa, GetDeployResult dr) // NIE DZIA£A
    {
        string contractName = "unknown";

        Dictionary<string, object> deployArgs =  MapArgsFromNode(dr);
        for (int count = 0; count < deployArgs.Count; count++)
        {
            //   string metadataString = JsonConvert.SerializeObject(deployArgs);
            foreach (var kvp in deployArgs)//d.Deploy.Session.ModuleBytes.Args)
            {
                bool ok = true;// CheckArgs(kvp.Value, kvp.Args, deployArgs);

                if (ok)
                {
                    string resolvedDeployType = kvp.Key;
                    string resolvedDeployValue = kvp.Value.ToString();

                    if (resolvedDeployType == "name") // if (resolvedDeployType == "contract_name")
                    {
                        resolvedDeployType = kvp.Value.ToString(); // "mint";
                        return (resolvedDeployType, null);
                    }
                }
            }
        }

        return ("unknown", new Exception($"deploy {dr.Deploy.Hash} doesn't have an contract name"));
    }
        
    public (string, Exception) GetNameFromNode(GetDeployResult d)
    {
        string contractName = "unknown";

        Dictionary<string, object> deployArgs = MapArgsFromNode(d);
        for (int count = 0; count < deployArgs.Count; count++)
        {
            foreach (var kvp in deployArgs)//d.Deploy.Session.ModuleBytes.Args)
            {
                bool ok = true;// CheckArgs(kvp.Value, kvp.Args, deployArgs);

                if (ok)
                {
                    string resolvedDeployType = kvp.Key;
                    string resolvedDeployValue = kvp.Value.ToString();

                    if (resolvedDeployType == "name") // if (resolvedDeployType == "contract_name")
                    {
                        resolvedDeployType = kvp.Value.ToString();
                        return (resolvedDeployType, null);
                    }
                }
            }
        }

        return ("unknown", new Exception($"deploy {d.Deploy.Hash} doesn't have an contract name"));
    }

    public (string, Exception) GetContractSymbolFromNode(GetDeployResult d)
    {
        string contractSymbol = "unknown";

        Dictionary<string, object> deployArgs = MapArgsFromNode(d);
        for (int count = 0; count < deployArgs.Count; count++)
        {
            foreach (var kvp in deployArgs)//d.Deploy.Session.ModuleBytes.Args)
            {
                bool ok = true;// CheckArgs(kvp.Value, kvp.Args, deployArgs);

                if (ok)
                {
                    string resolvedDeployType = kvp.Key;
                    string resolvedDeployValue = kvp.Value.ToString();

                    if (resolvedDeployType == "symbol") // if (resolvedDeployType == "contract_name")
                    {
                        resolvedDeployType = kvp.Value.ToString();
                        return (resolvedDeployType, null);
                    }
                }
            }
        }

        return ("unknown", new Exception($"deploy {d.Deploy.Hash} doesn't have an contract symbol"));
    }

    /*
    public (string, Exception) GetStoredContractHashFromNode(GetDeployResult d)
    {        
        var deployResult = d.Deploy.SerializeToJson();

        var jsonObject = JObject.Parse(deployResult);
        
        if (jsonObject.TryGetValue("session", out JToken sessionToken) && sessionToken is JObject sessionObject)
        {
            if (sessionObject.TryGetValue("ModuleBytes", out JToken storedContractByModuleBytesToken) && storedContractByModuleBytesToken is JObject storedContractByModuleBytesObject)
            {
                if (storedContractByModuleBytesObject.TryGetValue("hash", out JToken hashToken)) // DODA£EM TO ALE TEGO TU NIE POWINNO BYÆ
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredContractByHash", out JToken storedContractByHashToken) && storedContractByHashToken is JObject storedContractByHashObject)
            {
                if (storedContractByHashObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredContractByName", out JToken storedContractByNameToken) && storedContractByNameToken is JObject storedContractByNameObject)
            {
                if (storedContractByNameObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredVersionedContractByHash", out JToken storedVersionedContractByHashToken) && storedVersionedContractByHashToken is JObject storedVersionedContractByHashObject)
            {
                if (storedVersionedContractByHashObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredVersionedContractByName", out JToken storedVersionedContractByNameToken) && storedVersionedContractByNameToken is JObject storedVersionedContractByNameObject)
            {
                if (storedVersionedContractByNameObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

        }   

        return ("unknown", new Exception($"deploy {d.Deploy.Hash} doesn't have an contract hash"));
    }*/

    public (string, Exception) GetStoredContractHashFromNode(GetDeployResult d)
    {
        var deployResult = d.Deploy.SerializeToJson();

        var jsonObject = JObject.Parse(deployResult);

        var args = GetArgsFromNode(d);
        Dictionary<string, object> deployArgs = MapArgsFromNode(d);
        // var metadataString = System.Text.Json.JsonSerializer.Serialize(deployArgs);
                

        if (jsonObject.TryGetValue("session", out JToken sessionToken) && sessionToken is JObject sessionObject)
        {
            if (sessionObject.TryGetValue("ModuleBytes", out JToken storedContractByModuleBytesToken) && storedContractByModuleBytesToken is JObject storedContractByModuleBytesObject)
            {
                if (storedContractByModuleBytesObject.TryGetValue("args", out JToken argsModuleBytes) && argsModuleBytes is JObject argsModuleBytesObject) // DODA£EM TO ALE TEGO TU NIE POWINNO BYÆ
                {
                    if (argsModuleBytesObject.TryGetValue("hash", out JToken hashToken)) // DODA£EM TO ALE TEGO TU NIE POWINNO BYÆ
                    {
                        string hash = (string)hashToken;
                        return (hash, null);
                    }

                    if (argsModuleBytesObject.TryGetValue("contract_hash", out JToken contractHashToken)) // DODA£EM TO ALE TEGO TU NIE POWINNO BYÆ
                    {
                        string hash = (string)contractHashToken;
                        return (hash, null);
                    }
                }

                /*
                    if (storedContractByModuleBytesObject.TryGetValue("module_bytes", out JToken moduleBytes)) // DODA£EM TO ALE TEGO TU NIE POWINNO BYÆ
                    {
                        string hexString = (string)moduleBytes;
                        byte[] decodedBytes = DecodeBase16(hexString);
                        string decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
                        string hash = (string)moduleBytes;
                        return (hash, null);
                    }*/
            }

            if (sessionObject.TryGetValue("StoredContractByHash", out JToken storedContractByHashToken) && storedContractByHashToken is JObject storedContractByHashObject)
            {
                if (storedContractByHashObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredContractByName", out JToken storedContractByNameToken) && storedContractByNameToken is JObject storedContractByNameObject)
            {
                if (storedContractByNameObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredVersionedContractByHash", out JToken storedVersionedContractByHashToken) && storedVersionedContractByHashToken is JObject storedVersionedContractByHashObject)
            {
                if (storedVersionedContractByHashObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

            if (sessionObject.TryGetValue("StoredVersionedContractByName", out JToken storedVersionedContractByNameToken) && storedVersionedContractByNameToken is JObject storedVersionedContractByNameObject)
            {
                if (storedVersionedContractByNameObject.TryGetValue("hash", out JToken hashToken))
                {
                    string hash = (string)hashToken;
                    return (hash, null);
                }
            }

        }

        /*
        foreach (var argName in deployArgs)
        {
            if (argName.Key.Contains("contract_hash"))
            {
                string hash = argName.Value.ToString();
                return (hash, null);
            }
        }*/

        foreach (var argName in deployArgs)
        {
            if (argName.Key.Contains("contract_hash"))
            {
                string hash = argName.Value.ToString();
                int dashIndex = hash.IndexOf('-'); // Finds the index of the first '-' character
                if (dashIndex != -1) // Ensure '-' was found to avoid errors
                {
                    string refinedHash = hash.Substring(dashIndex + 1); // Extracts part of the string after '-'
                    return (refinedHash, null);
                }
                return (hash, null); // Return the original hash if '-' is not found
            }

            if (argName.Key.Contains("marketplace_hash"))
            {
                string hash = argName.Value.ToString();
                int dashIndex = hash.IndexOf('-'); // Finds the index of the first '-' character
                if (dashIndex != -1) // Ensure '-' was found to avoid errors
                {
                    string refinedHash = hash.Substring(dashIndex + 1); // Extracts part of the string after '-'
                    return (refinedHash, null);
                }
                return (hash, null); // Return the original hash if '-' is not found
            }
        }

        return ("unknown", new Exception($"deploy {d.Deploy.Hash} doesn't have an contract hash"));
    }

}