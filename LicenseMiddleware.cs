

using Microsoft.AspNetCore.Http;
using Npgsql;
using System.Threading.Tasks;
//using IDatabaseHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;//.Cng;
using System.Text;

namespace NodeCasperParser
{  
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            DatabaseHelper dh = new DatabaseHelper();

            if (!context.Request.Headers.TryGetValue("LicenseKey", out var licenseKeyHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("License key missing");
                return;
            }

            var (expirationDate, _, _) = dh.GetLicenseInfo(licenseKeyHeader);//_databaseHelper.GetLicenseInfo(licenseKeyHeader);

            if (!expirationDate.HasValue || DateTime.Now > expirationDate.Value)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid or expired license key");
                return;
            }

            await _next(context);
        }
        /*
        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey(LicenseHeader))
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("License key missing.");
                return;
            }

            var licenseKey = context.Request.Headers[LicenseHeader].ToString();

            using (var conn = new NpgsqlConnection("Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase"))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT expiration_date FROM licenses WHERE license_key = @key", conn))
                {
                    cmd.Parameters.AddWithValue("key", licenseKey);
                    var expirationDate = cmd.ExecuteScalar() as DateTime?;

                    if (expirationDate == null || expirationDate.Value < DateTime.UtcNow)
                    {
                        context.Response.StatusCode = 403; // Forbidden
                        await context.Response.WriteAsync("Invalid or expired license key.");
                        return;
                    }
                }
            }

            await _next(context);
        }*/
    }
}

