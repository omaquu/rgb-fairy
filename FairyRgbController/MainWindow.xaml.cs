using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FairyRgbController
{
    public partial class MainWindow : Window
    {
        private DeviceWatcher deviceWatcher;
        private List<DeviceInformation> foundDevices;
        private DeviceInformation selectedDevice;
        private GattCharacteristic selectedCharacteristic;

        // TODO: Replace with actual Hello Fairy service/characteristic UUIDs
        private static readonly Guid ServiceUuid = new Guid("0000ffe0-0000-1000-8000-00805f9b34fb");
        private static readonly Guid CharacteristicUuid = new Guid("0000ffe1-0000-1000-8000-00805f9b34fb");

        public MainWindow()
        {
            InitializeComponent();
            foundDevices = new List<DeviceInformation>();
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceWatcher != null)
            {
                deviceWatcher.Stop();
                deviceWatcher = null;
            }

            foundDevices.Clear();
            DevicesListBox.ItemsSource = null;

            string selector = BluetoothLEDevice.GetDeviceSelector();
            deviceWatcher = DeviceInformation.CreateWatcher(selector);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            deviceWatcher.Start();
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            foundDevices.Add(deviceInfo);
            await Dispatcher.InvokeAsync(() =>
            {
                DevicesListBox.ItemsSource = null;
                DevicesListBox.ItemsSource = foundDevices;
            });
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args) { }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args) { }

        private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDevice = DevicesListBox.SelectedItem as DeviceInformation;
            ConnectButton.IsEnabled = selectedDevice != null;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDevice == null) return;

            BluetoothLEDevice bluetoothDevice = await BluetoothLEDevice.FromIdAsync(selectedDevice.Id);
            if (bluetoothDevice == null)
            {
                MessageBox.Show("Failed to connect to device.");
                return;
            }

            var servicesResult = await bluetoothDevice.GetGattServicesAsync(ServiceUuid, BluetoothCacheMode.Uncached);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                MessageBox.Show("Service not found. Ensure the device is advertising the correct service.");
                return;
            }

            var service = servicesResult.Services[0];
            var characteristicsResult = await service.GetCharacteristicsAsync();
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
            {
                MessageBox.Show("Failed to get characteristics.");
                return;
            }

            selectedCharacteristic = characteristicsResult.Characteristics.FirstOrDefault(c => c.Uuid == CharacteristicUuid);
            if (selectedCharacteristic == null)
            {
                MessageBox.Show("Characteristic not found. Check UUIDs.");
                return;
            }

            SetColorButton.IsEnabled = true;
            SetEffectButton.IsEnabled = true;
            ConnectButton.IsEnabled = false;
        }

        private async void SetColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCharacteristic == null) return;
            // Example: send red (255,0,0) – adjust according to actual protocol
            byte[] colorPacket = new byte[] { 0xAA, 0x55, 0x01, 0x00, 0xFF, 0x00, 0x00 };
            await SendDataAsync(colorPacket);
        }

        private async void SetEffectButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCharacteristic == null) return;
            // Example effect packet
            byte[] effectPacket = new byte[] { 0xAA, 0x55, 0x02, 0x01 };
            await SendDataAsync(effectPacket);
        }

        private async Task SendDataAsync(byte[] data)
        {
            if (selectedCharacteristic == null) return;
            var writer = new DataWriter();
            writer.WriteBytes(data);
            IBuffer buffer = writer.DetachBuffer();
            var status = await selectedCharacteristic.WriteAsync(buffer, GattWriteOption.WriteWithResponse);
            if (status != GattCommunicationStatus.Success)
            {
                MessageBox.Show("Failed to send data to device.");
            }
        }
    }
}