using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FairyRgbController.Models;

namespace FairyRgbController.Services
{
    public interface IFairyLedService
    {
        event EventHandler<string>? StatusChanged;
        event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;
        event EventHandler<BleDeviceInfo>? AutoConnected;

        string? ConnectedDeviceName { get; }

        Task<bool> IsConnectedAsync();
        Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 30000);
        Task ConnectAsync(BleDeviceInfo device);
        Task DisconnectAsync();
        Task SetHsvAsync(int hue, int saturation, int value);
        Task SetPowerAsync(bool on);
        Task SetPresetAsync(byte presetId, int brightness);
        Task TurnOffAsync();
    }
}