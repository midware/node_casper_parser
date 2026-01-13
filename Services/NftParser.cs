using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.Clients.CEP78;
using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Types;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodeCasperParser.Services;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using SixLabors.ImageSharp.Drawing.Processing;
using Swashbuckle.SwaggerUi;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static NodeCasperParser.NftParser.NftParser;
using static System.Runtime.InteropServices.JavaScript.JSType;
using NodeCasperParser.Cryptography.CasperNetwork;
using static NodeCasperParser.Services.Contracts;
using System.Data;
using Npgsql;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NodeCasperParser.NftParser
{
    public class NftParser
    {
        private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
        private static string debugModes { get; } = ParserConfig.getToken("debugMode");
        private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");
        private static string chainName { get; } = ParserConfig.getToken("CHAIN_NAME");
        private static string sseClientConnection { get; } = ParserConfig.getToken("rpcSseUrl");

        public class Key
        {
            public string Account { get; set; }
        }

        public class Transfer_cep47
        {
            public string token_id { get; set; }
            public Key source_key { get; set; }
            public Key target_key { get; set; }
        }

        public class Meta_Cep47_Transfer
        {
            public cep47_token_transfer_recipient recipient { get; set; }
            public List<string> token_ids { get; set; }
        }

        public class cep47_token_transfer_recipient
        {
            public string Hash { get; set; }
        }


        public class TokenEvent
        {
            public string? sender { get; set; }
            public string? token_id { get; set; }
            public string? recipient { get; set; }
            public string? to { get; set; }
            public string? event_type { get; set; }
            public string? contract_package_hash { get; set; }
        }

        public class cep78_metadata
        {
            public string? amount { get; set; }
     //       public string? token_id { get; set; }
            public string? token_meta_data { get; set; }
            public string? token_owner { get; set; }
        }

        public class TokenMetadata
        {
            public string? token_id { get; set; }
            public string? token_meta_data { get; set; }
        }

        #region CEP47 update_token_meta
        public class cep47_update_token_meta
        {
            public string token_id { get; set; }
            public List<cep47_metadata_from_token_meta_update> token_meta { get; set; }
        }
        public class cep47_metadata_from_token_meta_update
        {
            public string key { get; set; }
            public string value { get; set; }
        }
        #endregion

        #region CEP47 mint
        public class cep47_mint
        {
            public cep47_mint_recipient recipient { get; set; }
            public List<string> token_ids { get; set; }
            public List<List<cep47_mint_meta>> token_metas { get; set; }
        }

        public class cep47_mint_recipient
        {
            public string Account { get; set; }
        }

        public class cep47_mint_meta
        {
            public string key { get; set; }
            public string value { get; set; }
        }
        #endregion

        #region CEP47 mint_copies
        public class cep47_mint_copies_recipient
        {
            public string Account { get; set; }
        }

        public class cep47_mint_copies_meta
        {
            public string key { get; set; }
            public string value { get; set; }
        }

        public class cep47_mint_copies
        {
            public string count { get; set; }
            public cep47_mint_copies_recipient recipient { get; set; }
            public List<cep47_mint_copies_meta> token_meta { get; set; }
        }
        #endregion

        #region CEP47 Event for mint_copies
        public class cep47_event_for_mint_copies
        {
            public string token_id { get; set; }
            public string recipient { get; set; }
            public string event_type { get; set; }
            public string contract_package_hash { get; set; }
        }
        #endregion

        #region CEP47 burn
        public class cep47_burn_owner
        {
            public string Account { get; set; }
        }

        public class cep47_burn
        {
            public cep47_burn_owner owner { get; set; }
            public string token_id { get; set; }
            public List<string> token_ids { get; set; }
        }
        public class event_cep47_burn_one
        {
            public string owner { get; set; }
            public string token_id { get; set; }
            public string event_type { get; set; }
            public string contract_package_hash { get; set; }
        }
        public class event_cep47_mint_one
        {
            public string contract_package_hash { get; set; }
            public string event_type { get; set; }
            public string recipient { get; set; }
            public string token_id { get; set; }
        }
        public class event_cep47_transfer_token
        {
            public string sender { get; set; }
            public string recipient { get; set; }
            public string token_id { get; set; }
            public string event_type { get; set; }
            public string contract_package_hash { get; set; }
        }
        #endregion

        public class metadata_cep78_mint
        {

        }

        public class event_cep78_mint
        {
            public string to { get; set; }
            public string balance { get; set; }
            public string token_id { get; set; }
            public string event_type { get; set; }
            public string contract_package_hash { get; set; }
        }
        public class event_cep78_burn
        {
            public string from { get; set; }
            public string balance { get; set; }
            public string token_id { get; set; }
            public string event_type { get; set; }
            public string contract_package_hash { get; set; }
        }
        public class event_cep78_transfer
        {
            public string to { get; set; }
            public string from { get; set; }
            public string token_id { get; set; }
            public string event_type { get; set; }
            public string contract_package_hash { get; set; }
        }

        public async Task<string> ParseMetadata(string metadata)
        //    public string ParseMetadata(string metadata)
        {
            string parsingResult = "{}";

            dynamic metadataToParsing = JsonConvert.DeserializeObject(metadata, typeof(object));

            foreach (var data in metadataToParsing)
            {
                StringWriter sw = new StringWriter();
                JsonTextWriter writer = new JsonTextWriter(sw);

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
                                            parsingResult = parsedData;

                                    }
                                    catch
                                    {
                                        parsingResult = metadata;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    parsingResult = metadata;
                }
            }

            return parsingResult;
        }

        private async Task<string> GetPublicKeyByAccountHash(string accountHash)
        {
            string sqlDataSource = psqlServer;
            NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource);

            // Initialize the public_key variable to null. It will store the result from the query.
            string publicKey = null;

            try
            {
                // Open the connection to the database.
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync().ConfigureAwait(false);

                // SQL query to execute.
                string query = "SELECT public_key FROM node_casper_accounts WHERE account_hash = @AccountHash";
                // Using statement to ensure the connection is closed properly.

                // Create a command object to execute the query.
                using (var command = new NpgsqlCommand(query, connection))
                {
                    // Add the accountHash parameter to prevent SQL injection.
                    command.Parameters.AddWithValue("@AccountHash", accountHash);

                    // Execute the command and get the result.
                    using (var reader = command.ExecuteReader())
                    {
                        // Check if there is a result.
                        if (reader.Read())
                        {
                            // Get the public_key from the result.
                            publicKey = reader.GetString(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }

            // Return the public_key. It will be null if the account_hash was not found.
            if (publicKey != null)
                return publicKey.ToLower();
            else
            {
                publicKey = null;
                return publicKey;
            }
        }


        public async Task GetNftFromEvent(Casper.Network.SDK.NetCasperClient client, bool result, string from, string contract_hash, string contract_package_hash, string events, string metadata_type, string metadata, string timestamp)
        {
            // block 2112502
            if (result)
            {
                CasperParser.ContractParser cp = new CasperParser.ContractParser();
                PostgresCasperNodeService pcn = new PostgresCasperNodeService();
                NodeCasperParser.Services.Contracts contract = new NodeCasperParser.Services.Contracts();

                events = events.Replace('\'', '\"'); // Ensure standard double quotes are used in JSON

                #region events for CEP47
                try
                {
                    List<event_cep47_burn_one> eventsCep47BurnList = JsonConvert.DeserializeObject<List<event_cep47_burn_one>>(events);

                    foreach (event_cep47_burn_one evt in eventsCep47BurnList)
                    {
                        if (evt.event_type == "cep47_burn_one")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = @"Key::Account\((.*?)\)";
                            Match match = Regex.Match(evt.owner, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                owner_account_hash = evt.owner;
                            }

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);
                            
                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(evt.contract_package_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            try
                            {
                                var cep47Client = new Casper.Network.SDK.Clients.CEP47Client(client, chainName);

                                long token_id = Convert.ToInt64(evt.token_id);

                                cep47Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                var actual_token_owner = await cep47Client.GetOwnerOf(token_id);
                                var actual_metadata = await cep47Client.GetTokenMetadata(token_id);

                                if (actual_metadata != null)
                                {
                                    Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();

                                    foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                    {
                                        actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                    }

                                    string actualTokenMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), true, timestamp);
                                }
                                else
                                {
                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", true, timestamp);
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", true, timestamp);
                            }
                        }
                    }
                }
                catch
                {

                }

                try
                {
                    List<event_cep47_mint_one> eventsCep47MintList = JsonConvert.DeserializeObject<List<event_cep47_mint_one>>(events);

                    foreach (event_cep47_mint_one evt in eventsCep47MintList)
                    {
                        if (evt.event_type == "cep47_mint_one")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = @"Key::Account\((.*?)\)";
                            Match match = Regex.Match(evt.recipient, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                owner_account_hash = evt.recipient;
                            }

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);

                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(evt.contract_package_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            try
                            {
                                var cep47Client = new Casper.Network.SDK.Clients.CEP47Client(client, chainName);

                                long token_id = Convert.ToInt64(evt.token_id);

                                cep47Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                var actual_token_owner = await cep47Client.GetOwnerOf(token_id);
                                var actual_metadata = await cep47Client.GetTokenMetadata(token_id);


                                Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();
                                if (actual_metadata != null)
                                {
                                    foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                    {
                                        actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                    }

                                    string actualTokenMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), false, timestamp);
                                }
                                else
                                {
                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", false, timestamp);
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", false, timestamp);
                            }
                        }
                    }
                }
                catch
                {

                }

                try
                {
                    List<event_cep47_transfer_token> eventsCep47TransferTokenList = JsonConvert.DeserializeObject<List<event_cep47_transfer_token>>(events);

                    foreach (event_cep47_transfer_token evt in eventsCep47TransferTokenList)
                    {
                        if (evt.event_type == "cep47_transfer_token")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = @"Key::Account\((.*?)\)";
                            Match match = Regex.Match(evt.recipient, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                owner_account_hash = evt.recipient;
                            }

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);

                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(evt.contract_package_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            try
                            {
                                var cep47Client = new Casper.Network.SDK.Clients.CEP47Client(client, chainName);

                                long token_id = Convert.ToInt64(evt.token_id);

                                cep47Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                var actual_token_owner = await cep47Client.GetOwnerOf(token_id);
                                var actual_metadata = await cep47Client.GetTokenMetadata(token_id);

                                Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();

                                if (actual_metadata != null)
                                {
                                    foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                    {
                                        actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                    }

                                    string actualTokenMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), false, timestamp);
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", false, timestamp);
                            }
                        }
                    }
                }
                catch
                {

                }
                #endregion

                #region events for CEP78
                try
                {
                    List<event_cep78_burn> eventsCep78BurnList = JsonConvert.DeserializeObject<List<event_cep78_burn>>(events);

                    foreach (event_cep78_burn evt in eventsCep78BurnList)
                    {
                        if (evt.event_type == "cep78_burn")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = @"Key::Account\((.*?)\)";
                            Match match = Regex.Match(evt.from, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                owner_account_hash = evt.from;
                            }

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);

                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(evt.contract_package_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            try
                            {
                                var cep78Client = new Casper.Network.SDK.Clients.CEP78.CEP78Client(client, chainName);

                                //  ulong token_id = Convert.ToUInt64(evt.token_id);
                                var token_id = evt.token_id;

                                cep78Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                GlobalStateKey actual_token_owner = await cep78Client.GetOwnerOf(token_id);
                                CEP78TokenMetadata actual_metadata = await cep78Client.GetMetadata<CEP78TokenMetadata>(evt.token_id);

                                var cep_78_metadata = new CEP78TokenMetadata()
                                {
                                    Name = actual_metadata.Name,
                                    TokenUri = actual_metadata.TokenUri,
                                    Checksum = actual_metadata.Checksum
                                };

                                string actualTokenMetadata = JsonConvert.SerializeObject(cep_78_metadata, Formatting.Indented);

                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), true, timestamp);
                            }
                            catch
                            {
                                Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", true, timestamp);
                            }

                        }
                    }
                }
                catch
                {

                }

                try
                {
                    List<event_cep78_mint> eventsCep78MintList = JsonConvert.DeserializeObject<List<event_cep78_mint>>(events);

                    foreach (event_cep78_mint evt in eventsCep78MintList)
                    {
                        if (evt.event_type == "cep78_mint" || evt.event_type == "mint")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = @"Key::Account\((.*?)\)";
                            Match match = Regex.Match(evt.to, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                owner_account_hash = evt.to;
                            }

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);

                            if (owner_public_key == null) // only in MINT
                            {
                                owner_public_key = from;
                            }

                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(evt.contract_package_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            try
                            {
                                var cep78Client = new Casper.Network.SDK.Clients.CEP78.CEP78Client(client, chainName);

                                var token_id = evt.token_id;

                                cep78Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                GlobalStateKey actual_token_owner = await cep78Client.GetOwnerOf(token_id);
                                CEP78TokenMetadata actual_metadata = await cep78Client.GetMetadata<CEP78TokenMetadata>(evt.token_id);

                                var cep_78_metadata = new CEP78TokenMetadata()
                                {
                                    Name = actual_metadata.Name,
                                    TokenUri = actual_metadata.TokenUri,
                                    Checksum = actual_metadata.Checksum
                                };

                                string actualTokenMetadata = JsonConvert.SerializeObject(cep_78_metadata, Formatting.Indented);

                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), false, timestamp);
                            }
                            catch
                            {
                                Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", false, timestamp);
                            }

                        }
                    }
                }
                catch
                {
                    string metadataListJson = "[" + metadata + "]";
                    List<cep78_metadata> metadataCep78MintList = JsonConvert.DeserializeObject<List<cep78_metadata>>(metadataListJson);
                    foreach (cep78_metadata evt in metadataCep78MintList)
                    {

                        if (metadata_type == "cep78_mint" || metadata_type == "mint")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = "\"Account\":\"(.*?)\"";
                            Match match = Regex.Match(evt.token_owner, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                return;
                            }

                            owner_account_hash = owner_account_hash.Replace("account-hash-", "");

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);

                            if(owner_public_key == null) // ONLY IN MINT
                            {
                                owner_public_key = from;
                            }

                            var getContract = await contract.GetContractStoredValue(contract_hash);
                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contract_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            var cep78Client = new Casper.Network.SDK.Clients.CEP78.CEP78Client(client, chainName);

                            cep78Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);

                            var mintedTokensCount = await cep78Client.GetNumberOfMintedTokens();

                            for (ulong tokenId = 0; tokenId < mintedTokensCount; tokenId++)
                            {
                                var token_id = tokenId.ToString();

                                try
                                {
                                    cep78Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                    GlobalStateKey actual_token_owner = await cep78Client.GetOwnerOf(token_id);
                                    CEP78TokenMetadata actual_metadata = await cep78Client.GetMetadata<CEP78TokenMetadata>(token_id);

                                    var cep_78_metadata = new CEP78TokenMetadata()
                                    {
                                        Name = actual_metadata.Name,
                                        TokenUri = actual_metadata.TokenUri,
                                        Checksum = actual_metadata.Checksum
                                    };

                                    string actualTokenMetadata = JsonConvert.SerializeObject(cep_78_metadata, Formatting.Indented);

                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), false, timestamp);
                                }
                                catch
                                {
                                    //Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", false, timestamp);
                                }
                            }
                        }
                    }
                }

                try
                {
                    List<event_cep78_transfer> eventsCep78TransferList = JsonConvert.DeserializeObject<List<event_cep78_transfer>>(events);

                    foreach (event_cep78_transfer evt in eventsCep78TransferList)
                    {
                        if (evt.event_type == "cep78_transfer")
                        {
                            string owner_account_hash = string.Empty;
                            string owner_public_key = string.Empty;

                            string pattern = @"Key::Account\((.*?)\)";
                            Match match = Regex.Match(evt.to, pattern);
                            if (match.Success)
                            {
                                owner_account_hash = match.Groups[1].Value;
                            }
                            else
                            {
                                owner_account_hash = evt.to;
                            }

                            owner_public_key = await GetPublicKeyByAccountHash(owner_account_hash);

                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(evt.contract_package_hash);

                            var jsonContractData = await contract.GetContract(jsonContractFromContractPackage.Item2);

                            var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                            var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                            var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                            try
                            {
                                var cep78Client = new Casper.Network.SDK.Clients.CEP78.CEP78Client(client, chainName);

                                //  ulong token_id = Convert.ToUInt64(evt.token_id);
                                var token_id = evt.token_id;

                                cep78Client.SetContractHash("hash-" + jsonContractFromContractPackage.Item2);
                                GlobalStateKey actual_token_owner = await cep78Client.GetOwnerOf(token_id);
                                CEP78TokenMetadata actual_metadata = await cep78Client.GetMetadata<CEP78TokenMetadata>(evt.token_id);

                                var cep_78_metadata = new CEP78TokenMetadata()
                                {
                                    Name = actual_metadata.Name,
                                    TokenUri = actual_metadata.TokenUri,
                                    Checksum = actual_metadata.Checksum
                                };

                                string actualTokenMetadata = JsonConvert.SerializeObject(cep_78_metadata, Formatting.Indented);

                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, actualTokenMetadata, await ParseMetadata(actualTokenMetadata), false, timestamp);
                            }
                            catch
                            {
                                Console.WriteLine("Can't update metadata for token: " + evt.token_id);
                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, evt.token_id, owner_account_hash, owner_public_key, metadata_type, "{}", "{}", false, timestamp);
                            }
                        }
                    }
                }
                catch
                {

                }
                #endregion
            }
        }

        public async Task<(string, string, string)> GetNftFromContract(Casper.Network.SDK.NetCasperClient client, bool result, string contractPackageHash, string contractHash, string from, string entrypoint, string metadata, string events, string timestamp)
        {
            bool burned = false;

            if (result)// if true
            {                
                contractHash = contractHash.ToLower();
                                
                if (!contractHash.Contains("unknown") && !contractHash.IsNullOrEmpty())
                {
                    CasperParser.ContractParser cp = new CasperParser.ContractParser();
                    PostgresCasperNodeService pcn = new PostgresCasperNodeService();
                    NodeCasperParser.Services.Contracts contract = new NodeCasperParser.Services.Contracts();

                    var jsonContractData = await contract.GetContract(contractHash);

                    var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                    var (contractTypeName, score) = cp.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                    var (contractName, contractSymbol) = await cp.GetContractNameAndSymbol(client, jsonContractData);

                    var entryPoint = entrypoint; // do usunięcia
                    var metaData = metadata; // 2112616 do usunięcia
                    var ev = events; // do usunięcia
                    
                    if (contractTypeName.ToLower().Contains("cep78")) // WYKRYTO CEP78
                    {
                        var cep78Client = new Casper.Network.SDK.Clients.CEP78.CEP78Client(client, chainName);
                                                
                        // ZROBIĆ 
                    }
                    else if (contractTypeName.ToLower().Contains("cep47")) // WYKRYTO CEP47
                    {
                        // test block: 2112505
                        // test kontrakt: d950b6fb1e487e054dff551ad1acd0106802bb482bf1e88630b6a1eec2de8ed9 
                        var cep47Client = new Casper.Network.SDK.Clients.CEP47Client(client, chainName);

                        #region CEP47 token_meta_update
                        if (entrypoint.ToLower().Equals("update_token_meta")) // WYKRYTO ZDARZENIE token_meta_update
                        {
                            try // Spróbuj pobrać dane bezpośrednio z RPC, catch wystąpi gdy token_id jest typu String (bo SDK nie obsługuje tego typu)
                            {                                
                                var data = JsonConvert.DeserializeObject<cep47_update_token_meta>(metadata);                                                                
                                long tokenId = Convert.ToInt64(data.token_id);
                                cep47Client.SetContractHash("hash-" + contractHash);
                                var actual_token_owner = await cep47Client.GetOwnerOf(tokenId);
                                var actual_metadata = await cep47Client.GetTokenMetadata(tokenId);

                                string owner_public_key = string.Empty;
                                owner_public_key = await GetPublicKeyByAccountHash(actual_token_owner.ToHexString().ToLower());

                                Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();

                                foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                {
                                    actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                }

                                string jsonMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, data.token_id, actual_token_owner.KeyIdentifier.ToString(), owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                            }
                            catch // NIE DA SIĘ POBRAĆ DANYCH Z RPC PO ID TOKENU WIĘC POBIERZ STATYCZNIE Z PRZEKAZANYCH STRINGÓW DO FUNKCJI
                            {
                                try
                                {
                                    Dictionary<string, string> createMetaDataFromStringDictionary = new Dictionary<string, string>();

                                    var data = JsonConvert.DeserializeObject<cep47_update_token_meta>(metadata);

                                    foreach (cep47_metadata_from_token_meta_update meta in data.token_meta) // stwórz kolekcję metadanych ze statycznych danych
                                    {
                                        createMetaDataFromStringDictionary.Add(meta.key, meta.value);
                                    }

                                    string jsonMetadata = JsonConvert.SerializeObject(createMetaDataFromStringDictionary, Formatting.Indented);

                                    var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                    string owner_public_key = string.Empty;
                                    owner_public_key = await GetPublicKeyByAccountHash(from);


                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, data.token_id, from, owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine("CEP47 ERROR: Can't parse data event: update_token_meta for contract: \n" + contractHash + " and metadata: " + metadata + "\n" + ex.ToString());
                                }
                            }
                        }
                        #endregion

                        #region CEP47 token_mint
                        if (entrypoint.ToLower().Equals("mint")) // WYKRYTO ZDARZENIE mint
                        {
                            try // Spróbuj pobrać dane bezpośrednio z RPC, catch wystąpi gdy token_id jest typu String (bo SDK nie obsługuje tego typu)
                            {
                                var data = JsonConvert.DeserializeObject<cep47_mint>(metadata);
                                long tokenId = Convert.ToInt64(data.token_ids);
                                cep47Client.SetContractHash("hash-" + contractHash);
                                var actual_token_owner = await cep47Client.GetOwnerOf(tokenId);
                                var actual_metadata = await cep47Client.GetTokenMetadata(tokenId);

                                string owner_public_key = string.Empty;
                                owner_public_key = await GetPublicKeyByAccountHash(actual_token_owner.ToHexString().ToLower());

                                Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();

                                foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                {
                                    actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                }

                                string jsonMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                foreach (string tokenIds in data.token_ids)
                                {
                                    var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, tokenIds, actual_token_owner.KeyIdentifier.ToString(), owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                                }
                            }
                            catch // NIE DA SIĘ POBRAĆ DANYCH Z RPC PO ID TOKENU WIĘC POBIERZ STATYCZNIE Z PRZEKAZANYCH STRINGÓW DO FUNKCJI
                            {
                                try
                                {
                                    Dictionary<string, string> createMetaDataFromStringDictionary = new Dictionary<string, string>();

                                    var data = JsonConvert.DeserializeObject<cep47_mint>(metadata);

                                    string recipient = data.recipient.Account.Replace("account-hash-", "");

                                    foreach (List<cep47_mint_meta> metaList in data.token_metas) // stwórz kolekcję metadanych ze statycznych danych
                                    {
                                        foreach (cep47_mint_meta meta in metaList)
                                        {
                                            createMetaDataFromStringDictionary.Add(meta.key, meta.value);
                                        }
                                    }

                                    string jsonMetadata = JsonConvert.SerializeObject(createMetaDataFromStringDictionary, Formatting.Indented);

                                    string owner_public_key = string.Empty;
                                    owner_public_key = await GetPublicKeyByAccountHash(recipient);


                                    foreach (string tokenIds in data.token_ids)
                                    {
                                        var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                        await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, tokenIds, recipient, owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("CEP47 ERROR: Can't parse data event: mint for contract: \n" + contractHash + " and metadata: " + metadata + "\n" + ex.ToString());
                                }
                            }
                        }
                        #endregion

                        #region CEP47 mint_copies
                        if (entrypoint.ToLower().Equals("mint_copies")) // WYKRYTO ZDARZENIE token_meta_update
                        {
                            try
                            {
                                // nie ma w metadanych mint_copies id tokenu. Dlatego trzeba go wyciągnąć z eventu.
                                List<cep47_event_for_mint_copies> eventsSerialized = JsonConvert.DeserializeObject<List<cep47_event_for_mint_copies>>(events);

                                foreach (cep47_event_for_mint_copies dataEvent in eventsSerialized)
                                {
                                    try // Spróbuj pobrać dane bezpośrednio z RPC, catch wystąpi gdy token_id jest typu String (bo SDK nie obsługuje tego typu)
                                    {
                                        var data = JsonConvert.DeserializeObject<cep47_mint_copies>(metadata);
                                        long tokenId = Convert.ToInt64(dataEvent.token_id);
                                        cep47Client.SetContractHash("hash-" + contractHash);
                                        var actual_token_owner = await cep47Client.GetOwnerOf(tokenId);
                                        var actual_metadata = await cep47Client.GetTokenMetadata(tokenId);

                                        string owner_public_key = string.Empty;
                                        owner_public_key = await GetPublicKeyByAccountHash(actual_token_owner.ToHexString().ToLower());

                                        Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();

                                        foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                        {
                                            actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                        }

                                        string jsonMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                        var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                        await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, dataEvent.token_id, actual_token_owner.KeyIdentifier.ToString().ToLower(), owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                                    }

                                    catch // NIE DA SIĘ POBRAĆ DANYCH Z RPC PO ID TOKENU WIĘC POBIERZ STATYCZNIE Z PRZEKAZANYCH STRINGÓW DO FUNKCJI
                                    {
                                        try
                                        {
                                            Dictionary<string, string> createMetaDataFromStringDictionary = new Dictionary<string, string>();

                                            var data = JsonConvert.DeserializeObject<cep47_mint_copies>(metadata);
                                            string recipient = dataEvent.recipient.Replace("account-hash-", "");

                                            string owner_public_key = string.Empty;
                                            owner_public_key = await GetPublicKeyByAccountHash(recipient);


                                            foreach (cep47_mint_copies_meta meta in data.token_meta) // stwórz kolekcję metadanych ze statycznych danych
                                            {
                                                createMetaDataFromStringDictionary.Add(meta.key, meta.value);
                                            }

                                            string jsonMetadata = JsonConvert.SerializeObject(createMetaDataFromStringDictionary, Formatting.Indented);

                                            var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                            await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, dataEvent.token_id, recipient, owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("CEP47 ERROR: Can't parse data event: update_token_meta for contract: \n" + contractHash + " and metadata: " + metadata + "\n" + ex.ToString());
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) // WYSTĄPI JEŻELI DANE Z STRING EVENT SĄ NIEZGODNE (np. ZMODYFIKOWANY CEP47)
                            {
                                Console.WriteLine("CEP47 ERROR: Can't parse meta data for event: update_token_meta for contract: \n" + contractHash + " and event: " + events + "\n" + ex.ToString());
                            }
                            }
                        #endregion


                        #region CEP47 burn
                        if (entrypoint.ToLower().Equals("burn")) // WYKRYTO ZDARZENIE mint
                        {
                            try // Spróbuj pobrać dane bezpośrednio z RPC, catch wystąpi gdy token_id jest typu String (bo SDK nie obsługuje tego typu)
                            {                                
                                var data = JsonConvert.DeserializeObject<cep47_burn>(metadata);
                                
                                cep47Client.SetContractHash("hash-" + contractHash);                                
                                                                
                                foreach (string tokenIds in data.token_ids)
                                {
                                    long tokenId = Convert.ToInt64(tokenIds);
                                    var actual_token_owner = await cep47Client.GetOwnerOf(tokenId);
                                    var actual_metadata = await cep47Client.GetTokenMetadata(tokenId);

                                    string owner_public_key = string.Empty;
                                    owner_public_key = await GetPublicKeyByAccountHash(actual_token_owner.ToHexString().ToLower());

                                    Dictionary<string, string> actualMetaDataFromRpcDictionary = new Dictionary<string, string>();

                                    foreach (var kvp in actual_metadata) // stwórz kolekcję metadanych z danych RPC
                                    {
                                        actualMetaDataFromRpcDictionary.Add(kvp.Key, kvp.Value);
                                    }

                                    string jsonMetadata = JsonConvert.SerializeObject(actualMetaDataFromRpcDictionary, Formatting.Indented);

                                    var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                    await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, tokenIds, actual_token_owner.KeyIdentifier.ToString(), owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), true, timestamp);
                                }
                            }
                            catch // NIE DA SIĘ POBRAĆ DANYCH Z RPC PO ID TOKENU WIĘC POBIERZ STATYCZNIE Z PRZEKAZANYCH STRINGÓW DO FUNKCJI
                            {
                                try
                                {
                                    Dictionary<string, string> createMetaDataFromStringDictionary = new Dictionary<string, string>();

                                    var data = JsonConvert.DeserializeObject<cep47_mint>(metadata);

                                    string recipient = data.recipient.Account.Replace("account-hash-", "");

                                    string owner_public_key = string.Empty;
                                    owner_public_key = await GetPublicKeyByAccountHash(recipient);


                                    foreach (List<cep47_mint_meta> metaList in data.token_metas) // stwórz kolekcję metadanych ze statycznych danych
                                    {
                                        foreach (cep47_mint_meta meta in metaList)
                                        {
                                            createMetaDataFromStringDictionary.Add(meta.key, meta.value);
                                        }
                                    }

                                    string jsonMetadata = JsonConvert.SerializeObject(createMetaDataFromStringDictionary, Formatting.Indented);

                                    foreach (string tokenIds in data.token_ids)
                                    {
                                        var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                        await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, tokenIds, recipient, owner_public_key, events, jsonMetadata, await ParseMetadata(jsonMetadata), true, timestamp);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("CEP47 ERROR: Can't parse data event: mint for contract: \n" + contractHash + " and metadata: " + metadata + "\n" + ex.ToString());
                                }
                            }
                        }
                        #endregion


                        if (entrypoint.ToLower().Contains("burn"))
                        {
                            burned = true;
                        }

                        // DATA FROM TRANSFER
                        if (entrypoint.ToLower().Equals("transfer"))
                        {
                            var data = JsonConvert.DeserializeObject<Meta_Cep47_Transfer>(metadata);

                            Console.WriteLine($"Recipient Hash: {data.recipient.Hash}");
                            Console.WriteLine("Token IDs:");

                            foreach (var tokenIds in data.token_ids)
                            {
                                try
                                {
                                    cep47Client.SetContractHash("hash-" + contractHash);
                                    long tokenId = Convert.ToInt64(tokenIds);
                                    var actual_metadata = await cep47Client.GetTokenMetadata(tokenId);
                                }
                                catch
                                { // zrobić statyczne z metadata lub events ze stringu.

                                }
                            }

                        }

                        List<TokenEvent> tokenEvents = JsonConvert.DeserializeObject<List<TokenEvent>>(events);
                       // PostgresCasperNodeService pcn = new PostgresCasperNodeService();

                        foreach (var tokenEvent in tokenEvents)
                        {
                            /*
                            Console.WriteLine($"Token ID: {tokenEvent.token_id}");
                            Console.WriteLine($"Recipient: {tokenEvent.recipient}");
                            Console.WriteLine($"To: {tokenEvent.to}");
                            Console.WriteLine($"Event Type: {tokenEvent.event_type}");
                            Console.WriteLine($"Contract Package Hash: {tokenEvent.contract_package_hash}");
                            Console.WriteLine("------------------------------");*/
                                                        
                            try
                            {
                                cep47Client.SetContractHash("hash-" + contractHash);
                                long tokenId = Convert.ToInt64(tokenEvent.token_id);
                                var actual_metadata = await cep47Client.GetTokenMetadata(tokenId);


                                Dictionary<string, string> myDictionary = new Dictionary<string, string>();

                                foreach (var kvp in actual_metadata)
                                {
                                    myDictionary.Add(kvp.Key, kvp.Value);
                                }

                                string jsonMetadata = JsonConvert.SerializeObject(myDictionary, Formatting.Indented);


                                var actual_token_owner = await cep47Client.GetOwnerOf(tokenId);

                                string owner_public_key = string.Empty;
                                owner_public_key = await GetPublicKeyByAccountHash(actual_token_owner.ToHexString().ToLower());

                                var jsonContractFromContractPackage = await contract.GetNewestContractFromContractPackageHash(contractPackageHash);

                                await pcn.InsertNft(jsonContractFromContractPackage.Item1, jsonContractFromContractPackage.Item2, contractTypeName, contractName, contractSymbol, tokenEvent.token_id, actual_token_owner.ToString(), owner_public_key, tokenEvent.event_type.ToString(), jsonMetadata, await ParseMetadata(jsonMetadata), burned, timestamp);
                            }
                            catch(Exception ex) // ID inne niż INT więc nie da się czytać z kontraktu przez SDK, trzeba czytać ze sparsowanych danych
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }                        
                    }
                    /*
                    if (metadataType.ToLower().Contains("mint") || metadataType.ToLower().Contains("transfer") || metadataType.ToLower().Contains("burn"))
                    {
                        JObject jsonMetaDataObject = JObject.Parse(metadata);
                        JObject jsonEventsDataObject = JObject.Parse(events);
                        // string contractName = string.Empty;
                        // string contractSymbol = string.Empty;

                        PostgresCasperNodeService pcn = new PostgresCasperNodeService();
                        //  await pcn.InsertNft()
                    }*/
                }
            }

            return (string.Empty, string.Empty, string.Empty);
        }
    }
}
