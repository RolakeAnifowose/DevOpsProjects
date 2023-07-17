// Reused and modified code from the workshops for my Azure functions
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using System.Threading.Tasks;
using System.Linq;
using System;

namespace Geolocation.Function
{
    //Class to hold my geolocation sensor data
    public class LocationItem
    {
        // Specifies the ID property of the Location item
        [JsonProperty("id")]
        public string Id {get; set;}
        // Specifies the Longitude property of the Location item
        public double Longitude {get; set;}
        // Specifies the Latitude property of the Location item
        public double Latitude {get; set;}
    }

    public class GeolocationIoTHubTrigger
    {
        private static HttpClient client = new HttpClient();
        
        //IoT Event hub trigger function that connects to the IoT hub and Cosmos DB database, deserializes JSON data gotten from the sensors, creates a new instance of LocationItem and passes the data to the CosmosDB Geolocation container
        [FunctionName("GeolocationIoTHubTrigger")]

        public static void Run([IoTHubTrigger("messages/events", Connection = "AzureEventHubConnectionString")] EventData message,
        [CosmosDB(databaseName: "GeolocationData",
                                 collectionName: "Geolocation",
                                 ConnectionStringSetting = "cosmosDBConnectionString")] out LocationItem output,
                       ILogger log)
        {
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");

            //Deserialize the JSON data using he Newtonsoft.JSON library
            var jsonBody = Encoding.UTF8.GetString(message.Body);
            dynamic data = JsonConvert.DeserializeObject(jsonBody);
            double longitude = data.longitude;
            double latitude = data.latitude;

            output = new LocationItem
            {
                Longitude = longitude,
                Latitude = latitude
            };
        }

        // HTTP Trigger function that queries my Cosmos DB database to retrieve the last 200 records from the "Geolocation" container
        [FunctionName("GetLocation")]
        public static IActionResult GetLocation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "geolocation/")] HttpRequest req,
            [CosmosDB(databaseName: "GeolocationData",
                    collectionName: "Geolocation",
                    ConnectionStringSetting = "cosmosDBConnectionString",
                        // SqlQuery to select the last 200 items from my Cosmos DB container because they represent actual longitude and latitude data of places like Lincoln, Manchester, London as seen on the heat map displayed on the Azure Web App
                         SqlQuery = "SELECT TOP 200 * FROM c ORDER BY c._ts DESC")] IEnumerable<LocationItem> locationItem,
                    ILogger log)
        {
            // Return an OkObjectResult which contains the locationItem object
            return new OkObjectResult(locationItem);
        }
    }
}