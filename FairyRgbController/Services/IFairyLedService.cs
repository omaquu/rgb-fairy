using System.Threading.Tasks;
using FairyRgbController.Models;

namespace FairyRgbController.Services
{
    public interface IFairyLedService
    {
        Task<bool> IsConnectedAsync();
        Task ScanAsync(int timeoutMs = 10000);
        Task ConnectAsync(BleDeviceInfo device);
        Task DisconnectAsync();
        Task SetHsvAsync(int hue, int saturation, int value); // hue 0-359, sat/val 0-1000
        Task SetPowerAsync(bool on);
        Task SetPresetAsync(byte presetId, int brightness); // brightness 0-1000
        Task TurnOffAsync(); // convenience
    }
}
