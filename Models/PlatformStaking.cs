using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Web;

using Microsoft.EntityFrameworkCore;

namespace NodeCasperParser.Models
{
    public class PlatformStaking
    {
        /// <summary>
        /// Validator Info
        /// </summary>
        [JsonProperty("node_public_key")]
        public string NodePublicKey { get; set; }

        [JsonProperty("node_active")]
        public bool NodeActive { get; set; }

        [JsonProperty("node_uptime")]
        public string NodeUptime { get; set; }

     //   [JsonProperty("era_id")]
     //   public int EraId { get; set; }

     //   [JsonProperty("height")]
     //   public int BlockHeight { get; set; }

        [JsonProperty("build_version")]
        public string NodeBuildVersion { get; set; }
        
        [JsonProperty("node_active_percentage")]
        public int NodeActivePercatage { get; set; }

        [JsonProperty("apy")]
        public double NodeApy { get; set; }

        [JsonProperty("staked_amount")]
        public decimal DelegatorStakedAmount { get; set; }

        [JsonProperty("total_stake")]
        public string TotalStake { get; set; }

        [JsonProperty("total_delegators")]
        public int TotalDelegators { get; set; }

        [JsonProperty("validator_rewards")]
        public decimal ValidatorRewards { get; set; }

        [JsonProperty("self_stake")]
        public decimal SelfStake { get; set; }

        /// <summary>
        /// User Staking Info
        /// </summary>
        /// 
        [JsonProperty("delegator_public_key")]
        public string DelegatorPublicKey { get; set; }

        [JsonProperty("delegator_already_staked")]
        public string DelegatorAlreadyStaked { get; set; }
    }
}
