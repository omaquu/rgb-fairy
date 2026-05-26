using System;

namespace FairyRgbController.Models
{
    public class BleDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Rssi { get; set; }
        public bool IsConnectable { get; set; }
        public bool IsPaired { get; set; }

        public string SignalIcon => Rssi switch
        {
            0 => "📡",
            > -50 => "🟢",
            > -70 => "🟡",
            _ => "🔴"
        };

        public string PairingIcon => IsPaired ? "🔗" : "📶";

        public override string ToString() => $"{PairingIcon} {Name}";
    }
}