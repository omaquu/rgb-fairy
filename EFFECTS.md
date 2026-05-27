# Hello Fairy F15C - Effect ID Reference

## Verified Working Effect IDs (F15C Model)

| ID  | Name (EN)      | Name (FI)      | Notes                              |
|-----|----------------|----------------|------------------------------------|
| 01  | White          | Valkoinen      | ✅ Verified - Solid white light    |
| 08  | Pumpkin        | Kurpitsa        | ✅ Verified - Halloween pumpkin   |
| 12  | Flower         | Kukka           | ✅ Verified - Flower pattern       |
| 20  | Heart          | Sydän          | ✅ Verified - Pulsing heart        |

## Known Effect IDs (from APK/Home Assistant)

| ID  | Name (EN)              | Name (FI)                    |
|-----|------------------------|------------------------------|
| 17  | Fireworks              | Ilotulite                    |
| 18  | Xmas                   | Joulu                        |
| 20  | Halloween              | Halloween                    |
| 39  | July 4th               | USA:n itsenäisyyspäivä       |
| 40  | Red Gold               | Punainen kulta               |
| 41  | Blue White Dissolve    | Sininen valkoinen haalistus  |
| 46  | Valentine              | Ystävänpäivä                 |
| 47  | St. Patrick            | Irlannin kansallispäivä      |
| 48  | May Day                | Vapunpäivä                   |
| 50  | Candy Cane             | Karkkikeppi                  |
| 54  | Snow Day               | Lumipäivä                    |
| 56  | Blue Sparkle           | Sininen kimmellys            |
| 57  | White Sparkle          | Valkoinen kimmellys          |

## Preset IDs 1-16 (Default UI Buttons)

These are the 16 buttons in the default RGB Fairy Windows UI:

```
┌─────────────────────────────────────────────────────────┐
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │  1  │ │  2  │ │  3  │ │  4  │   Valkoinen, Punainen, │
│  │ ⚪  │ │ 🔴  │ │ 🟢  │ │ 🔵  │   Vihreä, Sininen       │
│  └─────┘ └─────┘ └─────┘ └─────┘                       │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │  5  │ │  6  │ │  7  │ │  8  │   Keltainen, Violetti,  │
│  │ 🟡  │ │ 🟣  │ │ 🟠  │ │ 🎃  │   Oranssi, Kurpitsa     │
│  └─────┘ └─────┘ └─────┘ └─────┘                       │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │  9  │ │ 10  │ │ 11  │ │ 12  │   Lumihiutale, Sydän,   │
│  │ ❄️  │ │ ❤️  │ │ 🌹  │ │ 🌸  │   Ruusu, Kukka          │
│  └─────┘ └─────┘ └─────┘ └─────┘                       │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │ 13  │ │ 14  │ │ 15  │ │ 16  │   Taivas, Aalto, Strobo,│
│  │ 🌌  │ │ 🌊  │ │ ⚡  │ │ 🌧️  │   Sade                  │
│  └─────┘ └─────┘ └─────┘ └─────┘                       │
└─────────────────────────────────────────────────────────┘
```

## BLE Command Protocol

### Command Structure
```
AA 03 LL MM PP BB BB CC
│  │  │  │  │  │  │  └── Checksum
│  │  │  │  │  │  └────── Brightness (big-endian, 0-1000)
│  │  │  │  │  └───────── Preset ID (1-58)
│  │  │  │  └──────────── Mode: 02 = Preset
│  │  │  └─────────────── Length: 04
│  │  └────────────────── Command: 03 = Color/Preset
│  └───────────────────── Prefix: AA
```

### Example Commands
```python
# Valkoinen (White) at 100% brightness
AA-03-04-02-01-03-E8

# Kurpitsa (Pumpkin) at 100% brightness  
AA-03-04-02-08-03-E8

# Kukka (Flower) at 100% brightness
AA-03-04-02-0C-03-E8

# Sydän (Heart) at 80% brightness
AA-03-04-02-14-03-20
```

## F15C Model Limitations

⚠️ **Important:** The F15C model does NOT support direct HSV color control!

- HSV color commands (`AA-03-07-01-...`) are ignored
- Only preset effect IDs (1-58) work
- Brightness can be adjusted for any preset

## Adding New Effects

1. Find the effect ID from testing or APK analysis
2. Add to `Presets` array in `MainWindow.xaml.cs`:
   ```csharp
   new PresetDef(ID, "Name", "Icon", "Description"),
   ```
3. Test the effect and update this document

## Pixel Drawing (Future Feature)

The APK supports custom pixel patterns that can be drawn and animated.
This requires:
1. `CMD_send_daa` command type
2. A frame buffer of pixel data (8x?? matrix)
3. Animation speed control

Command format (TBD - needs reverse engineering):
```
AA DD LL 00 [pixel_data...] CC
```

Where:
- `DD` = command type for pixel/frame operations
- `LL` = length of pixel data
- `pixel_data` = RGB values for each LED pixel

## Color Mapping for Solid Colors

If solid colors (1-7) don't work as expected, the device may use different RGB values:

| ID | Approximate Color | RGB |
|----|-------------------|-----|
| 01 | White             | 255,255,255 |
| 02 | Red               | 255,0,0 |
| 03 | Green             | 0,255,0 |
| 04 | Blue              | 0,0,255 |
| 05 | Yellow            | 255,255,0 |
| 06 | Purple            | 128,0,128 |
| 07 | Orange            | 255,165,0 |

## Troubleshooting

### Effect doesn't change when selected
- Check that device is connected (Disconnect button enabled)
- Check log for TX packet being sent
- Try power cycling the fairy light

### Brightness doesn't update until app close
- This is a known F15C behavior - the device may buffer commands
- Try moving the slider slowly
- Check if using too many rapid commands

### Wrong effect shows
- The F15C model has different effect mappings than other models
- If ID 8 shows "Blue Pink Sparkle" instead of "Pumpkin", your device is a different model
- Find your model's effect IDs through testing