# Open Lock Screen Music Protocol (OLMP v2.0) Specification
**Chuẩn Giao Tiếp & Điều Khiển Trình Phát Nhạc Dành Cho Windows Phone 8.1 (Chuẩn Hóa Theo Android AOSP MediaSession)**

Tài liệu này đặc tả giao thức mở **Open Lock Screen Music Protocol (OLMP v2.0)**, được chuẩn hóa và ánh xạ 1:1 theo kiến trúc **Android Open Source Project (AOSP) Media Framework** (`android.media.session.PlaybackState` và `android.media.MediaMetadata`). 

Giao thức cho phép bất kỳ ứng dụng phát nhạc nào (Music Player, YTMusicWP, Spotify 3rd-party, SoundCloud, RetroPlayer...) trên Windows Phone 8.1 có thể kết nối, đồng bộ trạng thái bài hát thời gian thực và nhận lệnh điều khiển trực tiếp từ Màn hình khóa (**Two UI Lock Screen** / Live Lock Screen).

---

## 1. Kiến Trúc Tổng Quan (System Architecture)

Trên Windows Phone 8.1, tiến trình Màn hình khóa (`AgHost.exe` / `LockScreenHost`) chạy độc lập trong môi trường bảo vệ và không thể trực tiếp truy cập bộ nhớ của các ứng dụng phát nhạc khác. 

Giao thức **OLMP** sử dụng giải pháp **HTTP Local Loopback (`http://127.0.0.1:8088`)** tương đương với Binder IPC trên Android:
* **App phát nhạc** đóng vai trò là `MediaSession` (Provider): Chạy một Mini HTTP Server nhẹ tại cổng `8088`.
* **Màn hình khóa** đóng vai trò là `MediaController` (Consumer): Định kỳ thăm dò trạng thái, tính toán thanh thời gian thực và gửi lệnh điều khiển.
* **Ưu điểm:** Hoạt động 100% ổn định trên mọi máy Lumia (512MB RAM lẫn 1GB/2GB), không cần quyền Root/Interop Unlock, không làm gián đoạn luồng Background Audio của hệ điều hành.

```
+------------------------------------+          +------------------------------------+
|       TWO UI LOCK SCREEN          |          |        BẤT KỲ APP NHẠC NÀO         |
|      (Android MediaController)     |          |       (Android MediaSession)       |
|                                    |          |                                    |
|   [Thăm dò] GET /api/track         | -------> |   Nhận request & trả về JSON       |
|   [Lệnh]    GET /api/play          | -------> |   Xử lý MediaElement.Play()        |
|   [Lệnh]    GET /api/pause         | -------> |   Xử lý MediaElement.Pause()       |
|   [Lệnh]    GET /api/next          | -------> |   Chuyển bài tiếp theo             |
|   [Lệnh]    GET /api/seek?pos=ms   | -------> |   Tua vị trí bài hát               |
|   [Custom]  GET /api/action?name=..| -------> |   Xử lý sendCustomAction           |
+------------------------------------+          +------------------------------------+
```

### 1.1 Hỗ Trợ Đa Nền Tảng: Silverlight (XAP) & WinRT (AppX)

Giao thức OLMP v2.0 được thiết kế độc lập nền tảng (**Platform-Agnostic**), hỗ trợ trọn vẹn cả hai kiến trúc ứng dụng trên Windows Phone 8.1:

1. **Ứng Dụng Silverlight (WP 8.0 / WP 8.1 Silverlight - Định dạng gói `.xap`):**
   - **Âm thanh nền (Background Audio):** Quản lý qua `Microsoft.Phone.BackgroundAudio.AudioPlayerAgent` và `BackgroundAudioPlayer.Instance`.
   - **Mạng cục bộ (Local Socket):** Khởi chạy Mini HTTP Server thông qua `Windows.Networking.Sockets.StreamSocketListener` (WinRT Socket API được hỗ trợ sẵn trên Silverlight WP8.1) hoặc TCP `System.Net.Sockets.Socket`.
   - **Tài nguyên ảnh (Assets):** Đọc trực tiếp từ tệp cài đặt thông qua `Package.Current.InstalledLocation.Path` hoặc `Application.GetResourceStream`.

2. **Ứng Dụng WinRT (Universal WP 8.1 - Định dạng gói `.appx`):**
   - **Âm thanh nền (Background Audio):** Quản lý qua `Windows.Media.Playback.BackgroundMediaPlayer` và một Background Task triển khai `IBackgroundTask`.
   - **Mạng cục bộ (Local Socket):** Khởi chạy Mini HTTP Server thông qua `Windows.Networking.Sockets.StreamSocketListener` trong tiến trình nền của Audio Task.
   - **Tài nguyên ảnh (Assets):** Đọc trực tiếp thông qua `Package.Current.InstalledLocation.Path` hoặc `StorageFile.GetFileFromApplicationUriAsync`.

Do Màn hình khóa (`Two UI Lock Screen`) và App nghe nhạc giao tiếp hoàn toàn qua chuẩn HTTP mạng nội bộ (`127.0.0.1:8088`), **Màn hình khóa không phân biệt và hỗ trợ 100% cả Silverlight lẫn WinRT mà không gặp bất kỳ rào cản tương thích nào**.

### 1.2 Tên Miền Ảo Thân Thiện (`http://olmp.local`) & Cơ Chế Tự Động Phân Giải URL

Nhằm đem lại trải nghiệm tinh tế, chuẩn mực và thân thiện cho người dùng (tránh việc phải ghi nhớ và gõ thủ công chuỗi IP số học kèm cổng `127.0.0.1:8088`), giao thức OLMP v2.0 chuẩn hóa tên miền ảo thân thiện chính thức:

$$\mathbf{http://olmp.local} \quad \Longleftrightarrow \quad \mathbf{http://127.0.0.1:8088}$$

* **Tên miền mặc định trong hệ thống:** Trên trang Cài đặt (`MusicSettingsPage.xaml`), giá trị cấu hình mặc định là `http://olmp.local`.
* **Cơ chế phân giải tự động (Virtual Host Resolution - `ResolveNetworkUrl`):**
  - Màn hình khóa (`Two UI Lock Screen`) tích hợp sẵn bộ chuyển đổi địa chỉ thông minh trước khi thực hiện bất kỳ truy vấn HTTP nào:
    1. Khi phát hiện hostname có đuôi `.local` (ví dụ `olmp.local`, `nowplaying.local`, `lockmedia.local`) hoặc `localhost`, hệ thống tự động ánh xạ host về loopback `127.0.0.1`, đồng thời nếu cổng là 80 (hoặc không khai báo) sẽ tự động gán cổng chuẩn `:8088`.
    2. Khi người dùng nhập `http://127.0.0.1` mà không kèm cổng, hệ thống tự động bổ sung cổng `:8088` (`http://127.0.0.1:8088`).
    3. Nếu người dùng nhập địa chỉ IP mạng LAN cụ thể (ví dụ máy chủ từ xa `http://192.168.1.5:8088`), hệ thống giữ nguyên địa chỉ để hỗ trợ debug từ xa.
* **Về phía ứng dụng phát nhạc:**
  - Ứng dụng phát nhạc **không cần phải can thiệp vào file hosts hay cấu hình DNS của OS**. App nhạc chỉ cần lắng nghe socket trên cổng `8088` (`BindServiceNameAsync("8088")`). Mọi request gửi tới `http://olmp.local` đều được Màn hình khóa tự động phân giải về cổng `8088` của ứng dụng một cách hoàn toàn trong suốt.

---

## 2. Mô Hình Trạng Thái & Hằng Số Chuẩn Android AOSP

### 2.1 Mã Trạng Thái Phát (`PlaybackState.State`)
Hệ thống sử dụng các mã số nguyên chuẩn từ Android AOSP `PlaybackState.java`:

| Mã Số | Tên Trạng Thái (Constant) | Chuỗi (Alias) | Ý Nghĩa Kỹ Thuật | Phản Hồi Giao Diện Màn Hình Khóa |
| :---: | :--- | :--- | :--- | :--- |
| **0** | `STATE_NONE` | `"none"` | Chưa nạp bài hát hoặc đã reset phiên phát | Widget nhạc tự động ẩn (`Visibility.Collapsed`) |
| **1** | `STATE_STOPPED` | `"stopped"` | Đã dừng hẳn bài hát | Dừng đếm thời gian, nút Play hiển thị biểu tượng Play (`\uE102`) |
| **2** | `STATE_PAUSED` | `"paused"` | Đang tạm dừng phát | Nút hiển thị biểu tượng Play (`\uE102`), giữ nguyên vị trí bài |
| **3** | `STATE_PLAYING` | `"playing"` | Đang phát nhạc | Nút hiển thị biểu tượng Pause (`\uE103`), kim thời gian trôi |
| **4** | `STATE_FAST_FORWARDING` | `"fast_forwarding"` | Đang tua nhanh bài hát | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang tua nhanh >>` |
| **5** | `STATE_REWINDING` | `"rewinding"` | Đang tua lùi bài hát | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang tua lùi <<` |
| **6** | `STATE_BUFFERING` | `"buffering"` | Đang tải đệm mạng / nạp bài | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang tải...` (giữ widget trên màn hình) |
| **7** | `STATE_ERROR` | `"error"` | Xảy ra lỗi phát media | Hiển thị nhãn cảnh báo: Lấy từ `errorMessage` hoặc `"Lỗi phát lại"` |
| **8** | `STATE_CONNECTING` | `"connecting"` | Đang kết nối thiết bị phát | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang kết nối...` |
| **9** | `STATE_SKIPPING_TO_PREVIOUS` | `"prev"` | Đang nhảy về bài trước | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang chuyển bài trước...` |
| **10**| `STATE_SKIPPING_TO_NEXT` | `"next"` | Đang nhảy sang bài sau | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang chuyển bài tiếp...` |
| **11**| `STATE_SKIPPING_TO_QUEUE_ITEM` | `"queue"` | Đang nhảy đến một mục trong hàng đợi | Hiển thị nhãn phụ: `Tên Ca Sĩ • Đang nạp danh sách...` |

### 2.2 Cờ Năng Lực Nút Bấm (`PlaybackState.Actions` Bitmask)
Một số nguyên `long` (64-bit) biểu diễn danh sách năng lực và tính năng được app hỗ trợ, chuẩn hóa 1:1 theo `android.media.session.PlaybackState.Actions`:

* `ACTION_STOP = 1` (`1 << 0`) - Hỗ trợ dừng hẳn bài hát
* `ACTION_PAUSE = 2` (`1 << 1`) - Hỗ trợ tạm dừng
* `ACTION_PLAY = 4` (`1 << 2`) - Hỗ trợ tiếp tục phát
* `ACTION_REWIND = 8` (`1 << 3`) - Hỗ trợ tua lùi nhanh (10s/30s)
* `ACTION_SKIP_TO_PREVIOUS = 16` (`1 << 4`) - Hỗ trợ chuyển bài trước
* `ACTION_SKIP_TO_NEXT = 32` (`1 << 5`) - Hỗ trợ chuyển bài kế tiếp
* `ACTION_FAST_FORWARD = 64` (`1 << 6`) - Hỗ trợ tua tới nhanh
* `ACTION_SET_RATING = 128` (`1 << 7`) - Hỗ trợ người dùng đánh giá bài hát (Rating/Thích/Sao)
* `ACTION_SEEK_TO = 256` (`1 << 8`) - Hỗ trợ tua đến vị trí thời gian bất kỳ trên thanh tiến trình
* `ACTION_PLAY_PAUSE = 512` (`1 << 9`) - Hỗ trợ chuyển đổi nhanh Play/Pause
* `ACTION_PLAY_FROM_MEDIA_ID = 1024` (`1 << 10`) - Hỗ trợ phát qua ID bài hát
* `ACTION_PLAY_FROM_SEARCH = 2048` (`1 << 11`) - Hỗ trợ phát từ kết quả tìm kiếm
* `ACTION_SKIP_TO_QUEUE_ITEM = 4096` (`1 << 12`) - Hỗ trợ nhảy trực tiếp đến mục trong hàng đợi
* `ACTION_PLAY_FROM_URI = 8192` (`1 << 13`) - Hỗ trợ phát từ đường dẫn URI media
* `ACTION_PREPARE = 16384` (`1 << 14`) - Hỗ trợ chuẩn bị nguồn phát
* `ACTION_PREPARE_FROM_MEDIA_ID = 32768` (`1 << 15`) - Hỗ trợ chuẩn bị phát qua ID
* `ACTION_PREPARE_FROM_SEARCH = 65536` (`1 << 16`) - Hỗ trợ chuẩn bị từ tìm kiếm
* `ACTION_PREPARE_FROM_URI = 131072` (`1 << 17`) - Hỗ trợ chuẩn bị từ URI
* `ACTION_SET_PLAYBACK_SPEED = 4194304` (`1 << 22`) - Hỗ trợ thay đổi tốc độ phát lại (0.5x, 1.0x, 1.5x, 2.0x)
* `DEFAULT_ACTIONS = 822` - Bitmask mặc định chuẩn AOSP (Play + Pause + Play/Pause + SkipNext + SkipPrev + Seek).

> [!NOTE]
> **Quy tắc hiển thị giao diện theo Actions Bitmask trên Màn hình khóa:**
> - Nếu bit `ACTION_PLAY` (hoặc `ACTION_PLAY_PAUSE`) tắt khi đang tạm dừng: Nút Phát/Tạm dừng tự động mờ đi (`Opacity = 0.35`) và vô hiệu hóa tương tác cảm ứng.
> - Nếu bit `ACTION_PAUSE` (hoặc `ACTION_PLAY_PAUSE`) tắt khi đang phát: Nút Phát/Tạm dừng tự động mờ đi (`Opacity = 0.35`) và vô hiệu hóa tương tác cảm ứng.
> - Nếu bit `ACTION_SKIP_TO_PREVIOUS` tắt: Nút Lùi bài sẽ tự động mờ đi (`Opacity = 0.35`) và vô hiệu hóa tương tác cảm ứng.
> - Nếu bit `ACTION_SKIP_TO_NEXT` tắt: Nút Chuyển bài sẽ tự động mờ đi (`Opacity = 0.35`) và vô hiệu hóa tương tác cảm ứng.
> - Nếu bit `ACTION_SEEK_TO` tắt hoặc bài hát là chương trình trực tiếp (`DurationMs = 0`): Thanh tiến trình Seekbar tự động ẩn núm thumb và chuyển sang chế độ mờ (`Opacity = 0.45`), khóa thao tác kéo tua.

### 2.3 Các Chế Độ Bổ Trợ Chuẩn AOSP (Repeat, Shuffle, Rating, Output & Volume)

#### 2.3.1 Chế Độ Lặp Lại (`android.media.metadata.REPEAT_MODE`)
Hệ thống hỗ trợ chu trình chuyển đổi 3 nấc chuẩn AOSP:
* **0 - `REPEAT_MODE_NONE` ("none"):** Không lặp lại (icon xám mờ).
* **1 - `REPEAT_MODE_ONE` ("one"):** Lặp lại một bài hát hiện tại (icon vòng lặp số 1 `\uE1CC`, đổi màu xanh One UI `#70A6DE`).
* **2 - `REPEAT_MODE_ALL` ("all"):** Lặp lại toàn bộ danh sách phát (icon vòng lặp `\uE1CD`, đổi màu xanh One UI `#70A6DE`).
* **3 - `REPEAT_MODE_GROUP` ("group"):** Lặp lại theo nhóm danh sách.

#### 2.3.2 Chế Độ Trộn Bài (`android.media.metadata.SHUFFLE_MODE`)
* **0 - `SHUFFLE_MODE_NONE`:** Tắt chế độ xáo trộn bài.
* **1 - `SHUFFLE_MODE_ALL`:** Trộn ngẫu nhiên tất cả bài trong danh sách (icon mũi tên chéo `\uE148`, đổi màu xanh One UI `#70A6DE`).
* **2 - `SHUFFLE_MODE_GROUP`:** Trộn bài theo cụm/album.

#### 2.3.3 Kiểu Đánh Giá Nội Dung (`android.media.Rating`)
Ánh xạ các kiểu đánh giá bài hát chuẩn Android AOSP:
* **0 - `RATING_NONE`:** Không hỗ trợ đánh giá.
* **1 - `RATING_HEART`:** Đánh giá bằng Trái tim (`1.0` = Thích, `0.0` = Bỏ thích).
* **2 - `RATING_THUMB_UP_DOWN`:** Đánh giá Thích / Không thích (`1.0` = Thumbs Up, `-1.0` = Thumbs Down, `0.0` = Chưa đánh giá).
* **3 - `RATING_3_STARS`:** Đánh giá theo thang 1 - 3 sao (Slot 1 hiển thị icon ngôi sao `\uE1CF`/`\uE224` màu vàng One UI `#FFD700`).
* **4 - `RATING_4_STARS`:** Đánh giá theo thang 1 - 4 sao (icon ngôi sao màu vàng One UI `#FFD700`).
* **5 - `RATING_5_STARS`:** Đánh giá theo thang 1 - 5 sao (icon ngôi sao màu vàng One UI `#FFD700`).
* **6 - `RATING_PERCENTAGE`:** Đánh giá theo phần trăm từ `0.0` đến `100.0`.
* **`-1.0` - `RATING_NOT_RATED`:** Giá trị chuẩn khi bài hát chưa được đánh giá.

#### 2.3.4 Loại Thiết Bị Phát Lại (`MediaController.PlaybackInfo.getPlaybackType`)
* **1 - `PLAYBACK_TYPE_LOCAL`:** Phát âm thanh trực tiếp trên thiết bị (Loa máy / Tai nghe cắm dây).
* **2 - `PLAYBACK_TYPE_REMOTE`:** Truyền âm thanh ra thiết bị ngoại vi từ xa (Bluetooth, DLNA, Miracast, Google Cast).

#### 2.3.5 Điều Khiển Âm Lượng (`android.media.VolumeProvider`)
* **0 - `VOLUME_CONTROL_FIXED`:** Âm lượng cố định (không thể điều chỉnh từ xa).
* **1 - `VOLUME_CONTROL_RELATIVE`:** Điều chỉnh âm lượng theo từng nấc tăng/giảm tương đối.
* **2 - `VOLUME_CONTROL_ABSOLUTE`:** Điều chỉnh âm lượng trực tiếp đến giá trị tuyệt đối cụ thể (0 - 100).

#### 2.3.6 Cờ Phiên Làm Việc (`android.media.session.MediaSession.setFlags`)
* **`FLAG_HANDLES_MEDIA_BUTTONS = 1` (`1 << 0`):** Phiên nhận và điều khiển các phím media phần cứng hoặc phím bấm tai nghe.
* **`FLAG_HANDLES_TRANSPORT_CONTROLS = 2` (`1 << 1`):** Phiên hỗ trợ các lệnh điều khiển transport chuẩn (Play, Pause, Skip, Seek...).
* **`FLAG_EXCLUSIVE_GLOBAL_PRIORITY = 65536` (`1 << 16`):** Phiên chiếm quyền ưu tiên phát cao nhất trong toàn bộ hệ thống.
* Giá trị mặc định nếu app không chỉ định là `3` (`FLAG_HANDLES_MEDIA_BUTTONS | FLAG_HANDLES_TRANSPORT_CONTROLS`).

#### 2.3.7 Phân Cấp Thư Mục Bluetooth AVRCP 1.5 (`android.media.MediaDescription.BT_FOLDER_TYPE`)
Hỗ trợ duyệt phân cấp thư mục media chuẩn Bluetooth AVRCP:
* **0 - `BT_FOLDER_TYPE_MIXED`:** Thư mục hỗn hợp nhiều loại nội dung.
* **1 - `BT_FOLDER_TYPE_TITLES`:** Danh mục bài hát (Titles/Tracks).
* **2 - `BT_FOLDER_TYPE_ALBUMS`:** Danh mục album.
* **3 - `BT_FOLDER_TYPE_ARTISTS`:** Danh mục nghệ sĩ.
* **4 - `BT_FOLDER_TYPE_GENRES`:** Danh mục thể loại.
* **5 - `BT_FOLDER_TYPE_PLAYLISTS`:** Danh mục danh sách phát (Playlists).
* **6 - `BT_FOLDER_TYPE_YEARS`:** Danh mục năm phát hành.

#### 2.3.8 Kết Quả Xử Lý Phím Media (`android.media.session.MediaSessionManager`)
Mã phản hồi chuẩn khi điều phối sự kiện phím media:
* **0 - `RESULT_MEDIA_KEY_NOT_HANDLED`:** Sự kiện phím media không được xử lý hoặc bị bỏ qua.
* **1 - `RESULT_MEDIA_KEY_HANDLED`:** Sự kiện phím media đã được phiên media tiếp nhận và xử lý thành công.

---

## 3. Đặc Tả Dữ Liệu JSON (Data Schema Specification)

### Endpoint: `GET /api/track` (hoặc `GET /api/status`)
Trả về thông tin trạng thái bài hát hiện tại với header `Content-Type: application/json`.

```json
{
  "hasTrack": true,
  "appName": "Spotify",
  "packageName": "com.spotify.music",
  "launchUri": "spotify:album:12345",
  "appIcon": "http://olmp.local/api/icon",
  "title": "Seven (feat. Latto)",
  "artist": "Jung Kook",
  "album": "Golden",
  "author": "Andrew Watt",
  "writer": "Jon Bellion",
  "composer": "Andrew Watt",
  "albumArtist": "Jung Kook",
  "mediaUri": "http://olmp.local/stream/QU9c0053UAU",
  "thumbnailUrl": "http://olmp.local/cover.jpg",
  "trackId": "QU9c0053UAU",
  "durationMs": 184000,
  "positionMs": 42000,
  "bufferedPositionMs": 85000,
  "sessionFlags": 3,
  "state": 3,
  "speed": 1.0,
  "actions": 822,
  "isPlaying": true,
  "canSeek": true,
  "canSkipNext": true,
  "canSkipPrev": true,
  "audioOutput": "Điện thoại này",
  "repeatMode": 2,
  "shuffleMode": 0,
  "ratingStyle": 1,
  "ratingValue": 1.0,
  "subtitle": "Clean Ver.",
  "description": "Golden Album Edition",
  "genre": "Pop",
  "compilation": "Best of Pop",
  "date": "2023-07-14",
  "trackNumber": 1,
  "numTracks": 11,
  "year": 2023,
  "discNumber": 1,
  "queueTitle": "Top Hits Today",
  "activeQueueItemId": 101,
  "playbackType": 1,
  "volumeControl": 2,
  "volumeControlId": "device_speaker_0",
  "volume": 85,
  "maxVolume": 100,
  "errorMessage": "",
  "isFavorite": true,
  "isShuffle": false,
  "queue": [
    {
      "id": 101,
      "title": "Seven (feat. Latto)",
      "subtitle": "Jung Kook",
      "description": "Golden Album Edition",
      "mediaId": "QU9c0053UAU",
      "iconUri": "http://olmp.local/cover.jpg",
      "mediaUri": "http://olmp.local/stream/101",
      "btFolderType": 1
    }
  ],
  "customActions": [
    {
      "action": "android.media.action.FAVORITE",
      "name": "Favorite",
      "icon": "heart",
      "isActive": true
    },
    {
      "action": "android.media.action.SHUFFLE",
      "name": "Shuffle",
      "icon": "shuffle",
      "isActive": false
    }
  ]
}
```

### Bảng Chi Tiết Các Trường Dữ Liệu:

| Trường (Field) | Kiểu dữ liệu | Android Mapping | Mô tả & Lưu ý |
| :--- | :--- | :--- | :--- |
| `hasTrack` | `boolean` | `PlaybackState != NONE` | `true` nếu có bài hát đang nạp trong phiên. |
| `appName` | `string` | `PackageName` / App Label | Tên app phát nhạc (hiển thị khi artist trống). |
| `packageName` | `string` | App Package Name | Tên định danh gói ứng dụng Android/WP (alias: `package`). |
| `launchUri` | `string` | App Launch Intent URI | Đường dẫn URI khởi chạy ứng dụng hoặc trang phát bài hát (alias: `intentUri`, `appUri`). |
| `appIcon` | `string` | `METADATA_KEY_DISPLAY_ICON_URI` / `Icon.TYPE_URI` | URL/URI ảnh icon của ứng dụng phát nhạc (ví dụ `http://olmp.local/api/icon` hoặc `http://127.0.0.1:8088/api/icon`). Lock Screen sẽ tải trực tiếp từ URL này mà không đoán mò theo `appName`. (alias: `appIconUrl`, `displayIconUri`, `iconUri`). |
| `title` | `string` | `METADATA_KEY_TITLE` / `DISPLAY_TITLE` | Tiêu đề bài hát (alias: `displayTitle`, `trackName`). |
| `artist` | `string` | `METADATA_KEY_ARTIST` | Tên ca sĩ/nghệ sĩ biểu diễn (alias: `artistName`, `albumArtist`, `author`, `composer`). |
| `album` | `string` | `METADATA_KEY_ALBUM` | Tên album bài hát (alias: `albumTitle`). |
| `author` | `string` | `METADATA_KEY_AUTHOR` | Tác giả bài hát / tác phẩm. |
| `writer` | `string` | `METADATA_KEY_WRITER` | Tác giả viết lời bài hát. |
| `composer` | `string` | `METADATA_KEY_COMPOSER` | Nhạc sĩ sáng tác giai điệu bài hát. |
| `albumArtist` | `string` | `METADATA_KEY_ALBUM_ARTIST` | Nghệ sĩ chính của album. |
| `mediaUri` | `string` | `METADATA_KEY_MEDIA_URI` | URI đường dẫn luồng phát dữ liệu media bài hát. |
| `thumbnailUrl` | `string` | `METADATA_KEY_ALBUM_ART_URI` | Link ảnh bìa album (alias: `thumbnail`, `coverArt`, `albumArt`). |
| `trackId` | `string` | `METADATA_KEY_MEDIA_ID` | Mã ID bài hát (alias: `videoId`, `id`). |
| `durationMs` | `number` | `METADATA_KEY_DURATION` | Tổng thời lượng bài hát tính bằng **miligiây** (alias: `duration`). Nếu `0`: Nhận diện là luồng phát trực tiếp (Live Stream / Radio). |
| `positionMs` | `number` | `PlaybackState.mPosition` | Vị trí mốc ghi nhận bằng **miligiây** (alias: `position`). |
| `bufferedPositionMs`| `number`| `PlaybackState.mBufferedPosition` | Mốc thời gian đã tải đệm (miligiây) của luồng nhạc mạng (alias: `bufferedPosition`, `bufferedMs`, `bufferMs`). |
| `sessionFlags` | `number` | `MediaSession.setFlags / MediaController.getFlags` | Bitmask cờ năng lực phiên phát (1: Media Buttons, 2: Transport Controls, 65536: Global Priority, mặc định 3, alias: `flags`). |
| `state` | `number/string` | `PlaybackState.mState` | Mã trạng thái số nguyên (`3`: Playing, `2`: Paused, `6`: Buffering, `7`: Error...). |
| `speed` | `number` | `PlaybackState.mSpeed` | Tốc độ phát (mặc định `1.0`, alias: `playbackSpeed`). |
| `actions` | `number` | `PlaybackState.mActions` | Bitmask 64-bit năng lực điều khiển (mặc định `822`). |
| `isPlaying` | `boolean` | Legacy fallback | `true` nếu đang phát, `false` nếu tạm dừng. |
| `canSeek` | `boolean` | `ACTION_SEEK_TO` | Cho phép tua thanh tiến trình (`true`/`false`). |
| `canSkipNext` | `boolean` | `ACTION_SKIP_TO_NEXT` | Cho phép chuyển bài sau (`true`/`false`). |
| `canSkipPrev` | `boolean` | `ACTION_SKIP_TO_PREVIOUS` | Cho phép chuyển bài trước (`true`/`false`). |
| `audioOutput` | `string` | `METADATA_KEY_AUDIO_OUTPUT` | Tên thiết bị đầu ra âm thanh hiển thị ở góc card (ví dụ: `"Điện thoại này"`, `"Tai nghe Bluetooth"`, `"Google Cast"`...). (alias: `outputDevice`, `device`). |
| `repeatMode` | `number/string` | `METADATA_KEY_REPEAT_MODE` | Chế độ lặp lại 3 nấc: `0`: None, `1`: One, `2`: All (alias: `repeat`). |
| `shuffleMode` | `number` | `android.media.metadata.SHUFFLE_MODE` | Chế độ trộn bài: `0`: None, `1`: All, `2`: Group. |
| `ratingStyle` | `number` | `android.media.Rating.mRatingStyle` | Kiểu đánh giá bài hát: `1`: Heart, `2`: Thumb Up/Down, `3..5`: Star Rating. |
| `ratingValue` | `number` | `android.media.Rating.mRatingValue` | Giá trị đánh giá người dùng: `1.0` (Thích), `-1.0` (Không thích), `1..5` (Số sao). (alias: `userRating`, `rating`). |
| `subtitle` | `string` | `METADATA_KEY_DISPLAY_SUBTITLE` | Phụ đề / phiên bản (Remix, Acoustic...). Hiển thị dạng: `Subtitle • Artist`. |
| `description` | `string` | `METADATA_KEY_DISPLAY_DESCRIPTION` | Đoạn mô tả vắn tắt bài hát. |
| `genre` | `string` | `METADATA_KEY_GENRE` | Thể loại âm nhạc (Pop, Rock, EDM...). |
| `compilation` | `string` | `METADATA_KEY_COMPILATION` | Tên tuyển tập / album tổng hợp chứa bài hát. |
| `date` | `string` | `METADATA_KEY_DATE` | Ngày phát hành bài hát dạng chuỗi `"YYYY-MM-DD"`. |
| `trackNumber` | `number` | `METADATA_KEY_TRACK_NUMBER` | Số thứ tự bài trong album. Hiển thị dạng: `Artist (trackNumber/numTracks)`. |
| `numTracks` | `number` | `METADATA_KEY_NUM_TRACKS` | Tổng số bài trong album (alias: `trackCount`). |
| `year` | `number` | `METADATA_KEY_YEAR` | Năm phát hành bài hát (hoặc chuỗi `date` định dạng `"2023-..."`). |
| `discNumber` | `number` | `METADATA_KEY_DISC_NUMBER` | Số đĩa trong bộ đĩa album. |
| `queueTitle` | `string` | `METADATA_KEY_QUEUE_TITLE` | Tiêu đề danh sách phát / hàng đợi đang chạy (alias: `playlistTitle`). |
| `activeQueueItemId`| `number`| `PlaybackState.mActiveItemId` | ID của mục bài hát đang phát trong hàng đợi (alias: `activeItemId`). |
| `playbackType` | `number` | `MediaController.PlaybackInfo` | Loại ngõ phát: `1` = Local, `2` = Remote. |
| `volumeControl` | `number/string` | `VolumeProvider.mControlType` | Kiểu điều khiển âm lượng: `0`: Fixed, `1`: Relative, `2`: Absolute (alias: `fixed`, `relative`, `absolute`). |
| `volumeControlId`| `string` | `PlaybackInfo.getVolumeControlId()` | Định danh bộ điều khiển định tuyến âm lượng ngoại vi. |
| `volume` | `number` | `MediaController.PlaybackInfo.getVolume` | Mức âm lượng phát hiện tại (0 - 100). |
| `maxVolume` | `number` | `MediaController.PlaybackInfo.getMaxVol` | Mức âm lượng tối đa (thường là 100). |
| `errorMessage` | `string` | `PlaybackState.mErrorMessage` | Thông báo lỗi từ trình phát (alias: `error`, `message`). |
| `queue` | `array` | `MediaSession.getQueue()` | Mảng các mục bài hát trong hàng đợi (Xem mục 3.3). |
| `customActions` | `array` | `PlaybackState.getCustomActions()` | Mảng nút điều khiển tùy biến cho Slot 1 và Slot 5 (Xem mục 3.2). |
| `isFavorite` | `boolean` | `CustomAction.FAVORITE` | Tương thích ngược: Trạng thái yêu thích (nút Trái tim). |
| `isShuffle` | `boolean` | `CustomAction.SHUFFLE` | Tương thích ngược: Trạng thái phát trộn bài. |

---

### 3.1 Kiến Trúc Dynamic App Icon Chuẩn AOSP (Đọc Trực Tiếp Từ Thư Mục Assets/)

Cơ chế lấy icon ứng dụng hoạt động theo chuẩn AOSP (`METADATA_KEY_DISPLAY_ICON_URI` / `Icon.TYPE_URI`), hỗ trợ đầy đủ cho cả **WinRT (AppX)** lẫn **Silverlight (XAP)**:

* **Đối với Ứng dụng WinRT (Windows Phone 8.1 AppX):**
  - **Quy ước tệp:** Thường dùng `Assets/SmallLogo.scale-240.png`, `Assets/Square71x71Logo.scale-240.png`, `Assets/Logo.scale-240.png`.
  - **Cách đọc:** 
    - Cách 1: Đọc qua WinRT StorageFile: `StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/SmallLogo.scale-240.png"))`.
    - Cách 2: Đọc qua File I/O: `Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, @"Assets\SmallLogo.scale-240.png")` kết hợp `File.ReadAllBytes()`.

* **Đối với Ứng dụng Silverlight (Windows Phone 8.0 / 8.1 Silverlight XAP):**
  - **Quy ước tệp:** Thường dùng `Assets/ApplicationIcon.png`, `ApplicationIcon.png`, `Assets/Tiles/IconicTileSmall.png` hoặc `Assets/Tiles/FlipCycleTileSmall.png`.
  - **Cách đọc:**
    - Cách 1 (Chuẩn Silverlight truyền thống): Sử dụng `Application.GetResourceStream(new Uri("/Assets/ApplicationIcon.png", UriKind.Relative))` để đọc luồng dữ liệu (`Stream`) từ gói XAP rồi sao chép sang `MemoryStream` lấy mảng byte. Phương pháp này áp dụng được cho cả Build Action = `Content` lẫn `Resource`.
    - Cách 2 (File I/O trực tiếp): Trên Windows Phone 8.1 Silverlight, API `Windows.ApplicationModel.Package.Current.InstalledLocation.Path` vẫn hoạt động bình thường. Nếu tệp có Build Action là `Content`, app có thể đọc trực tiếp bằng `File.ReadAllBytes(Path.Combine(Package.Current.InstalledLocation.Path, @"Assets\ApplicationIcon.png"))`.

* **Điểm Chung & Nguyên Lý Bất Biến:**
  - **Tuyệt đối không dùng Base64:** Cả WinRT và Silverlight đều đọc trực tiếp từ tệp ảnh nhị phân nguyên bản trong thư mục dự án và chuyển thành `byte[]`.
  - **Cơ chế phân phối (URI Delivery):** Server nội bộ của app nhạc trả về file ảnh qua endpoint `GET /api/icon` với header `Content-Type: image/png` và cache trong RAM (`_cachedAppIconBytes`).
  - **Phân tách hoàn toàn (Platform-Agnostic):** Màn hình khóa (`Two UI Lock Screen`) hoạt động như một SystemUI / MediaController độc lập. Màn hình khóa **hoàn toàn không cần biết** app nhạc là WinRT hay Silverlight; nó chỉ tải ảnh qua HTTP từ URL được cấp trong trường `appIcon`. Nếu không lấy được icon, tự động fallback về glyph nốt nhạc Segoe UI Symbol (`\uE189`).

---

### 3.2 Kiến Trúc Nút Điều Khiển Tùy Biến (PlaybackState.CustomAction)

Tuân thủ kiến trúc Android Open Source Project (`PlaybackState.CustomAction`), dải điều khiển 5 nút trên Màn hình khóa One UI 6.1 được bố trí như sau:

$$\mathbf{[ \text{Slot 1: Nút Tùy Biến Trái} ]} \quad [ \text{Lùi Bài} ] \quad [ \text{Phát / Tạm Dừng} ] \quad [ \text{Chuyển Bài} ] \quad \mathbf{[ \text{Slot 5: Nút Tùy Biến Phải} ]}$$

App phát nhạc có thể toàn quyền cấu hình chức năng, tên và biểu tượng cho **Slot 1** và **Slot 5** thông qua mảng `customActions`:

```json
"customActions": [
  {
    "action": "android.media.action.FAVORITE",
    "name": "Yêu thích",
    "icon": "heart",
    "isActive": true
  },
  {
    "action": "android.media.action.REPEAT",
    "name": "Lặp lại",
    "icon": "repeat_one",
    "isActive": true
  }
]
```

#### Bảng Ánh Xạ Biểu Tượng Chuẩn (Standard Action Icons):

| Mã `icon` | Hành động tương đương | Trạng thái `isActive = false` | Trạng thái `isActive = true` |
| :--- | :--- | :--- | :--- |
| `heart` / `favorite` | Thích / Yêu thích | Tim viền trắng `\uE006` | Tim đặc đỏ `#FF4B4B` `\uE00B` |
| `shuffle` | Trộn bài ngẫu nhiên | Mũi tên chéo trắng `\uE148` | Mũi tên chéo xanh `#70A6DE` |
| `repeat` | Lặp lại toàn bộ danh sách | Vòng lặp trắng `\uE1CD` | Vòng lặp xanh `#70A6DE` |
| `repeat_one` | Lặp lại 1 bài hiện tại | Vòng lặp số 1 `\uE1CC` | Vòng lặp số 1 xanh `#70A6DE` |
| `thumbs_up` | Thích bài hát | Ngón cái lên trắng `\uE19F` | Ngón cái lên xanh `#70A6DE` |
| `thumbs_down` | Không thích | Ngón cái xuống trắng `\uE19E` | Ngón cái xuống đỏ `#FF4B4B` |
| `star` / `rating` | Đánh giá sao (AOSP Rating 3..5 Stars) | Sao viền trắng `\uE224` | Sao đặc vàng One UI `#FFD700` `\uE1CF` |
| `rewind` / `replay` | Tua lùi (Podcast/Sách nói) | Tua lùi 10s `\uE110` | Tua lùi 10s `\uE110` |
| `forward` / `fast_forward` | Tua nhanh (Podcast/Sách nói) | Tua tới 30s `\uE111` | Tua tới 30s `\uE111` |
| `queue` / `playlist`| Mở danh sách chờ | Danh sách `\uE14C` | Danh sách `\uE14C` |
| `stop` | Dừng hẳn phiên phát | Nút vuông `\uE15B` | Nút vuông `\uE15B` |
| `mute` / `volume` | Tắt / Mở âm lượng | Loa gạch chéo `\uE15D` | Loa gạch chéo đỏ `\uE15D` |

#### 3.2.1 Cơ Chế Render Biểu Tượng Đa Hình (Image URL & Glyph Vector)
Trường `icon` trong mỗi Custom Action hỗ trợ linh hoạt 3 cơ chế hiển thị:
1. **URL Ảnh Động Tùy Chỉnh (Custom Image Icon / HTTP URL):**
   - App phát nhạc có thể truyền đường dẫn HTTP/HTTPS URL trỏ đến endpoint phân phối icon nhị phân của chính app (ví dụ: `"icon": "http://127.0.0.1:8088/api/action_icon?name=custom"` hoặc chuỗi kết thúc bằng `.png`, `.jpg`).
   - Màn hình khóa tự động nạp ảnh qua `BitmapImage` hiển thị trên phần tử `Image` (`Width="18" Height="18" Stretch="Uniform"`), đồng thời ẩn `TextBlock` glyph.
   - Quản lý bộ nhớ nghiêm ngặt: giải phóng `ImageSource = null` ngay khi dừng phát nhạc hoặc hủy trang (`DisposeMusicWidget`) để tránh rò rỉ VRAM trên thiết bị 512MB RAM.
2. **Ký Tự Unicode Trực Tiếp (System Vector Glyph):**
   - Hỗ trợ truyền chuỗi ký tự Unicode của font hệ thống Segoe UI Symbol (ví dụ: `"\uE156"`, `"\uE10A"`). Màn hình khóa sẽ render vector sắc nét trên `TextBlock`.
3. **Từ Khóa Định Danh Chuẩn (Standard Keyword):**
   - Nhận diện các từ khóa chuẩn trong bảng trên (`heart`, `favorite`, `shuffle`, `repeat`, `repeat_one`, `thumbs_up`, `thumbs_down`, `star`, `rewind`, `forward`, `queue`, `stop`...) và tự động chuyển đổi màu sắc theo `isActive` (xanh One UI `#70A6DE`, đỏ `#FF4B4B`, vàng sao `#FFD700`, hoặc trắng `#FFFFFE`).

* **Gửi lệnh tương tác:** Khi người dùng bấm vào nút tùy biến, Màn hình khóa gọi endpoint:
  $$\text{GET } /api/action?name=\{action\}\&value=\{nextState\}$$
* **Cơ Chế Dự Phòng Thông Minh (Smart Fallback Architecture):**
  - Nếu app không gửi mảng `customActions`:
    - **Slot 1 (Bên trái):**
      - Nếu `ratingStyle == RATING_THUMB_UP_DOWN`: Tự động fallback thành `thumbs_up` (ngón cái).
      - Nếu `ratingStyle` nằm trong thang sao (`RATING_3_STARS` .. `RATING_5_STARS`): Tự động fallback thành `star` (ngôi sao vàng `#FFD700`).
      - Mặc định: Fallback thành `heart` (Trái tim yêu thích) dựa theo cờ `isFavorite`.
    - **Slot 5 (Bên phải):**
      - Nếu `repeatMode != REPEAT_MODE_NONE`: Tự động fallback thành `repeat` hoặc `repeat_one` màu xanh One UI.
      - Mặc định: Fallback thành `shuffle` (Trộn bài) dựa theo cờ `isShuffle`.
  - Hỗ trợ nhận diện cả hai đối tượng riêng lẻ `"customActionLeft": { ... }` và `"customActionRight": { ... }` trong payload JSON để tạo sự thuận tiện tối đa cho lập trình viên.

---

### 3.3 Cấu Trúc Hàng Đợi Danh Sách Bài Hát (MediaSession.QueueItem)

Chuẩn hóa 1:1 theo `android.media.session.MediaSession.QueueItem`, danh sách các bài hát trong hàng đợi được truyền qua mảng `queue`:

```json
"queue": [
  {
    "id": 101,
    "title": "Seven (feat. Latto)",
    "subtitle": "Jung Kook",
    "description": "Clean Ver.",
    "mediaId": "QU9c0053UAU",
    "iconUri": "http://olmp.local/cover.jpg",
    "mediaUri": "http://olmp.local/stream/101",
    "btFolderType": 1
  }
]
```

Mỗi phần tử trong hàng đợi bao gồm:
* `id` (`long`): Định danh duy nhất của mục bài hát trong hàng đợi (sử dụng với lệnh `/api/queue/skip?id={id}`, giá trị chưa xác định là `UNKNOWN_ID = -1`).
* `title` (`string`): Tiêu đề hiển thị của mục trong hàng đợi (`MediaDescription.getTitle()`).
* `subtitle` (`string`): Phụ đề hoặc nghệ sĩ của mục (`MediaDescription.getSubtitle()`).
* `description` (`string`): Đoạn mô tả vắn tắt bài hát (`MediaDescription.getDescription()`).
* `mediaId` (`string`): Mã định danh media bài hát (`MediaDescription.getMediaId()`).
* `iconUri` (`string`): URL ảnh đại diện của bài hát trong hàng đợi (`MediaDescription.getIconUri()`).
* `mediaUri` (`string`): URI đường dẫn luồng phát dữ liệu media (`MediaDescription.getMediaUri()`).
* `btFolderType` (`long`): Loại phân cấp thư mục duyệt media chuẩn Bluetooth AVRCP 1.5 (`0` = Mixed, `1` = Titles, `2` = Albums...).

---

## 4. Thuật Toán Đồng Bộ Thanh Tiến Trình Thời Gian Thực

Lock Screen áp dụng chính xác công thức toán học nội suy từ `PlaybackState.java` của Google:
$$\text{CurrentPosition} = \text{BasePosition} + \text{PlaybackSpeed} \times (\text{CurrentTime} - \text{UpdateTime})$$

* **Nguyên lý:** App phát nhạc **không cần gửi vị trí liên tục mỗi giây** (tránh nghẽn mạng). 
* Lock Screen sẽ tự động tính toán thời gian trôi qua dựa trên `speed` và `UpdateTime`. Kim thời gian trên Màn hình khóa sẽ trôi êm ái từng giây độc lập với chu kỳ mạng.

---

## 5. Danh Sách Các Lệnh Điều Khiển (Transport Controls Endpoints)

| Endpoint | Tham số (Query) | Hành động tương đương Android | Phản hồi (Response) |
| :--- | :--- | :--- | :--- |
| `GET /api/track` (hoặc `/api/status`) | Không | `MediaController.getMetadata()` | `200 OK` (JSON trạng thái bài hát & metadata) |
| `GET /api/icon` | Không | `ContentResolver.openInputStream(iconUri)` | `200 OK` (Ảnh PNG nhị phân đọc từ `Assets/` gốc của app) |
| `GET /api/play` | `?mediaId={id}` (hoặc `?uri={uri}`, `?query={q}`) | `TransportControls.play()`, `playFromMediaId()`, `playFromUri()`, `playFromSearch()` | `200 OK` (kèm JSON mới) |
| `GET /api/pause` | Không | `TransportControls.pause()` | `200 OK` (kèm JSON mới) |
| `GET /api/next` | Không | `TransportControls.skipToNext()` | `200 OK` |
| `GET /api/prev` | Không | `TransportControls.skipToPrevious()` | `200 OK` |
| `GET /api/seek` | `?pos={ms}` (hoặc `?ms={ms}`) | `TransportControls.seekTo(pos)` | `200 OK` |
| `GET /api/fastforward` | Không | `TransportControls.fastForward()` | `200 OK` |
| `GET /api/rewind` | Không | `TransportControls.rewind()` | `200 OK` |
| `GET /api/stop` | Không | `TransportControls.stop()` | `200 OK` |
| `GET /api/speed` | `?value={float}` | `TransportControls.setPlaybackSpeed(speed)` | `200 OK` |
| `GET /api/repeat` | `?mode={int}&value={none\|all\|one}` | Cập nhật RepeatMode 3 nấc AOSP | `200 OK` |
| `GET /api/shuffle` | `?mode={int}&value={true\|false}` | Cập nhật ShuffleMode AOSP | `200 OK` |
| `GET /api/rating` | `?style={int}&value={float}` | `TransportControls.setRating(rating)` | `200 OK` |
| `GET /api/queue/skip` | `?id={long}` | `TransportControls.skipToQueueItem(id)` | `200 OK` |
| `GET /api/volume` | `?value={int}` | `MediaController.setVolumeTo(vol)` | `200 OK` |
| `GET /api/volume/adjust` | `?direction={dir}` | `MediaController.adjustVolume(dir)` (`1`: Raise, `-1`: Lower, `0`: Same, `-100`: Mute, `100`: Unmute, `101`: Toggle Mute) | `200 OK` |
| `GET /api/prepare` | `?mediaId={id}` (hoặc `?uri={uri}`, `?query={q}`) | `TransportControls.prepare()`, `prepareFromMediaId()`, `prepareFromUri()`, `prepareFromSearch()` | `200 OK` |
| `GET /api/action` | `?name={action}&value={val}` | `TransportControls.sendCustomAction(name, args)` | `200 OK` |
| `GET /api/favorite`| `?value={true\|false}` | Tương thích ngược: Yêu thích | `200 OK`

> [!TIP]
> **Quy ước định dạng URL gọi lệnh:** Mọi endpoint điều khiển trong bảng trên có thể được truy xuất thông qua tên miền ảo thân thiện **`http://olmp.local/api/...`** (cấu hình mặc định của Lock Screen) hoặc IP loopback trực tiếp **`http://127.0.0.1:8088/api/...`**. Cơ chế `ResolveNetworkUrl` của Màn hình khóa đảm bảo cả hai định dạng đều được phân giải tức thì về cổng socket 8088 của ứng dụng phát nhạc.

---

## 6. Mã Nguồn Mẫu Cho Lập Trình Viên App Nhạc (C# MiniMusicServer.cs)

> [!IMPORTANT]
> **Đặc điểm nền tảng Windows Phone:** Thư viện BCL của Windows Phone 8.1 (cả Silverlight lẫn WinRT) **không có `System.Net.HttpListener`**. Do đó, giải pháp chuẩn mực, nhẹ nhất và tương thích 100% trên cả **Silverlight (XAP)** lẫn **WinRT (AppX)** là sử dụng **`Windows.Networking.Sockets.StreamSocketListener`** (tích hợp sẵn trong `Windows.winmd`).

Dưới đây là một lớp C# độc lập chuẩn mực nhúng sẵn vào bất kỳ app phát nhạc nào trên Windows Phone 8.1:

```csharp
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MyMusicApp
{
    /// <summary>
    /// Mini HTTP Socket Server chuẩn OLMP v2.0 dành cho Windows Phone 8.1.
    /// Hoạt động 100% trên cả Silverlight (XAP) lẫn WinRT (AppX).
    /// </summary>
    public class MiniMusicServer : IDisposable
    {
        private static MiniMusicServer _instance;
        public static MiniMusicServer Instance => _instance ?? (_instance = new MiniMusicServer());

        private StreamSocketListener _listener;
        private bool _isRunning;
        private int _port = 8088;

        // Trạng thái theo chuẩn Android PlaybackState & MediaMetadata
        public string AppName = "MyMusicApp";
        public string AppIcon = "http://olmp.local/api/icon";
        public string Title = "Tên Bài Hát";
        public string Artist = "Tên Nghệ Sĩ";
        public string Album = "Tên Album";
        public string Author = "";
        public string Writer = "";
        public string Composer = "";
        public string AlbumArtist = "";
        public string MediaUri = "";
        public string ThumbnailUrl = "";
        public string TrackId = "1";
        public long DurationMs = 210000;
        public long PositionMs = 0;
        public long BufferedPositionMs = 0;
        public int State = 2; // 3 = Playing, 2 = Paused, 6 = Buffering, 7 = Error
        public float Speed = 1.0f;
        public long Actions = 822; // Play, Pause, Next, Prev, Seek...
        public string AudioOutput = "Điện thoại này";
        public int RepeatMode = 0; // 0: None, 1: One, 2: All
        public int ShuffleMode = 0; // 0: None, 1: All
        public int RatingStyle = 1; // 1: Heart, 2: Thumbs, 3..5: Stars
        public float RatingValue = 0.0f;
        public string Subtitle = "";
        public string Description = "";
        public string Genre = "";
        public int TrackNumber = 1;
        public int NumTracks = 1;
        public int Year = 2024;
        public int DiscNumber = 1;
        public string QueueTitle = "";
        public long ActiveQueueItemId = -1;
        public int PlaybackType = 1; // 1: Local, 2: Remote
        public int Volume = 100;
        public int MaxVolume = 100;
        public string ErrorMessage = "";
        public bool IsFavorite = false;
        public bool IsShuffle = false;
        public string PackageName = "com.spotify.music";
        public string LaunchUri = "";
        public string Compilation = "";
        public string Date = "2024-01-01";
        public int VolumeControl = 2; // 0: Fixed, 1: Relative, 2: Absolute
        public string VolumeControlId = "device_speaker_0";
        public int SessionFlags = 3; // FLAG_HANDLES_MEDIA_BUTTONS (1) | FLAG_HANDLES_TRANSPORT_CONTROLS (2)
        public System.Collections.Generic.List<QueueItemModel> Queue = new System.Collections.Generic.List<QueueItemModel>();

        // Các sự kiện callback điều khiển phát nhạc
        public event Action OnPlayRequested;
        public event Action<string> OnPlayFromMediaIdRequested;
        public event Action<string> OnPlayFromUriRequested;
        public event Action<string> OnPlayFromSearchRequested;
        public event Action OnPauseRequested;
        public event Action OnNextRequested;
        public event Action OnPrevRequested;
        public event Action<long> OnSeekRequested;
        public event Action OnFastForwardRequested;
        public event Action OnRewindRequested;
        public event Action OnStopRequested;
        public event Action OnPrepareRequested;
        public event Action<string> OnPrepareFromMediaIdRequested;
        public event Action<string> OnPrepareFromSearchRequested;
        public event Action<string> OnPrepareFromUriRequested;
        public event Action<float> OnSpeedRequested;
        public event Action<int> OnRepeatModeRequested;
        public event Action<int> OnShuffleModeRequested;
        public event Action<int, float> OnRatingRequested;
        public event Action<long> OnSkipToQueueItemRequested;
        public event Action<int> OnVolumeRequested;
        public event Action<int> OnAdjustVolumeRequested;
        public event Action<string, string> OnCustomActionRequested;

        public async Task StartAsync(int port = 8088)
        {
            if (_isRunning) return;
            _port = port;
            try
            {
                _listener = new StreamSocketListener();
                _listener.ConnectionReceived += OnConnectionReceived;
                await _listener.BindServiceNameAsync(_port.ToString());
                _isRunning = true;
                System.Diagnostics.Debug.WriteLine("[MiniMusicServer] Started on port " + _port);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[MiniMusicServer] Start error: " + ex.Message);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_listener != null)
            {
                try { _listener.Dispose(); } catch { }
                _listener = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private async void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            if (!_isRunning) return;
            var socket = args.Socket;
            DataReader reader = null;
            DataWriter writer = null;

            try
            {
                reader = new DataReader(socket.InputStream);
                writer = new DataWriter(socket.OutputStream);
                reader.InputStreamOptions = InputStreamOptions.Partial;
                await reader.LoadAsync(2048);
                if (reader.UnconsumedBufferLength == 0) return;

                string request = reader.ReadString(reader.UnconsumedBufferLength);
                string firstLine = "";
                int lineEnd = request.IndexOfAny(new char[] { '\r', '\n' });
                firstLine = lineEnd > 0 ? request.Substring(0, lineEnd).Trim() : request.Trim();

                string[] parts = firstLine.Split(' ');
                string rawUrl = parts.Length > 1 ? parts[1] : "/";

                string path = rawUrl;
                string query = "";
                int qIdx = rawUrl.IndexOf('?');
                if (qIdx >= 0)
                {
                    path = rawUrl.Substring(0, qIdx);
                    query = rawUrl.Substring(qIdx + 1);
                }
                path = path.ToLowerInvariant();

                if (path == "/api/track" || path == "/api/status")
                {
                    await SendJsonResponseAsync(writer, BuildTrackJson());
                }
                else if (path == "/api/icon")
                {
                    byte[] iconBytes = GetAppIconBytes();
                    if (iconBytes != null && iconBytes.Length > 0)
                    {
                        string headers = "HTTP/1.1 200 OK\r\n" +
                                         "Content-Type: image/png\r\n" +
                                         "Cache-Control: public, max-age=86400\r\n" +
                                         "Connection: close\r\n" +
                                         "Content-Length: " + iconBytes.Length + "\r\n\r\n";
                        writer.WriteString(headers);
                        writer.WriteBytes(iconBytes);
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                    }
                    else
                    {
                        string notFound = "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                        writer.WriteString(notFound);
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                    }
                }
                else if (path == "/api/play")
                {
                    State = 3;
                    string mId = ExtractQueryParam(query, "mediaId");
                    string mUri = ExtractQueryParam(query, "uri");
                    string mQuery = ExtractQueryParam(query, "query") ?? ExtractQueryParam(query, "q");
                    if (!string.IsNullOrEmpty(mId)) OnPlayFromMediaIdRequested?.Invoke(mId);
                    else if (!string.IsNullOrEmpty(mUri)) OnPlayFromUriRequested?.Invoke(mUri);
                    else if (!string.IsNullOrEmpty(mQuery)) OnPlayFromSearchRequested?.Invoke(mQuery);
                    else OnPlayRequested?.Invoke();
                    await SendJsonResponseAsync(writer, BuildTrackJson());
                }
                else if (path == "/api/pause")
                {
                    State = 2;
                    OnPauseRequested?.Invoke();
                    await SendJsonResponseAsync(writer, BuildTrackJson());
                }
                else if (path == "/api/next")
                {
                    OnNextRequested?.Invoke();
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/prev")
                {
                    OnPrevRequested?.Invoke();
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/seek")
                {
                    string posStr = ExtractQueryParam(query, "pos") ?? ExtractQueryParam(query, "ms");
                    long pos = 0;
                    if (!string.IsNullOrEmpty(posStr) && long.TryParse(posStr, out pos))
                    {
                        PositionMs = pos;
                        OnSeekRequested?.Invoke(pos);
                    }
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/fastforward")
                {
                    OnFastForwardRequested?.Invoke();
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/rewind")
                {
                    OnRewindRequested?.Invoke();
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/stop")
                {
                    State = 1;
                    OnStopRequested?.Invoke();
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/speed")
                {
                    string speedStr = ExtractQueryParam(query, "value") ?? ExtractQueryParam(query, "speed");
                    float spd = 1.0f;
                    if (!string.IsNullOrEmpty(speedStr) && float.TryParse(speedStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out spd))
                    {
                        Speed = spd;
                        OnSpeedRequested?.Invoke(spd);
                    }
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/repeat")
                {
                    string modeStr = ExtractQueryParam(query, "mode");
                    int repMode = 0;
                    if (!string.IsNullOrEmpty(modeStr) && int.TryParse(modeStr, out repMode))
                    {
                        RepeatMode = repMode;
                    }
                    else
                    {
                        string val = (ExtractQueryParam(query, "value") ?? "").ToLowerInvariant();
                        if (val == "one") RepeatMode = 1;
                        else if (val == "all") RepeatMode = 2;
                        else RepeatMode = 0;
                    }
                    OnRepeatModeRequested?.Invoke(RepeatMode);
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/shuffle")
                {
                    string modeStr = ExtractQueryParam(query, "mode");
                    int shuffMode = 0;
                    if (!string.IsNullOrEmpty(modeStr) && int.TryParse(modeStr, out shuffMode))
                    {
                        ShuffleMode = shuffMode;
                        IsShuffle = (shuffMode != 0);
                    }
                    else
                    {
                        string val = ExtractQueryParam(query, "value") ?? "";
                        IsShuffle = (val == "true");
                        ShuffleMode = IsShuffle ? 1 : 0;
                    }
                    OnShuffleModeRequested?.Invoke(ShuffleMode);
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/rating")
                {
                    int style = 1;
                    float val = 0.0f;
                    string styleStr = ExtractQueryParam(query, "style");
                    string valStr = ExtractQueryParam(query, "value");
                    if (!string.IsNullOrEmpty(styleStr)) int.TryParse(styleStr, out style);
                    if (!string.IsNullOrEmpty(valStr)) float.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out val);
                    RatingStyle = style;
                    RatingValue = val;
                    if (style == 1) IsFavorite = (val > 0.0f);
                    OnRatingRequested?.Invoke(style, val);
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/queue/skip")
                {
                    string idStr = ExtractQueryParam(query, "id");
                    long id = 0;
                    if (!string.IsNullOrEmpty(idStr) && long.TryParse(idStr, out id))
                    {
                        ActiveQueueItemId = id;
                        OnSkipToQueueItemRequested?.Invoke(id);
                    }
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/volume")
                {
                    string volStr = ExtractQueryParam(query, "value");
                    int vol = 100;
                    if (!string.IsNullOrEmpty(volStr) && int.TryParse(volStr, out vol))
                    {
                        Volume = vol;
                        OnVolumeRequested?.Invoke(vol);
                    }
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/volume/adjust")
                {
                    string dirStr = ExtractQueryParam(query, "direction");
                    int dir = 0;
                    if (!string.IsNullOrEmpty(dirStr) && int.TryParse(dirStr, out dir))
                    {
                        OnAdjustVolumeRequested?.Invoke(dir);
                    }
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/prepare")
                {
                    string mId = ExtractQueryParam(query, "mediaId");
                    string mUri = ExtractQueryParam(query, "uri");
                    string mQuery = ExtractQueryParam(query, "query") ?? ExtractQueryParam(query, "q");
                    if (!string.IsNullOrEmpty(mId)) OnPrepareFromMediaIdRequested?.Invoke(mId);
                    else if (!string.IsNullOrEmpty(mUri)) OnPrepareFromUriRequested?.Invoke(mUri);
                    else if (!string.IsNullOrEmpty(mQuery)) OnPrepareFromSearchRequested?.Invoke(mQuery);
                    else OnPrepareRequested?.Invoke();
                    await SendTextResponseAsync(writer, "OK");
                }
                else if (path == "/api/action" || path == "/api/favorite")
                {
                    string actionName = ExtractQueryParam(query, "name");
                    if (string.IsNullOrEmpty(actionName))
                    {
                        if (path == "/api/favorite") actionName = "android.media.action.FAVORITE";
                        else actionName = "custom";
                    }
                    string actionVal = ExtractQueryParam(query, "value") ?? ExtractQueryParam(query, "val") ?? "";
                    if (actionName == "android.media.action.FAVORITE") IsFavorite = (actionVal == "true");

                    OnCustomActionRequested?.Invoke(actionName, actionVal);
                    await SendTextResponseAsync(writer, "OK");
                }
                else
                {
                    string notFound = "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                    writer.WriteString(notFound);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[MiniMusicServer] Client error: " + ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    try { reader.DetachStream(); reader.Dispose(); } catch { }
                }
                if (writer != null)
                {
                    try { writer.DetachStream(); writer.Dispose(); } catch { }
                }
                if (socket != null)
                {
                    try { socket.Dispose(); } catch { }
                }
            }
        }

        private string BuildTrackJson()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{{\"hasTrack\":true,\"appName\":\"{0}\",\"packageName\":\"{1}\",\"launchUri\":\"{2}\",\"appIcon\":\"{3}\",\"title\":\"{4}\",\"artist\":\"{5}\",\"album\":\"{6}\",\"author\":\"{7}\",\"writer\":\"{8}\",\"composer\":\"{9}\",\"albumArtist\":\"{10}\",\"mediaUri\":\"{11}\",\"thumbnailUrl\":\"{12}\",\"trackId\":\"{13}\",\"durationMs\":{14},\"positionMs\":{15},\"bufferedPositionMs\":{16},\"sessionFlags\":{17},\"state\":{18},\"speed\":{19:F2},\"actions\":{20},\"isPlaying\":{21},\"canSeek\":true,\"canSkipNext\":true,\"canSkipPrev\":true,\"audioOutput\":\"{22}\",\"repeatMode\":{23},\"shuffleMode\":{24},\"ratingStyle\":{25},\"ratingValue\":{26:F2},\"subtitle\":\"{27}\",\"description\":\"{28}\",\"genre\":\"{29}\",\"compilation\":\"{30}\",\"date\":\"{31}\",\"trackNumber\":{32},\"numTracks\":{33},\"year\":{34},\"discNumber\":{35},\"queueTitle\":\"{36}\",\"activeQueueItemId\":{37},\"playbackType\":{38},\"volumeControl\":{39},\"volumeControlId\":\"{40}\",\"volume\":{41},\"maxVolume\":{42},\"errorMessage\":\"{43}\",\"isFavorite\":{44},\"isShuffle\":{45}}}",
                EscapeJson(AppName), EscapeJson(PackageName), EscapeJson(LaunchUri), EscapeJson(AppIcon), EscapeJson(Title), EscapeJson(Artist), EscapeJson(Album),
                EscapeJson(Author), EscapeJson(Writer), EscapeJson(Composer), EscapeJson(AlbumArtist), EscapeJson(MediaUri),
                EscapeJson(ThumbnailUrl), EscapeJson(TrackId), DurationMs, PositionMs, BufferedPositionMs, SessionFlags,
                State, Speed, Actions, (State == 3 ? "true" : "false"), EscapeJson(AudioOutput),
                RepeatMode, ShuffleMode, RatingStyle, RatingValue, EscapeJson(Subtitle),
                EscapeJson(Description), EscapeJson(Genre), EscapeJson(Compilation), EscapeJson(Date), TrackNumber, NumTracks, Year,
                DiscNumber, EscapeJson(QueueTitle), ActiveQueueItemId, PlaybackType, VolumeControl, EscapeJson(VolumeControlId), Volume,
                MaxVolume, EscapeJson(ErrorMessage), (IsFavorite ? "true" : "false"), (IsShuffle ? "true" : "false"));
        }

        private static byte[] _cachedAppIconBytes;
        private static byte[] GetAppIconBytes()
        {
            if (_cachedAppIconBytes != null) return _cachedAppIconBytes;
            string[] candidateFiles = new string[]
            {
                // WinRT AppX Icons
                @"Assets\SmallLogo.scale-240.png",
                @"Assets\Square71x71Logo.scale-240.png",
                @"Assets\Logo.scale-240.png",
                @"Assets\SmallLogo.scale-100.png",
                @"Assets\Square71x71Logo.scale-100.png",
                @"Assets\Logo.scale-100.png",
                @"Assets\SmallLogo.png",
                @"Assets\Square71x71Logo.png",
                @"Assets\Logo.png",
                // Silverlight XAP Icons
                @"Assets\ApplicationIcon.png",
                @"ApplicationIcon.png",
                @"Assets\Tiles\IconicTileSmall.png",
                @"Assets\Tiles\FlipCycleTileSmall.png",
                @"Assets\Icon.png",
                @"Assets\AppIcon.png"
            };
            foreach (var relPath in candidateFiles)
            {
                try
                {
                    string fullPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, relPath);
                    if (File.Exists(fullPath))
                    {
                        _cachedAppIconBytes = File.ReadAllBytes(fullPath);
                        return _cachedAppIconBytes;
                    }
                }
                catch { }
            }
            return null;
        }

        private static string ExtractQueryParam(string query, string key)
        {
            if (string.IsNullOrEmpty(query)) return null;
            string[] pairs = query.Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] kv = pairs[i].Split('=');
                if (kv.Length == 2 && string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase))
                {
                    return kv[1];
                }
            }
            return null;
        }

        private static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        private static async Task SendJsonResponseAsync(DataWriter writer, string json)
        {
            byte[] buf = Encoding.UTF8.GetBytes(json);
            string headers = "HTTP/1.1 200 OK\r\n" +
                             "Content-Type: application/json; charset=utf-8\r\n" +
                             "Cache-Control: no-cache, no-store\r\n" +
                             "Connection: close\r\n" +
                             "Content-Length: " + buf.Length + "\r\n\r\n";
            writer.WriteString(headers);
            writer.WriteBytes(buf);
            await writer.StoreAsync();
            await writer.FlushAsync();
        }

        private static async Task SendTextResponseAsync(DataWriter writer, string text)
        {
            byte[] buf = Encoding.UTF8.GetBytes(text);
            string headers = "HTTP/1.1 200 OK\r\n" +
                             "Content-Type: text/plain; charset=utf-8\r\n" +
                             "Connection: close\r\n" +
                             "Content-Length: " + buf.Length + "\r\n\r\n";
            writer.WriteString(headers);
            writer.WriteBytes(buf);
            await writer.StoreAsync();
            await writer.FlushAsync();
        }
    }
}
```

---

## 7. Hướng Dẫn Tích Hợp Vào Dự Án Nhạc

### 7.1 Đối Với Dự Án Silverlight (Windows Phone 8.0 / 8.1 Silverlight)
Trong ứng dụng Silverlight, phát nhạc nền được quản lý bởi `AudioPlayerAgent` (`Microsoft.Phone.BackgroundAudio.AudioPlayerAgent`):
1. **Khởi động Server:** Gọi `MiniMusicServer.Instance.StartAsync();` khi `AudioPlayerAgent` được khởi tạo hoặc khi bài hát bắt đầu phát.
2. **Cập nhật Metadata:** Trong sự kiện `OnPlayStateChanged` hoặc khi chuyển bài, gán các thuộc tính:
   ```csharp
   MiniMusicServer.Instance.Title = currentTrack.Title;
   MiniMusicServer.Instance.Artist = currentTrack.Artist;
   MiniMusicServer.Instance.Album = currentTrack.Album;
   MiniMusicServer.Instance.DurationMs = (long)currentTrack.Duration.TotalMilliseconds;
   MiniMusicServer.Instance.PositionMs = (long)BackgroundAudioPlayer.Instance.Position.TotalMilliseconds;
   MiniMusicServer.Instance.BufferedPositionMs = (long)currentTrack.Duration.TotalMilliseconds;
   MiniMusicServer.Instance.AudioOutput = "Điện thoại này";
   MiniMusicServer.Instance.RepeatMode = 0; // None / One / All
   MiniMusicServer.Instance.ShuffleMode = 0; // None / All
   MiniMusicServer.Instance.RatingStyle = 1; // 1: Heart
   MiniMusicServer.Instance.State = BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing ? 3 : 2;
   ```
3. **Đăng ký sự kiện điều khiển:**
   ```csharp
   MiniMusicServer.Instance.OnPlayRequested += () => BackgroundAudioPlayer.Instance.Play();
   MiniMusicServer.Instance.OnPauseRequested += () => BackgroundAudioPlayer.Instance.Pause();
   MiniMusicServer.Instance.OnNextRequested += () => BackgroundAudioPlayer.Instance.SkipNext();
   MiniMusicServer.Instance.OnPrevRequested += () => BackgroundAudioPlayer.Instance.SkipPrevious();
   MiniMusicServer.Instance.OnSeekRequested += (pos) => BackgroundAudioPlayer.Instance.Position = TimeSpan.FromMilliseconds(pos);
   MiniMusicServer.Instance.OnFastForwardRequested += () => {
       BackgroundAudioPlayer.Instance.Position += TimeSpan.FromSeconds(30);
   };
   MiniMusicServer.Instance.OnRewindRequested += () => {
       BackgroundAudioPlayer.Instance.Position -= TimeSpan.FromSeconds(10);
   };
   MiniMusicServer.Instance.OnStopRequested += () => BackgroundAudioPlayer.Instance.Stop();
   MiniMusicServer.Instance.OnRepeatModeRequested += (mode) => {
       // Cập nhật chế độ lặp lại của trình phát
   };
   MiniMusicServer.Instance.OnShuffleModeRequested += (mode) => {
       // Cập nhật chế độ trộn bài của danh sách phát
   };
   MiniMusicServer.Instance.OnRatingRequested += (style, val) => {
       // Cập nhật đánh giá bài hát (Thích / Sao)
   };
   ```

### 7.2 Đối Với Dự Án WinRT (Windows Phone 8.1 Universal App)
Trong ứng dụng WinRT, phát nhạc nền được quản lý qua `BackgroundMediaPlayer` và một lớp kế thừa `IBackgroundTask`:
1. **Khởi động Server:** Gọi `MiniMusicServer.Instance.StartAsync();` trong phương thức `Run(IBackgroundTaskInstance taskInstance)` (tương tự như `AudioTask.cs` của YTMusicWP).
2. **Đăng ký sự kiện điều khiển:** Lắng nghe callback tương tự để gọi `BackgroundMediaPlayer.Current.Play()`, `Pause()`, `Position = TimeSpan.FromMilliseconds(pos)`, thiết lập volume, seek, custom action...
3. **Cập nhật Metadata:** Định kỳ hoặc khi bài hát thay đổi, gán `MiniMusicServer.Instance` với các thuộc tính tương ứng và phát sinh sự kiện thông báo để Màn hình khóa lập tức nhận diện trạng thái mới nhất.
