using System;
using System.Text;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;
using GrovePi;
using GrovePi.Sensors;
using Newtonsoft.Json;
using GrovePi.I2CDevices;

namespace Kramerica.IoT.RaspberryBackground
{
    /// <summary>
    /// Raspberry Pi, Windows 10 IoT, Azure IoT Hubs demo
    /// Magnus Ohlsson
    /// </summary>
    public sealed class StartupTask : IBackgroundTask
    {
        //This is NOT how the connectionstring with symmetric keys would be handled in a real world scenario
        const string DEVICEC_CONNECTION_STRING = "HostName=iotdemo03.azure-devices.net;DeviceId=raspwin01;SharedAccessKey=<MyDeviceKey>";
        const string DEVICEID = "raspwin01";
        const double LATITUDE = 56.06;
        const double LONGITUDE = 14.21;
        const int EVENT_INTERVAL = 1000;

        DeviceClient client;
        IRgbLcdDisplay display;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            display = DeviceFactory.Build.RgbLcdDisplay();
            client = DeviceClient.CreateFromConnectionString(DEVICEC_CONNECTION_STRING, TransportType.Amqp);

            SetDisplayToOK();

            //Setup C2D message receiver
            Task.Run(async () =>
            {
                await SetupCloudToDeviceMessageReceiver();
            });

            //setup D2C message sender
            SetupDeviceToCloudSensorDataSender();

            deferral.Complete();
        }

        private async void SetupDeviceToCloudSensorDataSender()
        {
            //Setup D2C sending sensordata
            var vibroSensor = DeviceFactory.Build.RotaryAngleSensor(Pin.AnalogPin0);
            var tempHumSensor = DeviceFactory.Build.DHTTemperatureAndHumiditySensor(Pin.DigitalPin4, DHTModel.Dht11);
            var resetButton = DeviceFactory.Build.ButtonSensor(Pin.DigitalPin8);

            while (true)
            {
                if (resetButton.CurrentState == SensorStatus.On)
                {
                    SetDisplayToOK();
                }

                var messageToSendJson = await GetSensorData(vibroSensor, tempHumSensor);
                var message = new Message(Encoding.ASCII.GetBytes(messageToSendJson));
                client.SendEventAsync(message).AsTask().Wait();

                Task.Delay(EVENT_INTERVAL).Wait();
            }
        }

        private void SetDisplayToOK()
        {
            display.SetBacklightRgb(0, 255, 255);
            display.SetText("STATUS:\nOK");
        }

        private async Task SetupCloudToDeviceMessageReceiver()
        {

            while (true)
            {
                Message receivedMessage = await client.ReceiveAsync();
                if (receivedMessage == null) continue;
                HandleReceivedMessage(receivedMessage);
                await client.CompleteAsync(receivedMessage);
            }
        }

        private void HandleReceivedMessage(Message receivedMessage)
        {
            try
            {
                dynamic message = Newtonsoft.Json.Linq.JObject.Parse(Encoding.UTF8.GetString(receivedMessage.GetBytes()));

                if ((bool)message.alert)
                {
                    display.SetBacklightRgb(250, 0, 0);
                    display.SetText($"STATUS: ALERT!\n{(string)message.display}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unknown message received! Exception {ex.Message}");
            }
        }

        private async Task<string> GetSensorData(IRotaryAngleSensor vibroSensor, IDHTTemperatureAndHumiditySensor temperatureHumiditySensor)
        {
            temperatureHumiditySensor.Measure();

            var eventDataJson = JsonConvert.SerializeObject(new
            {
                deviceId = DEVICEID,
                temperature = temperatureHumiditySensor.TemperatureInCelsius,
                humidity = temperatureHumiditySensor.Humidity,
                vibrationLevel = vibroSensor.SensorValue(),
                latitude = LATITUDE,
                longitude = LONGITUDE
            });

            Debug.WriteLine($"EventData {eventDataJson}");
            return eventDataJson;

        }
    }
}
