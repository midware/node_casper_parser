using Casper.Network.SDK.SSE;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
/*
public class ServerEventsWorker : IHostedService
{
    private readonly IServerSentEventsService client;
    IHostApplicationLifetime applicationLifetime;
    private readonly CancellationToken _cancellationToken;

    public ServerEventsWorker(IServerSentEventsService client)
    {
        this.client = client;
    }

      [HttpGet]
        [Route("ReadWebsite")]
        public async Task</*JsonResult*/

/*
string> ReadWebsite()
        {
    String text;

    WebClient web = new WebClient();
    System.IO.Stream stream = web.OpenRead("http://cointelegraph.com");
    using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
    {
        text = reader.ReadToEnd();
    }

    var result = text;// JsonConvert.DeserializeObject(sw.ToString());


    return result.ToString();// new JsonResult(result);
}

[HttpGet]
[Route("sseblocks2")]
public async Task<string> sseblocks2()
{
    StringWriter sw = new StringWriter();
    JsonTextWriter writer = new JsonTextWriter(sw);
    writer.Formatting = Formatting.Indented;

    int nBlocks = 0;

    var sse = new ServerEventsClient("195.201.86.100", 9999);

    sse.AddEventCallback(EventType.BlockAdded, "catch-blocks-cb",
        (SSEvent evt) =>
        {
            try
            {
                if (evt.EventType == EventType.BlockAdded)
                {
                    var block = evt.Parse<BlockAdded>();
                    Console.WriteLine("--------------------------------");
                    Console.WriteLine("Timestamp: " + block.Block.Header.Timestamp);
                    Console.WriteLine("ERA ID: " + block.Block.Header.EraId);
                    //   Console.WriteLine("Era End: " + block.Block.Header.EraEnd);
                    Console.WriteLine("Block Height: " + block.Block.Header.Height);
                    Console.WriteLine("Block Hash: " + block.BlockHash);
                    Console.WriteLine("Parent Hash: " + block.Block.Header.ParentHash);
                    Console.WriteLine("State Root Hash: " + block.Block.Header.StateRootHash);
                    Console.WriteLine("AccumulatedSeed: " + block.Block.Header.AccumulatedSeed);
                    // Console.WriteLine("Validator: " + block.Block.Body.Proposer);

                    writer.WriteStartObject();

                    writer.WritePropertyName("era_id");
                    writer.WriteValue(block.Block.Header.EraId);

                    //   writer.WritePropertyName("era_end");
                    //   writer.WriteValue(block.Block.Header.EraEnd);

                    writer.WritePropertyName("block_height");
                    writer.WriteValue(block.Block.Header.Height);

                    writer.WritePropertyName("block_hash");
                    writer.WriteValue(block.BlockHash);

                    writer.WritePropertyName("state_root_hash");
                    writer.WriteValue(block.Block.Header.StateRootHash);

                    writer.WritePropertyName("accumulated_seed");
                    writer.WriteValue(block.Block.Header.AccumulatedSeed);

                    //    writer.WritePropertyName("validator");
                    //    writer.WriteValue(block.Block.Body.Proposer);

                    writer.WritePropertyName("timestamp");
                    writer.WriteValue(block.Block.Header.Timestamp);

                    writer.WriteEndObject();

                    writer.WriteRaw(",");
                    //     writer.WriteEndArray();
                    //      writer.WriteRaw("}");

                    nBlocks++;
                }
                else
                {
                    Console.WriteLine(string.Format(($"No event different from BlockAdded expected. Received: '{evt.EventType}'")));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        },
        startFrom: 0);

    sse.StartListening();

    int retries = 0;
    while (nBlocks < 3 && retries++ < 5)
        Thread.Sleep(5000);

    Console.WriteLine(sw.ToString());

    sse.StopListening().Wait();

    //  IsTrue(nBlocks >= 3);

    var result = sw.ToString();// JsonConvert.DeserializeObject(sw.ToString());


    return result.ToString();// new JsonResult(result);
}

[HttpGet]
    [Route("sseblocks")]
    public async Task<string> sseblocks()
    {
        CancellationToken cancellationToken;
        cancellationToken = applicationLifetime.ApplicationStopping;

        //    _cancellationToken = applicationLifetime.ApplicationStopping;
        // Task my_result = StartAsync(cancellationToken);

        int value = RandomNumberGenerator.GetInt32(1, 100);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            // while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("SSE Loop ");
                var clients = client.GetClients();
                if (clients.Any())
                {

                    await client.SendEventAsync(
                        new ServerSentEvent
                        {
                            Id = "number",
                            Type = "number",
                            Data = new List<string>
                            {
                                value.ToString()
                            }
                        },
                        cancellationToken
                    );
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }

        return value.ToString();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Consume Scoped Service Hosted Service running.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            // while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("SSE Loop ");
                var clients = client.GetClients();
                if (clients.Any())
                {
                    int value = RandomNumberGenerator.GetInt32(1, 100);
                    await client.SendEventAsync(
                        new ServerSentEvent
                        {
                            Id = "number",
                            Type = "number",
                            Data = new List<string>
                            {
                                value.ToString()
                            }
                        },
                        cancellationToken
                    );
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Consume Scoped Service Hosted Service running.");

        await DoWork(cancellationToken);
    }

    public async Task DoWork(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var clients = client.GetClients();
                if (clients.Any())
                {
                    //  Number.Value = RandomNumberGenerator.GetInt32(1, 100);
                    await client.SendEventAsync(
                        new ServerSentEvent
                        {
                            Id = "number",
                            Type = "number",
                            Data = new List<string>
                            {
                             //   await sseblocks()
                             //   Number.Value.ToString()
                            }
                        },
                        cancellationToken
                    );
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

        public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    
}*/