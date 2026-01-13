using Ipfs;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NodeCasperParser.Services;
using Npgsql;
using System;
using System.Data;

namespace NodeCasperParser
{
    public interface IDatabaseHelper
    {
        (DateTime? ExpirationDate, int? CompoundUnits, string? LicenseKey) GetLicenseInfo(string license);
    }

    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            string connectionString = ParserConfig.getToken("psqlServer");

            _connectionString = connectionString;
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
                else if (typeof(T) == typeof(decimal))
                {
                    return (decimal)row[fieldName];
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

        public (DateTime? ExpirationDate, int? CompoundUnits, string? LicenseKey) GetLicenseInfo(string license)
        {
            using var conn = new NpgsqlConnection(_connectionString);

            DateTime? expirationDate = null;
            int? compoundUnits = null;
            string? licenseKey = null;

            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT expiration_date, compound_units, license_key FROM api_licensekeys WHERE license_key = @licenseKey", conn);
                cmd.Parameters.AddWithValue("licenseKey", license);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    expirationDate = reader.GetDateTime(0);
                    compoundUnits = reader.GetInt32(1);
                    licenseKey = reader.GetString(2);
                }
                
            }
            catch
            {
                return (null, null,null);
            }
            finally
            {
                conn.Close();
            }   

            return (expirationDate, compoundUnits, licenseKey);
        }

        public async Task<bool> IsLicenseKeysExpired(string licenceKey)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            bool result = false;
            bool returned = true;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimestampNow = now.ToUnixTimeSeconds();

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync().ConfigureAwait(false);

                NpgsqlCommand command = new NpgsqlCommand();

                command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT EXISTS (
                        SELECT 1 
                        FROM api_licensekeys 
                        WHERE license_key = '" + licenceKey + "' AND ( EXTRACT(EPOCH FROM expiration_date) < " + unixTimestampNow + " OR compound_units < 1));"
                ;

                result = Convert.ToBoolean(await command.ExecuteScalarAsync());

                return returned = result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubtractCompoundUnits: An error occurred: {ex.Message}");
                return returned;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        public async Task SubtractCompoundUnits(string licenceKey, int compundUnits)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync().ConfigureAwait(false);

                NpgsqlCommand command = new NpgsqlCommand();

                command = connection.CreateCommand();
                command.CommandText = "UPDATE api_licensekeys SET compound_units = compound_units - " + compundUnits + " WHERE license_key = '" + licenceKey + "'";

                int affectedRows = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SubtractCompoundUnits: An error occurred: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }

        public async Task UpgradeCompoundUnits(string licenceKey, int compundUnits)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync().ConfigureAwait(false);

                NpgsqlCommand command = new NpgsqlCommand();

                command = connection.CreateCommand();
                command.CommandText = "UPDATE api_licensekeys SET compound_units = compound_units + " + compundUnits + " WHERE license_key = '" + licenceKey + "'";

                int affectedRows = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpgradeCompoundUnits: An error occurred: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    await connection.CloseAsync();
            }
        }
    }
}
