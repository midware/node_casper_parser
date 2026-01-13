using Microsoft.AspNetCore.Mvc;
using System;
using NodeCasperParser;

namespace NodeCasperParser
{
    [ApiController]
    [Route("api/licenses")]
    public class LicensesController : ControllerBase
    {
        private readonly DatabaseHelper _databaseHelper;

        public LicensesController(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        [HttpGet("verify/{license}")]
        public IActionResult VerifyLicense(string license)
        {
            var (expirationDate, compoundUnits, licenseKey) = _databaseHelper.GetLicenseInfo(license);

            if (!expirationDate.HasValue || !compoundUnits.HasValue)
            {
                return NotFound(new { Message = "Invalid License" });
            }

            if (DateTime.Now > expirationDate.Value)
            {
                return BadRequest(new { Message = "License has expired" });
            }

          //  var (expirationDate, compoundUnits, licenseKey) = _databaseHelper.GetLicenseInfo(license);
            return Ok(new { ExpirationDate = expirationDate.Value, CompoundUnits = compoundUnits.Value, LicenseKey  = licenseKey.ToString() });
        }

        // Endpoint for generating a license key would require further logic.
        // For simplicity, I'm omitting that here.
    }
}
