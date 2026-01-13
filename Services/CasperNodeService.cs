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
using Casper.Network.SDK.JsonRpc.ResultTypes;
using Casper.Network.SDK.Types;

public interface ICasperNodeService
{
   
}

public class RewardPayload
{
    public string BlockHash { get; set; }
}

public class CasperNodeService : ICasperNodeService
{
    private readonly AppSettings _appSettings;
    private readonly IConfiguration _configuration;

    private static string psqlServer { get; } = ParserConfig.getToken("psqlServer");
    private static string rpcServer { get; } = ParserConfig.getToken("rpcUrl");
    private static string debugModes { get; } = ParserConfig.getToken("debugMode");
    bool debugMode = false;
    public const string TypeReward = "reward:raw";

    public CasperNodeService(/*IOptions<AppSettings> appSettings,*/ /*IConfiguration configuration*/)
    {
      //  _appSettings = appSettings.Value;
      //  _configuration = configuration;
    }

    public async Task GetBlocks(SSEvent evt)
    {
        if (evt.EventType == EventType.BlockAdded)
        {
            var block = evt.Parse<BlockAdded>();
            string hash = block.BlockHash.ToString().ToLower();
            int era = Convert.ToInt32(block.Block.Header.EraId);
            string timestamp = block.Block.Header.Timestamp.ToString();
            int height = Convert.ToInt32(block.Block.Header.Height);

            Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcServer);
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            var getBlock = client.GetBlock(hash);
            var json = getBlock.Result.Result.GetRawText().ToString();
            var eraEnd = block.Block.Header.EraEnd != null;
            var blockExecutedByValidator = block.Block.Body.Proposer.PublicKey?.ToString();
            var blockDeploysCount = block.Block.Body.DeployHashes.Count;
            var blockStateRootHash = block.Block.Header.StateRootHash.ToLower();

            Console.WriteLine("Dodawany blok: " + block.Block.Header.Height.ToString());

            var insertBlock = postgresCasperNodeServices.InsertBlock(hash, blockStateRootHash, era, timestamp, height, eraEnd, blockDeploysCount, blockExecutedByValidator, json);

            var missingBlocksList = postgresCasperNodeServices.GetMissingBlocks().Result.ToString();

            try
            {
                missingBlocksList = missingBlocksList.Remove(0, 1);
            }
            catch
            {
                missingBlocksList = string.Empty;
            }

            if (missingBlocksList != string.Empty)
            {
                List<int> TagIds = missingBlocksList.Split(',').Select(int.Parse).ToList();

                //  Console.WriteLine("Lista Brakuj¹cych bloków: " + TagIds.ToList().ToString());
                //  Console.WriteLine("---------------------------");

                foreach (var missingBlock in TagIds)
                {
                    Console.WriteLine("Brakuj¹cy blok: " + missingBlock.ToString());
                    var missingBlockToAdd = client.GetBlock(missingBlock);

                    var missingJson = missingBlockToAdd.Result.Result.GetRawText().ToString();

                    int missingEra = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.EraId);
                    string missingTimestamp = missingBlockToAdd.Result.Parse().Block.Header.Timestamp.ToString();
                    int missingHeight = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.Height);
                    string missingHash = missingBlockToAdd.Result.Parse().Block.Hash.ToString();
                    var missingEraEnd = missingBlockToAdd.Result.Parse().Block.Header.EraEnd != null;
                    var missingBlockExecutedByValidator = missingBlockToAdd.Result.Parse().Block.Body.Proposer.PublicKey?.ToString();
                    var missingBlockDeploysCount = missingBlockToAdd.Result.Parse().Block.Body.DeployHashes.Count;
                    var missingBlockStateRootHash = missingBlockToAdd.Result.Parse().Block.Header.StateRootHash.ToLower();

                    try
                    {
                        var insertMissingBlock = postgresCasperNodeServices.InsertBlock(missingHash, missingBlockStateRootHash, missingEra, missingTimestamp, missingHeight, missingEraEnd, missingBlockDeploysCount, missingBlockExecutedByValidator, missingJson);

                        Console.WriteLine("Dodano brakuj¹cy blok: " + missingHeight);
                    }
                    catch
                    {
                        Console.WriteLine("Nie dodano brakuj¹cego bloku: " + missingHeight);
                    }
                }
            }
        }
    }

    public async Task GetReward(SSEvent evt)
    {
        var block = evt.Parse<BlockAdded>();

        string sqlDataSource = _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        string rpcUrl = _configuration.GetConnectionString("rpcUrl");
        Casper.Network.SDK.NetCasperClient client = new NetCasperClient(rpcUrl);

        int height = Convert.ToInt32(block.Block.Header.Height);

        var getAuctionInfo = await client.GetAuctionInfo(height);

        var json = getAuctionInfo.Result.GetRawText().ToString();

        var bids = getAuctionInfo.Parse().AuctionState.Bids;

        foreach (var bid in bids)
        {
            var staked = "";
            var publicKey = "";
            var bondingPurse = "";
            var delegationRate = "";
            bool inactive = false;

            staked = bid.StakedAmount.ToString();
            publicKey = bid.PublicKey.ToString().ToLower();
            bondingPurse = bid.BondingPurse.ToString();
            delegationRate = bid.DelegationRate.ToString();
            inactive = bid.Inactive;

            foreach(var bidDelegators in bid.Delegators)
            {
               // bidDelegators.
            }

        }

    }

    public void HandleAccountTask(NetCasperClient client, int blockHeight)
    {
     //   string sqlDataSource = _configuration.GetConnectionString("psqlServer");
      //  NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

    //    string rpcUrl = _configuration.GetConnectionString("rpcUrl");
    //    var client = new NetCasperClient(rpcUrl);

        var getBlock = client.GetBlock(blockHeight);
        var proofs = getBlock.Result.Parse().Block.Proofs;

        Dictionary<string, string> proofsDictionaryList = new Dictionary<string, string>();

        if (proofs != null)
        {
            foreach (var p in proofs)
            {
                var publicKey = p.PublicKey.ToString();
                var accountHash = AccountHashKey.FromString(p.PublicKey.ToString());

                proofsDictionaryList.Add(publicKey, accountHash.ToHexString().ToLower());
            }
        }

        //  var eraParsed = await client.GetAccountBalance()
    }

    public async Task HandleRewardTask(int blockHeight)
    {
        string sqlDataSource = _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        string rpcUrl = _configuration.GetConnectionString("rpcUrl");
        var client = new NetCasperClient(rpcUrl);

        var eraParsed = await client.GetEraInfoBySwitchBlock(blockHeight);

        // Prepare rows to insert into the database
        var rowsToInsert = new List<object[]>();
        foreach (var s in eraParsed.Parse().EraSummary.StoredValue.EraInfo.SeigniorageAllocations)
        {
            string dpk = null;
            var vpk = "";
            var amount = "";
            if (s.IsDelegator)
            {
                dpk = s.DelegatorPublicKey.ToString().ToLower();
                vpk = s.ValidatorPublicKey.ToString().ToLower();
                amount = s.Amount.ToString();
            }
            if (!s.IsDelegator)
            {
                vpk = s.ValidatorPublicKey.ToString().ToLower();
                amount = s.Amount.ToString();
            }
            var row = new object[] { eraParsed.Parse().EraSummary.BlockHash.ToString().ToLower(), eraParsed.Parse().EraSummary.EraId, dpk, vpk, amount };
            rowsToInsert.Add(row);

        }
        /*
        // Insert the rows into the database
        using var conn = new NpgsqlConnection("Host=<host>;Username=<username>;Password=<password>;Database=<database>");
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand();
        cmd.Connection = conn;
        cmd.CommandText = @"
                INSERT INTO rewards (era_hash, era_id, delegator_pub_key, validator_pub_key, amount)
                VALUES (@eraHash, @eraId, @dpk, @vpk, @amount)";
        cmd.Parameters.AddWithValue("eraHash", NpgsqlTypes.NpgsqlDbType.Varchar, 64);
        cmd.Parameters.AddWithValue("eraId", NpgsqlTypes.NpgsqlDbType.Bigint);
        cmd.Parameters.AddWithValue("dpk", NpgsqlTypes.NpgsqlDbType.Varchar, 64);
        cmd.Parameters.AddWithValue("vpk", NpgsqlTypes.NpgsqlDbType.Varchar, 64);
        cmd.Parameters.AddWithValue("amount", NpgsqlTypes.NpgsqlDbType.Varchar, 32);
        foreach (var row in rowsToInsert)
        {
            cmd.Parameters[0].Value = row[0];
            cmd.Parameters[1].Value = row[1];
            cmd.Parameters[2].Value = row[2];
            cmd.Parameters[3].Value = row[3];
            cmd.Parameters[4].Value = row[4];
            await cmd.ExecuteNonQueryAsync();
        }
        */

        // using var connn = new NpgsqlConnection("<connection_string>");
                
        sqlCon.Open();
        using var tx = sqlCon.BeginTransaction();
        try
        {
            var database = new PostgresCasperNodeService();
          //  database.InsertRewards(rowsToInsert);
            tx.Commit();
        }
        catch (NpgsqlException ex)
        {
            tx.Rollback();
            if (ex.Message.Contains("unique constraint"))
            {
                using var cmd = new NpgsqlCommand();
                cmd.Connection.Open();
                cmd.CommandText = "DELETE FROM rewards WHERE block = " + new { Block = rowsToInsert[0][0] };
                var exec = cmd.ExecuteNonQuery();
                cmd.Connection.Close();
            }
            else
            {
                throw;
            }
        }
    }

    public async Task HandleAuctionTask(int blockHeigh)
    {
        string sqlDataSource = _configuration.GetConnectionString("psqlServer");
        NpgsqlConnection sqlCon = new NpgsqlConnection(sqlDataSource);

        string rpcUrl = _configuration.GetConnectionString("rpcUrl");
        var casperSdk = new NetCasperClient(rpcUrl);

        var auctionTasks = await casperSdk.GetAuctionInfo(blockHeigh);

        var bids = auctionTasks.Parse().AuctionState.Bids;

        foreach(var bid in bids)
        {
            //bid.
        }

    }
}