# RGB Fairy - BLE Protocol & Technical Documentation

## Device Information

**Manufacturer:** Hello Fairy (com.lenzetech.hellofairy)
**App Package:** com.lenzetech.hellofairy
**App Version:** Unknown (check APK)

---

## BLE UUIDs

### Main Service UUIDs (Custom Hello Fairy)
| UUID | Description |
|------|-------------|
| `0000ff12-0000-1000-8000-00805f9b34fb` | Hello Fairy Service (?) |
| `0000ff14-0000-1000-8000-00805f9b34fb` | Hello Fairy Service (?) |
| `0000ff15-0000-1000-8000-00805f9b34fb` | Hello Fairy Service (?) |

### Nordic UART Service (NUS)
| UUID | Description |
|------|-------------|
| `0000a951-0000-1000-8000-00805f9b34fb` | Nordic UART Service |
| `49535343-1e4d-4bd9-ba61-23c647249616` | Nordic UART TX Characteristic |
| `49535343-8841-43f4-a8d4-ecbe34729bb3` | Nordic UART RX Characteristic |
| `49535343-fe7d-4ae5-8fa9-9fafd205e455` | Nordic UART Control Point |

### Device Information Service
| UUID | Description |
|------|-------------|
| `0000a950-0000-1000-8000-00805f9b34fb` | Device Information |
| `0000a951-0000-1000-8000-00805f9b34fb` | Battery Service (?) |
| `0000a952-0000-1000-8000-00805f9b34fb` | Unknown |
| `0000a953-0000-1000-8000-00805f9b34fb` | Unknown |

### Hello Fairy Custom Service
| UUID | Description |
|------|-------------|
| `5833ff01-9b8b-5191-6142-22a4536ef123` | Custom Service Characteristic 1 |
| `5833ff02-9b8b-5191-6142-22a4536ef123` | Custom Service Characteristic 2 |
| `5833ff03-9b8b-5191-6142-22a4536ef123` | Custom Service Characteristic 3 |
| `5833ff04-9b8b-5191-6142-22a4536ef123` | Custom Service Characteristic 4 |

### Standard Bluetooth UUIDs
| UUID | Description |
|------|-------------|
| `00001101-0000-1000-8000-00805F9B34FB` | Serial Port Profile (SPP) |
| `00002902-0000-1000-8000-00805f9b34fb` | Client Characteristic Configuration |

---

## BLE Commands (Packet Format)

### Known Command Prefixes
| Command | Description |
|---------|-------------|
| `AA` | Packet prefix (all commands start with 0xAA) |
| `DA` | DIY command prefix (found in strings) |
| `03` | Scene/Preset command |

### Working Preset Command (F15C)
```
AA 03 04 02 [EFFECT_ID] [BRIGHTNESS_LSB] [BRIGHTNESS_MSB] [CHECKSUM]
```

**Verified working:** EFFECT_ID = 0x08 (Kurpitsa/Pumpkin)

---

## DIY Pixel Protocol

### CMD_send_daa Command
Found in APK: `CMD_send_daa`, `CMD_send_daa_new`

The DIY command is used to send custom pixel patterns to the device.

**Known Native Functions:**
- `led_write_rgbw` - writes RGBW data to LEDs
- `opLightDIYProc_Sel_Region_Leds` - selects region of LEDs
- `opLightSetRGB` - sets RGB color
- `hsv2rgb` / `hsv2rgb_diy` - HSV to RGB conversion

### DIY Animation Scenes (22 total)
| Scene | Description |
|-------|-------------|
| `opLightDIY_Internal_Scene_1` | Basic animation 1 |
| `opLightDIY_Internal_Scene_2` | Basic animation 2 |
| `opLightDIY_Internal_Scene_3` | Basic animation 3 |
| `opLightDIY_Internal_Scene_4` | Basic animation 4 |
| `opLightDIY_Internal_Scene_5` | Basic animation 5 |
| `opLightDIY_Internal_Scene_6` | Has `color_map` variant |
| `opLightDIY_Internal_Scene_7` | Basic animation 7 |
| `opLightDIY_Internal_Scene_8` | Basic animation 8 |
| `opLightDIY_Internal_Scene_9` | Basic animation 9 |
| `opLightDIY_Internal_Scene_10` | Basic animation 10 |
| `opLightDIY_Internal_Scene_11` | Basic animation 11 |
| `opLightDIY_Internal_Scene_12` | Basic animation 12 |
| `opLightDIY_Internal_Scene_13` | Basic animation 13 |
| `opLightDIY_Internal_Scene_14` | Has `_1to3` variants (multi-zone?) |
| `opLightDIY_Internal_Scene_15` | Basic animation 15 |
| `opLightDIY_Internal_Scene_16` | Basic animation 16 |
| `opLightDIY_Internal_Scene_17` | Basic animation 17 |
| `opLightDIY_Internal_Scene_18` | Basic animation 18 |
| `opLightDIY_Internal_Scene_19` | Basic animation 19 |
| `opLightDIY_Internal_Scene_20` | Basic animation 20 |
| `opLightDIY_Internal_Scene_21` | Basic animation 21 |
| `opLightDIY_Internal_Scene_22` | Basic animation 22 |

---

## Known Commands
| Command | Description |
|---------|-------------|
| `CMD_Init` | Device initialization |
| `CMD_GET_DEVICE_INFO_NEW` | Get device firmware/hardware info |
| `CMD_send_daa` | Send DIY pixel data |
| `CMD_send_daa:1` | DIY variant 1 |
| `CMD_send_daa:2` | DIY variant 2 |
| `CMD_send_daa_new` | New DIY command |
| `CMD_send_data_Dat` | Send data packet |
| `CMD_SceneLoopSetting_Class` | Scene loop settings |
| `CMD_CD_EN_Interface` | CD enable interface |
| `CMD_07_CameraFunction` | Camera function |

---

## Known Effect Names (CN_ prefix)

Found 55 effect names in APK:
```
CN_253, CN_254, CN_330
CN_CHRISTMAS_TREE, CN_CHRISTMAS_TREE_14_8, CN_CHRISTMAS_TREE_18_13
CN_CHRISTMAS_TREE_19_10, CN_CHRISTMAS_TREE_19_16, CN_CHRISTMAS_TREE_20_22
CN_CHRISTMAS_TREE_25_18, CN_CHRISTMAS_TREE_373, CN_CHRISTMAS_TREE_383
CN_COLDWARM_364, CN_COLDWARM_G40_15, CN_COLDWARM_G40_25
CN_COLDWARM_G40_366, CN_COLDWARM_S14_15, CN_COLDWARM_S14_25
CN_COLDWARM_S14_365, CN_COMBINATION_LIGHT
CN_COMPOSE_10_36, CN_COMPOSE_11_40, CN_COMPOSE_15_42_57
CN_COMPOSE_15_84, CN_COMPOSE_17_48_65, CN_COMPOSE_25_72
CN_COMPOSE_29_175, CN_COMPOSE_33_64, CN_COMPOSE_7_18
CN_COMPOSE_8_42, CN_COMPOSE_COLDWARM_S14_46
CN_COMPOSE_COLDWARM_S14_99, CN_COMPOSE_G40_25_72_97
CN_COMPOSE_S14_10_36, CN_COMPOSE_S14_15_84, CN_COMPOSE_S14_25_72
CN_COMPOSE_STAR_7_18
CN_G40_RGB_LIGHT, CN_ICE_STICK, CN_ICE_STICK_363, CN_ICE_STICK_968
CN_NORTHWEST_1, CN_NORTH_1
CN_NULL_PID
CN_S14_RGB_LIGHT, CN_SNOWFLAKE_LIGHT
CN_SOLAR_ENERGY_WARM_WHITE_G40, CN_SOLAR_ENERGY_WARM_WHITE_S14
CN_STARS_LIGHT, CN_STRAWBERRY_LIGHT
CN_TV
CN_WARM_351, CN_WARM_WHITE_G40, CN_WARM_WHITE_S14
CN_Z_FOLD_341
```

---

## Verified Working

| Effect ID | Name | Status |
|----------|------|--------|
| 0x08 | Kurpitsa (Pumpkin) | ✅ VERIFIED |

---

## Notes

- F15C uses preset commands only (no HSV direct control)
- Native library has `setHsv` function - may be supported on other models
- `real_led_count` variable in native lib suggests LED count detection
- Matrix dimensions found: 5x7, 5x10, 8x17, 14x8, 15x20, 17x17, 18x13, 19x10, 19x16, 20x22, 25x18