// Reused and modified code from the workshops for my Azure functions
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Speed.Function
{
    //Class to hold my speed data
    public class SpeedItem
    {
        // Specifies the ID property of the Speed item
        [JsonProperty("id")]
        public string Id {get; set;}
        // Specifies the distance property of the Speed item
        public double Distance {get; set;}
        // Specifies the time property of the Speed item
        public double Time {get; set;}
        // Specifies the speed property of the Speed item
        public double Speed {get; set;}
    }

    public class SpeedIoTHubTrigger
    {
        private static HttpClient client = new HttpClient();

        //IoT Event hub trigger function that connects to the IoT hub and Cosmos DB database, deserializes JSON data gotten from the sensors, creates a new instance of SpeedItem and passes the data to the CosmosDB Speed container
        [FunctionName("SpeedIoTHubTrigger")]
        public void Run([IoTHubTrigger("messages/events", Connection = "AzureEventHubConnectionString")]EventData message,
        [CosmosDB(databaseName: "GeolocationData",
                                 collectionName: "Speed",
                                 ConnectionStringSetting = "cosmosDBConnectionString")] out SpeedItem output,
        ILogger log)
        {
            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");

            //Deserialize the JSON data using he Newtonsoft.JSON library
            var jsonBody = Encoding.UTF8.GetString(message.Body);
            dynamic data = JsonConvert.DeserializeObject(jsonBody);
            double distance = data.distance;
            double time = data.time;
            double speed = data.speed;

            output = new SpeedItem
            {
                Distance = distance,
                Time = time,
                Speed = speed
            };
        }
        // HTTP Trigger function that queries a Cosmos DB database to retrieve the items from the Speed container
        [FunctionName("GetSpeed")]
        public static IActionResult GetSpeed(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "speed/")] HttpRequest req,
            [CosmosDB(databaseName: "GeolocationData",
                    collectionName: "Speed",
                    ConnectionStringSetting = "cosmosDBConnectionString",
                    //Return all the items in the Cosmos Db container for distance, time and speed
                        SqlQuery = "SELECT * FROM c")] IEnumerable<SpeedItem> SpeedItem,
                    ILogger log)
        {            
            // Return an OkObjectResult which contains the SpeedItem object
            return new OkObjectResult(SpeedItem);
        }
    }
}