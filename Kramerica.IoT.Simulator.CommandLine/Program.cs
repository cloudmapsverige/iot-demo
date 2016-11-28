using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kramerica.IoT.Simulator.CommandLine
{
    /// <summary>
    /// Device Simulator for IoTHub
    /// Azure IoT Demo - Magnus Ohlsson
    /// In real world the connection strings with symetric keys would be stored securely or
    /// use asymetric keys stored in TPM for authentication
    /// </summary>
    class Program
    {
        const int SLEEP_INTERVAL = 1000;
        const TransportType CLIENT_TRANSPORTTYPE = TransportType.Amqp;

        static void Main()
        {
            var clients = new List<dynamic> {
                new { deviceName = "csharpsim01", deviceConnectionString = "HostName=iotdemo03.azure-devices.net;DeviceId=csharpsim01;SharedAccessKey=<MyDevice1Key>", lat=56.09, lon=13.11},
                new { deviceName = "csharpsim02", deviceConnectionString = "HostName=iotdemo03.azure-devices.net;DeviceId=csharpsim02;SharedAccessKey=<MyDevice2Key>", lat=55.75, lon=13.51},
                new { deviceName = "csharpsim03", deviceConnectionString = "HostName=iotdemo03.azure-devices.net;DeviceId=csharpsim03;SharedAccessKey=<MyDevice3Key>", lat=55.53, lon=13.27},
                new { deviceName = "csharpsim04", deviceConnectionString = "HostName=iotdemo03.azure-devices.net;DeviceId=csharpsim04;SharedAccessKey=<MyDevice4Key>", lat=55.51, lon=14.21},
            };

            Parallel.ForEach(clients, client =>
            {
                SendSimulatedMessageAsync(client.deviceName, client.deviceConnectionString, client.lon, client.lat);
            });

            Console.WriteLine("Press [ANY] key to stop");
            Console.ReadKey();
        }

        public static async Task SendSimulatedMessageAsync(string deviceId, string deviceConnectionString, double longitude, double latitude)
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString,CLIENT_TRANSPORTTYPE);

            var telemetrySimulatorVibration = new TelemetrySimulator(300, 400, 0.07);
            var telemetrySimulatorTemperature = new TelemetrySimulator(22, 26, 0.02);
            var telemetrySimulatorHumidity = new TelemetrySimulator(50, 80, 0.03);

            while (true)
            {
                var fakeTelemetryData = new
                {
                    deviceId = deviceId,
                    temperature = telemetrySimulatorTemperature.GetNextSimulatedValue(),
                    humidity = telemetrySimulatorHumidity.GetNextSimulatedValue(),
                    vibrationLevel = telemetrySimulatorVibration.GetNextSimulatedValue(),
                    latitude = latitude,
                    longitude = longitude
                };

                var fakeTelemetryDataJson = JsonConvert.SerializeObject(fakeTelemetryData);

                await deviceClient.SendEventAsync(
                    new Message(UTF8Encoding.ASCII.GetBytes(fakeTelemetryDataJson)));

                Console.WriteLine($"Sent {fakeTelemetryDataJson}");

                System.Threading.Thread.Sleep(SLEEP_INTERVAL);
            }

        }

    }
}
