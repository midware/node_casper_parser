using Casper.Network.SDK.Clients;
//using MystrAPI.Services;

namespace NodeCasperParser
{
    public class CasperClient
    {
        public readonly string url;
        /// <summary>
        /// The Casper client is the main class, in which you can interact with Casper Network. Through the client you can use the SigningService, RpcService, HashService, DeployService and SseService
        /// </summary>
        /// <param name="rpcUrl">The RPC URL of a Casper Network Node</param>
        public CasperClient(string rpcUrl)
        {
            url = rpcUrl;
            SigningService = new Cryptography.CasperNetwork.SigningService();
       //     RpcService = new Platform
       //     .Shared.Cryptography.CasperNetwork.(rpcUrl);
            HashService = new Cryptography.CasperNetwork.HashService();
       //     SseService = new PlatformApi.Shared.Cryptography.CasperNetwork.SseService();
            //     DeployService = new PlatformApi.Shared.Cryptography.CasperNetwork.DeployService(RpcService, HashService, SigningService);
        }
        /// <summary>
        /// Signing Service
        /// </summary>
        public Cryptography.CasperNetwork.SigningService SigningService { get; }

        /// <summary>
        /// The RPC service uses Remote Procedure Calls (RPC) in Casper Network nodes. RPC enables the integartion with Capser Network.
        /// </summary>
   //     public PlatformApi.Shared.Cryptography.CasperNetwork.RpcService RpcService { get; }

        /// <summary>
        /// Hash Service
        /// </summary>
        public Cryptography.CasperNetwork.HashService HashService { get; }

        /// <summary>
        /// Deploy Service
        /// </summary>
     //   public PlatformApi.Shared.Cryptography.CasperNetwork.DeployService DeployService  { get; }

        /// <summary>
        /// SSE Service
        /// </summary>
   //     public PlatformApi.Shared.Cryptography.CasperNetwork.SseService SseService { get; set; }

    }
}
