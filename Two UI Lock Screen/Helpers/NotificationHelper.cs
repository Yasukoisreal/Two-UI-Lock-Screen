using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Info;

namespace Two_UI_Lock_Screen.Helpers
{
    public class NotificationBadgeItem
    {
        public BitmapImage Icon { get; set; }
        public string Counter { get; set; }
        public bool HasCounter
        {
            get { return !string.IsNullOrEmpty(Counter) && Counter != "0"; }
        }
        public Visibility CounterVisibility
        {
            get { return HasCounter ? Visibility.Visible : Visibility.Collapsed; }
        }
    }

    public class NotificationSnapshotResult
    {
        public bool HasAlarm { get; set; }
        public bool HasDriving { get; set; }
        public bool HasDoNotDisturb { get; set; }
        public string DetailedNotificationText { get; set; }

        public bool HasDetailedNotification
        {
            get { return !string.IsNullOrEmpty(DetailedNotificationText); }
        }

        public List<NotificationBadgeItem> Badges { get; set; }

        public bool HasBadges
        {
            get { return Badges != null && Badges.Count > 0; }
        }

        public NotificationSnapshotResult()
        {
            Badges = new List<NotificationBadgeItem>();
        }
    }

    public static class NotificationHelper
    {
        private static bool _bridgeFailed = false;
        private static string _resolution = null;
        private static readonly Dictionary<string, BitmapImage> _iconCache = new Dictionary<string, BitmapImage>();

        static NotificationHelper()
        {
            try
            {
                object obj;
                if (DeviceExtendedProperties.TryGetValue("PhysicalScreenResolution", out obj) && obj is Size)
                {
                    Size sz = (Size)obj;
                    _resolution = (int)sz.Width + "x" + (int)sz.Height;
                }
                else if (Application.Current != null && Application.Current.Host != null && Application.Current.Host.Content != null)
                {
                    double scale = Application.Current.Host.Content.ScaleFactor;
                    double w = scale * Application.Current.Host.Content.ActualWidth / 100.0;
                    double h = scale * Application.Current.Host.Content.ActualHeight / 100.0;
                    _resolution = (int)w + "x" + (int)h;
                }
            }
            catch
            {
                _resolution = "480x800";
            }
        }

        /// <summary>
        /// Đọc các biểu tượng thông báo (Badges), báo thức, và chi tiết từ LockScreen_Bridge Native C++.
        /// Khi chạy trên máy ảo PC x86 hoặc khi Native Bridge không khả dụng, tự động chuyển sang chế độ preview an toàn (zero crash).
        /// </summary>
        public static NotificationSnapshotResult GetSnapshot()
        {
            var result = new NotificationSnapshotResult();
            bool isEmulator = Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator;

            if (!_bridgeFailed && !isEmulator)
            {
                try
                {
                    GetRealSnapshot(result);
                    return result;
                }
                catch (Exception)
                {
                    _bridgeFailed = true;
                }
            }

            // Chế độ xem trước trên máy ảo hoặc fallback an toàn
            PopulateMockSnapshot(result);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GetRealSnapshot(NotificationSnapshotResult result)
        {
            var provider = new LockScreen_Bridge.LockScreenInfoProvider();
            var snapshot = new LockScreen_Bridge.DeviceLockscreenSnapshot();
            provider.GetSnapshot(snapshot);

            result.HasAlarm = !string.IsNullOrEmpty(snapshot.AlarmIconUri);
            result.HasDriving = !string.IsNullOrEmpty(snapshot.DrivingModeIcon);
            result.HasDoNotDisturb = !string.IsNullOrEmpty(snapshot.DoNotDisturbModeIconUri);

            // Nội dung thông báo chi tiết
            if (snapshot.DetailedTexts != null)
            {
                var texts = snapshot.DetailedTexts
                    .Where(t => t != null && !string.IsNullOrEmpty(t.Text))
                    .Select(t => t.Text)
                    .ToList();

                if (texts.Count > 0)
                {
                    result.DetailedNotificationText = string.Join("\n", texts.ToArray());
                }
            }

            // 5 biểu tượng thông báo nhanh (Quick Badges)
            if (snapshot.Badges != null)
            {
                foreach (var badge in snapshot.Badges)
                {
                    if (badge == null || string.IsNullOrEmpty(badge.BadgeIconUri))
                        continue;

                    var item = new NotificationBadgeItem
                    {
                        Counter = badge.BadgeValue,
                        Icon = LoadBadgeIcon(badge.BadgeIconUri)
                    };

                    result.Badges.Add(item);
                    if (result.Badges.Count >= 5)
                        break;
                }
            }
        }

        private static BitmapImage LoadBadgeIcon(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return null;

            if (_iconCache.ContainsKey(uri))
                return _iconCache[uri];

            BitmapImage bmp = null;
            try
            {
                string lower = uri.ToLowerInvariant();

                // Tra cứu các icon ứng dụng phổ biến đã lưu sẵn
                if (lower.Contains("facebookmessenger") || lower.Contains("3219d30d-4a23-4f58-a91c-c44b04e6a0c7"))
                {
                    bmp = GetCachedResourceImage("/Assets/Icons/Lock_FacebookMessenger.png");
                }
                else if (lower.Contains("facebook"))
                {
                    bmp = GetCachedResourceImage("/Assets/Icons/Lock_Facebook.png");
                }
                else if (lower.Contains("whatsapp") || lower.Contains("218a0ebb-1585-4c7e-a9ec-054cf4569a79"))
                {
                    bmp = GetCachedResourceImage("/Assets/Icons/Lock_WhatsApp.png");
                }
                else if (lower.Contains("instagram") || lower.Contains("3222a126-7f20-4273-ab4a-161120b21aea"))
                {
                    bmp = GetCachedResourceImage("/Assets/Icons/Lock_Instagram.png");
                }
                else if (lower.Contains("twitter") || lower.Contains("0b792c7c-14dc-df11-a844-00237de2db9e"))
                {
                    bmp = GetCachedResourceImage("/Assets/Icons/Lock_Twitter.png");
                }
                else if (uri.StartsWith("res:", StringComparison.OrdinalIgnoreCase))
                {
                    // Định dạng: res://<dll>!<resource_id>
                    string[] parts = uri.Substring(4).TrimStart('/').Split(new char[] { '!' }, 2);
                    if (parts.Length == 2)
                    {
                        string resourceId = parts[1];
                        string dllName = parts[0].Replace("{ScreenResolution}", _resolution ?? "480x800");
                        string dllPath = "c:\\windows\\system32\\" + dllName + ".dll";

                        byte[] bytes = LockScreen_Bridge.LockScreenInfoProvider.GetImageFromResource(dllPath, resourceId);
                        if (bytes != null && bytes.Length > 0)
                        {
                            bmp = new BitmapImage();
                            using (var ms = new MemoryStream(bytes))
                            {
                                bmp.SetSource(ms);
                            }
                        }
                    }
                }
                else
                {
                    Uri parsedUri;
                    if (Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out parsedUri))
                    {
                        bmp = new BitmapImage(parsedUri);
                    }
                }
            }
            catch
            {
            }

            // Fallback icon mặc định nếu không tải được
            if (bmp == null)
            {
                bmp = GetCachedResourceImage("/Assets/DefaultLockImage.png");
            }

            if (bmp != null)
            {
                _iconCache[uri] = bmp;
            }

            return bmp;
        }

        private static BitmapImage GetCachedResourceImage(string path)
        {
            if (_iconCache.ContainsKey(path))
                return _iconCache[path];

            try
            {
                Uri resUri;
                if (Uri.TryCreate(path, UriKind.Relative, out resUri))
                {
                    var bmp = new BitmapImage(resUri);
                    _iconCache[path] = bmp;
                    return bmp;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static void PopulateMockSnapshot(NotificationSnapshotResult result)
        {
            result.HasAlarm = false;

            try
            {
                var msgIcon = GetCachedResourceImage("/Assets/Icons/Lock_FacebookMessenger.png");
                if (msgIcon != null)
                {
                    result.Badges.Add(new NotificationBadgeItem
                    {
                        Counter = "3",
                        Icon = msgIcon
                    });
                }

                var waIcon = GetCachedResourceImage("/Assets/Icons/Lock_WhatsApp.png");
                if (waIcon != null)
                {
                    result.Badges.Add(new NotificationBadgeItem
                    {
                        Counter = "5",
                        Icon = waIcon
                    });
                }

                var igIcon = GetCachedResourceImage("/Assets/Icons/Lock_Instagram.png");
                if (igIcon != null)
                {
                    result.Badges.Add(new NotificationBadgeItem
                    {
                        Counter = "1",
                        Icon = igIcon
                    });
                }
            }
            catch
            {
            }
        }
    }
}
