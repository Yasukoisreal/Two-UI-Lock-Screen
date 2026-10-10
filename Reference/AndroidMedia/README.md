# Android Media Framework Reference (AOSP Original Java Sources)

Thư mục này chứa toàn bộ các file mã nguồn Java gốc trích xuất trực tiếp từ dự án **Android Open Source Project (AOSP)** của Google (`frameworks/base/media/`). 

Các file này được lưu trữ phục vụ cho việc tra cứu đối chiếu chuẩn xác tuyệt đối (1:1) giữa giao thức Màn hình khóa của Windows Phone 8.1 và kiến trúc điều khiển âm nhạc tiêu chuẩn của Android.

---

## Danh Mục File Mã Nguồn Gốc

| Tên File | Package Gốc | Ý Nghĩa Kỹ Thuật & Phạm Vi Tra Cứu |
| :--- | :--- | :--- |
| **`PlaybackState.java`** | `android.media.session` | Định nghĩa trạng thái phát (`STATE_PLAYING`, `STATE_PAUSED`, v.v.), danh sách cờ năng lực nút bấm (`ACTION_PLAY`, `ACTION_SEEK_TO`, v.v.), công thức tính thời gian thực `getPosition()`. |
| **`MediaMetadata.java`** | `android.media` | Danh mục toàn bộ các khóa dữ liệu bài hát (`METADATA_KEY_TITLE`, `METADATA_KEY_ARTIST`, `METADATA_KEY_ALBUM_ART_URI`, `METADATA_KEY_DURATION`...). |
| **`MediaSession.java`** | `android.media.session` | Tầng Provider phía App phát nhạc: Token, Callback nhận lệnh (`onPlay`, `onPause`, `onSeekTo`), cơ chế phát sinh sự kiện state change. |
| **`MediaController.java`** | `android.media.session` | Tầng Consumer phía UI (Lock Screen / SystemUI): `TransportControls`, nhận callback cập nhật metadata và playback state. |
| **`MediaDescription.java`** | `android.media` | Dạng dữ liệu rút gọn (Title, Subtitle, Description, Icon) dùng cho widget hiển thị nhanh. |
| **`MediaSessionManager.java`** | `android.media.session` | Dịch vụ hệ thống điều phối các phiên phát nhạc đang hoạt động trong toàn OS. |
| **`Rating.java`** | `android.media` | Đối tượng đánh giá bài hát (Heart / Thumbs up / 5 sao). |

---

## Bảng Tra Cứu Nhanh Các Hằng Số Quan Trọng

### 1. Trạng Thái Phát (`PlaybackState.java` - `@State`)
- `STATE_NONE = 0`: Chưa nạp media
- `STATE_STOPPED = 1`: Đang dừng hẳn
- `STATE_PAUSED = 2`: Đang tạm dừng
- `STATE_PLAYING = 3`: Đang phát
- `STATE_FAST_FORWARDING = 4`: Đang tua nhanh
- `STATE_REWINDING = 5`: Đang tua lùi
- `STATE_BUFFERING = 6`: Đang tải đệm
- `STATE_ERROR = 7`: Lỗi phát
- `STATE_CONNECTING = 8`: Đang kết nối
- `STATE_SKIPPING_TO_PREVIOUS = 9`: Đang chuyển về bài trước
- `STATE_SKIPPING_TO_NEXT = 10`: Đang chuyển sang bài kế tiếp

### 2. Cờ Năng Lực Nút Bấm (`PlaybackState.java` - Bitmask `@Actions`)
- `ACTION_STOP = 1 << 0` (1)
- `ACTION_PAUSE = 1 << 1` (2)
- `ACTION_PLAY = 1 << 2` (4)
- `ACTION_REWIND = 1 << 3` (8)
- `ACTION_SKIP_TO_PREVIOUS = 1 << 4` (16)
- `ACTION_SKIP_TO_NEXT = 1 << 5` (32)
- `ACTION_FAST_FORWARD = 1 << 6` (64)
- `ACTION_SET_RATING = 1 << 7` (128)
- `ACTION_SEEK_TO = 1 << 8` (256)
- `ACTION_PLAY_PAUSE = 1 << 9` (512)
- `ACTION_SET_PLAYBACK_SPEED = 1 << 22`

### 3. Khóa Metadata Thường Dùng (`MediaMetadata.java`)
- `METADATA_KEY_TITLE = "android.media.metadata.TITLE"`
- `METADATA_KEY_ARTIST = "android.media.metadata.ARTIST"`
- `METADATA_KEY_ALBUM = "android.media.metadata.ALBUM"`
- `METADATA_KEY_DURATION = "android.media.metadata.DURATION"`
- `METADATA_KEY_ALBUM_ART_URI = "android.media.metadata.ALBUM_ART_URI"`
- `METADATA_KEY_DISPLAY_ICON = "android.media.metadata.DISPLAY_ICON"`
- `METADATA_KEY_DISPLAY_ICON_URI = "android.media.metadata.DISPLAY_ICON_URI"`
- `METADATA_KEY_USER_RATING = "android.media.metadata.USER_RATING"`

---

## 4. Kiến Trúc App Icon Chuẩn AOSP (Đối Chiếu Dự Án)
- **AOSP SystemUI:** Lấy icon thông qua `Notification.smallIcon` hoặc `packageManager.getApplicationIcon(packageName)`. Trong `MediaDescription.java`, icon được đại diện bởi `getIconUri()` (`METADATA_KEY_DISPLAY_ICON_URI`). SystemUI tuyệt đối **không nhúng trước icon của bất kỳ app thứ ba nào** và không phỏng đoán theo tên ứng dụng.
- **Ánh xạ Windows Phone 8.1:** Ứng dụng phát nhạc đóng vai trò Content Provider, đọc file icon gốc từ thư mục `Assets/` (`ms-appx:///Assets/...`) và cung cấp URL HTTP qua trường `appIcon` (`http://127.0.0.1:8088/api/icon`). Màn hình khóa đóng vai trò SystemUI Consumer thuần túy, tải động và hiển thị trực tiếp.

---

## 5. Kiến Trúc Custom Actions (`PlaybackState.CustomAction` & Ánh Xạ 5 Nút One UI 6.1)
- **AOSP Media Framework:** Trong `android.media.session.PlaybackState`, Google cung cấp lớp `CustomAction` (`mAction`, `mName`, `mIcon`, `mExtras`). Các ứng dụng phát nhạc tự do đăng ký các nút chức năng bổ sung ngoài các nút cơ bản (`ACTION_PLAY`, `ACTION_PAUSE`, `ACTION_SKIP_TO_NEXT`, `ACTION_SKIP_TO_PREVIOUS`).
- **Samsung One UI 6.1 Media Panel:** Dải 5 nút điều khiển được thiết kế theo tỷ lệ đối xứng:
  `[ Slot 1 (Custom Action) ] [ Prev ] [ Play/Pause ] [ Next ] [ Slot 5 (Custom Action) ]`
  - Với app nghe nhạc trực tuyến: Slot 1 là Like/Favorite (icon đổi giữa tim viền trắng và tim đặc đỏ), Slot 5 là Shuffle hoặc Repeat (đổi giữa tắt, repeat all, repeat one).
  - Với app podcast/sách nói: Slot 1 và Slot 5 chuyển thành Replay 10s và Fast Forward 30s.
- **Ánh xạ Windows Phone 8.1:** Màn hình khóa đọc `customActions` từ JSON của app nhạc, hiển thị vector glyph tương ứng trong Segoe UI Symbol (hoặc nạp icon động), và kích hoạt lệnh qua `GET /api/action?name={action}&value={state}`. Tương thích ngược 100% với `isFavorite` và `isShuffle`.

