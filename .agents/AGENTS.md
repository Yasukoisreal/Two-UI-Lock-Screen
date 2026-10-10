# AGENTS.md / GEMINI.md - Two UI Lock Screen

**Core Directive:** Highest cognitive effort, ultra-deep reasoning, and zero-defect technical execution. Operate with the technical precision, paranoia, and architectural rigor of a principal systems engineer (Claude Opus 5.5 / Maximum Effort tier).

**Tradeoff:** Correctness, precision, safety, and deep verification strictly supersede speed and conciseness. Never rush, never cut corners, never make unverified assumptions.

---

## 1. Deep Reasoning & High-Effort Thinking Protocol

### A. Root Cause Analysis (5 Whys & Execution Tracing)
- **Trace the full stack:** When diagnosing any bug, visual defect, or unexpected behavior, trace execution flow through the entire system:
  1. Silverlight Visual Tree & XAML layout engine.
  2. Touch gesture processing (`ManipulationStarted`, `ManipulationDelta`, `ManipulationCompleted`).
  3. Live Lock Screen Extensibility API (`Windows.Phone.System.LockScreenExtensibility.ExtensibilityApp`).
  4. OS System Protection (`Windows.Phone.System.SystemProtection`).
  5. Cross-process IPC / Named Mutex (`TwoUILockScreenMutex`).
  6. Hardware controls (`FlashlightHelper`, `VibrateController`, `Battery`, `DeviceStatus`).
- **Never patch symptoms:** Do not apply shallow null checks or crude try-catches that mask underlying logic flaws. Find the fundamental root cause and fix it architecturally.
- **Understand concurrency:** Always map out thread ownership, async continuations, Silverlight Dispatcher requirements (`Deployment.Current.Dispatcher.BeginInvoke`), and reentrancy risks before modifying code.

### B. Multi-Hypothesis & Tradeoff Evaluation
- When facing architectural, visual layout, or physics engine problems, consider at least 2–3 viable solutions.
- Explicitly evaluate tradeoffs: Memory allocation (RAM / GC), CPU cycles, 60 FPS gesture responsiveness, platform quirks, and complexity.
- Propose or pick the simplest, most robust solution that satisfies all constraints (apply YAGNI strictly).

### C. Adversarial Self-Critique & Pre-Mortem
Before writing or finalizing any code, stress-test your solution against worst-case scenarios:
1. *"What if the user swipes rapidly, flings, or cancels a gesture mid-drag?"* (Manipulation delta jitter / snap-back animation collision / unlock race condition).
2. *"What if the user presses the hardware Power button or Back key while locked?"* (Lifecycle suspension, Back key leak, background resource drain).
3. *"What happens on a 512MB RAM device under heavy memory pressure (Lumia 520 / 530)?"* (OOM crash, texture allocation failure, garbage collector stutter).
4. *"What happens if the phone has a secure PIN/Password lock set in OS Settings?"* (ExtensibilityApp PIN pad overlap, incorrect `unlockThreshold`, security bypass risk).
5. *"Are there any language features from C# 7.0+ accidentally included?"* (Build failure on MSBuild 14.0).

### D. Mandatory Deep Thinking & Prohibition of Fast/Shallow Reasoning (All Model Tiers)
- **Zero Tolerance for Fast Heuristics:** Rapid reflexive answering, intuitive guessing, and superficial code skimming are **strictly forbidden**.
- **Enforced Deliberate Cognitive Pacing:** You MUST deliberately slow down, think deeply, and reason with exhaustive thoroughness. Never leap prematurely to conclusions or code modifications.
- **Step-by-Step Mental Simulation:** Before writing, editing, or proposing any line of code, you must mentally simulate the execution path from start to finish:
  1. Trace all visual transformations, render bounds, and layout passes.
  2. Map object lifetimes, hardware camera/torch disposal, and timer lifecycles.
  3. Validate all failure edge cases (unplugged battery, missing custom wallpaper, rapid toggling).

### E. Clarify Ambiguities Before Modification (Never Guess Intent)
- **Always ask before acting:** If a user request, prompt, or bug report is ambiguous, underspecified, or open to multiple valid interpretations, **ALWAYS ask the user for clarification before making any code or file changes**.
- **Never make silent assumptions:** Do not guess user intent, assume unspoken requirements, or silently pick an arbitrary design. When in doubt, present the options clearly and ask.

---

## 2. Project Architecture & Live Lock Screen Core Principles

### A. Windows Phone 8.1 Silverlight Live Lock Screen Contract
The project is built on **Windows Phone 8.1 Silverlight** (`Microsoft.Phone.Controls.PhoneApplicationPage`), strictly complying with `LIVE_LOCK_SCREEN_ARCHITECTURE.md`:
- **App Manifest (`WMAppManifest.xml`):**
  - Requires `ActivationPolicy="Resume"` to ensure instantaneous wakeup from suspension without cold startup delays.
  - Declares capabilities: `ID_CAP_SHELL_DEVICE_LOCK_UI_API`, `ID_CAP_ISV_CAMERA`, `ID_CAP_APPOINTMENTS`.
- **Extension Contract (`Extensions\LockAppExtension.xml`):**
  - Declares descriptor with `xmlns:x="urn:LockApp"` and consumer contract ID `{CD4601F6-351B-43C7-9087-6B12BD98ED63}`.
- **Anti-Desync Trampoline Router (`LockRouter.xaml`):**
  - Every application launch enters `LockRouter.xaml` first.
  - If `SystemProtection.ScreenLocked == true` -> Navigate to `LockView.xaml`.
  - If `SystemProtection.ScreenLocked == false` -> Navigate to `MainPage.xaml`.
  - Purges backstack (`NavigationService.RemoveBackEntry()`) immediately upon routing.
- **Tetra Zero-Leak Lifecycle Strategy (`App.xaml.cs`):**
  - Once the user successfully unlocks the device via `SystemProtection.RequestScreenUnlock()`, the lock screen's lifecycle role is finished.
  - To prevent background memory accumulation and ensure 100% of RAM is returned to foreground apps on 512MB devices, the process terminates cleanly on unlock via `Application.Current.Terminate()`.

### B. Decoupled Multi-Layer Gesture & Physics Engine
- **Decoupled Gesture Principles:**
  - Layer 1 (Wallpaper `BackgroundContainer`): Cached in VRAM via `BitmapCache`, kept static at $Y=0$ during drag to avoid disorientation.
  - Layer 2 (Dynamic Content `ForegroundPanel`): Moves upward via `ForegroundTransform.TranslateY` on `ManipulationDelta`. Fades out proportionally via `Opacity = 1.0 - (|Y| / 450.0)`.
- **Spring Bounce Physics (`LocksScreenBounceEase.cs`):**
  - If released without reaching `unlockThreshold` (and velocity $v_y > -1200\text{ px/s}$), snap back using the official mathematical spring bounce equation (`Distance = 100.0, Bounciness = 2.0`).
- **PIN Pad Awareness:**
  - Query `ExtensibilityApp.GetLockPinpadHeight()` upon `ManipulationStarted`. If PIN is configured, clamp `unlockThreshold` to the exact PIN pad top boundary and invoke `ExtensibilityApp.BeginUnlock()`.

### C. Samsung One UI 6.1 Design Integrity (Pixel-Perfect Replication)
Strictly maintain 1:1 visual parity with real Samsung One UI 6.1 lock screen reference assets (`Assets/Reference/`):
1. **Clock System:**
   - Single-line horizontal clock (`12:06` / `12:39`) is the primary default style (`FontSize="88"`, `FontWeight="SemiBold"`, pastel cyan `#CBE7FF` or white).
   - Support stacked 2-line clock as an alternate configurable style.
2. **Sub-Header:**
   - Line 1: `T.4, 7 Th10  [///] 26°` (Date + One UI 3-slanted-wind-bars icon + temperature).
   - Line 2: `(27 Th 8 ÂL)` (Vietnamese lunar calendar centered underneath).
3. **Widget Container (3 Independent Squircle Tiles):**
   - The widgets are **3 separate rounded squircle tiles** (`Background="#354956"`, `CornerRadius="18"`), NOT a single monolithic pill!
     - **Tile 1 (Battery `68 × 68`):** Semicircular arc gauge ring, smartphone silhouette glyph (`ic_phone_glyph.png`) in center, `⚡ %` below.
     - **Tile 2 (UV Index `68 × 68`):** Semicircular arc gauge ring, UV sun glyph (`ic_sun_uv.png`) in center, index value `8` below.
     - **Tile 3 (Weather `144 × 68`):** Location name `Điện Minh` on top, large temperature `26°`, and 3 slanted wind bars (`ic_weather_wind.png`) on the right.
4. **Transparent Notification Row:**
   - Icons sit directly on the wallpaper with **100% transparent background** (no rounded border/pill behind each icon).
   - Unread counter (`+5`) uses a compact cyan pill at the end.
5. **Bottom Shortcut Buttons:**
   - Dimensions: `Width="50" Height="50" CornerRadius="17" Background="#FFFFFE"`.
   - Pure black Samsung glyphs, tightly cropped with 0px wasted padding:
     - Left: Torch (`ic_flash_off_black.png`, `Width="26" Height="30"`).
     - Right: Camera (`ic_camera_black.png`, `Width="30" Height="26"`).
6. **Bottom Info / Contact:**
   - Unplugged: Clean plain text `Yasuko` (or custom signature) centered at bottom.
   - Charging: Dynamic battery glyph (`ic_battery_bolt.png`), percentage, and charging time estimate (`4 giờ 55 phút nữa sẽ đầy`).
7. **One UI 6.1 Music Widget (OLMP v2.0):**
   - Hỗ trợ 2 chế độ xem: Thu gọn (Collapsed 3 dòng với nút điều khiển nhanh) và Mở rộng (Expanded với thanh tiến trình seekbar pill thumb).
   - Icon ứng dụng phát nhạc ở góc trên bên trái tuân thủ chuẩn AOSP (`METADATA_KEY_DISPLAY_ICON_URI` / `GET /api/icon`): được tải động trực tiếp từ URL do app phát nhạc cung cấp (app đọc từ thư mục `Assets/` gốc của nó).
   - Màn hình khóa tuyệt đối không nhúng cứng icon của bên thứ ba và không dùng heuristic phỏng đoán theo `appName`.
   - Hàng 5 nút điều khiển tuân thủ chuẩn AOSP `PlaybackState.CustomAction`: Slot 1 (ngoài cùng bên trái) và Slot 5 (ngoài cùng bên phải) tự động cấu hình và hiển thị icon tùy biến do app nhạc chỉ định (`Favorite`, `Shuffle`, `Repeat`, `Rewind`, `FastForward`, `ThumbsUp`, `ThumbsDown`...), kích hoạt lệnh tương tác qua `GET /api/action?name={action}&value={state}`.

---

## 3. Zero-Defect Technical Constraints (WP8.1 Silverlight & C# 6.0)

### A. Strict C# 6.0 Compatibility (MSBuild 14.0 / VS 2015 Community Update 3)
The compiler strictly enforces C# 6.0. **NEVER use C# 7.0+ syntax:**
- ❌ **No Out Variables:** `int.TryParse(s, out var val);` is INVALID.
  - ✅ Must write: `int val; int.TryParse(s, out val);`
- ❌ **No ValueTuples / Tuple Syntax:** `(int a, string b)` is INVALID.
  - ✅ Must write: custom POCO, `Tuple<int, string>`, or `KeyValuePair<K, V>`.
- ❌ **No Pattern Matching:** `if (obj is Track track)` is INVALID.
  - ✅ Must write: `var track = obj as Track; if (track != null) { ... }`
- ❌ **No Local Functions:** `void Helper() { ... }` inside a method is INVALID.
  - ✅ Must write: private helper method or lambda `Action helper = () => { ... };`
- ❌ **No Throw Expressions, Ref Locals, or Digit Separators (`1_000_000`).**

### B. 512MB RAM Budget (Lumia 520 / 530)
- **Aggressive Disposal:** Every `Stream`, `IsolatedStorageFileStream`, and hardware capture resource MUST be enclosed in a `using` block or explicitly disposed.
- **Bitmap Management:** In Silverlight, always use `BitmapImage.SetSource(stream)` within `using` blocks and null out `ImageSource` upon `OnNavigatedFrom` to avoid VRAM leaks.
- **Full-Bleed Status Bar:**
  - Set `shell:SystemTray.IsVisible="True"`, `shell:SystemTray.Opacity="0"`, `shell:SystemTray.ForegroundColor="#FFFFFE"`.
  - Never hide the system tray completely as that violates OS E911 emergency call regulations and disrupts shell composition.

### C. Cross-Process Synchronization (Named Mutex)
- All configuration changes between `MainPage.xaml` (Settings UI) and `LockView.xaml` (Live Lock Screen UI) MUST be synchronized using the system-wide named Mutex `TwoUILockScreenMutex`.
- Maintain an atomic `.bak` file rollback mechanism (`two_ui_settings.xml.bak`) to guarantee settings integrity even during abrupt device restarts.

---

## 4. Surgical Code Modification & Tooling Rules

### A. Strict Tooling Discipline
- **NEVER use terminal commands** (`cat`, `Get-Content`, `Set-Content`, `Select-String`, `grep`, `sed`, `echo`) to read, search, or edit source code files.
  - Terminal commands corrupt UTF-8 encoding (breaking UI icons and Unicode text) and cause Visual Studio `Inconsistent Line Endings` warnings.
  - **READ / VIEW:** ALWAYS use the native `view_file` tool.
  - **SEARCH:** ALWAYS use the native `grep_search` or `find_by_name` tools.
  - **EDIT / WRITE:** ALWAYS use `replace_file_content` or `write_to_file`.

### B. Surgical & Minimalist Changes
- **"If it works, DO NOT touch it" (Preserve Stable Code):** If code is running stably and without errors, **DO NOT TOUCH IT**. Strictly forbid unprompted refactoring, unsolicited "cleanups", or speculative rewrites.
- Read the entire file or surrounding context completely with `view_file` before making any edit.
- Make targeted replacements. Do not reformat unrelated code, alter indentation, or touch existing comments.
- Keep the git working tree clean and organized.

---

## 5. Verification & Empirical Evidence Protocol

**Never claim success without hard empirical evidence.**

### A. Build Verification Command
To verify compilation and `.xap` package generation:
```powershell
& "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" "Two UI Lock Screen.sln" /p:Configuration=Debug /p:Platform="Any CPU" /v:m
```

### B. Deployment & Emulator Testing Command
To deploy the generated `.xap` directly to the Windows Phone 8.1 Emulator (Device 8: WVGA 4 inch):
```powershell
& "C:\Program Files (x86)\Microsoft SDKs\Windows Phone\v8.1\Tools\AppDeploy\AppDeployCmd.exe" /installfiles "Two UI Lock Screen\Bin\Debug\Two_UI_Lock_Screen_Debug_AnyCPU.xap" /targetdevice:8
```

---

## 6. Communication & Version Control Rules

- **Language:** Always reply to the user in **Vietnamese** (natural, concise, engineering-focused tone). Code identifiers, file paths, and commit messages remain in English.
- **Direct Communication:** Zero sycophancy, zero empty disclaimers, zero boilerplate apologies. Deliver crisp technical reasoning, diffs, and verification proof.
- **Proactive Clarification:** If a prompt or requirement is ambiguous or open to multiple interpretations, **ALWAYS ask the user for clarification before modifying code**. Never guess or assume intent.
- **Git Commits & AI Policy:**
  - **NEVER** append AI trailers (`Co-Authored-By: Claude...`, `Generated with...`) to git commit messages or PR descriptions.
  - Write standard semantic commit messages (`feat: ...`, `fix: ...`, `perf: ...`, `refactor: ...`, `docs: ...`).

---

## 7. Project Reference Documents

- **Platform Architecture Guide:** `D:\Documents\Visual Studio 2015\Projects\HyperOS\LIVE_LOCK_SCREEN_ARCHITECTURE.md`
- **One UI 6.1 Design Blueprint:** `ONE_UI_6_1_ANALYSIS_AND_BLUEPRINT.md`
- **Visual Reference Gallery:** `Two UI Lock Screen\Assets\Reference\` (Real Samsung One UI 6.1 screenshots)
