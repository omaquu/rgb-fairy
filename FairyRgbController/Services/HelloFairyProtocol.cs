using System;
using System.Collections.Generic;
using System.Linq;
using FairyRgbController;

namespace FairyRgbController.Services
{
    public static class HelloFairyProtocol
    {
        public static readonly Guid ServiceUuid = new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455");
        public static readonly Guid CommandCharacteristicUuid = new Guid("49535343-8841-43f4-a8d4-ecbe34729bb3");
        public static readonly Guid NotifyCharacteristicUuid = new Guid("49535343-1E4D-4BD9-BA61-23C647249616");

        public const byte Prefix = 0xAA;
        public const byte CmdPower = 0x02;
        public const byte CmdColorPreset = 0x03;
        public const byte ModeColor = 0x01;
        public const byte ModePreset = 0x02;

        public static byte[] BuildPacket(byte command, byte[] payload)
        {
            var packet = new List<byte>();
            packet.Add(Prefix);         // 0xAA
            packet.Add(command);        // command type (0x02=power, 0x03=color/preset)
            packet.Add((byte)payload.Length);  // LENGTH byte (was missing!)
            packet.AddRange(payload);
            byte checksum = (byte)(packet.Sum(b => b) % 256);
            packet.Add(checksum);
            // Debug log the packet
            AppLogger.WriteLine("BLE", $"TX: {BitConverter.ToString(packet.ToArray())}");
            return packet.ToArray();
        }

        public static byte[] BuildPowerCommand(bool on)
        {
            byte[] payload = new byte[] { (byte)(on ? 1 : 0) };
            return BuildPacket(CmdPower, payload);
        }

        public static byte[] BuildColorCommand(int hue, int sat, int val)
        {
            var payload = new byte[7];
            payload[0] = ModeColor;
            payload[1] = (byte)(hue & 0xFF);
            payload[2] = (byte)((hue >> 8) & 0xFF);
            payload[3] = (byte)(sat & 0xFF);
            payload[4] = (byte)((sat >> 8) & 0xFF);
            payload[5] = (byte)(val & 0xFF);
            payload[6] = (byte)((val >> 8) & 0xFF);
            return BuildPacket(CmdColorPreset, payload);
        }

        public static byte[] BuildPresetCommand(byte presetId, int brightness)
        {
            var payload = new byte[4];
            payload[0] = ModePreset;
            payload[1] = presetId;
            // Big-endian: high byte first, then low byte
            payload[2] = (byte)((brightness >> 8) & 0xFF);
            payload[3] = (byte)(brightness & 0xFF);
            return BuildPacket(CmdColorPreset, payload);
        }
    }
}
