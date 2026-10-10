# One UI 6.1 Fidelity & Live Weather Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refine Two UI Lock Screen to achieve 1:1 pixel parity with Samsung One UI 6.1 by replacing shortcut buttons with authentic squircle super-ellipses and enlarged glyphs, removing the fake notification row completely, rendering translucent frosted glass widget tiles, correcting the header weather icon, and integrating live weather data via Open-Meteo API and GPS.

**Architecture:** 
- Visual Tree: Update `LockView.xaml` to purge `NotificationIconsPanel`, apply authentic 32-bit antialiased squircle assets (`ic_shortcut_flash.png`, `ic_shortcut_camera.png`), adopt frosted glass widget tiles (`#2AFFFFFF` with `#20FFFFFF` border), and replace the sub-header wind icon with an authentic weather condition glyph (`ic_weather_cloud_rain.png`).
- Weather Service: Create `WeatherService.cs` implementing cached, throttled background queries to Open-Meteo's REST API and reverse geocoding via `WebClient`, supporting GPS (`Geolocator`) with graceful fallback when offline.
- Lifecycle & Memory: Adhere strictly to Windows Phone 8.1 Silverlight constraints (C# 6.0, 512MB RAM budget, explicit bitmap disposal, no layout reflows during manipulation).

**Tech Stack:** C# 6.0, Windows Phone 8.1 Silverlight (`Microsoft.Phone`), XAML, Open-Meteo REST API, MSBuild 14.0.

**Spec Reference:** Real One UI 6.1 screenshot assets (`media_1791351602515.png`, `scratch_btn_left_exact.png`, `scratch_btn_right_exact.png`).

## Global Constraints
- Target Framework: Windows Phone 8.1 Silverlight (`Microsoft.Phone.Controls.PhoneApplicationPage`).
- Compiler: C# 6.0 strict (MSBuild 14.0) — No out vars, no tuples, no pattern matching, no local functions.
- Memory: Strict 512MB RAM limit; dispose `WebClient`, streams, and sever `ImageSource` on `OnNavigatedFrom`.
- Response Language: Vietnamese for all user communications.

---

### Task 1: Generate Authentic One UI Squircle Button & Weather Assets

**Files:**
- Create: `Two UI Lock Screen\Assets\Icons\ic_shortcut_flash.png`
- Create: `Two UI Lock Screen\Assets\Icons\ic_shortcut_flash_on.png`
- Create: `Two UI Lock Screen\Assets\Icons\ic_shortcut_camera.png`
- Create: `Two UI Lock Screen\Assets\Icons\ic_weather_cloud_rain.png`
- Modify: `Two UI Lock Screen\Two UI Lock Screen.csproj`

**Interfaces:**
- Consumes: Raw pixel data from `scratch_btn_left_exact.png`, `scratch_btn_right_exact.png`, and `media_1791351602515.png`.
- Produces: 32-bit transparent PNG assets with continuous-curvature One UI squircle border and centered glyphs.

- [ ] **Step 1: Extract authentic One UI squircle buttons with transparent backgrounds**
  Run Python script to extract high-resolution, perfectly antialiased 64x64 PNGs for flashlight (off & on) and camera with true Samsung super-ellipse curve.

- [ ] **Step 2: Extract sub-header weather cloud-rain glyph**
  Extract the authentic cloud with rain glyph from `media_1791351602515.png` as `ic_weather_cloud_rain.png`.

- [ ] **Step 3: Register assets in `Two UI Lock Screen.csproj`**
  Add `<Content Include="Assets\Icons\ic_shortcut_flash.png">...` and related assets to `Two UI Lock Screen.csproj`.

- [ ] **Step 4: Verify build with MSBuild 14.0**
  Run MSBuild to ensure all assets package cleanly into the `.xap`.

---

### Task 2: Purge Fake Notification Row and Modernize Widget Tiles

**Files:**
- Modify: `Two UI Lock Screen\LockView.xaml`
- Modify: `Two UI Lock Screen\LockView.xaml.cs`

**Interfaces:**
- Consumes: Assets from Task 1.
- Produces: Clean, notification-free XAML layout with frosted glass widget containers and authentic squircle shortcuts.

- [ ] **Step 1: Remove `NotificationIconsPanel` from `LockView.xaml`**
  Delete the entire fake notification row `<StackPanel x:Name="NotificationIconsPanel" ...>`.

- [ ] **Step 2: Update 3 Widget Squircle Tiles to Frosted Glass**
  Change widget tile backgrounds from opaque `#354956` to frosted glass `Background="#2BFFFFFF" BorderBrush="#20FFFFFF" BorderThickness="1" CornerRadius="18"`.

- [ ] **Step 3: Replace sub-header weather icon**
  Change `WeatherHeaderIcon` Source to `/Assets/Icons/ic_weather_cloud_rain.png` and update text foreground to white `#FFFFFE`.

- [ ] **Step 4: Embed authentic One UI squircle shortcut buttons**
  Replace standard `Border CornerRadius="17"` buttons with borderless 52x52 tap containers hosting `ic_shortcut_flash.png` and `ic_shortcut_camera.png`.

- [ ] **Step 5: Verify build with MSBuild 14.0**
  Compile and verify zero warnings/errors.

---

### Task 3: Implement Live Weather Service (Open-Meteo & Geolocation)

**Files:**
- Create: `Two UI Lock Screen\Helpers\WeatherService.cs`
- Modify: `Two UI Lock Screen\Properties\WMAppManifest.xml`
- Modify: `Two UI Lock Screen\Two UI Lock Screen.csproj`
- Modify: `Two UI Lock Screen\LockView.xaml.cs`

**Interfaces:**
- Consumes: `Windows.Devices.Geolocation.Geolocator`, Open-Meteo REST API.
- Produces: `WeatherData` model with temperature, weather code, and location name.

- [ ] **Step 1: Add `ID_CAP_LOCATION` to `WMAppManifest.xml`**
  Enable location capability in app manifest.

- [ ] **Step 2: Create `WeatherService.cs`**
  Implement non-blocking async weather retrieval with 30-minute caching, HTTP error tolerance, and fallback coordinates (Da Nang / Dien Minh / Hanoi).

- [ ] **Step 3: Wire `WeatherService` into `LockView.xaml.cs`**
  Call `WeatherService.GetWeatherAsync()` on warm resume / startup if cache is expired, updating `WeatherText`, `WidgetTempText`, and `LocationText` on UI Dispatcher.

- [ ] **Step 4: Verify build with MSBuild 14.0**
  Verify compilation and clean execution.

---

### Task 4: End-to-End Build Verification & Emulator Deployment

**Files:**
- Verify: `Two UI Lock Screen\Bin\Debug\Two_UI_Lock_Screen_Debug_AnyCPU.xap`

- [ ] **Step 1: Build solution with MSBuild 14.0**
  Ensure 0 errors, 0 warnings.

- [ ] **Step 2: Deploy to Windows Phone 8.1 Emulator (Device 8: WVGA 4 inch)**
  Deploy using `AppDeployCmd.exe` and test live rendering.

- [ ] **Step 3: Report empirical results in Vietnamese to user**
  Present clear evidence, before/after analysis, and confirm all user preferences are satisfied.
