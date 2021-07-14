using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace marketing_functions
{
    public static class MarketingDetailsHttpTrigger
    {
        [FunctionName("MarketingDetailsHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            string htmlResponse = string.Empty; 

            string campaignId = req.Query["campaignId"];
            string userId = req.Query["userId"];

            var cnnString = Environment.GetEnvironmentVariable("SqlConnection");

            using (var conn = new SqlConnection(cnnString))
            {
                await conn.OpenAsync();
                var sql = string.Format("SELECT * FROM [dbo].[Campaign] WHERE Id = {0};", campaignId);

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    var rows = await cmd.ExecuteNonQueryAsync();
                    var campaign = (Campaign)cmd.ExecuteReader()[0];
                    htmlResponse = BuildHtmlResponse(campaign);
                }
                // var campaign = (await conn.QueryAsync<Campaign>(sql, new { CampaignId = campaignId })).FirstOrDefault();
            }

            return new OkObjectResult(htmlResponse);
        }
    
        private static string BuildHtmlResponse(Campaign campaign)
        {
            var marketingStorageUri = Environment.GetEnvironmentVariable("MarketingStorageUri");

            return string.Format(@"
            <html>
            <head>
                <link href='https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-alpha.6/css/bootstrap.min.css' rel='stylesheet'>
            </head>
            <header>
                <title>Campaign Details</title>
            </header>
            <body>
                <div class='container'>
                </br>
                <div class='card-deck'>
                    <div class='card text-center'>
                    <img class='card-img-top' src='{0}' alt='Card image cap'>
                    <div class='card-block'>
                        <h4 class='card-title'>{1}</h4>
                        <p class='card-text'>{2}</p>
                        <div class='card-footer'>
                        <small class='text-muted'>From {3} until {4}</small>
                        </div>
                    </div>
                    </div>
                </div>
                </div>
            </body>
            </html>",
            $"{marketingStorageUri}{campaign.PictureName}",
            campaign.Name,
            campaign.Description,
            campaign.From.ToString("MMMM dd, yyyy"),
            campaign.From.ToString("MMMM dd, yyyy"));
        }
 
        public class Campaign
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public DateTime From { get; set; }

            public DateTime To { get; set; }

            public string PictureUri { get; set; }

            public string PictureName { get; set; }
        }
    }

}
