# RGB Fairy - Pikseli-Editor & DIY Protokolla

## Pikselidata JSON-formaatti (APK:sta)

APK tallentaa DIY-kuviot JSON-muodossa:
```json
{
  "form": {
    "widthPixel": 15,
    "heightPixel": 20,
    "type": "pencil",
    "color": "rgba(255, 174, 0, 1)",
    "monochrome": false,
    "grid": true,
    "size": 58
  },
  "dotList": [
    [[255, 255, 255, 1], [0, 0, 0, 1], ...],  // row 0
    [[0, 0, 0, 1], [255, 0, 0, 1], ...],        // row 1
    ...
  ]
}
```

## LED-Matriisi Koot

| Laite | Koko | Pikseleitä |
|-------|------|------------|
| F15C Verho | 8×32 | 256 |
| Joulukalenteri | 20×15 | 300 |
| Sydän | 17×17 | 289 |

## BLE-komento: CMD_send_daa

Tuntematon tarkka muoto, mutta todennäköinen rakenne:

```
AA <CMD> <LEN> <W> <H> <pixel_data> <CHECKSUM>
│  │    │   │  │  │           │
│  │    │   │  │  │           └── Sum of all bytes % 256
│  │    │   │  │  └── RGB-pikselidata (W×H×3 tai W×H×4 tavua)
│  │    │   │  └── Korkeus (1-2 tavua, little tai big-endian)
│  │    │   └── Leveys (1-2 tavua)
│  │    └── Pituus (pikselidatan pituus + 4 leveys+korkeus + mahdollinen tyyppi)
│  └── Komennon numero (todennäköisesti 0x0D tai 0xDA)
└── Alku: 0xAA
```

## Vaihtoehtoinen: CMD_send_daa:1 ja CMD_send_daa:2

APK sisältää kolme eri DIY-komentoa:
- `CMD_send_daa` - Lähetä yksittäinen kehys
- `CMD_send_daa:1` - ???
- `CMD_send_daa:2` - ???
- `CMD_send_daa_new` - Uusi versio (mahdollisesti pidemmille datoille)

## Natiivikirjaston DIY-Skenaariot (22 kpl)

```
opLightDIY_Internal_Scene_1  - Valo mihin tahansa kohtaan
opLightDIY_Internal_Scene_2  - ???
...
opLightDIY_Internal_Scene_22 - ???
```

Nämä ovat sisäänrakennettuja animaatioefektejä joita voi soveltaa pikseleihin.

## LED-Operaatiot (natiivikirjastosta)

```
led_static_mode   - Staattinen tila (yksi väri)
led_jump_mode     - Hyppivä tila (välitön vaihto)
led_dimmer_mode   - Himmennys (smooth fade)
led_write_rgbw    - Kirjoita RGBW data
led_refresh_ram   - Päivitä RAM
hsv2rgb_diy       - HSV → RGB DIY-muunnos
```

## Protokollan Selvittäminen

### Vaihe 1: Katsele liikennettä
1. Asenna BLE-sniffer puhelimeen (nRF Connect tai similar)
2. Käytä APK:ta ja katso mitä komentoja lähetetään
3. Vertaa eri kuvioiden datamääriä

### Vaihe 2: Kokeile tunnettuja formeja
```csharp
// Kokeile näitä:
public static byte[] BuildDiyCommand(byte[] pixelData, int width, int height)
{
    var cmd = new List<byte>();
    cmd.Add(0xAA);                    // Prefix
    cmd.Add(0xDA);                    // CMD_send_daa
    cmd.Add((byte)(pixelData.Length + 4)); // Length
    cmd.Add((byte)(width & 0xFF));   // Width LSB
    cmd.Add((byte)((width >> 8) & 0xFF)); // Width MSB (if needed)
    cmd.Add((byte)(height & 0xFF));  // Height LSB
    cmd.Add((byte)((height >> 8) & 0xFF)); // Height MSB
    cmd.AddRange(pixelData);          // RGB data (no alpha)
    cmd.Add(CalculateChecksum(cmd));  // Checksum
    return cmd.ToArray();
}
```

## Pikseli-Editorin Suunnitelma (WPF)

### UI-Layout
```
┌──────────────────────────────────────────────┐
│  Pikseli-Editor                        [X]   │
├──────────────────────────────────────────────┤
│  Koko: [8] × [32]  ↓ Valitse: Joulukalenteri │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │  □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ │  │
│  │  ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ │  │
│  │  □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ ■ □ □ │  │
│  │  ... (8 riviä)                         │  │
│  └────────────────────────────────────────┘  │
│                                              │
│  Väri: [🔴][🟢][🔵][🟡][⚪][🟣][🟠][🎨 Lisää] │
│                                              │
│  Työkalu: [✏️ Piirrä] [🧹 Pyyhi] [⬜ Täytä]  │
│                                              │
│  Nopeus: ────○─────── 100ms                  │
│  Animaatio: [▶ Toista] [⏸ Tauko] [⏹ Stop]   │
│                                              │
│  Kehykset: [1] [2] [3] [+ Lisää]            │
│  [💾 Tallenna] [📤 Lähetä] [📥 lataa]       │
└──────────────────────────────────────────────┘
```

### Toteutusaskeleet

1. **Pikseligridin luonti** (WPF Canvas tai ItemsControl)
2. **Värivalitsin** (väripaletti + custom väri)
3. **Piirtologistiikka** (mouse down, move, up)
4. **JSON-tallennus** (APK:n formaatilla)
5. **BLE-lähetys** (cmd_send_daa kokeilu)
6. **Animaatiotuki** (multiple frames, timer)

## Testattavat DIY-Komennot

| Komento | Kuvaus | Kokeiltu? |
|---------|--------|-----------|
| 0xAA 0x0D ... | CMD_send_daa | ❌ |
| 0xAA 0xDA ... | Vaihtoehtoinen DAA | ❌ |
| 0xAA 0x0E ... | CMD_send_daa_new | ❌ |
| 0xAA 0x07 0x01 ... | HSV color (vain Preset 1) | ✅ Ei toimi F15C:llä |

## Seuraavat Askeleet

1. [ ] Selvitä CMD_send_daa:n tarkka formaatti (BLE-sniffer)
2. [ ] Kokeile eri komentoja F15C:llä
3. [ ] Jos DAA toimii, rakenna pikseli-editori
4. [ ] Jos ei, selvite onko toinen komento (CMD_send_data_Dat?)

Tarvitaan: BLE-sniffer data APK:n ja F15C:n välisestä liikenteestä kun lähetetään DIY-kuva.