using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NodeCasperParser
{
    public class ParserConfig
    {
        public static string getToken(string type)
        {
            var builder = new ConfigurationBuilder()
                        .AddJsonFile($"appsettings.json", true, false);// true, true
            // .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: false);
            var config = builder.Build();
            string Token = config[$"ConnectionStrings:{type}"].ToString();
            return Token;
        }

        public Tuple<int, int> ExtractQueryCostAndTime(string costAndExecutionTime)
        {
            int roundedCost = 0;
            int roundedTime = 0;
            // Extracting "cost=xx.xx..xx.xx" from the input string
            var costMatch = Regex.Match(costAndExecutionTime, @"cost=(\d+\.\d+)(?:\.\.\d+\.\d+)?");
            //string costValue = costMatch.Groups[1].Value;

            if (costMatch.Groups[1].Value.Contains("."))
            {
                decimal costDouble = Convert.ToDecimal(costMatch.Groups[1].Value.Replace(".", ",")); // Convert to double
                roundedCost = (int)Math.Round(costDouble); // Round to integer
            }
            else
            {
                decimal costDouble = Convert.ToDecimal(costMatch.Groups[1].Value); // Convert to double
                roundedCost = (int)Math.Round(costDouble); // Round to integer
            }

            // Extracting "actual time=xx.xxx..xx.xxx" from the input string
            var timeMatch = Regex.Match(costAndExecutionTime, @"actual time=(\d+\.\d+)(?:\.\.\d+\.\d+)?");
            //string timeValue = timeMatch.Groups[1].Value;

            if (timeMatch.Groups[1].Value.Contains("."))
            {
                decimal timeDouble = Convert.ToDecimal(timeMatch.Groups[1].Value.Replace(".", ",")); // Convert to double
                roundedTime = (int)Math.Round(timeDouble); // Round to integer
            }
            else
            {
                decimal timeDouble = Convert.ToDecimal(timeMatch.Groups[1].Value); // Convert to double
                roundedTime = (int)Math.Round(timeDouble); // Round to integer
            }



            return new Tuple<int, int>(roundedCost, roundedTime);
        }
    }

}
