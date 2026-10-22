# Dynamic Island for Windows

**English** | [Bahasa Indonesia](README.id.md)

An iOS-style **Dynamic Island** for Windows 10 and 11. A frosted-glass pill sits at the top-center of your screen, expands with a springy animation when you hover over it, and gives you a calendar, media controls with lyrics, system stats, a control center and more, without opening a single window.

![Showcase](docs/screenshots/00-showcase.png)

## Download

<p align="center">
  <a href="https://github.com/LitRHap/dynamic-island-windows/releases/latest/download/DynamicIsland.exe">
    <img src="https://img.shields.io/badge/Download-DynamicIsland.exe-0a84ff?style=for-the-badge&logo=windows&logoColor=white" alt="Download DynamicIsland.exe">
  </a>
  <br>
  <a href="https://github.com/LitRHap/dynamic-island-windows/releases/latest">
    <img src="https://img.shields.io/github/v/release/LitRHap/dynamic-island-windows?label=latest%20version" alt="Latest version">
  </a>
</p>

**Install:**

1. Download **[DynamicIsland.exe](https://github.com/LitRHap/dynamic-island-windows/releases/latest/download/DynamicIsland.exe)**. It is a single file; no installer and no extra runtime are needed.
2. Put it in any folder you like (e.g. `C:\Apps\DynamicIsland`) and run it. The island appears at the top-center of your screen.
3. *(Optional)* Right-click the island and choose **Start with Windows**.

> **"Windows protected your PC" (SmartScreen)?** The app is not code-signed. Click **More info → Run anyway**. The full source code is on [GitHub](https://github.com/LitRHap/dynamic-island-windows).


## Features

- **Frosted glass**: the island blurs whatever is behind it, with a soft tint and a glass rim. Flat top flush with the screen edge, rounded bottom.
- **iOS-like motion**: spring-based animation that can be interrupted smoothly (move in and out and it reverses without restarting).
- **Hover to expand, scroll to switch pages**: use the mouse wheel or touchpad, or click the icons at the bottom.
- **Soft rounded icons**: all icons are vector shapes with round caps and joins, no sharp corners.

### Pages

| Page | What it does |
|---|---|
| **Calendar & events** (default) | Month calendar, a list of events for the selected day, and a **+** button to add events |
| **Media** | Now playing from Spotify, browsers and other players: cover, title, artist, play/pause/skip, seekable progress bar, and **synced lyrics** (can be switched off) |
| **Performance** | Connected Wi-Fi name and its data usage, plus CPU, RAM, GPU and network speed, updated every second |
| **Control Center** | Volume and brightness sliders, Wi-Fi, Bluetooth, mute, screenshot, lock, Settings |
| **Notifications** | Windows notifications (see limitations) or a shortcut to the Notification Center |
| **Timer** | Quick presets (1 / 5 / 10 / 25 min), pause and reset |
| **File shelf** | Drag files onto the island, drag them out again anywhere |

### Live indicators

- Volume change, power plugged/unplugged and low-battery toasts
- **Caps Lock reminder** (works in every app) with a small badge on the pill while it is on
- Timer remaining time on the collapsed pill
- **Bluetooth battery**: a toast with the battery level when a headset connects (if the device reports it to Windows), and a small indicator on the island
- **Download indicator**: a moving bar and the current speed while a browser download is running, and a toast when it finishes
- **Break reminder**: after 60 minutes of continuous computer use the island reminds you to rest your eyes. The interval (30 / 45 / 60 / 90 min, or off) is set from the right-click menu. Being away from the keyboard and mouse for 3 minutes counts as a break and restarts the count.
- The island **slides away** when a window (e.g. your browser's tab bar) covers its area; move the cursor to the top edge to bring it back

![Calendar](docs/screenshots/02-calendar.png)
![Media](docs/screenshots/03-media.png)
![Performance](docs/screenshots/04-stats.png)
![Control Center](docs/screenshots/05-control-center.png)
![Caps Lock toast](docs/screenshots/06-capslock-toast.png)
![Break reminder](docs/screenshots/08-break-toast.png)

## Requirements

| | |
|---|---|
| **OS** | Windows 10 version 2004 (build 19041) or later, or Windows 11. 64-bit |
| **Runtime** | None. `DynamicIsland.exe` already includes everything it needs |
| **Permissions** | No administrator rights needed |
| **Internet** | Only for lyrics. Everything else works offline |
| **Optional hardware** | Laptop or internal display (brightness slider), Wi-Fi / Bluetooth adapter (toggles, Wi-Fi name), GPU with Windows performance counters (GPU stats) |

## Usage

| Action | Result |
|---|---|
| Hover over the pill | Expands (opens on the Calendar page) |
| Mouse wheel / touchpad scroll | Next / previous page |
| Click the icons at the bottom | Jump to a page |
| Click a date, press **+** | See or add events for that day (type `14:00 Team meeting`, then Enter) |
| Drag files onto the island | Opens the file shelf |
| Move cursor to the very top edge | Reveals the island when auto-hidden |
| Right-click the island | Start with Windows, **show/hide lyrics**, **break reminder interval**, Exit |

Your events are saved in `%AppData%\DynamicIsland\events.json`.

## Lyrics

Synced lyrics are shown on the Media page and follow the song position. They are fetched at runtime from [LRCLIB](https://lrclib.net), a free community lyrics database, and cached in `%AppData%\DynamicIsland\lyrics`. Only the song title and artist are sent. Not every song has lyrics; when none are found the panel stays hidden. Turn the feature on or off from the right-click menu. Your choice is remembered in `%AppData%\DynamicIsland\prefs.json`.

## Troubleshooting

- **SmartScreen warning**: see the note in *Download*; the app is unsigned.
- **Antivirus flags the app**: the Caps Lock reminder uses a low-level keyboard hook that only reacts to the Caps Lock key. The code is open for review.
- **Caps Lock reminder does nothing while a "Run as administrator" window is focused**: Windows does not let normal apps see keys typed into elevated windows.
- **The island doesn't show up**: check it isn't auto-hidden behind a maximized window; move the cursor to the very top edge of the screen.
- **No lyrics**: the song may not be in LRCLIB, or the title/artist reported by the player is unusual (e.g. a long YouTube title).

## Known limitations

- **Reading Windows notifications** needs a packaged (MSIX) app. When run unpackaged, the Notifications page falls back to a button that opens the Windows Notification Center.
- The **brightness** slider only appears on laptops / internal displays.
- **GPU** usage uses Windows performance counters and may show `N/A` on some systems.
- **Wi-Fi data usage** comes from Windows' own "Data usage" statistics, which only keep the last 30 days.
- **Bluetooth battery** only appears if the device reports its battery to Windows. If it isn't shown in *Settings → Bluetooth & devices*, it won't be shown here.
- The **download indicator** shows activity and speed, not a percentage: browsers don't tell Windows the total file size.
- Primary monitor only.

## Source code

The full source code, issues and discussions are on GitHub:
**[github.com/LitRHap/dynamic-island-windows](https://github.com/LitRHap/dynamic-island-windows)**

## Credits

- Lyrics provided by [LRCLIB](https://lrclib.net)
- Audio via [NAudio](https://github.com/naudio/NAudio)

## License

[MIT](LICENSE)
