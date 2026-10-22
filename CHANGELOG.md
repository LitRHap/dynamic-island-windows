# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

Download the latest build: [DynamicIsland.exe](https://github.com/LitRHap/dynamic-island-windows/releases/latest/download/DynamicIsland.exe)

## [Unreleased]

## [1.0.0] - 2026-10-22

First public release.

### Added

- **Island and motion**
  - Frosted-glass pill at the top-center of the screen: backdrop blur with color boost, soft tint, glass rim and a gentle shadow.
  - Spring-based animation that can be interrupted smoothly.
  - Expands on hover, switches pages with the mouse wheel or the tab icons.
  - Slides away when a window covers it; move the cursor to the top edge to bring it back.
  - Soft vector icons with rounded caps and joins, replacing emoji and sharp symbols.
- **Calendar & events** (default page when you hover)
  - Month calendar with a day-by-day event list.
  - Add events with **+** (`14:00 Team meeting`, then Enter) and delete them again. Saved to `%AppData%\DynamicIsland\events.json`.
  - Fixed-date Indonesian public holidays built in.
- **Media**
  - Now-playing cover, title and artist, play/pause/skip and a seekable progress bar for Spotify, browsers and other players.
  - Synced lyrics from LRCLIB, cached locally and switchable from the right-click menu.
- **Performance**
  - CPU, RAM, GPU and network speed, updated every second.
  - Connected Wi-Fi name and its total data usage over the last 30 days.
- **Control Center**: volume and brightness sliders, Wi-Fi, Bluetooth, mute, screenshot, lock screen and Windows Settings.
- **Notifications** page, **Timer** (1 / 5 / 10 / 25 min presets) and a **File shelf** (drag files in, drag them out anywhere).
- **Live indicators**
  - Toasts for volume, power plugged/unplugged and low battery.
  - Caps Lock reminder with a badge on the collapsed pill.
  - Timer countdown on the collapsed pill.
  - Bluetooth battery: toast and indicator when a headset connects and reports its battery level.
  - Download indicator: moving bar and live speed while a browser download runs, plus a toast when it finishes.
  - Break reminder after continuous computer use (30 / 45 / 60 / 90 min or off, from the right-click menu).
- Right-click menu: *Start with Windows*, lyrics on/off, break reminder interval and Exit.
- Single-file `DynamicIsland.exe` build with the .NET runtime included.

### Known limitations

- Reading Windows notifications needs a packaged (MSIX) app; unpackaged builds open the Notification Center instead.
- The brightness slider is limited to laptops and internal displays.
- The download indicator shows activity and speed, not a percentage.
- Wi-Fi data usage is limited to the 30 days Windows keeps.
- Primary monitor only.

[Unreleased]: https://github.com/LitRHap/dynamic-island-windows/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/LitRHap/dynamic-island-windows/releases/tag/v1.0.0
