# RGB Fairy - F15C Product Reference

## Product Identification

| Property | Value |
|----------|-------|
| **Product Code** | FF15 (seen in APK code) |
| **APK Asset Folder** | `picture_853853/` |
| **Product ID (PID)** | 853853 |
| **Matrix Size** | 15 x 20 LEDs |
| **DIY Pictures** | 10 built-in |

### F15C DIY Pictures (from APK)
```
Christmas_hat_0.json
Christmas_hat_walking_stick_boot_0.json
Christmas_heart_0.json
Red_white_raindrops_0.json
Turn_add_cap_0.json
candle_0.json
christmas_trees_0.json
dollar_0.json
rose_0.json
strawberry_0.json
```

### F15C vs Other Products

| PID | Product Type | Matrix Size |
|-----|--------------|-------------|
| 261261 | Unknown | - |
| 262262 | Unknown | - |
| 263263 | Unknown | - |
| 264264 | Unknown | - |
| 282282 | Unknown | - |
| 289289 | Unknown | - |
| 310310 | Unknown | - |
| **853853** | **F15C (FF15)** | **15x20** |

### How APK Detects Product

The APK detects product type via BLE device name or advertisement data:
1. Device advertises with name containing "FF15" or similar
2. APK reads PID from device firmware via `CMD_01_Device_Type` command
3. APK loads corresponding `picture_[PID]/` folder for DIY content

---

## F15C BLE Protocol Details

### Service UUIDs (Same for all Hello Fairy products)
| Service | UUID |
|---------|------|
| Hello Fairy Service | `0000ff12-0000-1000-8000-00805f9b34fb` |
| Hello Fairy TX Char | `0000ff14-0000-1000-8000-00805f9b34fb` (Write) |
| Hello Fairy RX Char | `0000ff15-0000-1000-8000-00805f9b34fb` (Notify) |
| Nordic UART Service | `0000a951-0000-1000-8000-00805f9b34fb` |
| Nordic UART TX | `0000a952-0000-1000-8000-00805f9b34fb` |
| Nordic UART RX | `0000a953-0000-1000-8000-00805f9b34fb` |

### Custom Hello Fairy UUIDs (found in APK)
| UUID | Description |
|------|-------------|
| `5833ff01-...` | Custom service |
| `5833ff02-...` | Custom characteristic (read) |
| `5833ff03-...` | Custom characteristic (write) |
| `5833ff04-...` | Custom characteristic (notify) |

---

## F15C Preset Command Format

Based on protocol analysis, preset command format:

```
Header: 0xAA
Length: 0x03
CmdType: 0x04 (preset)
SubType: 0x02 (set_effect)
EffectID: 0x00 - 0x39 (1-57)
Brightness LSB: 0x00 - 0xFF
Brightness MSB: 0x00 (typically 0)
Checksum: (Header ^ Length ^ CmdType ^ SubType ^ EffectID ^ Brightness LSB ^ Brightness MSB) & 0xFF
```

### Verified F15C Effect IDs

| ID (hex) | Effect Name (CN_) | Working? |
|----------|-------------------|----------|
| 08 | Kurpitsa (pumpkin) | ✅ Verified |

### Effect Names (CN_ prefix) - Sample from APK
```
CN_CHRISTMAS_TREE
CN_STRAWBERRY_LIGHT
CN_MERRY_CHRISTMAS
CN_The_Curtain_Light_40_40_ESP32
CN_CoconutTree_353
CN_CoconutTree_369
CN_Cone_TREE_15_13
CN_Cone_TREE_15_17
CN_Cone_TREE_BALL_15_11
CN_Developer_DIY_StraightCutting
CN_Mural_67LED_223_DeepSnowflake
```

---

## F15C DIY Pixel Command Format

### JSON Pixel Format (from APK)
```json
{
  "form": {
    "widthPixel": 15,
    "heightPixel": 20,
    "type": "pencil",
    "color": "rgba(255, 174, 0, 1)"
  },
  "dotList": [
    [[R, G, B, A], [R, G, B, A], ...],  // row 0 (20 pixels)
    [[R, G, B, A], [R, G, B, A], ...],  // row 1
    ...                                   // 15 rows total
  ]
}
```

Where:
- `R, G, B`: 0-255 (color values)
- `A`: Alpha (1 = LED on, 0 = LED off)

### BLE Command Building

**CMD_send_daa** is the command for sending DIY pixel data.

Native functions found:
- `SendRGBCmd` - Send RGB color command
- `SendRGBWCmd` - Send RGBW color command
- `SendRGBWCmdByAddr` - Send with address
- `SendRGBWCmdRandomCode` - Send with random code
- `opLightDIYProc_Sel_Region_Leds` - Select LED region

### Animation Scenes (29 total in native library)
```
Scene_1 through Scene_29
```

Some scenes have `_1to3` variants indicating multi-zone support:
- `Scene_14_run`
- `Scene_14_run_1to3`
- `Scene_14_init`
- `Scene_14_init_1to3`

---

## Testing F15C

To test F15C-specific commands:

1. **Read Device Info**: `CMD_01_Device_Type` returns product type
2. **Test Effect IDs**: Try IDs 0x01-0x39 with brightness 0x64
3. **Test DIY**: Send pixel data via `CMD_send_daa`

### Test Packet Example (Effect 0x08 at brightness 100)
```
AA 03 04 02 08 64 00 XX
     |  |  |  |  |   |
     |  |  |  |  |   +-- Checksum
     |  |  |  |  +------ Brightness MSB (0)
     |  |  |  +--------- Brightness LSB (100 = 0x64)
     |  |  +------------ Effect ID (8 = Kurpitsa)
     |  +--------------- SubType (0x02)
     +------------------ CmdType (0x04)
```

---

## Known Issues

1. **F15C does NOT support HSV color control** - Only preset command colors work
2. **F15C may have different effect IDs** than other models
3. **853853 PID not in Global_Config** - Pictures are bundled in APK
4. **DIY command format (CMD_send_daa)** still needs protocol discovery

---

## Next Steps

1. ✅ Document F15C product info
2. ⬜ Test preset commands to map all 57 effect IDs
3. ⬜ Discover DIY pixel command protocol (CMD_send_daa)
4. ⬜ Test animation scene selection
5. ⬜ Implement full F15C support in WPF app