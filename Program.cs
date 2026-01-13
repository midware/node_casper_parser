using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using NodeCasperParser.Helpers;
using NodeCasperParser.Services;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using FileUpload;

using System.Threading;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Types;
using Google.Protobuf.WellKnownTypes;
using System.Runtime.CompilerServices;
//using EnvisionStaking.Casper.SDK.Model.Base;
using NodeCasperParser;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Casper.Network.SDK.JsonRpc.ResultTypes;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using Casper.Network.SDK.JsonRpc;
using Microsoft.Extensions.Options;
//using NodeCasperParser.Middleware;
using Swashbuckle.AspNetCore;//.Filters;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Validations.Rules;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Hosting;
using NodeCasperParser.Controllers;
using NodeCasperParser.NftParser;
using Microsoft.Extensions.Logging;
using static NodeCasperParser.Services.CasperNodeDeployService;
using CasperParser;
using SixLabors.ImageSharp.Drawing.Processing;
using Swashbuckle.Swagger;

double blockTime = 16000;//900000; // co 30 min    //600000; // 10 min
double stakingUpdateTime = 3600000; // 60 min = 3600000
double marketplaceCollectionsUpdateTime = 600000; // 5min

var builder = WebApplication.CreateBuilder(args);
// add services to DI container
{    
    var services = builder.Services;
    var env = builder.Environment;
    
    services.AddCors();

    services.AddServerSentEvents();

 //   services.AddControllersWithViews();// zbędne ?
//    services.AddEndpointsApiExplorer(); // zbędne?


    services.AddCors(Options =>
    {

        // TUTORIAL: https://code-maze.com/enabling-cors-in-asp-net-core/
        Options.AddPolicy("AllowOrigin",
            builder => builder.WithOrigins("https://mystra.app", "http://mystra.app", "https://mystra.io", "http://mystra.io", "http://api.mystra.io", "http://api.testnet.mystra.io", "https://api.testnet.mystra.io", "https://api.mystra.io", "https://apiv1.casperarmy.org", "http://localhost:8081", "https://localhost:8081", "http://localhost:5001", "https://localhost:5001", "https://localhost:3000", "http://localhost:3000", "https://localhost:3000", "https://casperarmy.org", "https://casperarmy.io", "https://casper.army"));
    });

    services.AddRouting();

    services.AddControllers().AddNewtonsoftJson();

    /*
    services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        // Handle self-referencing loops in object graphs
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

        // Use default contract resolver which includes the default .NET naming policy etc.
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();

        // Optionally, if you want to serialize enums as strings (similar to what you did with System.Text.Json)
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    });*/

    /*
    //JSON Serializer - wersja ze starego API backend
    services.AddControllersWithViews().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
        .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver
        = new DefaultContractResolver());
    */

    
    services.AddControllers().AddJsonOptions(x => // wersja z auth web2 api
    {
        // serialize enums as strings in api responses (e.g. Role)
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
       
    // configure strongly typed settings object
    services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

    services.AddSwaggerGen(c =>
    {
        var environment = builder.Configuration.GetConnectionString("ENVIRONMENT");

        c.ExampleFilters(); // add this to support examples

        c.OperationFilter<FileUploadFilter>();
        //c.CustomSchemaIds((type) => type.FullName);

        c.SwaggerDoc("v1",
            new OpenApiInfo
            {
                Title = environment + " Casper Network SSE Node Indexer API - V1",
                Version = "v1",
                Description = "SSE Postgres Casper Node Indexer/Parser. Build in C# .NET 7",
                TermsOfService = new Uri("https://docs.mystra.io/docs/what-is-casperarmy/1.7-Terms-of-use"),

                Contact = new OpenApiContact
                {
                    Name = "Author: Kamil Szymoniak",
                    Email = "headquarters@mystra.io"
                },
                License = new OpenApiLicense
                {
                    Name = "Documentation",
                    Url = new Uri("https://docs.mystra.io/docs/api/9.1-overview")
                }
            }
        );

        // c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
        //c.OperationFilter<SecurityRequirementsOperationFilter>(true, "LicenseKey");
        /* c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
         {
             Description = "Standard Authorization header using the Bearer scheme (JWT). Example: \"bearer {token}\"",
             Name = "Authorization",
             In = ParameterLocation.Header,
             Type = SecuritySchemeType.ApiKey,
             Scheme = "Bearer"
         });*/

        /*
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                },
                new List<string>()
            }
        });*/

        c.AddSecurityDefinition("LicenseKey", new OpenApiSecurityScheme
        {
            Description = "Licence Key Authorization",
            Type = SecuritySchemeType.ApiKey,
            Name = "LicenseKey",
            In = ParameterLocation.Header,
            Scheme = "ApiKeyScheme"
        });
        var key = new OpenApiSecurityScheme()
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "LicenseKey"
            },
            In = ParameterLocation.Header
        };
        var requirement = new OpenApiSecurityRequirement
                    {
                             { key, new List<string>() }
                    };
        c.AddSecurityRequirement(requirement);

        /*

        // Zabezpeczenie Swagera kluczem licencji.
        c.AddSecurityDefinition("LicenseKey", new OpenApiSecurityScheme
        {
            Description = "Licence Key Authorization",
            In = ParameterLocation.Header,
            Name = "LicenseKey",
            Type = SecuritySchemeType.ApiKey
        });
        // Zabezpeczenie Swagera kluczem licencji.
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "LicenseKey"
                    }
                }, Array.Empty<string>()
            }
        });*/

        var filePath = Path.Combine(System.AppContext.BaseDirectory, "ApiDocumentation.xml");
        c.IncludeXmlComments(filePath);
    });

    services.AddSwaggerExamplesFromAssemblyOf<Program>(); // to automatically search all the example from assembly.

    // configure DI for application services
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IDatabaseHelper, DatabaseHelper>();
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

async Task UpdateStakingTable()
{
    NodeCasperParser.Controllers.CasperNetworkStaking stakingTable = new NodeCasperParser.Controllers.CasperNetworkStaking(builder.Configuration);

   // stakingTable.AddCasperNodeStakingToList();

    stakingTable.GetAllStakingFromCasperNode();
}

async Task UpdateCollectionsTable()
{
    try
    {
        NodeCasperParser.Controllers.CasperNetworkMarketplaceBatch collectionsTable = new NodeCasperParser.Controllers.CasperNetworkMarketplaceBatch(builder.Configuration);

        collectionsTable.UpdateMarketplaceCollectionsData();
    }
    catch
    {
        Console.WriteLine("UpdateCollectionsTable failed.");
    }
}



async Task AddMissingBlocks()
{
    CasperNodeDeployService cnds = new CasperNodeDeployService();
    PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();
    var client = new NetCasperClient(builder.Configuration.GetConnectionString("rpcUrl"));
                        
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

        Console.WriteLine("Missing blocks founded. Start adding missing blocks.");
        Console.WriteLine("---------------------------");

        foreach (var missingBlock in TagIds)
        {

            var missingBlockToAdd = client.GetBlock(missingBlock);
            int missingHeight = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.Height);
            try
            {
                var missingJson = missingBlockToAdd.Result.Result.GetRawText().ToString();
                int missingEra = Convert.ToInt32(missingBlockToAdd.Result.Parse().Block.Header.EraId);
                string missingTimestamp = missingBlockToAdd.Result.Parse().Block.Header.Timestamp.ToString();
                string missingHash = missingBlockToAdd.Result.Parse().Block.Hash.ToString();
                var missingEraEnd = missingBlockToAdd.Result.Parse().Block.Header.EraEnd != null;
                var missingBlockExecutedByValidator = missingBlockToAdd.Result.Parse().Block.Body.Proposer.PublicKey?.ToString();
                var missingBlockDeploysCount = missingBlockToAdd.Result.Parse().Block.Body.DeployHashes.Count;
                var missingTransfersDeploysCount = missingBlockToAdd.Result.Parse().Block.Body.TransferHashes.Count;
                var missingBlockStateRootHash = missingBlockToAdd.Result.Parse().Block.Header.StateRootHash.ToLower();

                var insertMissingBlock = postgresCasperNodeServices.InsertBlock(missingHash, missingBlockStateRootHash, missingEra, missingTimestamp, missingHeight, missingEraEnd, missingBlockDeploysCount, missingBlockExecutedByValidator, missingJson);

                if (missingBlockDeploysCount > 0)
                {
                    // DODAJ BRAKUJĄCE DEPLOYE DLA BRAKUJĄCYCH BLOKÓW
                    var missingDeploysInBlock = missingBlockToAdd.Result.Parse().Block.Body.DeployHashes;
                    cnds.GetDeploy(client, missingDeploysInBlock);
                }

                if (missingTransfersDeploysCount > 0)
                {
                    // dodaj transfery
                    var missingTransfersInAddedBlock = missingBlockToAdd.Result.Parse().Block.Body.TransferHashes;
                    cnds.GetDeploy(client, missingTransfersInAddedBlock);
                }

                Console.WriteLine("Added missing block height: " + missingHeight);
            }
            catch
            {
                Console.WriteLine("ERROR: Can't add missing block height: " + missingHeight);
            }
        }
    }
    await Task.CompletedTask;
}

async Task SSEListen()
{
    int nBlocks = 0;
    int nSignatures = 0;
    int nApiVersion = 0;
    int nDeployAccepted = 0;
    int nDeployProcessed = 0;

    string rpcServer = ParserConfig.getToken("rpcUrl");
    string rpcSSEServer = ParserConfig.getToken("rpcSseUrl");

    var sse = new ServerEventsClient(rpcSSEServer, 9999); //http://cspr-testnet.mystra.io:9999/rpc cspr-testnet.mystra.io

    CasperNodeService cns = new CasperNodeService();
    CasperNodeDeployService cnds = new CasperNodeDeployService();
    var client = new NetCasperClient(builder.Configuration.GetConnectionString("rpcUrl"));
    PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

    sse.AddEventCallback(EventType.All, "catch-all-cb",
        (SSEvent evt) =>
        {
            try
            {
                if (evt.EventType == EventType.FinalitySignature)
                {
                }
                else if (evt.EventType == EventType.BlockAdded)
                {
                    try
                    {
                        var block = evt.Parse<BlockAdded>();
                        
                        string hash = block.BlockHash.ToString().ToLower();

                        int era = Convert.ToInt32(block.Block.Header.EraId);
                        string timestamp = block.Block.Header.Timestamp.ToString();
                        int height = Convert.ToInt32(block.Block.Header.Height);

                        var eraEnd = block.Block.Header.EraEnd != null;
                        var blockExecutedByValidator = block.Block.Body.Proposer.PublicKey?.ToString();
                        var blockDeploysCount = block.Block.Body.DeployHashes.Count;
                        var blockTransfersCount = block.Block.Body.TransferHashes.Count;

                        var getBlock = client.GetBlock(hash);
                        var json = getBlock.Result.Result.GetRawText().ToString();

                        var blockStateRootHash = block.Block.Header.StateRootHash.ToLower();

                        try
                        {
                            var insertBlock = postgresCasperNodeServices.InsertBlock(hash, blockStateRootHash, era, timestamp, height, eraEnd, blockDeploysCount, blockExecutedByValidator, json);

                            // var insertRawBlock = postgresCasperNdeServices.InsertRawBlock(hash, json);

                            Console.WriteLine("SSE Event Added Block: " + height + " | Transfers in this block: " + blockTransfersCount + " | Deploys in this block: " + blockDeploysCount);
                        }
                        catch
                        {
                            Console.WriteLine("SSE Event can't add Block: " + height + " | Transfers in this block: " + blockTransfersCount + " | Deploys in this block: " + blockDeploysCount);
                        }

                        if (blockDeploysCount > 0)
                        {
                            // dodaj deploye
                            var deploysInAddedBlock = block.Block.Body.DeployHashes;
                            cnds.GetDeploy(client, deploysInAddedBlock);
                        }

                        if (blockTransfersCount > 0)
                        {
                            // dodaj transfery
                            var deploysInAddedBlock = block.Block.Body.TransferHashes;
                            cnds.GetDeploy(client, deploysInAddedBlock);
                        }
                    }
                    catch//(Exception ex)
                    {
                       // Console.WriteLine("ERROR SSE Listening EventType.BlockAdded: " + ex.ToString());
                    }

                    nBlocks++;
                }
                else if (evt.EventType == EventType.DeployAccepted)
                {
                    nDeployAccepted++;
                }
                else if (evt.EventType == EventType.DeployProcessed)
                {
                    nDeployProcessed++;
                }
                else if (evt.EventType == EventType.ApiVersion)
                {
                    nApiVersion++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //  throw;
            }
        },
        startFrom: 0);

    sse.StartListening();

    int retries = 0;
//    while (nDeployProcessed == 0 && retries++ < 5)
//        Thread.Sleep(5000);

    while (nBlocks == 0 && retries++ < 5)
        Thread.Sleep(5000);

    sse.StopListening().Wait();
}

async Task SSEHandleTimerAsync()
{
    try
    {
        Console.WriteLine("Date: " + DateTime.Now.ToString() + " | Casper Node Parser SSE Event listening. Block Time interval: " + blockTime + " ms.");
        await SSEListen();
    }
    catch (Exception e)
    {
        System.Console.WriteLine("ERROR: Date: " + DateTime.Now.ToString() + " | Casper Node Parser SSE Event listen failed!" + e.ToString());
    }
    await Task.CompletedTask;
}

var app = builder.Build();



System.Timers.Timer timer1 = new(interval: blockTime); //interval: 600000 = 10min | interval: 3600000 - REFRESH 1 x ON HOUR
timer1.Elapsed += async (sender, e) => await SSEHandleTimerAsync();
timer1.Start();
//await SSEListen();

System.Timers.Timer timer2 = new(interval: blockTime); //interval: 600000 = 10min | interval: 3600000 - REFRESH 1 x ON HOUR
timer2.Elapsed += async (sender, e) => await AddMissingBlocks();
timer2.Start();

System.Timers.Timer timer3 = new(interval: stakingUpdateTime); //interval: 600000 = 10min | interval: 3600000 - REFRESH 1 x ON HOUR
timer3.Elapsed += async (sender, e) => await UpdateStakingTable();
timer3.Start();


System.Timers.Timer timer4 = new(interval: marketplaceCollectionsUpdateTime); //interval: 600000 = 10min | interval: 3600000 - REFRESH 1 x ON HOUR
timer4.Elapsed += async (sender, e) => await UpdateCollectionsTable();
timer4.Start();




// configure HTTP request pipeline
{
    // generated swagger json and swagger ui middleware
    app.UseSwagger();

    app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", builder.Configuration.GetConnectionString("ENVIRONMENT") + " Casper Node SSE Event Store Parser API"));

    // global cors policy
    app.UseCors(x => x
        .SetIsOriginAllowed(origin => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    app.UseHttpsRedirection();

     //   app.UseAuthentication();

     //   app.UseAuthorization();

    //   app.UseRouting();

    // global error handler
  //  app.UseMiddleware<LicenseKeyAuthorizeAttribute>(); // LicenseKeyAuthorizeAttribute
    app.UseMiddleware<LicenseMiddleware>();
    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.UseAuthorization();

    app.MapControllers();
}

app.Run();