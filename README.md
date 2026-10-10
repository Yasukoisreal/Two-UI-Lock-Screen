# Two UI Lock Screen

A pixel-perfect, high-performance Samsung One UI 6.1 Live Lock Screen experience crafted for **Windows Phone 8.1 Silverlight** (Lumia 520/530 up to Lumia 930/1520).

---

## Features

### 1. Samsung One UI 6.1 Visual Parity
- **Clock Styles:** Single-line horizontal clock (`FontSize="88"`, `Samsung Sans`) and stacked 2-line clock options.
- **Sub-Header:** Solar date, Vietnamese lunar calendar (`Âm lịch`), and weather info with slanted wind bars.
- **3 Independent Squircle Widgets:**
  - **Battery:** Semicircular arc gauge ring, smartphone silhouette glyph, and live percentage.
  - **UV Index:** Semicircular arc gauge ring with sun glyph.
  - **Weather:** Location name, large temperature display, and weather condition icon.
- **Transparent Notification Row:** Notification icons sit directly on the wallpaper with cyan unread badge pills.
- **Bottom Shortcuts:** Torch and Camera quick action buttons with pure black Samsung glyphs.
- **Signature & Charging Status:** Signature text (`Yasuko`) or dynamic charging estimation (`4 giờ 55 phút nữa sẽ đầy`).

### 2. One UI 6.1 Music Widget (OLMP v2.0 Sync)
- **Two View Modes:**
  - **Collapsed View:** 56×56 album art, audio output info, song title, artist, and compact playback controls.
  - **Expanded View:** Full-bleed blurred album art background, interactive seekbar, and 5 full-size control buttons (Favorite, Prev, Play/Pause, Next, Shuffle).
- **Dual Ocean Wave Seekbar:** 2 undulating wave ripple layers with smooth sine physics and dynamic thumb tracking.
- **Adaptive Color Palette:** Real-time dominant color extraction from song album art with One UI pastel tinting.

### 3. Architecture & Performance
- **Target OS:** Windows Phone 8.1 Silverlight (`Microsoft.Phone.Controls.PhoneApplicationPage`).
- **512MB RAM Optimization:** Strict memory lifecycle management, aggressive stream disposal, zero VRAM leaks on low-end Lumia devices.
- **Trampoline Router (`LockRouter.xaml`):** Anti-desync routing between `LockView.xaml` and `MainPage.xaml` with backstack purging.
- **Cross-Process Sync:** System-wide named Mutex (`TwoUILockScreenMutex`) with atomic settings rollback.

---

## Building the Project

### Requirements
- Visual Studio 2015 Community Update 3 (or VS 2013 with WP8.1 SDK)
- Windows Phone 8.1 SDK
- MSBuild 14.0

### Build via Command Line
```powershell
& "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" "Two UI Lock Screen.sln" /p:Configuration=Debug /p:Platform="Any CPU" /v:m
```

### Deploy to Emulator or Device
```powershell
& "C:\Program Files (x86)\Microsoft SDKs\Windows Phone\v8.1\Tools\AppDeploy\AppDeployCmd.exe" /installfiles "Two UI Lock Screen\Bin\Debug\Two_UI_Lock_Screen_Debug_AnyCPU.xap" /targetdevice:8
```

---

## License
Created by Yasuko. Licensed under the MIT License.
