# RGB Fairy - Minimalistinen UI Suunnitelma

## Visio
Moderni, tumma, minimalistinen käyttöliittymä joka sopii Windows-ympäristöön. Focus on effects, ei tilaa vieviä elementtejä.

## Väripaletti
```
Tausta:     #1A1A1A (lähes musta)
Kortti:     #2D2D2D (harmaa)
Akcentti:   #6C5CE7 (violetti)
On:         #00B894 (vihreä)
Off:        #636E72 (harmaa)
Teksti:     #FFFFFF (valkoinen)
Teksti 2:   #B2BEC3 (vaaleanharmaa)
```

## Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  RGB Fairy                                     [Scan] [Connect] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │              🎃  Kurpitsa (ID:08)                        │   │
│  │                                                          │   │
│  │              Kirkkaus: ████████░░ 80%                   │   │
│  │                                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  EFektit                                                       │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐       │
│  │  1 │ │  2 │ │  3 │ │  4 │ │  5 │ │  6 │ │  7 │ │  8 │  ←→   │
│  │ ⚪ │ │ 🔴 │ │ 🟢 │ │ 🔵 │ │ 🟡 │ │ 🟣 │ │ 🟠 │ │ 🎃 │       │
│  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘       │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐       │
│  │  9 │ │ 10 │ │ 11 │ │ 12 │ │ 13 │ │ 14 │ │ 15 │ │ 16 │  ←→   │
│  │ ❄️ │ │ ❤️ │ │ 🌹 │ │ 🌸 │ │ 🌌 │ │ 🌊 │ │ ⚡ │ │ 🌧️ │       │
│  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘       │
│  ─────────────────────────────────────────────────────────────  │
│  [Kaikki 58 →]                                                  │
├─────────────────────────────────────────────────────────────────┤
│  [🎨 Pikseli]  [💾 Tallenna]  [⚡ Staattinen väri]  [⏻ OFF]   │
└─────────────────────────────────────────────────────────────────┘
```

## Uudet Komponentit

### 1. EffectGrid (4x4 tai scrollattava)
- Kompaktit napit 48x48px
- Hover: varjostus + scale 1.05
- Aktiivinen: violetti reunus
- Pitkä painallus: Lisätietoa popup

### 2. Pikseli-Editori (erillinen ikkuna)
```
┌─────────────────────────────────────────┐
│  Pikseli-Editor                    [X]  │
├─────────────────────────────────────────┤
│  ┌─────────────────────────────────────┐ │
│  │  ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □  │ │
│  │  □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■  │ │
│  │  ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □  │ │
│  │  □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■  │ │  ← 8x32 LED matrix
│  │  ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □  │ │
│  │  □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■  │ │
│  │  ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □  │ │
│  │  □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■ □ ■  │ │
│  └─────────────────────────────────────┘ │
│                                         │
│  Väri: [🔴] [🟢] [🔵] [🟡] [⚪] [🎨]    │
│                                         │
│  Nopeus: ──────○────── 50ms              │
│                                         │
│  [▶ Toista]  [💾 Tallenna]  [Lähetä]     │
└─────────────────────────────────────────┘
```

### 3. 58 Efektin Lista (modal/sivu)
- Koko ruudun lista kategorioittain
- Haku
- Suosikit (tähdellä)
- Viimeksi käytetyt

## Tekninen Toteutus

### WPF Elementit
- `Grid` + `UniformGrid` napittomille
- `ItemsControl` + `DataTemplate` efekteille
- `WrapPanel` scrollausta varten
- `Window` erilliselle pikseli-editorille
- `Border` + `DropShadowEffect` korteille

### Värit XAML:ssä
```xml
<Window.Resources>
    <SolidColorBrush x:Key="bg" Color="#1A1A1A"/>
    <SolidColorBrush x:Key="card" Color="#2D2D2D"/>
    <SolidColorBrush x:Key="accent" Color="#6C5CE7"/>
    <SolidColorBrush x:Key="on" Color="#00B894"/>
    <SolidColorBrush x:Key="off" Color="#636E72"/>
</Window.Resources>
```

### Hover Efektit
```csharp
<Button.Style>
    <Style TargetType="Button">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="border" ...>
                        <ContentPresenter/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="border" Property="RenderTransform">
                                <Setter.Value>
                                    <ScaleTransform ScaleX="1.05" ScaleY="1.05"/>
                                </Setter.Value>
                            </Setter>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</Button.Style>
```

## Pixel Drawing Protokolla

APK:ssa on `CMD_send_daa` ja `Send_Ble_AssemblyUnitDiyDynamicControl`.

Tuntematon komento rakenne (tarvitsee APK dekompiloinnin):
```
AA <CMD> <LEN> <DATA> <CHECKSUM>
```

Pixel frame data:
- Jokainen LED = RGB (3 bytes)
- 8x32 = 256 LEDs = 768 bytes per frame
- Animoidaan lähettämällä frameja nopeasti

## Kehitysjärjestys

1. ✅ Nykyinen UI toimii (build 61)
2. 🔄 Uusi tumma teema + kompakti layout
3. 📋 58 efektin lista modalissa
4. 🎨 Pikseli-editori (tarvitsee APK tutkimista)
5. ♻️ Animointi looppi