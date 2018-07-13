using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace SimulatedBowl
{
    class Program
    {
        static DeviceClient deviceClient;
        static string iotHubUri = "francescoiothub.azure-devices.net";
        static string DeviceKey = "INSERTDEVICEKEY";
        static string DeviceId = "ciotola";
        static double doses;
        static double maxDoses;
        static bool isCatAlive = true;




        private static async void SendDeviceToCloudMessagesAsync()
        {

                      
            int messageId = 1;
   
    
           
            var telemetryDataPoint = new
            {
                messageId = messageId++,
                deviceId = DeviceId,
                dosesCounter = doses
            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            message.Properties.Add("needsRefill", (doses < 1) ? "true" : "false");

            await deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
        }



        static async Task Main(string[] args)
        {
            Console.WriteLine("Simulated Bowl \n");
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey), TransportType.Mqtt);
            var twin = await deviceClient.GetTwinAsync();

            maxDoses = Convert.ToDouble(twin.DesiredProperty("dosesCounter"));

            if (twin.IsReportedPropertyEmpty("dosesCounter"))
            {
                doses = maxDoses;
                Console.WriteLine($"Doses {doses}");
            }
            else
            {
                doses = Convert.ToDouble(twin.ReportedProperty("dosesCounter"));
            }


            while (true)
            {
                Console.WriteLine($"Doses {doses}");
                Console.WriteLine($"[enter command (type 'i' for instructions)]");
                string input = Console.ReadLine();
                switch (input.ToLower())
                {
                    case "cat wants to eat":
                        await CatEats(deviceClient);
                        break;
                    case "add dose":
                        await AddDose(deviceClient);
                        break;
                    case "refill":
                        await Refill(deviceClient);
                        break;
                    case "i":
                        Console.WriteLine("- cat wants to eat: a dose gets eaten");
                        Console.WriteLine("- add dose: add a dose");
                        Console.WriteLine("- refill: restore doses quantity");
                        break;
                    default:
                        Console.WriteLine("Syntax error");
                        break;
                }
            }
            



        }


        private static async Task CatEats(DeviceClient client) {
            if (doses > 0 && isCatAlive)
            {
                doses--;
                Console.WriteLine("Cat eats");
                var twinBowlColl = new TwinCollection();
                twinBowlColl["dosesCounter"] = doses;
                await client.UpdateReportedPropertiesAsync(twinBowlColl);
                SendDeviceToCloudMessagesAsync();
            }
            else
            {
                isCatAlive = false;
                Console.WriteLine("Cat died from starvation");
            }

        }

        private static async Task AddDose(DeviceClient client)
        {
            if (doses < maxDoses)
            {
                doses++;
                Console.WriteLine("I add a dose");
                var twinBowlColl = new TwinCollection();
                twinBowlColl["dosesCounter"] = doses;
                await client.UpdateReportedPropertiesAsync(twinBowlColl);
            }
            else
            {
                Console.WriteLine("DOSES ALREADY AT MAXIMUM LEVEL !!!");
            }

        }

        private static async Task Refill(DeviceClient client)
        {
            doses=maxDoses;
            Console.WriteLine("I refill the doses");
            var twinBowlColl = new TwinCollection();
            twinBowlColl["dosesCounter"] = doses;
            await client.UpdateReportedPropertiesAsync(twinBowlColl);

        }


    }
}
