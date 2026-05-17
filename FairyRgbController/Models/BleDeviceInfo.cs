using System;

namespace FairyRgbController.Models
{
    public class BleDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Rssi { get; set; }
        public bool IsConnectable { get; set; }

        public override string ToString() => $"{Name} ({Id}) [RSSI: {Rssi}]";
    }
}
