using Microsoft.AspNetCore.Mvc;
using System;
using NodeCasperParser;
using NodeCasperParser.Services;

namespace NodeCasperParser
{
    [ApiController]
    [Route("/database")]
    public class ApiConfigurationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        bool debugMode = false;

        public ApiConfigurationController(IConfiguration configuration)
        {
            _configuration = configuration;
            debugMode = Convert.ToBoolean(_configuration.GetConnectionString("debugMode"));

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_contracts/{startFromBlockHeigh}")]
        public async Task<IActionResult> GetMissingContractPackageFromBlockHeightToActualBlock(int startFromBlockHeigh) // blok testowy  1950912
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if (licenseKey == "19820731Asia")
                {
                    try
                    {
                        var job = postgresCasperNodeServices.GetMissingContractPackageFromBlockHeightToActualBlock(startFromBlockHeigh);
                        return Ok(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingContractPackageFromBlockHeightToActualBlock Error: " + ex.ToString());
                        return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                    return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
                return BadRequest("LicenseKey header is missing.");
            }
        }

       // [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_deploy/{deploy_hash}")]
        public async Task GetMissingDeploy(string deploy_hash) // 2390553  2,390,553 blok testowy  1950912
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if (licenseKey == "19820731Asia")
                {
                    try
                    {
                        var job = postgresCasperNodeServices.GetMissingDeploy(deploy_hash);
                    //    return Ok(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingDeploy Error: " + ex.ToString());
                    //    return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                  //  return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
              //  return BadRequest("LicenseKey header is missing.");
            }
        }

      //  [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_deploys/{start_from_block_heigh_to_actual_block}")]
        public async Task GetMissingDeploysFromBlockToActualBlock(int start_from_block_heigh_to_actual_block) // 2390553  2,390,553 blok testowy  1950912
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if (licenseKey == "19820731Asia")
                {
                    try
                    {
                        var job = postgresCasperNodeServices.GetMissingDeploysFromBlockToActualBlock(start_from_block_heigh_to_actual_block);
                        
                        //await Task.CompletedTask;

                       
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingDeploysFromBlockToActualBlock Error: " + ex.ToString());
                      //  return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                   // return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
              //  return BadRequest("LicenseKey header is missing.");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_nfts/{start_from_block_heigh_to_actual_block}")]
        public async Task<IActionResult> GetMissingNftsFromBlockToActualBlock(int start_from_block_heigh_to_actual_block) //  1000047 blok testowy  1950912
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if (licenseKey == "19820731Asia")
                {
                    try
                    {
                        var job = postgresCasperNodeServices.GetMissingNftsFromBlockToActualBlock(start_from_block_heigh_to_actual_block);
                        return Ok(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingNftsFromBlockToActualBlock Error: " + ex.ToString());
                        return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                    return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
                return BadRequest("LicenseKey header is missing.");
            }
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_blocks_from_lowest_existing_block")]
        public async Task<IActionResult> GetMissingBlocksFromLowestExistingBlock()
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if (licenseKey == "19820731Asia")
                {
                    try
                    {
                        var job = postgresCasperNodeServices.GetMissingBlocksFromLowestExistingBlock();
                        return Ok(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingBlocksFromLowestExistingBlock Error: " + ex.ToString());
                        return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                    return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
                return BadRequest("LicenseKey header is missing.");
            }
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_blocks/{start_from_block_heigh_to_actual_block}")]
        public async Task GetMissingBlocksFromBlock(int start_from_block_heigh_to_actual_block)
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if (licenseKey == "19820731Asia")
                {
                    try
                    {
                        postgresCasperNodeServices.GetMissingBlocksFromBlock(start_from_block_heigh_to_actual_block);
                      //  return Ok();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingBlocksFromBlock Error: " + ex.ToString());
                     //   return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                  //  return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
             //   return BadRequest("LicenseKey header is missing.");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("missing_state_root_hash_from_lowest_existing_block")]
        public async Task<IActionResult> GetMissingStateRootHashFromLowestExistingBlock()
        {
            PostgresCasperNodeService postgresCasperNodeServices = new PostgresCasperNodeService();

            // Retrieve the LicenseKey from request headers
            if (HttpContext.Request.Headers.TryGetValue("LicenseKey", out var licenseKey))
            {
                if(licenseKey == "19820731Asia")
                {
                    try
                    {
                        // Use the licenseKey as needed, e.g., pass it to your service method
                        //  var job = postgresCasperNodeServices.GetMissingStateRootHashFromLowestExistingBlock();
                        var job = postgresCasperNodeServices.GetMissingStateRootHashFromLowestExistingBlock();

                        // Assuming the method returns some result you want to send back
                        return Ok(job);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GetMissingStateRootHashFromLowestExistingBlock Error: " + ex.ToString());
                        return StatusCode(500, "Internal server error");
                    }
                }
                else
                {
                    return BadRequest("You're not a admin. LicenseKey header wrong.");
                }
            }
            else
            {
                return BadRequest("LicenseKey header is missing.");
            }
        }
    }
}
