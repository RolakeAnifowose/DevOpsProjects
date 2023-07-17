// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

// Code obtained from workshop projects. Simulated device code edited to fit the project theme

using Microsoft.Azure.Devices.Client;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

namespace SimulatedDevice
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry. For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/DeviceReconnectionSample"/>.
    /// </summary>
    internal class Program
    {
        private static DeviceClient s_deviceClient;
        private static readonly TransportType s_transportType = TransportType.Mqtt;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        // private static string s_connectionString = "HostName=PhilsTemporaryHub.azure-devices.net;DeviceId=pcarlisle_simdevice;SharedAccessKey=6BdXKq0+6S9giBRi0gcn7M4/wPvLfMyv/BdDDtVoAag=";
		
        private static string s_connectionString = "HostName=geolocationdatahub.azure-devices.net;DeviceId=speed_simulationdevice;SharedAccessKey=GINwIzJKfucmqvIx8rVvZMbl+gF/e+/wWbSN1TLfo0k=";

        private static async Task Main(string[] args)
        {
            Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device for distance, time and speed.");

            // This sample accepts the device connection string as a parameter, if present
            ValidateConnectionString(args);

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            // Set up a condition to quit the sample
            Console.WriteLine("Press control-C to exit.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Run the telemetry loop
            await SendDeviceToCloudMessagesAsync(cts.Token);

            s_deviceClient.Dispose();
            Console.WriteLine("Speed simulator finished.");
        }

        //Validate the connection string for my IoT hub
        private static void ValidateConnectionString(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    var cs = IotHubConnectionStringBuilder.Create(args[0]);
                    s_connectionString = cs.ToString();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error: Unrecognizable parameter '{args[0]}' as connection string.");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    _ = IotHubConnectionStringBuilder.Create(s_connectionString);
                }
                catch (Exception)
                {
                    Console.WriteLine("This sample needs a device connection string to run. Program.cs can be edited to specify it, or it can be included on the command-line as the only parameter.");
                    Environment.Exit(1);
                }
            }
        }

        // Async method to send simulated telemetry data. Method that sends device-to-cloud messages using an Azure IoT device client. It generates random distance and time data, calculates and sends a JSON message containing that data to my IoT hub
        private static async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            // Initial distance, time
            double distance = 50;
            double time = 3;
            double speed = distance/time;
            var rand = new Random();

            while (!ct.IsCancellationRequested)
            {
                // Generate random distance and time data and calculate the speed
                double currentDistance = distance + rand.NextDouble() * 5;
                double currentTime = time + rand.NextDouble() * 2;
                double currentSpeed = currentDistance/currentTime;

                //Format the random telemetry data to 3 decimal places
                string formattedDistance = currentDistance.ToString("F3");
                string formattedTime = currentTime.ToString("F3");
                string formattedSpeed = currentSpeed.ToString("F3");

                // Create JSON message
                string messageBody = JsonSerializer.Serialize(
                    new
                    {
                        distance = formattedDistance,
                        time = formattedTime,
                        speed = formattedSpeed
                    });
                using var message = new Message(Encoding.ASCII.GetBytes(messageBody))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8",
                };

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                message.Properties.Add("speedAlert", (currentSpeed > 15) ? "true" : "false");

                // Send the telemetry message
                await s_deviceClient.SendEventAsync(message);
                // Print out the telemetry data to the console
                Console.WriteLine($"{DateTime.Now} > Sending Distance, Time and Calculated Speed: {messageBody}");
                // dynamic json = JsonConvert.DeserializeObject(messageBody);

                // Time interval between each message sent from the simulated device set to 5 seconds
                await Task.Delay(5000);
            }
        }
    }
}
