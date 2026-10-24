# Crosshair Overlay

A lightweight, always-on-top, click-through crosshair overlay for Windows. Renders over borderless/windowed games without touching game files or triggering anti-cheat.

## Features

- **Shape presets:** Default (Apex-style), Crosshair, Dot, Circle, T-Bar
- **Color & contrast:** Custom colors, bright presets (Neon Green, Cyan, Red, Yellow, White), opacity control, outline color
- **Size adjustments:** Line length, thickness, center gap, dot size/opacity
- **Positioning:** Sub-pixel offset with arrow-key nudging (Ctrl+Arrow globally)
- **Quick controls:** F9 toggle visibility, F10 settings
- **Taskbar integration:** Stays on the taskbar when hidden; jump list with Toggle/Settings/Exit
- **Settings draft mode:** Changes preview live but only apply on "Apply"; Cancel discards
- **Low overhead:** Transparent WPF overlay, click-through

## Hotkeys

| Key | Action |
|-----|--------|
| F9 | Toggle crosshair visibility |
| F10 | Open settings |
| Ctrl + Arrow keys | Nudge offset by 1px |

## Build

Requires .NET Core 3.1 SDK with Windows Desktop workload.

```bash
dotnet build
dotnet run
```

Publish a self-contained single-file exe:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/netcoreapp3.1/win-x64/publish/CrosshairOverlay.exe`

## Settings

Stored at `%AppData%\CrosshairOverlay\settings.json`.

## License

MIT
