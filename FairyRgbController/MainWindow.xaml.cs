using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using FairyRgbController.Services;
using FairyRgbController.Models;

namespace FairyRgbController
{
    public partial class MainWindow : Window
    {
    private DeviceWatcher deviceWatcher;
        foundDevices = new List<BleDeviceInfo>();
    }ow()
        {
            InitializeComponent();
        foundDevices = new List<BleDeviceInfo>();
    }
    {
        if (deviceWatcher != null)
        {
            deviceWatcher.Stop();
        foundDevices = new List<BleDeviceInfo>();
    }
    }
        foundDevices = new List<BleDeviceInfo>();
    }
    {
        foundDevices.Add(new BleDeviceInfo { Id = deviceInfo.Id, Name = deviceInfo.Name });
        await Dispatcher.InvokeAsync(() =>
        {
            DevicesListBox.ItemsSource = null;
            DevicesListBox.ItemsSource = foundDevices;
        foundDevices = new List<BleDeviceInfo>();
    }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args) { }

    private void DevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selectedDevice = DevicesListBox.SelectedItem as BleDeviceInfo;
        foundDevices = new List<BleDeviceInfo>();
    }
    {
        if (selectedDevice == null) return;
        try
        {
            await _fairyService.ConnectAsync(new BleDeviceInfo { Id = selectedDevice.Id, Name = selectedDevice.Name });
            SetColorButton.IsEnabled = true;
            SetEffectButton.IsEnabled = true;
            ConnectButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to connect: {ex.Message}");
        foundDevices = new List<BleDeviceInfo>();
    }
        {
        if (!await _fairyService.IsConnectedAsync()) return;
        foundDevices = new List<BleDeviceInfo>();
    }
        {
        if (!await _fairyService.IsConnectedAsync()) return;
        foundDevices = new List<BleDeviceInfo>();
    }
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