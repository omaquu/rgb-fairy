# RGB Fairy - DIY Pixel Editor Protocol

## Overview

The Hello Fairy APK includes a DIY (Do-It-Yourself) feature that allows users to:
1. Draw custom pixel art on a grid
2. Apply animations to the drawn patterns
3. Send the pixel data to the device via BLE

---

## Pixel Data JSON Format

### File Location
APK stores DIY pictures in: `assets/picture_853853/*.json`

### Available Pictures (10 total)
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

### JSON Structure

```json
{
  "form": {
    "widthPixel": 15,
    "heightPixel": 20,
    "type": "pencil",
    "shape": "square",
    "color": "rgba(255, 0, 0, 1)",
    "monochrome": false,
    "grid": true,
    "size": 60
  },
  "dotList": [
    [[R, G, B, A], [R, G, B, A], ...],  // Row 0
    [[R, G, B, A], [R, G, B, A], ...],  // Row 1
    ...
  ]
}
```

### Fields Explained

| Field | Type | Description |
|-------|------|-------------|
| `form.widthPixel` | int | Grid width in pixels |
| `form.heightPixel` | int | Grid height in pixels |
| `form.type` | string | Drawing tool type ("pencil" typical) |
| `form.shape` | string | "square" or "round" pixel shape |
| `form.color` | string | Current draw color in rgba() format |
| `form.monochrome` | bool | True if single color only |
| `form.grid` | bool | True if grid lines shown |
| `form.size` | int | Total pixels (width × height) |
| `dotList` | array | 2D array of pixel data |

### Pixel Data [R, G, B, A]

Each pixel is a 4-element array:
- **R**: Red (0-255)
- **G**: Green (0-255)
- **B**: Blue (0-255)
- **A**: Alpha/Active (1 = pixel ON, 0 = pixel OFF)

**Example:**
```json
[255, 0, 0, 1]    // Red pixel, active
[255, 255, 255, 0] // White pixel, inactive (off)
[0, 0, 0, 1]      // Black pixel, active
[0, 0, 0, 0]      // Transparent/off pixel
```

---

## Common Grid Sizes

| Width | Height | Typical Use |
|-------|--------|-------------|
| 8 | 32 | F15C Curtain |
| 15 | 20 | Christmas calendar, candle, rose |
| 17 | 17 | Heart shape |
| 20 | 15 | Rose, various |
| 14 | 8 | Christmas tree (small) |
| 18 | 13 | Christmas tree (medium) |
| 19 | 10 | Christmas tree (wide) |
| 19 | 16 | Christmas tree (tall) |
| 20 | 22 | Christmas tree (large) |
| 25 | 18 | Christmas tree (extra large) |

---

## DIY BLE Command Format

### Command: CMD_send_daa

The exact packet format for sending DIY pixel data is **not yet reverse engineered**.
The native function `led_write_rgbw` handles the actual LED writing.

### Experimental Format (WIP)

Based on analysis, the command likely follows this pattern:

```
AA DA [LEN] [W] [H] [RGB_DATA...] [CHECKSUM]
```

| Byte | Description |
|------|-------------|
| `AA` | Packet prefix |
| `DA` | DIY command ID |
| `LEN` | Length of remaining data |
| `W` | Width |
| `H` | Height |
| `RGB_DATA` | Pixel data (R, G, B for each pixel) |
| `CHECKSUM` | Simple sum checksum |

**Note:** This format is experimental and needs testing.

---

## Animation Scenes

The APK has 22 built-in animation "scenes" that can be applied to DIY pictures:

| Scene | Description |
|-------|-------------|
| 1-5 | Basic animations |
| 6 | Has color_map variant (color remapping) |
| 7-13 | Basic animations |
| 14 | Has 1to3 variants (multi-zone support) |
| 15-22 | Basic animations |

### How Animations Work

1. User draws pixel art in the app
2. User selects an animation scene (1-22)
3. The native library `opLightDIY_Internal_Scene_N_run` function applies the animation
4. Pixel data is sent to device via `CMD_send_daa`

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Pixel Editor UI | ✅ Done | WPF window created |
| Grid Drawing | ✅ Done | Click to draw/erase |
| Color Selection | ✅ Done | RGB sliders |
| JSON Export | ✅ Done | APK-compatible format |
| JSON Import | ✅ Done | Load existing pictures |
| DIY BLE Command | 🔧 WIP | Format not verified |
| Animation Selection | ⏳ Pending | Need to add UI |
| Send to Device | ⏳ Pending | Need protocol verification |

---

## Testing Needed

1. **Sniff BLE traffic** from APK to F15C while using DIY feature
2. **Capture packet format** for `CMD_send_daa`
3. **Verify animation scenes** - which scene number does what
4. **Test RGBW mode** - does F15C support RGBW or just RGB?

---

## Related Files

- **Native Library:** `lib/arm64-v8a/libhellofairy.so`
- **Key Functions:**
  - `led_write_rgbw` - writes RGBW LED data
  - `opLightDIYProc_Sel_Region_Leds` - select LED region
  - `SendRGBCmd` / `SendRGBWCmd` - send RGB[W] command
  - `hsv2rgb_diy` - HSV to RGB for DIY mode
  - `module_curtain_scene_init/run` - curtain animations
  - `module_peter_scene_init/run` - peter (pattern?) animations

---

## References

- APK Package: `com.lenzetech.hellofairy`
- App Name: "Hello Fairy"
- S3 Bucket: `hellofairyapp.s3.amazonaws.com`
- Config URL: `https://hellofairyapp.s3.amazonaws.com/UIConfig/Global_Config.json`