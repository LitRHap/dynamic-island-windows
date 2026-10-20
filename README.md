<div align="center">

# Dynamic Island for Windows

**An iOS-style Dynamic Island for your Windows desktop.**
Frosted-glass look, smooth spring animations, and a calendar, media player, system stats, quick controls and more, all in one pill at the top of your screen.

[**⬇️ Download for Windows**]([https://github.com/USERNAME/REPO](https://github.com/LitRHap/dynamic-island-windows)/releases/latest/download/DynamicIsland.exe)
&nbsp;·&nbsp;
[All releases]([https://github.com/USERNAME/REPO](https://github.com/LitRHap/dynamic-island-windows)/releases)
&nbsp;·&nbsp;
[Bahasa Indonesia](README.id.md)

<br>

<img src="docs/00-showcase.png" alt="Dynamic Island for Windows: calendar, media, stats and control center" width="900">

</div>

---

## Showcase

<table>
  <tr>
    <td align="center" width="50%">
      <img src="docs/02-calendar.png" alt="Calendar and events"><br>
      <b>Calendar &amp; events</b><br>
      <sub>The default view when you hover the island</sub>
    </td>
    <td align="center" width="50%">
      <img src="docs/03-media.png" alt="Media player"><br>
      <b>Media player</b><br>
      <sub>Spotify, YouTube in the browser, and more</sub>
    </td>
  </tr>
  <tr>
    <td align="center">
      <img src="docs/04-stats.png" alt="System stats"><br>
      <b>System stats</b><br>
      <sub>CPU, RAM, GPU and network speed</sub>
    </td>
    <td align="center">
      <img src="docs/05-control-center.png" alt="Control center"><br>
      <b>Control center</b><br>
      <sub>Volume, brightness, Wi-Fi, Bluetooth and shortcuts</sub>
    </td>
  </tr>
  <tr>
    <td align="center">
      <img src="docs/01-collapsed.png" alt="Collapsed island"><br>
      <b>Collapsed</b><br>
      <sub>A small pill that stays out of your way</sub>
    </td>
    <td align="center">
      <img src="docs/07-volume-toast.png" alt="Volume toast"><br>
      <b>Live toasts</b><br>
      <sub>Volume, battery, Caps Lock, timers and more</sub>
    </td>
  </tr>
</table>

<div align="center">
  <img src="docs/06-capslock-toast.png" alt="Caps Lock toast" width="600"><br>
  <sub>Caps Lock indicator</sub>
</div>

---

## Download

**[Download DynamicIsland.exe (latest version)](https://github.com/USERNAME/REPO/releases/latest/download/DynamicIsland.exe)**

No installer needed: download the file and run it.

> Windows SmartScreen may show a "Windows protected your PC" warning because the app is not code-signed yet.
> Click **More info → Run anyway**.

## Features

- **Calendar & events**: monthly calendar and a per-day event list. Events are stored locally, and fixed-date Indonesian public holidays are built in.
- **Media player**: title, cover art, play/pause/skip and a seekable progress bar for whatever is playing (Spotify, YouTube in the browser, etc.).
- **System stats**: CPU, RAM, GPU, live network speed, the name of the connected Wi-Fi and its total data usage over the last 30 days.
- **Control center**: volume, brightness, Wi-Fi, Bluetooth, mute, screenshot, lock screen and Windows Settings.
- **Notifications**, a **timer**, and a **file shelf** (drag files onto the island, drag them out whenever you need them).
- **Bluetooth battery**: see your headset's battery level when it connects.
- **Download indicator**: a moving activity bar and the current speed while a browser download is running, plus a toast when it finishes.
- **Toasts** for volume, laptop battery, Caps Lock and finished timers.
- **Auto-hide**: the island slides up when a window covers it. Move the cursor to the top edge to bring it back.
- Frosted-glass backdrop with rounded, soft icons.

## Requirements

| | |
|---|---|
| **OS** | Windows 10 version 2004 (build 19041) or newer, including Windows 11 |
| **Architecture** | 64-bit (x64) |
| **.NET** | Not required: the release already includes the runtime |
| **Display** | The island sits at the top center of your primary display |

Feature notes:

- **Screen brightness** is only available on laptops / built-in displays (via WMI).
- **Bluetooth battery** only appears if your device reports its battery to Windows.
  Check *Settings → Bluetooth & devices*: if it isn't shown there, it won't be shown here either.
- **Notifications**: Windows asks for permission the first time you open the notifications page.
- **GPU usage** needs the "GPU Engine" performance counters (Windows 10 1709+ with a WDDM 2.0+ driver).
- **Wi-Fi data usage** uses Windows' own "Data usage" statistics, which only keep the last 30 days.
- **Download indicator** watches your *Downloads* folder for browsers' temporary files (`.crdownload`, `.part`, …).
  Browsers don't expose the total size to Windows, so it shows activity and speed rather than a percentage.

## How to use

- Hover over the island to open the calendar and today's events.
- **Scroll** (mouse wheel or touchpad) to switch pages, or click the tab icons at the bottom.
- Click a date to see its events. Press **+** to add one, for example `14:00 Team meeting`, then Enter.
- **Right-click** the island for *Start with Windows* and *Exit*.

Your events are saved to `%AppData%\DynamicIsland\events.json`.

## Privacy

Everything runs locally and nothing is sent over the internet.
The app installs a keyboard hook only to detect the **Caps Lock** key; no other key is read or stored.
Because of that hook, some antivirus programs may raise a false positive.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows.

```powershell
git clone https://github.com/USERNAME/REPO.git
cd REPO
dotnet run
```

Create a single self-contained `.exe`:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

The result is `publish\DynamicIsland.exe`.

