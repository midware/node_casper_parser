using Casper.Network.SDK.Types;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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
using Casper.Network.SDK.SSE;
using System;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using NodeCasperParser.Models;
using CasperParser;
using System.Linq.Expressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics.Metrics;

public interface IPostgresCasperNodeService
{
    public dynamic getRowValue<T>(DataRow row, string fieldName);
    Task<int> InsertNft(string contract_package_hash, string contract_hash, string contract_type, string contract_name, string contract_symbol, string token_id, string token_owner_account_hash, string token_owner_public_key, string metadata_type, string metadata, string metadata_parsed, bool token_burned, string timestamp);
    Task<int> InsertRawNft(string contract_package_hash, string contract_hash, string token_id, string json);
    Task<int> InsertBlock(string hash, string state_root_hash, int era, string timestamp, int height, bool eraEnd, int deploys_count, string validator, string json);
    Task<int> InsertRawBlock(string hash, string json);
    Task<int> InsertDeploy(string hash, string from, string cost, bool result, string timestamp, string block, string deployType, string json, string metadataType, string contractHash, string contractName, string contractSymbol, string entrypoint, string metadata, string events);
    Task<int> InsertRawDeploy(string hash, string json);
    Task<int> InsertContract(string hash, string packageHash, string deploy, string from, string contractName, string contractSymbol, string contractType, double score, string data);
    Task<int> InsertNamedKey(string uref, string name, bool isPurse, string initialValue, string contractHash);
    Task<int> InsertAccountHash(string hash, string purse);
    Task<int> InsertAccount(string publicKey, string hash, string purse, decimal balance, string timestamp);
    Task<int> InsertPurse(string hash);
    Task<int> InsertPurseBalance(string hash, string balance);
    Task<int> InsertContractPackage(string hash, string deploy, string from, string data);

    Task<int> UpdateStateRootHashBlock(string hash, string state_root_hash, int height);
    Task<int> UpdateDeploy(string hash, string from, string cost, bool result, string timestamp, string block, string deployType, string metadataType, string contractHash, string contractName, string contractSymbol, string entrypoint, string metadata, string events);
    Task<string> GetMissingBlocks();
    Task<string> GetMissingMetadataDeploysHash();
    Task<string> GetRawDeploy(string hash);
    Task<string> GetRawBlock(string hash);
    Task<int> CountDeploys(string[] hashes);
    Task<int> ValidateBlock(string hash);
    public void InsertRewards(List<List<object>> rowsToInsert);
}

public class PostgresCasperNodeService : IPostgresCasperNodeService
{
    private readonly AppSettings _appSettings;
    private readonly IConfiguration _configuration;

    private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
    private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");
    private static string debugModes { get; } = ParserConfig.getToken("debugMode");
    bool debugMode = false;

    public PostgresCasperNodeService()
    {
    }

    public dynamic getRowValue<T>(DataRow row, string fieldName)
    {
        if (!DBNull.Value.Equals(row[fieldName]))
        {
            if (typeof(T) == typeof(long))
            {
                return (long)row[fieldName];
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)row[fieldName];
            }
            else if (typeof(T) == typeof(string))
            {
                return (string)row[fieldName];
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)row[fieldName];
            }
            else if (typeof(T) == typeof(bool))
            {
                return (bool)row[fieldName];
            }
            else if (typeof(T) == typeof(DateTime))
            {
                return (DateTime)row[fieldName];
            }
            else
            {
                return "Undefined row type";
            }
        }
        else
        {
            return null;
        }
    }

    public async Task<int> InsertRawNft(string contract_package_hash, string contract_hash, string token_id, string json)
    {
        long queryAffected = -1;
        int error = -1;
        bool rawExist = false;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        contract_hash = contract_hash.ToLower();

        string sqlCountQuery = @"SELECT COUNT(contract_hash) FROM node_casper_raw_nfts WHERE contract_hash = '" + contract_hash + "' AND token_id = '" + token_id + "'";
        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlCountQuery, sqlCon))
            {
                queryAffected = (long)(await myCommand.ExecuteScalarAsync());
                if (queryAffected > 0)
                    rawExist = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Query error: " + sqlCountQuery + " " + ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }
        queryAffected = -1;

        if (rawExist)
        {
            // DO UPDATE

            string sqlUpdateQuery = @"UPDATE node_casper_raw_nfts SET data = '" + json.Replace("'", "''") + "' WHERE contract_hash = '" + contract_hash + "' AND token_id = '" + token_id + "'";

            try
            {
                if (sqlCon.State != ConnectionState.Open)
                    await sqlCon.OpenAsync().ConfigureAwait(false);

                using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlUpdateQuery, sqlCon))
                {
                    var errorA = await myCommand.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Query error: " + sqlUpdateQuery + " "+ ex.ToString());
            }
            finally
            {
                if (sqlCon.State == ConnectionState.Open)
                    await sqlCon.CloseAsync();
            }
        }
        else
        {
            // DO INSERT INSERT INTO node_casper_raw_nfts (contract_hash, token_id, data)

            string sqlInserteQuery = @"INSERT INTO node_casper_raw_nfts (contract_package_hash, contract_hash, token_id, data) VALUES ('" + contract_package_hash + "','" + contract_hash + "','" + token_id + "', '" + json.Replace("'", "''") + "')";

            try
            {
                if (sqlCon.State != ConnectionState.Open)
                    await sqlCon.OpenAsync().ConfigureAwait(false);

                using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlInserteQuery, sqlCon))
                {
                    var errorA = await myCommand.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Query error: " + sqlInserteQuery + " " + ex.ToString());
            }
            finally
            {
                if (sqlCon.State == ConnectionState.Open)
                    await sqlCon.CloseAsync();
            }
        }

        var returnedValue = (int)queryAffected;
        return returnedValue;
    }

    /// <summary>
    /// InsertNft
    /// </summary>
    /// <param name="contract_package_hash"></param>
    /// <param name="contract_hash"></param>
    /// <param name="contract_type"></param>
    /// <param name="contract_name"></param>
    /// <param name="contract_symbol"></param>
    /// <param name="token_id"></param>
    /// <param name="token_owner_account_hash"></param>
    /// <param name="token_owner_public_key"></param>
    /// <param name="metadata_type"></param>
    /// <param name="metadata"></param>
    /// <param name="metadata_parsed"></param>
    /// <param name="token_burned"></param>
    /// <param name="timestamp"></param>
    /// <returns>queryAffected</returns>
    public async Task<int> InsertNft(string contract_package_hash, string contract_hash, string contract_type, string contract_name, string contract_symbol, string token_id, string token_owner_account_hash, string token_owner_public_key, string metadata_type, string metadata, string metadata_parsed, bool token_burned, string timestamp)
    {
        int queryAffected = -1;
        int error = -1;
        contract_hash = contract_hash.ToLower();

        try
        {
            await InsertRawNft(contract_package_hash, contract_hash, token_id, metadata);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in InsertNft: " + ex.ToString());
            return error;
        }

        return await UpdateNft(contract_package_hash, contract_hash, contract_type, contract_name, contract_symbol, token_id, token_owner_account_hash, token_owner_public_key, metadata_type, metadata, metadata_parsed, token_burned, timestamp);
    }

    /// <summary>
    /// UpdateNft
    /// </summary>
    /// <param name="contract_package_hash"></param>
    /// <param name="contract_hash"></param>
    /// <param name="contract_type"></param>
    /// <param name="contract_name"></param>
    /// <param name="contract_symbol"></param>
    /// <param name="token_id"></param>
    /// <param name="token_owner_account_hash"></param>
    /// <param name="token_owner_public_key"></param>
    /// <param name="metadata_type"></param>
    /// <param name="metadata"></param>
    /// <param name="metadata_parsed"></param>
    /// <param name="token_burned"></param>
    /// <param name="timestamp"></param>
    /// <returns>queryAffected</returns>
    public async Task<int> UpdateNft(string contract_package_hash, string contract_hash, string contract_type, string contract_name, string contract_symbol, string token_id, string token_owner_account_hash, string token_owner_public_key, string metadata_type, string metadata, string metadata_parsed, bool token_burned, string timestamp)
    {
        long queryAffected = -1;
        int error = -1;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        var timestamp2 = Convert.ToInt64(timestamp);
        var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp2);
        var dateTimeFromTimestamp = dt.ToString("yyyy-MM-dd HH:mm:ss+02");
        contract_hash = contract_hash.ToLower();
        bool rawExist = false;

        string sqlCountQuery = @"SELECT COUNT(contract_hash) FROM node_casper_nfts WHERE contract_hash = '" + contract_hash + "' AND token_id = '" + token_id + "'";
        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlCountQuery, sqlCon))
            {
                queryAffected = (long)(await myCommand.ExecuteScalarAsync());
                if (queryAffected > 0)
                    rawExist = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        queryAffected = -1; // reset sate

        if (rawExist)
        {
            // DO UPDATE

            string sqlUpdateQuery = @"UPDATE node_casper_nfts SET contract_type = '" + contract_type + "', contract_name = '" + contract_name.Replace("'", "''") + "', contract_symbol = '" + contract_symbol.Replace("'", "''") + "', token_id = '" + token_id.Replace("'", "''") + "', token_owner_account_hash = '" + token_owner_account_hash + "', token_owner_public_key = '" + token_owner_public_key + "', metadata_type = '" + metadata_type + "', metadata = '" + metadata.Replace("'", "''") + "', metadata_parsed = '" + metadata_parsed.Replace("'", "''") + "', token_burned = " + token_burned + ", timestamp = '" + dateTimeFromTimestamp + "' WHERE contract_hash = '" + contract_hash + "' AND token_id = '" + token_id + "'";

            try
            {
                if (sqlCon.State != ConnectionState.Open)
                    await sqlCon.OpenAsync().ConfigureAwait(false);

                using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlUpdateQuery, sqlCon))
                {
                    var errorA = await myCommand.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (sqlCon.State == ConnectionState.Open)
                    await sqlCon.CloseAsync();
            }
        }
        else
        {
            // DO INSERT INSERT INTO node_casper_raw_nfts (contract_hash, token_id, data)

            string sqlInserteQuery = @"INSERT INTO node_casper_nfts (contract_package_hash, contract_hash, contract_type, contract_name, contract_symbol, token_id, token_owner_account_hash, token_owner_public_key, metadata_type, metadata, metadata_parsed, token_burned, ""timestamp"") 
            VALUES ('" + contract_package_hash + "','" +  contract_hash + "','" + contract_type + "','" + contract_name.Replace("'", "''") + "','" + contract_symbol.Replace("'", "''") + "','" + token_id.Replace("'", "''") + "','" + token_owner_account_hash + "','" + token_owner_public_key + "','" + metadata_type + "','" + metadata.Replace("'", "''") + "','" + metadata_parsed.Replace("'", "''") + "'," + token_burned + ",'" + dateTimeFromTimestamp + "')";

            try
            {
                if (sqlCon.State != ConnectionState.Open)
                    await sqlCon.OpenAsync().ConfigureAwait(false);

                using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlInserteQuery, sqlCon))
                {
                    var errorA = await myCommand.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (sqlCon.State == ConnectionState.Open)
                    await sqlCon.CloseAsync();
            }
        }

        return error;
    }

    public async Task<int> InsertContractPackage(string hash, string deploy, string from, string data)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();
        
        // Remove not necessary long binary data in deploy.session.ModuleBytes.module_bytes for database size optimalization.
        JObject jObjectData = JObject.Parse(data);
        try
        {
            JObject jObj = (JObject)jObjectData.SelectToken($"deploy.session.ModuleBytes");

            if(jObj != null)
                jObj.Property("module_bytes").Remove();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        var newData = jObjectData.ToString();

        string sqlQuery = @"INSERT INTO public.node_casper_contract_packages(
	    hash, deploy, ""from"", data)
	    VALUES ('"+ hash + "', '"+ deploy + "', '"+ from + "', '"+ newData.Replace("'", "''") + @"')
	    ON CONFLICT (hash)
	    DO UPDATE
	    SET deploy = '"+ deploy + @"',
	    ""from"" = '"+ from + @"',
	    data = '"+ newData.Replace("'", "''") + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }

            Console.WriteLine("ContractPackage hash added: " + hash);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }       

        return queryAffected;
    }

    public async Task<int> InsertPurseBalance(string hash, string balance)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();

        string sqlQuery = @"INSERT INTO node_casper_purses (purse, balance)
        VALUES('" + hash + "', '"+ balance + @"')
        ON CONFLICT(purse)
        DO UPDATE
        SET balance = '"+ balance + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public async Task<int> InsertPurse(string hash)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();

        string sqlQuery = @"INSERT INTO node_casper_purses (purse, balance)
        VALUES('" + hash + @"', NULL) 
        ON CONFLICT(purse)
        DO UPDATE
        SET balance = NULL";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public async Task<int> InsertAccount(string publicKey, string hash, string purse, decimal balance, string timestamp)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        publicKey = publicKey.ToLower();
        hash = hash.ToLower();

        var timestamp2 = Convert.ToInt64(timestamp);
        var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp2);
        var dateTimeFromTimestamp = dt.ToString("yyyy-MM-dd HH:mm:ss+02");

        string sqlQuery = @"INSERT INTO node_casper_accounts(
	    account_hash, public_key, main_purse, balance, timestamp) VALUES ('" + hash + "', '" + publicKey + "', '" + purse + "', " + balance + ", '" + dateTimeFromTimestamp + "') " +
        "ON CONFLICT (account_hash) " +
        "DO UPDATE " +
        "SET public_key = '" + publicKey + "', main_purse = '"+ purse + "' , balance = " + balance + ", timestamp = '" + dateTimeFromTimestamp + "';";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public async Task<int> InsertAccountHash(string hash, string purse)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();

        string sqlQuery = @"INSERT INTO node_casper_accounts(
	    account_hash, main_purse) VALUES ('"+ hash + "', , '"+ purse + "')";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public async Task<int> InsertNamedKey(string uref, string name, bool isPurse, string initialValue, string contractHash)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        if (initialValue == null)
            initialValue = "null";

        contractHash = contractHash.ToLower();

        string sqlQuery = @"INSERT INTO node_casper_named_keys(
	    uref, name, is_purse, initial_value)
        VALUES ('"+ uref + "', '"+ name + "', '"+ isPurse + "', '"+ initialValue + @"')
	    ON CONFLICT (uref)
	    DO UPDATE
	    SET name = '"+ name + @"',
	    is_purse = '"+ isPurse + @"',    
	    initial_value = '"+ initialValue + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }
                
        string sqlQuery2 = @"INSERT INTO node_casper_contracts_named_keys (contract_hash, named_key_uref) 
        VALUES ('"+ contractHash + "', '"+ uref + "') ON CONFLICT DO NOTHING";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery2, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }


        return queryAffected;
    }

    public async Task<int> InsertContract(string hash, string packageHash, string deploy, string from, string contractName, string contractSymbol, string contractType, double score, string data)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        hash = hash.ToLower();
        
        string sqlQuery = @"INSERT INTO node_casper_contracts (hash, ""package"", deploy, ""from"", name, symbol, type, score, data) 
        VALUES ('" + hash + "', '" + packageHash + "', '" + deploy + "', '" + from + "','" + contractName.Replace("'", "''") + "','" + contractSymbol.Replace("'", "''") + "','" + contractType + "', '" + score.ToString().Replace(",", ".") + "','" + data.Replace("'", "''") + "'" + @") 
        ON CONFLICT (hash)
	    DO UPDATE
	    SET package = '" + packageHash + @"',
	    deploy = '"+ deploy + @"',    
	    ""from"" = '"+ from + @"',
        name = '"+ contractName.Replace("'", "''") + @"', 
        symbol = '"+ contractSymbol.Replace("'", "''") + @"', 
        type = '" + contractType + @"',
	    score = '"+ score.ToString().Replace(",",".") + @"',
	    data = '"+ data.Replace("'", "''") + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }

            Console.WriteLine("Contract hash added: " + hash);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {            
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }
        
        return queryAffected;
    }
   
    public async Task<int> InsertBlock(string hash, string state_root_hash, int era, string timestamp, int height, bool eraEnd, int deploys_count, string validator, string json)
    {
        hash = hash.ToLower();
        bool validated = false;
        try
        {
            await InsertRawBlock(hash, json);
            validated = true;
        }
        catch (Exception ex)
        {
            validated = false;
            Console.WriteLine("Can't InsertRawDeploy: " + hash + ", validated:: false.");
            return -1;
        }

        return await UpdateBlock(hash, state_root_hash, era, timestamp, height, eraEnd, deploys_count, validator, validated);
    }

    public async Task<int> UpdateStateRootHashBlock(string hash, string state_root_hash,int height)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();
        state_root_hash = state_root_hash.ToLower();

        string sqlQuery = "UPDATE node_casper_blocks SET state_root_hash = '" + state_root_hash + "' WHERE hash = '" + hash + "' AND height = " + height + "";
               
        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public async Task<int>UpdateBlock(string hash, string state_root_hash, int era, string timestamp, int height, bool eraEnd, int deploys_count, string validator, bool validated)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();
        if (validator != null)
        {
            validator = validator.ToLower();
        }
        else
        {
            validator = "00";
        }

        string sqlQuery = @"INSERT INTO node_casper_blocks(
	    hash, state_root_hash, era, timestamp, height, era_end, deploys_count, validator, validated) 
	    VALUES ('" + hash + "', '" + state_root_hash + "', " + era + ", '" + timestamp + "', " + height + "," + eraEnd + "," + deploys_count + ",'" + validator + "','" + validated + @"') 
        ON CONFLICT(hash) 
        DO UPDATE 
        SET state_root_hash = '" + state_root_hash + @"',
        era = " + era + @",
	    timestamp = '" + timestamp + @"',
	    height = " + height + @",
	    era_end = '" + eraEnd + @"',
        deploys_count = " + deploys_count + @",
        validator = '" + validator + @"',
	    validated = '" + validated + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public async Task<int> InsertRawBlock(string hash, string json)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();

        string sqlQuery = @"INSERT INTO node_casper_raw_blocks (hash, data) 
	    VALUES ('" + hash + "', '" + json.Replace("'", "''") + @"') 
	    ON CONFLICT (hash) 
        DO UPDATE 
        SET data = '" + json + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    /// <summary>
    /// InsertDeploy
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="from"></param>
    /// <param name="cost"></param>
    /// <param name="result"></param>
    /// <param name="timestamp"></param>
    /// <param name="block"></param>
    /// <param name="deployType"></param>
    /// <param name="json"></param>
    /// <param name="metadataType"></param>
    /// <param name="contractName"></param>
    /// <param name="contractHash"></param>
    /// <param name="contractSymbol"></param>
    /// <param name="entrypoint"></param>
    /// <param name="metadata"></param>
    /// <param name="events"></param>
    /// <returns>queryAffected</returns>
    public async Task<int> InsertDeploy(string hash, string from, string cost, bool result, string timestamp, string block, string deployType, string json, string metadataType, string contractHash, string contractName, string contractSymbol, string entrypoint, string metadata, string events)
    {
        hash = hash.ToLower();

        if(hash.Length > 68)
        {
            Console.WriteLine("TO LONG HASH");
        }
        if (from.Length > 68)
        {
            Console.WriteLine("TO LONG FROM");
        }
        if (block.Length > 68)
        {
            Console.WriteLine("TO LONG BLOCK");
        }
        if (contractHash.Length > 68)
        {
            Console.WriteLine("TO LONG contractHash");
        }

        try
        {
            await InsertRawDeploy(hash, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in InsertDeploy: " + ex.ToString());
            return -1;
        }

        return await UpdateDeploy(hash, from, cost, result, timestamp, block, deployType, metadataType, contractHash, contractName, contractSymbol, entrypoint, metadata, events);
    }

    /// <summary>
    /// UpdateDeploy
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="from"></param>
    /// <param name="cost"></param>
    /// <param name="result"></param>
    /// <param name="timestamp"></param>
    /// <param name="block"></param>
    /// <param name="deployType"></param>
    /// <param name="metadataType"></param>
    /// <param name="contractHash"></param>
    /// <param name="contractName"></param>
    /// <param name="entrypoint"></param>
    /// <param name="metadata"></param>
    /// <param name="events"></param>
    /// <returns>queryAffected</returns>
    public async Task<int> UpdateDeploy(string hash, string from, string cost, bool result, string timestamp, string block, string deployType, string metadataType, string contractHash, string contractName, string contractSymbol, string entrypoint, string metadata, string events)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        var timestamp2 = Convert.ToInt64(timestamp);
        var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp2);
        var dateTimeFromTimestamp = dt.ToString("yyyy-MM-dd HH:mm:ss+02");
        hash = hash.ToLower();

        string sqlQuery = @"INSERT INTO node_casper_deploys(
	    hash, ""from"", cost, result, ""timestamp"", block, type, metadata_type, contract_hash, contract_name, contract_symbol, entrypoint, metadata, events)  
        VALUES('" + hash + "','" + from + "','" + cost + "','" + result + "','" + dateTimeFromTimestamp + "','" + block + "','" + deployType + "','" + metadataType + "','" + contractHash + "','" + contractName.Replace("'", "''") + "','" + contractSymbol.Replace("'", "''") + "','" + entrypoint.Replace("'", "''") + "','" + metadata.Replace("'", "''") + "','" + events.Replace("'", "''") + "')" + @"
        ON CONFLICT(hash) 
        DO UPDATE 
        SET ""from"" = '" + from + "', cost = '" + cost + "', result = '" + result + "'," + @"""timestamp"" = '" + dateTimeFromTimestamp + "', block = '" + block + "', type = '" + deployType + "', metadata_type = '" + metadataType + "', contract_hash = '" + contractHash + "', contract_name = '" + contractName.Replace("'", "''") + "', contract_symbol = '" + contractSymbol.Replace("'", "''") + "', entrypoint = '" + entrypoint.Replace("'", "''") + "', metadata = '" + metadata.Replace("'", "''") + "', events = '" + events.Replace("'", "''") + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            queryAffected = -1;
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    /// <summary>
    /// InsertRawDeploy
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="json"></param>
    /// <returns>queryAffected</returns>
    public async Task<int> InsertRawDeploy(string hash, string json)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();
        
        string sqlQuery = @"INSERT INTO node_casper_raw_deploys (hash, data) 
	    VALUES ('" + hash + "', '" + json.Replace("'","''") + @"') 
	    ON CONFLICT (hash) 
        DO UPDATE 
        SET data = '" + json.Replace("'", "''") + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {           
            Console.WriteLine(ex.ToString());
            queryAffected = -1;
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }
    
    public async Task GetMissingNftsFromBlockToActualBlock(int blockHeigh)
    {
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string missingBlocks = string.Empty;
        long lowestExistingBlock;

        NodeCasperParser.Services.Contracts contract = new NodeCasperParser.Services.Contracts();

        CasperNodeDeployService cnds = new CasperNodeDeployService();
        PostgresCasperNodeService pcns = new PostgresCasperNodeService();

        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
        int lastBlockHeight = (int)client.GetBlock().Result.Parse().Block.Header.Height;

        int counter = 1;

        for (int blockHeightToScan = blockHeigh; blockHeightToScan < lastBlockHeight; blockHeightToScan++)
        {

            counter += 1;
            var missingBlockToScan = await client.GetBlock(blockHeightToScan);

            int deploysCount = missingBlockToScan.Parse().Block.Body.DeployHashes.Count;

            Console.WriteLine(counter + ". Scanning Block for missing NFTs: " + blockHeightToScan + " | Found: " + deploysCount + " deploys.");

            if (deploysCount > 0)
            {
                List<string> deploysList = missingBlockToScan.Parse().Block.Body.DeployHashes;


                foreach (string deploy in deploysList)
                { // error na bloku 1950475
                    var deployHash = deploy.ToString().ToLower();

                    string from = string.Empty;
                    string json = string.Empty;

                    try
                    {
                        var rpcDeploy = client.GetDeploy(deployHash);
                        from = rpcDeploy.Result.Parse().Deploy.Header.Account.ToString().ToLower();
                        json = rpcDeploy.Result.Result.GetRawText().ToString();
                       // var json2 = rpcDeploy.Result.Result.GetRawText();

                        if (from.Length > 0 && json.Length > 0)
                        {
                            var blockHash = rpcDeploy.Result.Parse().ExecutionResults[0].BlockHash.ToLower();
                            var resultDeployParse = rpcDeploy.Result.Parse();

                            var timestamp = rpcDeploy.Result.Parse().Deploy.Header.Timestamp.ToString();

                            var deployType = rpcDeploy.Result.Parse().Deploy.Session.GetType().Name.Replace("DeployItem", "");

                            var (result, cost, CostException) = cnds.GetResultAndCostFromNode(rpcDeploy.Result.Parse());

                            try
                            {
                                NodeCasperParser.NftParser.NftParser nft = new NodeCasperParser.NftParser.NftParser();

                                var events = cnds.GetEventsFromNode(client, rpcDeploy.Result.Parse());
                                var (metadataType, metadata) = cnds.GetDeployMetadataFromNode(resultDeployParse);

                                var (contractHash, contractHashException) = cnds.GetStoredContractHashFromNode(resultDeployParse);

                                string contract_hash = string.Empty;
                                string contract_package_hash = string.Empty;

                                contract_hash = contractHash;

                                await nft.GetNftFromEvent(client, result, from, contract_hash, contract_package_hash, events, metadataType, metadata, timestamp);
                                
                            }
                            catch
                            {
                                /*
                                // piszê teraz GetModuleByteMetadataFromNode
                                var (contractHash, contractHashException) = cnds.GetStoredContractHashFromNode(resultDeployParse);
                                //   var (contractName, contractNameException) = GetNameFromNode(contractHash, resultDeployParse);
                                var (contractName, contractNameException) = cnds.GetNameFromNode(resultDeployParse);

                                var (entrypoint, EntryPointException) = cnds.GetEntrypointFromNode(resultDeployParse);
                                var (metadataType, metadata) = cnds.GetDeployMetadataFromNode(resultDeployParse);
                                //  var events = GetEventsFromNode(rpcDeploy.Result.Parse()); // nie dzia³a w pe³ni
                                // var events = GetEventsFromJson(rpcDeploy.Result.Parse()); // nie dzia³a w pe³ni chyba

                                var events = cnds.GetEventsFromNode(client, resultDeployParse); // DZIA£A - TEGO U¯YWAÆ 

                                //  var insertDeploy = pcns.InsertDeploy(deployHash, from, cost, result, timestamp, blockHash, deployType, json, metadataType, contractHash, contractName, entrypoint, metadata, events);

                                NodeCasperParser.NftParser.NftParser nft = new NodeCasperParser.NftParser.NftParser();
                                var (contract_nft, token_nft_id, nftException) = nft.GetNftFromContract(client, result, contractHash, from, entrypoint, metadata, events, timestamp).Result;
                                */
                            }

                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(counter + ". ERROR in scanned Block: " + blockHeightToScan + " | Founded: " + deploysCount + " deploys. " + ex.ToString());

                    }
                }
            }
        }
        Console.WriteLine("DONE: GetMissingNftsFrom block: " + blockHeigh + " to actual block.");
    }

    public async Task GetMissingDeploy(string deploy_hash)
    {
        CasperNodeDeployService cnds = new CasperNodeDeployService();
       
        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);

        int counter = 1;
      
        var blockHash = client.GetDeploy(deploy_hash).Result.Parse().ExecutionResults[0].BlockHash;

        counter += 1;
      
        var missingBlockToScan = client.GetBlock(blockHash).Result.Parse();

        int deploysCount = missingBlockToScan.Block.Body.DeployHashes.Count;
        int transfersCount = missingBlockToScan.Block.Body.TransferHashes.Count;

        int blockHeight = (int)missingBlockToScan.Block.Header.Height;

        Console.WriteLine(counter + ". Scanning Block for missing deploys: " + blockHeight + " | Found: " + deploysCount + " deploys | " + transfersCount + " transfers.");

        if (deploysCount > 0)
        {
            List<string> deploysList = missingBlockToScan.Block.Body.DeployHashes;

            cnds.GetDeploy(client, deploysList);
        }

        if (transfersCount > 0)
        {
            List<string> transfersList = missingBlockToScan.Block.Body.TransferHashes;

            cnds.GetDeploy(client, transfersList);
        }
    }

    public async Task GetMissingDeploysFromBlockToActualBlock(int blockHeigh)
    {
        string sqlDataSource = psqlServer;
        string missingBlocks = string.Empty;

        CasperNodeDeployService cnds = new CasperNodeDeployService();

        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);

        int counter = 1;
        int lastBlockHeight = (int)client.GetBlock().Result.Parse().Block.Header.Height;

        for (int blockHeightToScan = blockHeigh; blockHeightToScan < lastBlockHeight; blockHeightToScan++)
        {            
            counter += 1;
       
            var missingBlockToScan = await client.GetBlock(blockHeightToScan);

            int deploysCount = missingBlockToScan.Parse().Block.Body.DeployHashes.Count;
            int transfersCount = missingBlockToScan.Parse().Block.Body.TransferHashes.Count;

            //  Console.WriteLine(counter + ". Scanning Block for missing deploys: " + blockHeightToScan + " | Found: " + deploysCount + " deploys.");
            Console.WriteLine(counter + ". Scanning Block for missing deploys: " + blockHeightToScan + " | Found: " + deploysCount + " deploys | " + transfersCount + " transfers.");
                        

            if (transfersCount > 0)
            {                
                List<string> transfersList = missingBlockToScan.Parse().Block.Body.TransferHashes;

                cnds.GetDeploy(client, transfersList);
            }

            if (deploysCount > 0)
            {
                List<string> deploysList = missingBlockToScan.Parse().Block.Body.DeployHashes;

                cnds.GetDeploy(client, deploysList);
                /*
                foreach (string deploy in deploysList)
                { // error na bloku 1950475
                    var deployHash = deploy.ToString().ToLower();

                    string from = string.Empty;
                    string json = string.Empty;

                    try
                    {
                        var rpcDeploy = client.GetDeploy(deployHash);
                        from = rpcDeploy.Result.Parse().Deploy.Header.Account.ToString().ToLower();
                        json = rpcDeploy.Result.Result.GetRawText().ToString();

                        if (from.Length > 0 && json.Length > 0)
                        {
                            //string deployHash = deploy.DeployHash.ToString().ToLower();

                            //var rpcDeploy = client.GetDeploy(deployHash);// testowo - dc2f9253820ad4bfe11a78fd43bc6c463e0784bfc61625b2aad3643e01cfc4da
                            //            string blockHash = deploy.BlockHash.ToString().ToLower();
                            var blockHash = rpcDeploy.Result.Parse().ExecutionResults[0].BlockHash.ToLower();
                            var resultDeployParse = rpcDeploy.Result.Parse();

                            //var from = rpcDeploy.Result.Parse().Deploy.Header.Account.ToString().ToLower();

                            // var json = rpcDeploy.Result.Result.GetRawText().ToString();
                            var timestamp = rpcDeploy.Result.Parse().Deploy.Header.Timestamp.ToString();

                            var deployType = rpcDeploy.Result.Parse().Deploy.Session.GetType().Name.Replace("DeployItem", "");

                            var (result, cost, CostException) = cnds.GetResultAndCostFromNode(rpcDeploy.Result.Parse());

                            // piszê teraz GetModuleByteMetadataFromNode
                            var (contractHash, contractHashException) = cnds.GetStoredContractHashFromNode(resultDeployParse);

                            if(contractHash == "unknown")
                            {
                               // cnds.GetWriteContractPackageFromJson(json);
                                var (contractPackageHash, contractPackageHashException) = cnds.GetStoredContractHashFromNode(resultDeployParse);

                            }
                            //   var (contractName, contractNameException) = GetNameFromNode(contractHash, resultDeployParse);
                            var (contractName, contractNameException) = cnds.GetNameFromNode(resultDeployParse);

                            var (entrypoint, EntryPointException) = cnds.GetEntrypointFromNode(resultDeployParse);
                            var (metadataType, metadata) = cnds.GetDeployMetadataFromNode(resultDeployParse);
                            //  var events = GetEventsFromNode(rpcDeploy.Result.Parse()); // nie dzia³a w pe³ni
                            // var events = GetEventsFromJson(rpcDeploy.Result.Parse()); // nie dzia³a w pe³ni chyba

                            var events = cnds.GetEventsFromNode(client, resultDeployParse); // DZIA£A - TEGO U¯YWAÆ 

                            var insertDeploy = pcns.InsertDeploy(deployHash, from, cost, result, timestamp, blockHash, deployType, json, metadataType, contractHash, contractName, contractSymbol, entrypoint, metadata, events);

                        //    NodeCasperParser.NftParser.NftParser nft = new NodeCasperParser.NftParser.NftParser();
                        //    var (contract_nft, token_nft_id, nftException) = nft.GetNftFromDeploy(client, result, contractHash, from, entrypoint, metadata, events, timestamp).Result;

                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(counter + ". ERROR adding deploy: " + deployHash + " in scanned Block: " + blockHeightToScan + " | Founded: " + deploysCount + " deploys. " + ex.ToString());

                    }
                }*/
            }
        }
    }
    

    public async Task GetMissingContractPackageFromBlockHeightToActualBlock(int blockHeigh) // blok testowy  1950912
    {
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string missingBlocks = string.Empty;
        long lowestExistingBlock;

        NodeCasperParser.Services.Contracts contract = new NodeCasperParser.Services.Contracts();

        CasperNodeDeployService cnds = new CasperNodeDeployService();
        PostgresCasperNodeService pcns = new PostgresCasperNodeService();

        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);

      
        int counter = 1;

        int lastBlockHeight = (int)client.GetBlock().Result.Parse().Block.Header.Height;

        for (int blockHeightToScan = blockHeigh; blockHeightToScan < lastBlockHeight; blockHeightToScan++)
        {
       //     for (int blockHeightToScan = Convert.ToInt32(blockHeigh); blockHeightToScan > 0; blockHeightToScan--)
       // {
            
            counter += 1;
            var missingBlockToScan = await client.GetBlock(blockHeightToScan);

            int deploysCount = missingBlockToScan.Parse().Block.Body.DeployHashes.Count;

            Console.WriteLine(counter + ". Scanning Block: " + blockHeightToScan + " | Found: " + deploysCount + " deploys.");

            if (deploysCount > 0)
            {
                List<string> deploysList = missingBlockToScan.Parse().Block.Body.DeployHashes;


                foreach (string deploy in deploysList)
                { // error na bloku 1950475
                    var deployHash = deploy.ToString();

                    string from = string.Empty;
                    string json = string.Empty;
                                        
                    try
                    {
                        var rpcDeploy = client.GetDeploy(deployHash);
                        from = rpcDeploy.Result.Parse().Deploy.Header.Account.ToString().ToLower();
                        json = rpcDeploy.Result.Result.GetRawText().ToString();
                    }
                    catch
                    {
                        Console.WriteLine(counter + ". ERROR in scanned Block: " + blockHeightToScan + " | Founded: " + deploysCount + " deploys.");
                                      
                    }

                    if (from.Length > 0 && json.Length > 0)
                    {
                        var writeContractPackage = cnds.GetWriteContractPackageFromJson(json);

                        for (int countContractPackage = 0; countContractPackage < writeContractPackage.Length; countContractPackage++)
                        {
                            var insertContractPackage = pcns.InsertContractPackage(writeContractPackage[countContractPackage], deployHash, from, json);

                            var writeContract = cnds.GetWriteContractFromJson(json);

                            for (int countContract = 0; countContract < writeContract.Length; countContract++)
                            {
                                var jsonContractData = await contract.GetContract(writeContract[countContract]);
                               
                                var jsonContractTypesLayoutFilePatch = "contractTypes.json";
                                var parser = new ContractParser();
                                var (contractTypeName, score) = parser.GetContractTypeAndScore(jsonContractData, jsonContractTypesLayoutFilePatch);

                                var (contractName, contractSymbol) = parser.GetContractNameAndSymbol(client, jsonContractData).Result;

                                var namedKeys = await parser.RetrieveNamedKeyValues(client, jsonContractData);
                                foreach (var namedKey in namedKeys)
                                {
                                    var insertNamedKey = pcns.InsertNamedKey(namedKey.key, namedKey.name, namedKey.is_purse, namedKey.initial_value?.ToString(), writeContract[countContract]);
                                }
                               
                                var insertContract = pcns.InsertContract(writeContract[countContract], writeContractPackage[countContractPackage], deployHash, from, contractName, contractSymbol, contractTypeName, score, jsonContractData.ToString());
                            }
                        }
                    }
                }
            }
        }

        //return missingBlocks;

    }

    public async Task GetMissingStateRootHashFromLowestExistingBlock()
    {
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string missingBlocks = string.Empty;
        long lowestExistingBlock;

        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
        int lastBlockHeight = (int)client.GetBlock().Result.Parse().Block.Header.Height;

        string sqlQuery = @"SELECT MIN(height) FROM node_casper_blocks WHERE state_root_hash IS NULL";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        foreach (DataRow row in queryResultTable.Rows)
        {
            lowestExistingBlock = (long)row["min"];
            Console.WriteLine("Lowest existing block founded: " + lowestExistingBlock + "");

            for (int blockHeightAdded = Convert.ToInt32(lowestExistingBlock); blockHeightAdded < lastBlockHeight; blockHeightAdded++)
            {
                Console.WriteLine("UPDATING state_root_hash for block: " + blockHeightAdded + "");

                var missingBlockToAdd = client.GetBlock(blockHeightAdded);

                int missingHeight = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.Height);
                string missingHash = missingBlockToAdd.Result.Parse().Block.Hash.ToString();

                var missingBlockStateRootHash = missingBlockToAdd.Result.Parse().Block.Header.StateRootHash.ToLower();

                var insertMissingBlock = UpdateStateRootHashBlock(missingHash, missingBlockStateRootHash, missingHeight);

            }
        }
    }

    public void GetMissingBlocksFromBlock(int blockHeight)
    {
        int counter = 0;
        CasperNodeDeployService cnds = new CasperNodeDeployService();

        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
        int lastBlockHeight = (int)client.GetBlock().Result.Parse().Block.Header.Height;

        Console.WriteLine("Newest existing block founded: " + lastBlockHeight + "");

        for (int blockHeightAdded = blockHeight; blockHeightAdded < lastBlockHeight; blockHeightAdded++)
        {
            Console.WriteLine("UPDATING data for block: " + blockHeightAdded + "");

            var missingBlockToAdd = client.GetBlock(blockHeightAdded).Result;

            var missingJson = missingBlockToAdd.Result.GetRawText().ToString();

            int missingEra = Convert.ToInt32(missingBlockToAdd.Parse().Block.Header.EraId);
            string missingTimestamp = missingBlockToAdd.Parse().Block.Header.Timestamp.ToString();
            int missingHeight = Convert.ToInt32(missingBlockToAdd.Parse().Block.Header.Height);
            string missingHash = missingBlockToAdd.Parse().Block.Hash.ToString();
            var missingEraEnd = missingBlockToAdd.Parse().Block.Header.EraEnd != null;
            var missingBlockExecutedByValidator = missingBlockToAdd.Parse().Block.Body.Proposer.PublicKey?.ToString();//.ToAccountHex();
            var missingBlockDeploysCount = missingBlockToAdd.Parse().Block.Body.DeployHashes.Count;

            var missingBlockStateRootHash = missingBlockToAdd.Parse().Block.Header.StateRootHash.ToLower();

            var insertMissingBlock = InsertBlock(missingHash, missingBlockStateRootHash, missingEra, missingTimestamp, missingHeight, missingEraEnd, missingBlockDeploysCount, missingBlockExecutedByValidator, missingJson).Result;

            counter += 1;

            int deploysCount = missingBlockToAdd.Parse().Block.Body.DeployHashes.Count;
            int transfersCount = missingBlockToAdd.Parse().Block.Body.TransferHashes.Count;

            //  Console.WriteLine(counter + ". Scanning Block for missing deploys: " + blockHeightToScan + " | Found: " + deploysCount + " deploys.");
            Console.WriteLine(counter + ". Scanning Block Height: " + missingBlockToAdd.Parse().Block.Header.Height + " for missing deploys: | Found: " + deploysCount + " deploys | " + transfersCount + " transfers.");

            if (transfersCount > 0)
            {
                List<string> transfersList = missingBlockToAdd.Parse().Block.Body.TransferHashes;

                cnds.GetDeploy(client, transfersList);
            }

            if (deploysCount > 0)
            {
                List<string> deploysList = missingBlockToAdd.Parse().Block.Body.DeployHashes;

                cnds.GetDeploy(client, deploysList);
            }
        }
    }

    public async Task GetMissingBlocksFromLowestExistingBlock()
    {
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string missingBlocks = string.Empty;
        long lowestExistingBlock;

        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
        int lastBlockHeight = (int)client.GetBlock().Result.Parse().Block.Header.Height;

        string sqlQuery = @"SELECT MIN(height) FROM node_casper_blocks";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        foreach (DataRow row in queryResultTable.Rows)
        {
            lowestExistingBlock = (long)row["min"];
            Console.WriteLine("Lowest existing block founded: " + lowestExistingBlock + "");
                       
            for (int blockHeightAdded = Convert.ToInt32(lowestExistingBlock); blockHeightAdded < lastBlockHeight; blockHeightAdded++)
            {
                Console.WriteLine("UPDATING data for block: " + blockHeightAdded + "");

                var missingBlockToAdd = client.GetBlock(blockHeightAdded);

                var missingJson = missingBlockToAdd.Result.Result.GetRawText().ToString();

                int missingEra = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.EraId);
                string missingTimestamp = missingBlockToAdd.Result.Parse().Block.Header.Timestamp.ToString();
                int missingHeight = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.Height);
                string missingHash = missingBlockToAdd.Result.Parse().Block.Hash.ToString();
                var missingEraEnd = missingBlockToAdd.Result.Parse().Block.Header.EraEnd != null;
                var missingBlockExecutedByValidator = missingBlockToAdd.Result.Parse().Block.Body.Proposer.PublicKey?.ToString();//.ToAccountHex();
                var missingBlockDeploysCount = missingBlockToAdd.Result.Parse().Block.Body.DeployHashes.Count;

                var missingBlockStateRootHash = missingBlockToAdd.Result.Parse().Block.Header.StateRootHash.ToLower();

                var insertMissingBlock = InsertBlock(missingHash, missingBlockStateRootHash, missingEra, missingTimestamp, missingHeight, missingEraEnd, missingBlockDeploysCount, missingBlockExecutedByValidator, missingJson);

            }

        }
        //return missingBlocks;
    }

    /// <summary>
    /// GetMissingBlocks
    /// </summary>
    /// <returns>missingBlocks</returns>
    public async Task<string> GetMissingBlocks()
    {
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string missingBlocks = string.Empty;
        long missingBlock;

        string sqlQuery = @"SELECT all_ids AS missing_ids FROM generate_series((SELECT MIN(height) FROM node_casper_blocks), (SELECT MAX(height) FROM node_casper_blocks)) all_ids EXCEPT SELECT height FROM node_casper_blocks ORDER BY missing_ids ASC LIMIT 100";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        foreach (DataRow row in queryResultTable.Rows)
        {
            missingBlock = (long)row["missing_ids"];

          //  missingBlocks = string.Join(",", missingBlock.ToString());
            missingBlocks = missingBlocks + "," + missingBlock;

            Console.WriteLine("Missing block found: id =" + missingBlock + "");
        }

        return missingBlocks;
    }

    public class MissingBlocksWhereFoundMissingDeploys
    {
        public long block_height { get; set; }
        public string block_hash { get; set; }
        public int block_deploys { get; set; }
        public long deploy_count_in_deploys { get; set; }
    }

    /// <summary>
    /// GetMissingDeploysFromExistingBlocks
    /// </summary>
    /// <returns>missingBlocks hashes where founded missing deploys</returns>
    public async Task<List<MissingBlocksWhereFoundMissingDeploys>> GetMissingDeploysFromExistingBlocks()
    {
        string sqlDataSource = psqlServer;
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();

        string missingBlock = string.Empty; ;
        long countMissingDeploysInBlock;
        int deploy_count_in_deploys;

        string sqlQuery = @"SELECT 
	    b.height AS block_height,
        b.hash AS block_hash, 
        b.deploys_count AS block_deploys,  
        COUNT(d.block) AS deploy_count_in_deploys
        FROM 
        node_casper_blocks b
        LEFT JOIN 
        node_casper_deploys d ON b.hash = d.block
        GROUP BY 
        b.hash, 
        b.deploys_count 
        HAVING 
        b.deploys_count > COUNT(d.block)
        ORDER BY 
        b.height DESC;";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }
        
        var blocks = new List<MissingBlocksWhereFoundMissingDeploys>();

        foreach (DataRow row in queryResultTable.Rows)
        {
            countMissingDeploysInBlock = (long)row["block_deploys"];
            deploy_count_in_deploys = (int)row["deploy_count_in_deploys"];
            long missingDeploys = countMissingDeploysInBlock - deploy_count_in_deploys;

            blocks.Add(new MissingBlocksWhereFoundMissingDeploys
            {
                block_height = (long)row["block_height"],
                block_hash = (string)row["block_hash"],
                block_deploys = (int)row["block_deploys"],
                deploy_count_in_deploys = (long)row["deploy_count_in_deploys"]
            });

            // missingBlocks = missingBlocks + "," + missingBlock;

            if (debugMode)
            {
                Console.WriteLine("Found " + missingDeploys + " missing deploys in block : " + missingBlock + "");
            }
        }

        return blocks;
    }
        
    /// <summary>
    /// GetMissingMetadataDeploysHash
    /// </summary>
    /// <returns>missingDeploys</returns>
    public async Task<string> GetMissingMetadataDeploysHash()
    {
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string missingDeploys = string.Empty;
        string missingDeploy = string.Empty;

        string sqlQuery = @"SELECT hash FROM node_casper_deploys WHERE metadata IS NULL";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        foreach (DataRow row in queryResultTable.Rows)
        {
            missingDeploy = (string)row["hash"];

            missingDeploys = string.Join(",", missingDeploy);
        }

        return missingDeploys;
    }

    /// <summary>
    /// GetRawDeploy
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>deploy</returns>
    public async Task<string> GetRawDeploy(string hash)
    {
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string deploy = string.Empty;

        string sqlQuery = @"SELECT data FROM node_casper_raw_deploys WHERE hash = '" + hash + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        foreach (DataRow row in queryResultTable.Rows)
        {
            deploy = (string)row["data"];
        }

        return deploy;
    }
    /// <summary>
    /// GetRawBlock
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>block</returns>
    public async Task<string> GetRawBlock(string hash)
    {
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
        NpgsqlDataReader dbReader;
        DataTable queryResultTable = new DataTable();
        string block = string.Empty;

        string sqlQuery = @"SELECT data FROM node_casper_raw_blocks WHERE hash ='" + hash + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                dbReader = await myCommand.ExecuteReaderAsync();
                queryResultTable.Load(dbReader);
                dbReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        foreach (DataRow row in queryResultTable.Rows)
        {
            block = (string)row["data"];
        }

        return block;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hashes"></param>
    /// <returns></returns>
    public async Task<int> CountDeploys(string[] hashes)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);
                
        string hashesString = string.Join(",", hashes);

        hashesString = hashesString.ToLower();

        string sqlQuery = "SELECT count(*) FROM node_casper_deploys WHERE hash IN ('" + hashesString + "')";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = (int)(await myCommand.ExecuteScalarAsync());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            queryAffected = -1;
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    /// <summary>
    /// ValidateBlock
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>-1 if error, 0 no update, else ok</returns>
    public async Task<int> ValidateBlock(string hash)
    {
        int queryAffected = 0;
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        hash = hash.ToLower();

        string sqlQuery = "UPDATE node_casper_blocks SET validated = true WHERE hash = '" + hash + "'";

        try
        {
            if (sqlCon.State != ConnectionState.Open)
                await sqlCon.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand myCommand = new NpgsqlCommand(sqlQuery, sqlCon))
            {
                queryAffected = await myCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            queryAffected = -1;
        }
        finally
        {
            if (sqlCon.State == ConnectionState.Open)
                await sqlCon.CloseAsync();
        }

        return queryAffected;
    }

    public void InsertRewards(List<List<object>> rowsToInsert)
    {
        string sqlDataSource = psqlServer;// _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection npgsql = new NpgsqlConnection(sqlDataSource);

        using var conn = npgsql.BeginBinaryImport("COPY rewards (block, era, delegator_public_key, validator_public_key, amount) FROM STDIN (FORMAT BINARY)");
        foreach (var row in rowsToInsert)
        {
            conn.StartRow();
            foreach (var col in row)
            {
                conn.Write(col, NpgsqlTypes.NpgsqlDbType.Unknown);
            }
        }
        conn.Complete();
        conn.Close();
    }

}