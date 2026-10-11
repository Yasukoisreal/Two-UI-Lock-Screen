using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Windows.Phone.Devices.Power;
using Windows.Phone.System;
using Windows.Phone.System.LockScreenExtensibility;
using Microsoft.Devices;
using Windows.Storage;
using Two_UI_Lock_Screen.Helpers;
using Two_UI_Lock_Screen.Models;

namespace Two_UI_Lock_Screen
{
    public partial class LockView : PhoneApplicationPage
    {
        private DispatcherTimer minuteSyncTimer;
        private DispatcherTimer minuteRecurringTimer;
        private DispatcherTimer chargingDisplayTimer;
        private Storyboard chargingToContactStoryboard;
        private IsolatedStorageFileStream _videoStream;

        private double unlockThreshold = 140.0;
        private bool isPinLocked = false;
        private double pinpadBoundaryY = 0.0;
        private bool isUnlockingStarted = false;
        private bool isPinPadOpen = false;
        private Storyboard bounceBackStory;
        private Storyboard slideStory;
        private Storyboard wakeupEntranceStory;
        private Storyboard musicTransitionStory;
        private Storyboard cameraAnimStory;
        private PhotoCamera _photoCamera;
        private CameraType _currentCameraType = CameraType.Primary;
        private FlashMode _currentFlashMode = FlashMode.Auto;
        private bool _isCapturing = false;
        private bool _isFocusing = false;
        private Storyboard _shutterFlashStory;
        private DispatcherTimer _cameraToastTimer;
        private double cumulativeDragX = 0;
        private double cumulativeDragY = 0;
        private Storyboard _shortcutPressStory;
        private Storyboard _shortcutSnapBackStory;
        private double _shortcutDragX = 0;
        private double _shortcutDragY = 0;
        private bool _isShortcutDragging = false;
        private bool _isRightShortcutActive = false;
        private bool _hasShortcutVibrated = false;
        private const double ShortcutUnlockThreshold = 110.0;

        public LockView()
        {
            InitializeComponent();
        }

        #region Page Lifecycle & Navigation Reciprocal Guards

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                // 1. Kiểm tra trạng thái chéo (Anti-Desync Guard):
                // Nếu người dùng mở app từ Start Screen khi máy đã mở khóa, chuyển ngay về MainPage
                if (!SystemProtection.ScreenLocked)
                {
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                    return;
                }

                // 2. Dọn dẹp sạch Navigation Backstack (xóa LockRouter)
                while (NavigationService.CanGoBack)
                {
                    NavigationService.RemoveBackEntry();
                }

                // 3. Đặt lại trạng thái Transform & cờ mở khóa
                isUnlockingStarted = false;
                isPinPadOpen = false;
                isPinLocked = false;
                pinpadBoundaryY = 0.0;
                if (bounceBackStory != null)
                {
                    try { bounceBackStory.Stop(); } catch { }
                    bounceBackStory = null;
                }
                if (slideStory != null)
                {
                    try { slideStory.Stop(); } catch { }
                    slideStory = null;
                }
                if (wakeupEntranceStory != null)
                {
                    try { wakeupEntranceStory.Stop(); } catch { }
                    wakeupEntranceStory = null;
                }
                if (musicTransitionStory != null)
                {
                    try { musicTransitionStory.Stop(); } catch { }
                    musicTransitionStory = null;
                }
                if (cameraAnimStory != null)
                {
                    try { cameraAnimStory.Stop(); } catch { }
                    cameraAnimStory = null;
                }
                if (CameraOverlay != null)
                {
                    CameraOverlay.Visibility = Visibility.Collapsed;
                    CameraOverlayTransform.ScaleX = 1.0;
                    CameraOverlayTransform.ScaleY = 1.0;
                    CameraOverlay.Opacity = 1.0;
                }
                StopShortcutAnimations();
                ResetShortcutGestureState();
                double initialPinHeight = GetPinpadHeightDips();
                if (initialPinHeight > 0)
                {
                    isPinLocked = true;
                    pinpadBoundaryY = -initialPinHeight;
                }
                cumulativeDragX = 0;
                cumulativeDragY = 0;
                ForegroundTransform.TranslateX = 0;
                ForegroundTransform.TranslateY = 0;
                ForegroundTransform.ScaleX = 1.0;
                ForegroundTransform.ScaleY = 1.0;
                ForegroundPanel.Opacity = 1.0;
                BackgroundTransform.TranslateX = 0;
                BackgroundTransform.TranslateY = 0;
                BackgroundTransform.ScaleX = 1.0;
                BackgroundTransform.ScaleY = 1.0;
                VideoTransform.TranslateX = 0;
                VideoTransform.TranslateY = 0;
                VideoTransform.ScaleX = 1.0;
                VideoTransform.ScaleY = 1.0;
                if (LockGlyphIcon != null) LockGlyphIcon.Opacity = 0.9;
                if (ContactText != null) ContactText.Opacity = 1.0;
                if (BottomChargingPanel != null) BottomChargingPanel.Opacity = 1.0;

                // 4. Áp dụng cấu hình và hình nền
                ApplySettings();

                // 5. Khởi động bộ đồng bộ thời gian 2 giai đoạn chuẩn xác và tiết kiệm pin
                StartClockTimer();

                // 6. Cập nhật trạng thái pin (hiện popup sạc 3s nếu đang sạc)
                UpdateBatteryStatus(true);

                // 7. Cập nhật thời tiết thực tế
                UpdateWeatherDisplayAsync();

                // 8. Cập nhật thông báo hệ thống
                UpdateNotifications();

                // 9. Khởi tạo tiện ích âm nhạc
                InitMusicWidget();

                // 10. Gắn hiệu ứng phản hồi xúc giác One UI (Press/Scale feedback)
                SetupPressFeedbackEffects();

                // 11. Kích hoạt hiệu ứng xuất hiện đánh thức màn hình (Wake-up Entrance Animation)
                PlayWakeupEntranceAnimation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] OnNavigatedTo error: " + ex.Message);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // 1. Tắt đèn pin phần cứng nếu đang bật để tránh hao pin
            FlashlightHelper.TurnOffAndDispose();

            // 2. Hủy toàn bộ timer và storyboards
            StopClockTimer();
            if (chargingDisplayTimer != null)
            {
                chargingDisplayTimer.Stop();
                chargingDisplayTimer = null;
            }
            if (chargingToContactStoryboard != null)
            {
                try { chargingToContactStoryboard.Stop(); } catch { }
                chargingToContactStoryboard = null;
            }
            if (wakeupEntranceStory != null)
            {
                try { wakeupEntranceStory.Stop(); } catch { }
                wakeupEntranceStory = null;
            }
            if (musicTransitionStory != null)
            {
                try { musicTransitionStory.Stop(); } catch { }
                musicTransitionStory = null;
            }
            if (cameraAnimStory != null)
            {
                try { cameraAnimStory.Stop(); } catch { }
                cameraAnimStory = null;
            }
            if (bounceBackStory != null)
            {
                try { bounceBackStory.Stop(); } catch { }
                bounceBackStory = null;
            }
            if (slideStory != null)
            {
                try { slideStory.Stop(); } catch { }
                slideStory = null;
            }
            StopShortcutAnimations();
            ResetShortcutGestureState();

            // 3. Ngắt kết nối tiện ích âm nhạc và dọn dẹp RAM
            DisposeMusicWidget();

            // 4. Giải phóng Video Wallpaper decoder và stream
            StopVideoWallpaper();

            // 5. Giải phóng ImageSource để tối ưu RAM cho máy 512MB
            if (BackgroundBrush != null)
            {
                BackgroundBrush.ImageSource = null;
            }

            // 6. Giải phóng Camera phần cứng và phím chụp
            UnhookHardwareCameraButtons();
            DisposeCamera();
            if (_cameraToastTimer != null)
            {
                _cameraToastTimer.Stop();
                _cameraToastTimer = null;
            }

            // 7. Chủ động dọn rác
            GC.Collect();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            // BẮT BUỘC: Chặn phím cứng Back trên màn hình khóa
            e.Cancel = true;

            // Nếu đang mở camera overlay thì đóng camera mượt mà
            if (CameraOverlay.Visibility == Visibility.Visible)
            {
                AnimateCloseCamera();
                return;
            }

            // Nếu bàn phím PIN đang mở, bấm phím Back sẽ đóng PIN và quay lại màn hình khóa
            if (isPinPadOpen)
            {
                isPinPadOpen = false;
                AnimateBackToNormal();
            }
        }

        #endregion

        #region One UI 6.1 Animations & Feedback Effects

        private void PlayWakeupEntranceAnimation()
        {
            if (wakeupEntranceStory != null)
            {
                try { wakeupEntranceStory.Stop(); } catch { }
                wakeupEntranceStory = null;
            }

            ForegroundTransform.TranslateY = -12.0;
            ForegroundPanel.Opacity = 0.0;

            var sb = new Storyboard();

            var moveAnim = new DoubleAnimation
            {
                From = -12.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(moveAnim, ForegroundTransform);
            Storyboard.SetTargetProperty(moveAnim, new PropertyPath("TranslateY"));
            sb.Children.Add(moveAnim);

            var fadeAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(240),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeAnim, ForegroundPanel);
            Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
            sb.Children.Add(fadeAnim);

            sb.Completed += (s, e) =>
            {
                wakeupEntranceStory = null;
                ForegroundTransform.TranslateY = 0;
                ForegroundPanel.Opacity = 1.0;
            };

            wakeupEntranceStory = sb;
            sb.Begin();
        }

        private bool _pressEffectsAttached = false;

        private void SetupPressFeedbackEffects()
        {
            if (_pressEffectsAttached) return;
            _pressEffectsAttached = true;

            AttachPressEffect(BatteryTileBorder);
            AttachPressEffect(UvTileBorder);
            AttachPressEffect(WeatherTileBorder);

            // Các nút tiện ích âm nhạc One UI 6.1 (Press Feedback)
            AttachPressEffect(MusicArrowBtn);
            AttachPressEffect(MusicPrevBtnCollapsed);
            AttachPressEffect(MusicPlayPauseBtnCollapsed);
            AttachPressEffect(MusicNextBtnCollapsed);

            AttachPressEffect(MusicLeftActionBtn);
            AttachPressEffect(MusicPrevBtnExpanded);
            AttachPressEffect(MusicPlayPauseBtnExpanded);
            AttachPressEffect(MusicNextBtnExpanded);
            AttachPressEffect(MusicRightActionBtn);
        }

        private void AttachPressEffect(FrameworkElement element)
        {
            if (element == null) return;
            var transform = element.RenderTransform as CompositeTransform;
            if (transform == null) return;

            element.ManipulationStarted += (s, e) =>
            {
                try { AnimateScale(transform, 0.94, 80); } catch { }
            };

            element.ManipulationCompleted += (s, e) =>
            {
                try { AnimateScale(transform, 1.0, 160); } catch { }
            };
        }

        private void AnimateScale(CompositeTransform transform, double toScale, double durationMs)
        {
            if (transform == null) return;
            var sb = new Storyboard();

            var animX = new DoubleAnimation
            {
                To = toScale,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animX, transform);
            Storyboard.SetTargetProperty(animX, new PropertyPath("ScaleX"));
            sb.Children.Add(animX);

            var animY = new DoubleAnimation
            {
                To = toScale,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animY, transform);
            Storyboard.SetTargetProperty(animY, new PropertyPath("ScaleY"));
            sb.Children.Add(animY);

            sb.Completed += (s, e) =>
            {
                transform.ScaleX = toScale;
                transform.ScaleY = toScale;
            };

            sb.Begin();
        }

        #endregion

        #region Clock Synchronization & System Status

        private void StartClockTimer()
        {
            StopClockTimer();
            UpdateDateTimeDisplay();

            DateTime now = DateTime.Now;
            int msUntilNextMinute = 60000 - (now.Second * 1000 + now.Millisecond);
            if (msUntilNextMinute <= 0) msUntilNextMinute = 1000;

            // Giai đoạn 1: Timer 1 lần căn chỉnh chuẩn miligiây về đầu phút (:00.000)
            minuteSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(msUntilNextMinute) };
            minuteSyncTimer.Tick += (s, e) =>
            {
                if (minuteSyncTimer != null)
                {
                    minuteSyncTimer.Stop();
                    minuteSyncTimer = null;
                }

                UpdateDateTimeDisplay();

                // Giai đoạn 2: Timer lặp lại mỗi 60 giây
                minuteRecurringTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
                minuteRecurringTimer.Tick += (st, et) =>
                {
                    UpdateDateTimeDisplay();
                    UpdateBatteryStatus(false);
                    UpdateWeatherDisplayAsync();
                    UpdateNotifications();
                };
                minuteRecurringTimer.Start();
            };
            minuteSyncTimer.Start();
        }

        private void StopClockTimer()
        {
            if (minuteSyncTimer != null)
            {
                minuteSyncTimer.Stop();
                minuteSyncTimer = null;
            }
            if (minuteRecurringTimer != null)
            {
                minuteRecurringTimer.Stop();
                minuteRecurringTimer = null;
            }
        }

        private void UpdateDateTimeDisplay()
        {
            DateTime now = DateTime.Now;

            // Giờ và Phút
            ClassicClockText.Text = now.ToString("HH:mm");
            HourText.Text = now.ToString("HH");
            MinuteText.Text = now.ToString("mm");

            // Thứ, Ngày tháng Dương lịch tiếng Việt phong cách One UI: "T.4, 7 Th10"
            string dayOfWeekStr = GetDayOfWeekShort(now.DayOfWeek);
            SolarDateText.Text = string.Format("{0}, {1} Th{2}", dayOfWeekStr, now.Day, now.Month);

            // Âm lịch Việt Nam
            if (SettingsManager.Current.ShowLunarCalendar)
            {
                LunarDateText.Visibility = Visibility.Visible;
                LunarDateText.Text = VietnameseCalendar.FormatLunarDateShort(now);
            }
            else
            {
                LunarDateText.Visibility = Visibility.Collapsed;
            }
        }

        private string GetDayOfWeekShort(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Monday: return "T.2";
                case DayOfWeek.Tuesday: return "T.3";
                case DayOfWeek.Wednesday: return "T.4";
                case DayOfWeek.Thursday: return "T.5";
                case DayOfWeek.Friday: return "T.6";
                case DayOfWeek.Saturday: return "T.7";
                case DayOfWeek.Sunday: return "CN";
                default: return "";
            }
        }

        private bool lastKnownCharging = false;

        private void UpdateBatteryStatus(bool triggerChargingPopup = false)
        {
            try
            {
                var battery = Battery.GetDefault();
                int percent = (battery != null) ? battery.RemainingChargePercent : 100;
                bool isCharging = false;
                try
                {
                    isCharging = (DeviceStatus.PowerSource == PowerSource.External);
                }
                catch { }

                // Phát hiện vừa cắm sạc khi màn hình đang bật
                if (!lastKnownCharging && isCharging)
                {
                    triggerChargingPopup = true;
                }
                lastKnownCharging = isCharging;

                // Slot 1 trong Widget Card
                if (BatteryPercentText != null) BatteryPercentText.Text = percent.ToString();
                if (BatteryWidgetIcon != null) BatteryWidgetIcon.Text = isCharging ? "⚡" : "";
                UpdateBatteryArc(percent);

                // Khối thông tin sạc dưới đáy
                if (BottomBatteryPercent != null) BottomBatteryPercent.Text = percent + "%";
                if (BottomBatteryImage != null)
                {
                    BottomBatteryImage.Source = isCharging 
                        ? new BitmapImage(new Uri("/Assets/Icons/ic_battery_bolt.png", UriKind.Relative))
                        : new BitmapImage(new Uri("/Assets/Icons/ic_battery_normal.png", UriKind.Relative));
                }

                if (BottomChargingEstimateText != null)
                {
                    if (percent >= 100)
                    {
                        BottomChargingEstimateText.Text = "Đã sạc đầy";
                    }
                    else
                    {
                        int remaining = 100 - percent;
                        int totalMinutes = (int)Math.Round(remaining * 1.5);
                        int hours = totalMinutes / 60;
                        int mins = totalMinutes % 60;
                        if (hours > 0)
                        {
                            BottomChargingEstimateText.Text = string.Format("{0} giờ {1} phút nữa sẽ đầy", hours, mins);
                        }
                        else
                        {
                            BottomChargingEstimateText.Text = string.Format("{0} phút nữa sẽ đầy", mins);
                        }
                    }
                }

                var settings = SettingsManager.Current;
                if (ContactText != null)
                {
                    ContactText.Text = (settings != null && !string.IsNullOrEmpty(settings.ContactInfo)) ? settings.ContactInfo : "Yasuko";
                }

                if (isCharging)
                {
                    if (triggerChargingPopup)
                    {
                        ShowChargingBriefly();
                    }
                    else if (chargingDisplayTimer == null || !chargingDisplayTimer.IsEnabled)
                    {
                        // Đã qua 3 giây hiển thị thông tin sạc, giữ nguyên hiển thị danh bạ
                        if (BottomChargingPanel != null) BottomChargingPanel.Visibility = Visibility.Collapsed;
                        if (ContactText != null)
                        {
                            ContactText.Visibility = Visibility.Visible;
                            ContactText.Opacity = 1.0;
                        }
                    }
                }
                else
                {
                    // Rút sạc hoặc không sạc: dừng timer và hiển thị thông tin danh bạ
                    if (chargingDisplayTimer != null)
                    {
                        chargingDisplayTimer.Stop();
                    }
                    if (chargingToContactStoryboard != null)
                    {
                        try { chargingToContactStoryboard.Stop(); } catch { }
                        chargingToContactStoryboard = null;
                    }

                    if (BottomChargingPanel != null) BottomChargingPanel.Visibility = Visibility.Collapsed;
                    if (ContactText != null)
                    {
                        ContactText.Visibility = Visibility.Visible;
                        ContactText.Opacity = 1.0;
                    }
                }
            }
            catch { }
        }

        private void ShowChargingBriefly()
        {
            if (BottomChargingPanel == null || ContactText == null) return;

            if (chargingToContactStoryboard != null)
            {
                try { chargingToContactStoryboard.Stop(); } catch { }
                chargingToContactStoryboard = null;
            }

            if (chargingDisplayTimer == null)
            {
                chargingDisplayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                chargingDisplayTimer.Tick += (s, e) =>
                {
                    chargingDisplayTimer.Stop();
                    AnimateChargingToContact();
                };
            }
            else
            {
                chargingDisplayTimer.Stop();
            }

            BottomChargingPanel.Opacity = 1.0;
            BottomChargingPanel.Visibility = Visibility.Visible;
            ContactText.Visibility = Visibility.Collapsed;

            chargingDisplayTimer.Start();
        }

        private void AnimateChargingToContact()
        {
            if (BottomChargingPanel == null || ContactText == null) return;

            if (chargingToContactStoryboard != null)
            {
                try { chargingToContactStoryboard.Stop(); } catch { }
                chargingToContactStoryboard = null;
            }

            var settings = SettingsManager.Current;
            ContactText.Text = (settings != null && !string.IsNullOrEmpty(settings.ContactInfo)) ? settings.ContactInfo : "Yasuko";

            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(fadeOut, BottomChargingPanel);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(fadeIn, ContactText);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

            chargingToContactStoryboard = new Storyboard();
            chargingToContactStoryboard.Children.Add(fadeOut);
            chargingToContactStoryboard.Children.Add(fadeIn);

            ContactText.Opacity = 0.0;
            ContactText.Visibility = Visibility.Visible;

            chargingToContactStoryboard.Completed += (s, e) =>
            {
                if (BottomChargingPanel != null)
                {
                    BottomChargingPanel.Visibility = Visibility.Collapsed;
                    BottomChargingPanel.Opacity = 1.0;
                }
                if (ContactText != null)
                {
                    ContactText.Opacity = 1.0;
                }
                chargingToContactStoryboard = null;
            };

            chargingToContactStoryboard.Begin();
        }

        private void BottomInfo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                bool isCharging = false;
                try
                {
                    isCharging = (DeviceStatus.PowerSource == PowerSource.External);
                }
                catch { }

                if (isCharging)
                {
                    e.Handled = true;
                    UpdateBatteryStatus(true);
                }
            }
            catch { }
        }

        private void UpdateBatteryArc(int percent)
        {
            try
            {
                double p = Math.Max(0.02, Math.Min(1.0, percent / 100.0));
                double sweepDeg = p * 260.0;
                double phiDeg = sweepDeg - 130.0;
                double phiRad = phiDeg * Math.PI / 180.0;

                double endX = 34.0 + 20.0 * Math.Sin(phiRad);
                double endY = 29.0 - 20.0 * Math.Cos(phiRad);

                var pg = new PathGeometry();
                var fig = new PathFigure { StartPoint = new Point(18.68, 41.86), IsClosed = false };
                var arc = new ArcSegment
                {
                    Point = new Point(endX, endY),
                    Size = new Size(20.0, 20.0),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = (sweepDeg > 180.0)
                };
                fig.Segments.Add(arc);
                pg.Figures.Add(fig);
                BatteryArcProgress.Data = pg;
            }
            catch { }
        }

        private async void UpdateWeatherDisplayAsync()
        {
            try
            {
                var cached = WeatherService.CurrentData;
                if (cached != null && cached.HasData)
                {
                    ApplyWeatherData(cached);
                }

                var fresh = await WeatherService.GetWeatherAsync(forceRefresh: false);
                if (fresh != null && fresh.HasData)
                {
                    ApplyWeatherData(fresh);
                }
            }
            catch { }
        }

        private void ApplyWeatherData(WeatherData data)
        {
            if (data == null || !data.HasData) return;
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var settings = SettingsManager.Current;
                    string tempStr = data.Temperature + "°";
                    WeatherText.Text = tempStr;
                    WeatherText.Visibility = settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
                    WeatherHeaderIcon.Visibility = settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
                    WidgetTempText.Text = tempStr;
                    LocationText.Text = data.LocationName ?? "";
                    if (!string.IsNullOrEmpty(data.WeatherIconUri))
                    {
                        WeatherHeaderIcon.Source = new BitmapImage(new Uri(data.WeatherIconUri, UriKind.Relative));
                    }
                    UpdateUvDisplay(data.UvIndex);
                }
                catch { }
            });
        }

        private void UpdateUvDisplay(double uv)
        {
            try
            {
                if (UvIndexText != null)
                {
                    int roundedUv = (int)Math.Round(uv);
                    if (roundedUv < 0) roundedUv = 0;
                    UvIndexText.Text = roundedUv.ToString();
                }

                if (UvArcProgress != null)
                {
                    double p = Math.Max(0.02, Math.Min(1.0, uv / 11.0));
                    double sweepDeg = p * 260.0;
                    double phiDeg = sweepDeg - 130.0;
                    double phiRad = phiDeg * Math.PI / 180.0;

                    double endX = 34.0 + 20.0 * Math.Sin(phiRad);
                    double endY = 29.0 - 20.0 * Math.Cos(phiRad);

                    var pg = new PathGeometry();
                    var fig = new PathFigure { StartPoint = new Point(18.68, 41.86), IsClosed = false };
                    var arc = new ArcSegment
                    {
                        Point = new Point(endX, endY),
                        Size = new Size(20.0, 20.0),
                        SweepDirection = SweepDirection.Clockwise,
                        IsLargeArc = (sweepDeg > 180.0)
                    };
                    fig.Segments.Add(arc);
                    pg.Figures.Add(fig);
                    UvArcProgress.Data = pg;
                }
            }
            catch { }
        }

        private void ApplySettings()
        {
            var settings = SettingsManager.Current;

            // 1. Áp dụng Kiểu đồng hồ
            if (settings.ClockStyle == 1) // Stacked
            {
                StackedClockPanel.Visibility = Visibility.Visible;
                ClassicClockText.Visibility = Visibility.Collapsed;
            }
            else // 0: Classic Horizontal (12:06 - Mặc định One UI 6.1)
            {
                StackedClockPanel.Visibility = Visibility.Collapsed;
                ClassicClockText.Visibility = Visibility.Visible;
            }

            // 2. Chữ ký danh bạ
            ContactText.Text = string.IsNullOrEmpty(settings.ContactInfo) ? "Yasuko" : settings.ContactInfo;

            // 3. Thời tiết, Địa điểm & Chỉ số UV
            var initialWeather = WeatherService.CurrentData;
            if (initialWeather != null && initialWeather.HasData)
            {
                string tempStr = initialWeather.Temperature + "°";
                WeatherText.Text = tempStr;
                WeatherText.Visibility = settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
                WeatherHeaderIcon.Visibility = settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
                if (!string.IsNullOrEmpty(initialWeather.WeatherIconUri))
                {
                    WeatherHeaderIcon.Source = new BitmapImage(new Uri(initialWeather.WeatherIconUri, UriKind.Relative));
                }

                WidgetTempText.Text = tempStr;
                LocationText.Text = initialWeather.LocationName ?? "";
                UpdateUvDisplay(initialWeather.UvIndex);
            }
            else
            {
                WeatherText.Text = "";
                WeatherText.Visibility = Visibility.Collapsed;
                WeatherHeaderIcon.Visibility = Visibility.Collapsed;
                WidgetTempText.Text = "";
                LocationText.Text = "";
                UpdateUvDisplay(0);
            }

            // 4. Khay Widgets
            WidgetsPillBar.Visibility = settings.ShowBatteryWidget ? Visibility.Visible : Visibility.Collapsed;

            // 5. Phím tắt góc dưới (One UI Squircle)
            LeftShortcutImage.Source = FlashlightHelper.IsOn
                ? new BitmapImage(new Uri("/Assets/Icons/ic_shortcut_flash_on.png", UriKind.Relative))
                : new BitmapImage(new Uri("/Assets/Icons/ic_shortcut_flash.png", UriKind.Relative));

            RightShortcutImage.Source = new BitmapImage(new Uri("/Assets/Icons/ic_shortcut_camera.png", UriKind.Relative));

            // 6. Tải hình nền (Video Wallpaper hoặc Static Image)
            if (settings.UseVideoWallpaper && !string.IsNullOrEmpty(settings.CustomWallpaperVideoFile))
            {
                LoadWallpaper(settings.CustomWallpaperFile);
                PlayVideoWallpaper(settings.CustomWallpaperVideoFile);
            }
            else
            {
                UseStaticWallpaper();
            }

            // 7. Cập nhật thông báo hệ thống
            if (settings.ShowNotifications)
            {
                UpdateNotifications();
            }
            else
            {
                NotificationContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateNotifications()
        {
            try
            {
                var settings = SettingsManager.Current;
                if (!settings.ShowNotifications)
                {
                    NotificationContainer.Visibility = Visibility.Collapsed;
                    return;
                }

                var snapshot = NotificationHelper.GetSnapshot();
                if (snapshot != null && snapshot.HasBadges)
                {
                    NotificationIconsRow.ItemsSource = snapshot.Badges;
                    NotificationContainer.Visibility = Visibility.Visible;

                    if (snapshot.HasDetailedNotification)
                    {
                        NotificationDetailedText.Text = snapshot.DetailedNotificationText;
                        NotificationDetailedText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        NotificationDetailedText.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    NotificationContainer.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                NotificationContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadWallpaper(string customFileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(customFileName))
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (store.FileExists(customFileName))
                        {
                            using (var stream = store.OpenFile(customFileName, FileMode.Open, FileAccess.Read))
                            {
                                var bmp = new BitmapImage();
                                bmp.SetSource(stream);
                                BackgroundBrush.ImageSource = bmp;
                                return;
                            }
                        }
                    }
                }
            }
            catch { }

            // Hình nền mặc định có sẵn trong dự án
            try
            {
                BackgroundBrush.ImageSource = new BitmapImage(new Uri("/Assets/SplashScreen.png", UriKind.Relative));
            }
            catch { }
        }

        private void PlayVideoWallpaper(string videoFileName)
        {
            try
            {
                StopVideoWallpaper();

                if (string.IsNullOrEmpty(videoFileName))
                {
                    UseStaticWallpaper();
                    return;
                }

                var store = IsolatedStorageFile.GetUserStoreForApplication();
                if (!store.FileExists(videoFileName))
                {
                    UseStaticWallpaper();
                    return;
                }

                _videoStream = store.OpenFile(videoFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                WallpaperVideo.Volume = 0;
                WallpaperVideo.IsMuted = true;
                WallpaperVideo.SetSource(_videoStream);
                WallpaperVideo.Play();
                VideoContainer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] PlayVideoWallpaper failed: " + ex.Message);
                UseStaticWallpaper();
            }
        }

        private void StopVideoWallpaper()
        {
            try
            {
                if (WallpaperVideo != null)
                {
                    WallpaperVideo.Stop();
                }
            }
            catch { }

            if (_videoStream != null)
            {
                try
                {
                    _videoStream.Dispose();
                }
                catch { }
                _videoStream = null;
            }

            if (VideoContainer != null)
            {
                VideoContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void UseStaticWallpaper()
        {
            StopVideoWallpaper();
            var settings = SettingsManager.Current;
            LoadWallpaper(settings != null ? settings.CustomWallpaperFile : null);
        }

        private void WallpaperVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WallpaperVideo != null)
                {
                    WallpaperVideo.Position = TimeSpan.Zero;
                    WallpaperVideo.Play();
                }
            }
            catch { }
        }

        private void WallpaperVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[LockView] WallpaperVideo failed: " + (e != null && e.ErrorException != null ? e.ErrorException.Message : "unknown"));
            UseStaticWallpaper();
        }

        #endregion

        #region Touch Gestures, Physics & Security Unlock Flow

        private double GetPinpadHeightDips()
        {
            try
            {
                int rawHeight = ExtensibilityApp.GetLockPinpadHeight();
                if (rawHeight <= 0) return 0.0;

                double ratio = (double)Application.Current.Host.Content.ScaleFactor / 100.0;
                try
                {
                    object obj;
                    if (Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("PhysicalScreenResolution", out obj) && obj is Size)
                    {
                        Size sz = (Size)obj;
                        if (sz.Width > 0)
                        {
                            ratio = sz.Width / 480.0;
                        }
                    }
                }
                catch { }

                return (ratio > 0) ? (rawHeight / ratio) : (double)rawHeight;
            }
            catch
            {
                return 0.0;
            }
        }

        private void LayoutRoot_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            if (!SystemProtection.ScreenLocked) return;

            try
            {
                if (bounceBackStory != null)
                {
                    try { bounceBackStory.Stop(); } catch { }
                    bounceBackStory = null;
                }
                if (slideStory != null)
                {
                    try { slideStory.Stop(); } catch { }
                    slideStory = null;
                }
                if (wakeupEntranceStory != null)
                {
                    try { wakeupEntranceStory.Stop(); } catch { }
                    wakeupEntranceStory = null;
                }

                // Nếu không phải đang mở PIN, đảm bảo các biến đổi và độ mờ ở trạng thái ban đầu
                if (!isPinPadOpen)
                {
                    ForegroundTransform.ScaleX = 1.0;
                    ForegroundTransform.ScaleY = 1.0;
                    ForegroundTransform.TranslateX = 0;
                    ForegroundTransform.TranslateY = 0;
                    ForegroundPanel.Opacity = 1.0;
                    BackgroundTransform.TranslateX = 0;
                    BackgroundTransform.TranslateY = 0;
                    VideoTransform.TranslateX = 0;
                    VideoTransform.TranslateY = 0;
                    if (LockGlyphIcon != null) LockGlyphIcon.Opacity = 0.9;
                    if (ContactText != null) ContactText.Opacity = 1.0;
                    if (BottomChargingPanel != null) BottomChargingPanel.Opacity = 1.0;
                }

                cumulativeDragX = 0;
                cumulativeDragY = 0;

                double w = LayoutRoot.ActualWidth > 0 ? LayoutRoot.ActualWidth : 480.0;
                double h = LayoutRoot.ActualHeight > 0 ? LayoutRoot.ActualHeight : 800.0;
                ForegroundTransform.CenterX = w / 2.0;
                ForegroundTransform.CenterY = h / 2.0;

                double pinHeight = GetPinpadHeightDips();
                if (pinHeight > 0)
                {
                    isPinLocked = true;
                    pinpadBoundaryY = -pinHeight;
                }

                unlockThreshold = 140.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] ManipulationStarted error: " + ex.Message);
            }
        }

        private void LayoutRoot_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!SystemProtection.ScreenLocked) return;

            try
            {
                // 1. Khi bàn phím PIN đang mở: Kéo ảnh nền xuống để đóng PIN Pad
                if (isPinPadOpen)
                {
                    double newBgY = BackgroundTransform.TranslateY + e.DeltaManipulation.Translation.Y;
                    if (newBgY > 0) newBgY = 0;
                    if (newBgY < pinpadBoundaryY) newBgY = pinpadBoundaryY;
                    BackgroundTransform.TranslateY = newBgY;
                    VideoTransform.TranslateY = newBgY;
                    return;
                }

                // 2. Trạng thái bình thường: Vuốt mở khóa
                cumulativeDragX += e.DeltaManipulation.Translation.X;
                cumulativeDragY += e.DeltaManipulation.Translation.Y;

                double distance = Math.Sqrt(cumulativeDragX * cumulativeDragX + cumulativeDragY * cumulativeDragY);

                // Chỉ báo cho OS Shell nâng PIN pad khi người dùng thực sự bắt đầu vuốt ngón tay (distance >= 15px)
                // Ngăn chặn hoàn toàn việc gửi tín hiệu nâng/hạ PIN giả lên OS khi người dùng chỉ bấm nút hoặc chạm nhẹ
                if (!isPinPadOpen && !isUnlockingStarted && distance >= 15.0)
                {
                    isUnlockingStarted = true;
                    try { ExtensibilityApp.BeginUnlock(); } catch { }
                }

                // Ảnh nền TUYỆT ĐỐI GIỮ NGUYÊN 100% tại Y=0 trong lúc vuốt tay
                BackgroundTransform.TranslateX = 0;
                BackgroundTransform.TranslateY = 0;
                BackgroundTransform.ScaleX = 1.0;
                BackgroundTransform.ScaleY = 1.0;
                VideoTransform.TranslateX = 0;
                VideoTransform.TranslateY = 0;
                VideoTransform.ScaleX = 1.0;
                VideoTransform.ScaleY = 1.0;

                // Tiện ích không di chuyển vị trí X/Y
                ForegroundTransform.TranslateX = 0;
                ForegroundTransform.TranslateY = 0;

                // Icon ổ khóa và thông tin danh bạ / sạc pin mờ dần trong 30px đầu
                double quickFade = Math.Max(0.0, 1.0 - (distance / 30.0));
                if (LockGlyphIcon != null) LockGlyphIcon.Opacity = quickFade * 0.9;
                if (ContactText != null) ContactText.Opacity = quickFade;
                if (BottomChargingPanel != null) BottomChargingPanel.Opacity = quickFade;

                // CHỈ phóng to từ tâm và mờ dần toàn bộ tiện ích
                double progress = Math.Min(1.0, distance / unlockThreshold);
                double currentScale = 1.0 + (progress * 0.18);
                ForegroundTransform.ScaleX = currentScale;
                ForegroundTransform.ScaleY = currentScale;
                ForegroundPanel.Opacity = Math.Max(0.0, 1.0 - progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] ManipulationDelta error: " + ex.Message);
            }
        }

        private void LayoutRoot_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (!SystemProtection.ScreenLocked) return;

            try
            {
                // 1. Nếu bàn phím PIN đang mở và người dùng vừa thao tác kéo để đóng/mở
                if (isPinPadOpen)
                {
                    double newBgY = BackgroundTransform.TranslateY;
                    bool isFlickDown = e.FinalVelocities.LinearVelocity.Y > 800.0;
                    bool isFlickUp = e.FinalVelocities.LinearVelocity.Y < -800.0;
                    bool shouldClosePin = (newBgY > pinpadBoundaryY * 0.5 || isFlickDown) && !isFlickUp;

                    if (shouldClosePin)
                    {
                        isPinPadOpen = false;
                        AnimateBackToNormal();
                    }
                    else
                    {
                        isPinPadOpen = true;
                        AnimateWallpaperTo(pinpadBoundaryY, 200);
                    }
                    return;
                }

                // 2. Trạng thái bình thường: Người dùng vừa vuốt mở khóa
                double distance = Math.Sqrt(cumulativeDragX * cumulativeDragX + cumulativeDragY * cumulativeDragY);
                double vx = e.FinalVelocities.LinearVelocity.X;
                double vy = e.FinalVelocities.LinearVelocity.Y;
                double velocityMag = Math.Sqrt(vx * vx + vy * vy);

                if (!isPinLocked)
                {
                    double pinHeight = GetPinpadHeightDips();
                    if (pinHeight > 0)
                    {
                        isPinLocked = true;
                        pinpadBoundaryY = -pinHeight;
                    }
                }

                bool isFlick = (isPinLocked && vy < -700.0) || (velocityMag >= 900.0);
                if (distance >= unlockThreshold || isFlick)
                {
                    InitiateUnlockSequence();
                }
                else
                {
                    AnimateBackToNormal();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] ManipulationCompleted error: " + ex.Message);
                AnimateBackToNormal();
            }
        }

        private void AnimateBackToNormal()
        {
            // Chỉ gửi tín hiệu hạ PIN về OS nếu trước đó đã thực sự phát tín hiệu BeginUnlock()
            if (isUnlockingStarted)
            {
                try { ExtensibilityApp.EndUnlock(); } catch { }
                isUnlockingStarted = false;
            }
            isPinPadOpen = false;

            if (bounceBackStory != null) { try { bounceBackStory.Stop(); } catch { } bounceBackStory = null; }
            if (slideStory != null) { try { slideStory.Stop(); } catch { } slideStory = null; }

            ForegroundTransform.TranslateX = 0;
            ForegroundTransform.TranslateY = 0;
            BackgroundTransform.TranslateX = 0;
            VideoTransform.TranslateX = 0;

            var sb = new Storyboard();

            // 1. Phục hồi Scale tiện ích về 1.0
            var scaleXAnim = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleXAnim, ForegroundTransform);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("ScaleX"));
            sb.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleYAnim, ForegroundTransform);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("ScaleY"));
            sb.Children.Add(scaleYAnim);

            // 2. Phục hồi Opacity của ForegroundPanel về 1.0
            var opacityAnim = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(opacityAnim, ForegroundPanel);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            sb.Children.Add(opacityAnim);

            // 3. Phục hồi Opacity icon khóa và danh bạ / sạc pin
            if (LockGlyphIcon != null)
            {
                var lockAnim = new DoubleAnimation
                {
                    To = 0.9,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(lockAnim, LockGlyphIcon);
                Storyboard.SetTargetProperty(lockAnim, new PropertyPath("Opacity"));
                sb.Children.Add(lockAnim);
            }

            if (ContactText != null)
            {
                var contactAnim = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(contactAnim, ContactText);
                Storyboard.SetTargetProperty(contactAnim, new PropertyPath("Opacity"));
                sb.Children.Add(contactAnim);
            }

            if (BottomChargingPanel != null)
            {
                var bottomAnim = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(bottomAnim, BottomChargingPanel);
                Storyboard.SetTargetProperty(bottomAnim, new PropertyPath("Opacity"));
                sb.Children.Add(bottomAnim);
            }

            // 4. Nếu ảnh nền đang trượt lên (do mở PIN): Trượt êm ái về 0.0
            if (BackgroundTransform.TranslateY != 0)
            {
                var bgBackAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(260),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(bgBackAnim, BackgroundTransform);
                Storyboard.SetTargetProperty(bgBackAnim, new PropertyPath("TranslateY"));
                sb.Children.Add(bgBackAnim);

                var videoBackAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(260),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(videoBackAnim, VideoTransform);
                Storyboard.SetTargetProperty(videoBackAnim, new PropertyPath("TranslateY"));
                sb.Children.Add(videoBackAnim);
            }

            sb.Completed += (s, e) =>
            {
                bounceBackStory = null;
                ForegroundTransform.ScaleX = 1.0;
                ForegroundTransform.ScaleY = 1.0;
                ForegroundTransform.TranslateX = 0;
                ForegroundTransform.TranslateY = 0;
                ForegroundPanel.Opacity = 1.0;
                if (LockGlyphIcon != null) LockGlyphIcon.Opacity = 0.9;
                if (ContactText != null) ContactText.Opacity = 1.0;
                if (BottomChargingPanel != null) BottomChargingPanel.Opacity = 1.0;
                BackgroundTransform.TranslateX = 0;
                BackgroundTransform.TranslateY = 0.0;
                VideoTransform.TranslateX = 0;
                VideoTransform.TranslateY = 0.0;
            };

            bounceBackStory = sb;
            sb.Begin();
        }

        private void AnimateWallpaperTo(double targetY, double durationMs = 280, EventHandler completed = null)
        {
            if (slideStory != null) { try { slideStory.Stop(); } catch { } slideStory = null; }

            slideStory = new Storyboard();
            var bgSlideAnim = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(bgSlideAnim, BackgroundTransform);
            Storyboard.SetTargetProperty(bgSlideAnim, new PropertyPath("TranslateY"));
            slideStory.Children.Add(bgSlideAnim);

            var videoSlideAnim = new DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(videoSlideAnim, VideoTransform);
            Storyboard.SetTargetProperty(videoSlideAnim, new PropertyPath("TranslateY"));
            slideStory.Children.Add(videoSlideAnim);

            slideStory.Completed += (s, e) =>
            {
                slideStory = null;
                BackgroundTransform.TranslateY = targetY;
                VideoTransform.TranslateY = targetY;
                if (completed != null) completed(s, e);
            };

            slideStory.Begin();
        }

        private void InitiateUnlockSequence()
        {
            try
            {
                if (bounceBackStory != null) { try { bounceBackStory.Stop(); } catch { } bounceBackStory = null; }
                if (slideStory != null) { try { slideStory.Stop(); } catch { } slideStory = null; }

                if (!isPinLocked)
                {
                    double pinHeight = GetPinpadHeightDips();
                    if (pinHeight > 0)
                    {
                        isPinLocked = true;
                        pinpadBoundaryY = -pinHeight;
                    }
                }

                // Đảm bảo OS Shell chắc chắn đã kích hoạt BeginUnlock()
                if (!isUnlockingStarted)
                {
                    isUnlockingStarted = true;
                    try { ExtensibilityApp.BeginUnlock(); } catch { }
                }

                ForegroundTransform.TranslateX = 0;
                ForegroundTransform.TranslateY = 0;
                BackgroundTransform.TranslateX = 0;
                BackgroundTransform.ScaleX = 1.0;
                BackgroundTransform.ScaleY = 1.0;
                VideoTransform.TranslateX = 0;
                VideoTransform.ScaleX = 1.0;
                VideoTransform.ScaleY = 1.0;

                // GIAI ĐOẠN 1: Phóng to hết cỡ (1.20) và mờ hoàn toàn tiện ích về 0.0
                var zoomFadeStory = new Storyboard();

                var scaleXAnim = new DoubleAnimation
                {
                    To = 1.20,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleXAnim, ForegroundTransform);
                Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("ScaleX"));
                zoomFadeStory.Children.Add(scaleXAnim);

                var scaleYAnim = new DoubleAnimation
                {
                    To = 1.20,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleYAnim, ForegroundTransform);
                Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("ScaleY"));
                zoomFadeStory.Children.Add(scaleYAnim);

                var fadeAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(140),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnim, ForegroundPanel);
                Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
                zoomFadeStory.Children.Add(fadeAnim);

                // GIAI ĐOẠN 2: CHỈ KHI TIỆN ÍCH ĐÃ PHÓNG TO VÀ MỜ HẾT TOÀN BỘ MỚI TIẾN HÀNH BƯỚC TIẾP THEO
                zoomFadeStory.Completed += (sZoom, eZoom) =>
                {
                    try
                    {
                        ForegroundPanel.Opacity = 0.0;
                        ForegroundTransform.ScaleX = 1.20;
                        ForegroundTransform.ScaleY = 1.20;

                        if (isPinLocked)
                        {
                            // Đảm bảo OS Shell ở trạng thái BeginUnlock() chuẩn xác
                            try { ExtensibilityApp.BeginUnlock(); } catch { }
                            isUnlockingStarted = true;

                            // CÓ MÃ PIN: Trượt êm ái ảnh nền lên để lộ bàn phím PIN của OS (Chuẩn HyperOS)
                            // TUYỆT ĐỐI KHÔNG GỌI RequestScreenUnlock() vì OS đã tự nâng bàn phím PIN qua BeginUnlock()!
                            // Khi người dùng nhập đúng PIN, OS sẽ tự mở khóa và kết thúc Live Lock Screen.
                            AnimateWallpaperTo(pinpadBoundaryY, 280, (s, e) =>
                            {
                                isPinPadOpen = true;
                            });
                        }
                        else
                        {
                            // KHÔNG CÓ MÃ PIN: Ảnh nền giữ nguyên 100% tại Y=0, mở khóa thẳng vào Start Screen (Chuẩn One UI)
                            try
                            {
                                BackgroundTransform.TranslateY = 0;
                                VideoTransform.TranslateY = 0;
                                if (SystemProtection.ScreenLocked)
                                {
                                    SystemProtection.RequestScreenUnlock();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Unlock error: " + ex.Message);
                                isUnlockingStarted = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[LockView] zoomFadeStory.Completed error: " + ex.Message);
                        isUnlockingStarted = false;
                    }
                };

                zoomFadeStory.Begin();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] InitiateUnlockSequence error: " + ex.Message);
                isUnlockingStarted = false;
            }
        }

        #endregion

        #region Bottom Shortcuts Manipulation & Quick Launch (One UI 6.1)

        private void RightShortcut_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            e.Handled = true;
            StartShortcutGesture(true);
        }

        private void RightShortcut_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            e.Handled = true;
            UpdateShortcutGestureDelta(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y, true);
        }

        private void RightShortcut_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            e.Handled = true;
            CompleteShortcutGesture(e, true);
        }

        private void LeftShortcut_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            e.Handled = true;
            StartShortcutGesture(false);
        }

        private void LeftShortcut_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            e.Handled = true;
            UpdateShortcutGestureDelta(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y, false);
        }

        private void LeftShortcut_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            e.Handled = true;
            CompleteShortcutGesture(e, false);
        }

        private void StartShortcutGesture(bool isRight)
        {
            StopShortcutAnimations();
            _isRightShortcutActive = isRight;
            _isShortcutDragging = true;
            _hasShortcutVibrated = false;
            _shortcutDragX = 0;
            _shortcutDragY = 0;

            double w = LayoutRoot.ActualWidth > 0 ? LayoutRoot.ActualWidth : 480.0;
            double h = LayoutRoot.ActualHeight > 0 ? LayoutRoot.ActualHeight : 800.0;
            ForegroundTransform.CenterX = w / 2.0;
            ForegroundTransform.CenterY = h / 2.0;

            var activeBtn = isRight ? RightShortcutButton : LeftShortcutButton;
            var activeTrans = isRight ? RightShortcutTransform : LeftShortcutTransform;
            var activePanel = isRight ? CameraSwipePanel : TorchSwipePanel;
            var activeIcon = isRight ? (UIElement)CameraSwipeIcon : (UIElement)TorchSwipeIcon;
            var activeIconTrans = isRight ? CameraSwipeIconTransform : TorchSwipeIconTransform;

            // Ẩn bảng điều khiển phía đối diện nếu đang hiển thị
            var inactivePanel = isRight ? TorchSwipePanel : CameraSwipePanel;
            if (inactivePanel != null) inactivePanel.Visibility = Visibility.Collapsed;

            // Khởi tạo lớp phủ làm tối màn hình khóa
            if (ShortcutDimOverlay != null)
            {
                ShortcutDimOverlay.Visibility = Visibility.Visible;
                ShortcutDimOverlay.Opacity = 0.0;
            }

            if (activePanel != null)
            {
                activePanel.Visibility = Visibility.Visible;
                activePanel.Width = 0.0;
                activePanel.Opacity = 0.0;
            }
            if (activeIcon != null)
            {
                activeIcon.Opacity = 0.35;
            }
            if (activeIconTrans != null)
            {
                activeIconTrans.ScaleX = 0.88;
                activeIconTrans.ScaleY = 0.88;
            }
            if (isRight)
            {
                if (CameraSwipeIcon != null) CameraSwipeIcon.Margin = new Thickness(0, 0, 10, 0);
            }
            else
            {
                if (TorchSwipeIcon != null) TorchSwipeIcon.Margin = new Thickness(10, 0, 0, 0);
            }

            // Hiệu ứng chạm & giữ ban đầu (Press & Hold feedback One UI 6.1):
            // Nút góc thu nhỏ và fade out trong 120ms, lớp nền cong đen mờ nhú ra từ mép màn hình 28px
            var sb = new Storyboard();
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

            if (activeTrans != null)
            {
                var scaleX = new DoubleAnimation { To = 0.75, Duration = TimeSpan.FromMilliseconds(120), EasingFunction = easeOut };
                Storyboard.SetTarget(scaleX, activeTrans);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
                sb.Children.Add(scaleX);

                var scaleY = new DoubleAnimation { To = 0.75, Duration = TimeSpan.FromMilliseconds(120), EasingFunction = easeOut };
                Storyboard.SetTarget(scaleY, activeTrans);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
                sb.Children.Add(scaleY);
            }

            if (activeBtn != null)
            {
                var btnFade = new DoubleAnimation { To = 0.0, Duration = TimeSpan.FromMilliseconds(110), EasingFunction = easeOut };
                Storyboard.SetTarget(btnFade, activeBtn);
                Storyboard.SetTargetProperty(btnFade, new PropertyPath("Opacity"));
                sb.Children.Add(btnFade);
            }

            if (activePanel != null)
            {
                var panelPeek = new DoubleAnimation { From = 0.0, To = 28.0, Duration = TimeSpan.FromMilliseconds(120), EasingFunction = easeOut };
                Storyboard.SetTarget(panelPeek, activePanel);
                Storyboard.SetTargetProperty(panelPeek, new PropertyPath("Width"));
                sb.Children.Add(panelPeek);

                var panelFade = new DoubleAnimation { From = 0.0, To = 0.50, Duration = TimeSpan.FromMilliseconds(120), EasingFunction = easeOut };
                Storyboard.SetTarget(panelFade, activePanel);
                Storyboard.SetTargetProperty(panelFade, new PropertyPath("Opacity"));
                sb.Children.Add(panelFade);
            }

            sb.Completed += (s, ev) =>
            {
                _shortcutPressStory = null;
                if (activeTrans != null) { activeTrans.ScaleX = 0.75; activeTrans.ScaleY = 0.75; }
                if (activeBtn != null) activeBtn.Opacity = 0.0;
                if (activePanel != null && !_isShortcutDragging)
                {
                    activePanel.Width = 28.0;
                    activePanel.Opacity = 0.50;
                }
            };

            _shortcutPressStory = sb;
            sb.Begin();
        }

        private void UpdateShortcutGestureDelta(double deltaX, double deltaY, bool isRight)
        {
            if (!_isShortcutDragging) return;

            if (_shortcutPressStory != null)
            {
                try { _shortcutPressStory.Stop(); } catch { }
                _shortcutPressStory = null;
            }

            _shortcutDragX += deltaX;
            _shortcutDragY += deltaY;

            double dist = Math.Sqrt(_shortcutDragX * _shortcutDragX + _shortcutDragY * _shortcutDragY);
            double progress = Math.Min(1.0, dist / ShortcutUnlockThreshold);

            var activePanel = isRight ? CameraSwipePanel : TorchSwipePanel;
            var activeIcon = isRight ? (UIElement)CameraSwipeIcon : (UIElement)TorchSwipeIcon;
            var activeIconTrans = isRight ? CameraSwipeIconTransform : TorchSwipeIconTransform;
            var activeBtn = isRight ? RightShortcutButton : LeftShortcutButton;
            var activeTrans = isRight ? RightShortcutTransform : LeftShortcutTransform;

            if (activeBtn != null) activeBtn.Opacity = 0.0;
            if (activeTrans != null) { activeTrans.ScaleX = 0.75; activeTrans.ScaleY = 0.75; }

            // 1. Độ rộng & độ đậm bảng nền đen cong One UI 6.1:
            // Mở rộng từ 28px tới 200px, nền đen từ mờ 0.50 chuyển dần sang đen rõ ràng 0.94
            if (activePanel != null)
            {
                double targetWidth = 28.0 + (progress * 172.0);
                if (dist > ShortcutUnlockThreshold)
                {
                    targetWidth += Math.Min(25.0, (dist - ShortcutUnlockThreshold) * 0.18);
                }
                activePanel.Width = targetWidth;
                activePanel.Opacity = 0.50 + (progress * 0.44);
            }

            // 2. Icon sáng dần từ mờ (0.35) đến sáng rõ nét (1.0), dịch chuyển nhẹ vào trong và phóng to nhẹ (0.88 -> 1.18)
            if (activeIcon != null)
            {
                activeIcon.Opacity = 0.35 + (progress * 0.65);
            }
            if (activeIconTrans != null)
            {
                double iconScale = 0.88 + (progress * 0.30);
                activeIconTrans.ScaleX = iconScale;
                activeIconTrans.ScaleY = iconScale;
            }

            double marginOffset = 10.0 + (progress * 48.0);
            if (isRight)
            {
                if (CameraSwipeIcon != null) CameraSwipeIcon.Margin = new Thickness(0, 0, marginOffset, 0);
            }
            else
            {
                if (TorchSwipeIcon != null) TorchSwipeIcon.Margin = new Thickness(marginOffset, 0, 0, 0);
            }

            // 3. Các tiện ích màn hình khóa thu nhỏ nhẹ (1.0 -> 0.92), tối dần và fade out
            if (ForegroundTransform != null)
            {
                double scale = 1.0 - (progress * 0.08);
                ForegroundTransform.ScaleX = scale;
                ForegroundTransform.ScaleY = scale;
            }

            if (ForegroundPanel != null)
            {
                ForegroundPanel.Opacity = Math.Max(0.12, 1.0 - (progress * 0.88));
            }

            if (ShortcutDimOverlay != null)
            {
                ShortcutDimOverlay.Opacity = progress * 0.35;
            }

            // 4. Rung phản hồi haptic 1 lần khi vuốt đủ xa (vượt ngưỡng)
            if (dist >= ShortcutUnlockThreshold)
            {
                if (!_hasShortcutVibrated)
                {
                    _hasShortcutVibrated = true;
                    try
                    {
                        Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(25));
                    }
                    catch { }
                }
            }
            else
            {
                _hasShortcutVibrated = false;
            }
        }

        private void CompleteShortcutGesture(ManipulationCompletedEventArgs e, bool isRight)
        {
            if (!_isShortcutDragging) return;
            _isShortcutDragging = false;

            if (_shortcutPressStory != null)
            {
                try { _shortcutPressStory.Stop(); } catch { }
                _shortcutPressStory = null;
            }

            double dist = Math.Sqrt(_shortcutDragX * _shortcutDragX + _shortcutDragY * _shortcutDragY);
            double vx = e.FinalVelocities.LinearVelocity.X;
            double vy = e.FinalVelocities.LinearVelocity.Y;
            double vel = Math.Sqrt(vx * vx + vy * vy);
            bool isFlick = vel >= 800.0;
            bool triggered = (dist >= ShortcutUnlockThreshold) || isFlick;

            if (triggered)
            {
                ExecuteShortcutAction(isRight);
            }
            else
            {
                AnimateShortcutBack(isRight);
            }
        }

        private async void ExecuteShortcutAction(bool isRight)
        {
            var settings = SettingsManager.Current;

            if (isRight)
            {
                // Phím tắt Phải: Mặc định Máy ảnh (Camera)
                ResetShortcutGestureState();

                if (settings.RightShortcut == "Camera")
                {
                    // Tắt đèn pin phần cứng nếu đang bật để tránh xung đột quyền truy cập camera
                    if (FlashlightHelper.IsOn)
                    {
                        await FlashlightHelper.ToggleAsync();
                        if (LeftShortcutImage != null)
                        {
                            LeftShortcutImage.Source = new BitmapImage(new Uri("/Assets/Icons/ic_shortcut_flash.png", UriKind.Relative));
                        }
                    }

                    // Mở In-process Viewfinder với Animation phóng tỏa One UI từ góc dưới phải
                    AnimateOpenCamera();
                    InitializeCamera(CameraType.Primary);
                    HookHardwareCameraButtons();
                }
            }
            else
            {
                // Phím tắt Trái: Đèn pin - Thu gọn bảng điều khiển êm ái về vị trí cũ
                AnimateShortcutBack(false);

                if (settings.LeftShortcut == "Calculator")
                {
                    try
                    {
                        Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(40));
                    }
                    catch { }
                    return;
                }

                // Mặc định: Chế độ Đèn pin
                bool isOn = await FlashlightHelper.ToggleAsync();
                if (LeftShortcutImage != null)
                {
                    LeftShortcutImage.Source = isOn 
                        ? new BitmapImage(new Uri("/Assets/Icons/ic_shortcut_flash_on.png", UriKind.Relative)) 
                        : new BitmapImage(new Uri("/Assets/Icons/ic_shortcut_flash.png", UriKind.Relative));
                }
            }
        }

        private void AnimateShortcutBack(bool isRight)
        {
            StopShortcutAnimations();

            var activeBtn = isRight ? RightShortcutButton : LeftShortcutButton;
            var activeTrans = isRight ? RightShortcutTransform : LeftShortcutTransform;
            var activePanel = isRight ? CameraSwipePanel : TorchSwipePanel;
            var activeIcon = isRight ? (UIElement)CameraSwipeIcon : (UIElement)TorchSwipeIcon;
            var activeIconTrans = isRight ? CameraSwipeIconTransform : TorchSwipeIconTransform;

            var sb = new Storyboard();
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(220);

            // 1. Thu nhỏ chiều rộng panel về 0 và mờ dần
            if (activePanel != null && activePanel.Width > 0)
            {
                var panelWidthAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = duration,
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(panelWidthAnim, activePanel);
                Storyboard.SetTargetProperty(panelWidthAnim, new PropertyPath("Width"));
                sb.Children.Add(panelWidthAnim);

                var panelFadeAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = duration,
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(panelFadeAnim, activePanel);
                Storyboard.SetTargetProperty(panelFadeAnim, new PropertyPath("Opacity"));
                sb.Children.Add(panelFadeAnim);
            }

            // 2. Thu nhỏ icon về tỉ lệ ban đầu (0.88) và độ mờ 0.35
            if (activeIconTrans != null)
            {
                var iconScaleX = new DoubleAnimation { To = 0.88, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(iconScaleX, activeIconTrans);
                Storyboard.SetTargetProperty(iconScaleX, new PropertyPath("ScaleX"));
                sb.Children.Add(iconScaleX);

                var iconScaleY = new DoubleAnimation { To = 0.88, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(iconScaleY, activeIconTrans);
                Storyboard.SetTargetProperty(iconScaleY, new PropertyPath("ScaleY"));
                sb.Children.Add(iconScaleY);
            }

            if (activeIcon != null)
            {
                var iconFade = new DoubleAnimation { To = 0.35, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(iconFade, activeIcon);
                Storyboard.SetTargetProperty(iconFade, new PropertyPath("Opacity"));
                sb.Children.Add(iconFade);
            }

            // 3. Phục hồi nút góc về Scale 1.0 và Opacity 1.0
            if (activeTrans != null)
            {
                var scaleXAnim = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(scaleXAnim, activeTrans);
                Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("ScaleX"));
                sb.Children.Add(scaleXAnim);

                var scaleYAnim = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(scaleYAnim, activeTrans);
                Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("ScaleY"));
                sb.Children.Add(scaleYAnim);
            }

            if (activeBtn != null)
            {
                var btnFadeAnim = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(btnFadeAnim, activeBtn);
                Storyboard.SetTargetProperty(btnFadeAnim, new PropertyPath("Opacity"));
                sb.Children.Add(btnFadeAnim);
            }

            // 4. Phục hồi tiện ích màn hình khóa về Scale 1.0 và Opacity 1.0
            if (ForegroundTransform != null && (ForegroundTransform.ScaleX != 1.0 || ForegroundTransform.ScaleY != 1.0))
            {
                var fgScaleX = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(fgScaleX, ForegroundTransform);
                Storyboard.SetTargetProperty(fgScaleX, new PropertyPath("ScaleX"));
                sb.Children.Add(fgScaleX);

                var fgScaleY = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(fgScaleY, ForegroundTransform);
                Storyboard.SetTargetProperty(fgScaleY, new PropertyPath("ScaleY"));
                sb.Children.Add(fgScaleY);
            }

            if (ForegroundPanel != null && ForegroundPanel.Opacity < 1.0)
            {
                var fgFade = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(fgFade, ForegroundPanel);
                Storyboard.SetTargetProperty(fgFade, new PropertyPath("Opacity"));
                sb.Children.Add(fgFade);
            }

            // 5. Phục hồi lớp phủ tối ShortcutDimOverlay về 0.0
            if (ShortcutDimOverlay != null && ShortcutDimOverlay.Opacity > 0.0)
            {
                var dimFade = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = easeOut };
                Storyboard.SetTarget(dimFade, ShortcutDimOverlay);
                Storyboard.SetTargetProperty(dimFade, new PropertyPath("Opacity"));
                sb.Children.Add(dimFade);
            }

            sb.Completed += (s, ev) =>
            {
                _shortcutSnapBackStory = null;
                ResetShortcutGestureState();
            };

            _shortcutSnapBackStory = sb;
            sb.Begin();
        }

        private void StopShortcutAnimations()
        {
            if (_shortcutPressStory != null)
            {
                try { _shortcutPressStory.Stop(); } catch { }
                _shortcutPressStory = null;
            }
            if (_shortcutSnapBackStory != null)
            {
                try { _shortcutSnapBackStory.Stop(); } catch { }
                _shortcutSnapBackStory = null;
            }
        }

        private void ResetShortcutGestureState()
        {
            _isShortcutDragging = false;
            _hasShortcutVibrated = false;
            _shortcutDragX = 0;
            _shortcutDragY = 0;

            if (CameraSwipePanel != null)
            {
                CameraSwipePanel.Visibility = Visibility.Collapsed;
                CameraSwipePanel.Width = 0.0;
                CameraSwipePanel.Opacity = 0.0;
            }
            if (CameraSwipeIcon != null)
            {
                CameraSwipeIcon.Opacity = 0.35;
                CameraSwipeIcon.Margin = new Thickness(0, 0, 10, 0);
            }
            if (CameraSwipeIconTransform != null)
            {
                CameraSwipeIconTransform.ScaleX = 0.88;
                CameraSwipeIconTransform.ScaleY = 0.88;
            }

            if (TorchSwipePanel != null)
            {
                TorchSwipePanel.Visibility = Visibility.Collapsed;
                TorchSwipePanel.Width = 0.0;
                TorchSwipePanel.Opacity = 0.0;
            }
            if (TorchSwipeIcon != null)
            {
                TorchSwipeIcon.Opacity = 0.35;
                TorchSwipeIcon.Margin = new Thickness(10, 0, 0, 0);
            }
            if (TorchSwipeIconTransform != null)
            {
                TorchSwipeIconTransform.ScaleX = 0.88;
                TorchSwipeIconTransform.ScaleY = 0.88;
            }

            if (ShortcutDimOverlay != null)
            {
                ShortcutDimOverlay.Visibility = Visibility.Collapsed;
                ShortcutDimOverlay.Opacity = 0.0;
            }

            if (LeftShortcutButton != null) LeftShortcutButton.Opacity = 1.0;
            if (LeftShortcutTransform != null) { LeftShortcutTransform.ScaleX = 1.0; LeftShortcutTransform.ScaleY = 1.0; }

            if (RightShortcutButton != null) RightShortcutButton.Opacity = 1.0;
            if (RightShortcutTransform != null) { RightShortcutTransform.ScaleX = 1.0; RightShortcutTransform.ScaleY = 1.0; }

            if (ForegroundTransform != null) { ForegroundTransform.ScaleX = 1.0; ForegroundTransform.ScaleY = 1.0; }
            if (ForegroundPanel != null) ForegroundPanel.Opacity = 1.0;
        }

        #endregion

        private void CloseCamera_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            AnimateCloseCamera();
        }

        private void AnimateOpenCamera()
        {
            if (cameraAnimStory != null)
            {
                try { cameraAnimStory.Stop(); } catch { }
                cameraAnimStory = null;
            }

            CameraOverlay.Visibility = Visibility.Visible;
            CameraOverlay.Opacity = 0.0;
            CameraOverlayTransform.ScaleX = 0.08;
            CameraOverlayTransform.ScaleY = 0.08;

            var sb = new Storyboard();

            var scaleXAnim = new DoubleAnimation
            {
                From = 0.08,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleXAnim, CameraOverlayTransform);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("ScaleX"));
            sb.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                From = 0.08,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleYAnim, CameraOverlayTransform);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("ScaleY"));
            sb.Children.Add(scaleYAnim);

            var opacityAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(opacityAnim, CameraOverlay);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            sb.Children.Add(opacityAnim);

            sb.Completed += (s, e) =>
            {
                cameraAnimStory = null;
                CameraOverlayTransform.ScaleX = 1.0;
                CameraOverlayTransform.ScaleY = 1.0;
                CameraOverlay.Opacity = 1.0;
            };

            cameraAnimStory = sb;
            sb.Begin();
        }

        private void AnimateCloseCamera()
        {
            if (cameraAnimStory != null)
            {
                try { cameraAnimStory.Stop(); } catch { }
                cameraAnimStory = null;
            }

            var sb = new Storyboard();

            var scaleXAnim = new DoubleAnimation
            {
                To = 0.08,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(scaleXAnim, CameraOverlayTransform);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("ScaleX"));
            sb.Children.Add(scaleXAnim);

            var scaleYAnim = new DoubleAnimation
            {
                To = 0.08,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(scaleYAnim, CameraOverlayTransform);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("ScaleY"));
            sb.Children.Add(scaleYAnim);

            var opacityAnim = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(opacityAnim, CameraOverlay);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            sb.Children.Add(opacityAnim);

            sb.Completed += (s, e) =>
            {
                cameraAnimStory = null;
                CameraOverlay.Visibility = Visibility.Collapsed;
                CameraOverlayTransform.ScaleX = 1.0;
                CameraOverlayTransform.ScaleY = 1.0;
                CameraOverlay.Opacity = 1.0;
                UnhookHardwareCameraButtons();
                DisposeCamera();
            };

            cameraAnimStory = sb;
            sb.Begin();
        }

        #region In-Process Camera Controller & Events

        private void InitializeCamera(CameraType cameraType)
        {
            DisposeCamera();
            try
            {
                if (!PhotoCamera.IsCameraTypeSupported(cameraType))
                {
                    cameraType = CameraType.Primary;
                }
                _currentCameraType = cameraType;
                _photoCamera = new PhotoCamera(cameraType);
                _photoCamera.Initialized += PhotoCamera_Initialized;
                _photoCamera.CaptureImageAvailable += PhotoCamera_CaptureImageAvailable;
                _photoCamera.CaptureThumbnailAvailable += PhotoCamera_CaptureThumbnailAvailable;
                _photoCamera.AutoFocusCompleted += PhotoCamera_AutoFocusCompleted;

                CameraVideoBrush.SetSource(_photoCamera);

                if (CameraFlipBtn != null)
                {
                    CameraFlipBtn.Visibility = PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] InitializeCamera error: " + ex.Message);
                ShowCameraToast("Không thể khởi động máy ảnh");
            }
        }

        private void DisposeCamera()
        {
            if (_photoCamera != null)
            {
                try
                {
                    _photoCamera.Initialized -= PhotoCamera_Initialized;
                    _photoCamera.CaptureImageAvailable -= PhotoCamera_CaptureImageAvailable;
                    _photoCamera.CaptureThumbnailAvailable -= PhotoCamera_CaptureThumbnailAvailable;
                    _photoCamera.AutoFocusCompleted -= PhotoCamera_AutoFocusCompleted;
                    _photoCamera.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[LockView] DisposeCamera error: " + ex.Message);
                }
                _photoCamera = null;
            }
            if (CameraVideoBrush != null)
            {
                try { CameraVideoBrush.SetSource((CaptureSource)null); } catch { }
            }
            if (_shutterFlashStory != null)
            {
                try { _shutterFlashStory.Stop(); } catch { }
                _shutterFlashStory = null;
            }
            if (CameraShutterFlash != null)
            {
                CameraShutterFlash.Opacity = 0.0;
                CameraShutterFlash.Visibility = Visibility.Collapsed;
            }
            _isCapturing = false;
            _isFocusing = false;
        }

        private void PhotoCamera_Initialized(object sender, CameraOperationCompletedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!e.Succeeded)
                {
                    ShowCameraToast("Khởi tạo máy ảnh thất bại");
                    return;
                }
                if (_photoCamera != null)
                {
                    CameraBrushTransform.Rotation = _photoCamera.Orientation;
                    UpdateFlashUI();
                }
            });
        }

        private void PhotoCamera_AutoFocusCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            _isFocusing = false;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (CameraFocusRing != null)
                {
                    var fadeAnim = new DoubleAnimation
                    {
                        To = 0.0,
                        Duration = TimeSpan.FromMilliseconds(400)
                    };
                    Storyboard sb = new Storyboard();
                    Storyboard.SetTarget(fadeAnim, CameraFocusRing);
                    Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
                    sb.Completed += (s, ev) =>
                    {
                        CameraFocusRing.Visibility = Visibility.Collapsed;
                        CameraFocusRing.Opacity = 1.0;
                    };
                    sb.Begin();
                }
            });
        }

        private void PhotoCamera_CaptureThumbnailAvailable(object sender, ContentReadyEventArgs e)
        {
            try
            {
                byte[] thumbBytes;
                using (var ms = new MemoryStream())
                {
                    e.ImageStream.CopyTo(ms);
                    thumbBytes = ms.ToArray();
                }

                int orientation = (_photoCamera != null) ? (int)Math.Round(_photoCamera.Orientation) : 90;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        using (var ms = new MemoryStream(thumbBytes))
                        {
                            WriteableBitmap wb = Microsoft.Phone.PictureDecoder.DecodeJpeg(ms);
                            if (wb != null)
                            {
                                int rot = orientation;
                                if (rot <= 0 || (rot == 0 && wb.PixelWidth > wb.PixelHeight))
                                {
                                    rot = 90;
                                }
                                WriteableBitmap finalThumb = RotateWriteableBitmap(wb, rot);
                                if (CameraThumbImage != null && CameraThumbEmptyIcon != null)
                                {
                                    CameraThumbImage.Source = finalThumb;
                                    CameraThumbEmptyIcon.Visibility = Visibility.Collapsed;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[LockView] Thumbnail UI error: " + ex.Message);
                        try
                        {
                            var fallbackMs = new MemoryStream(thumbBytes);
                            var bmp = new BitmapImage();
                            bmp.SetSource(fallbackMs);
                            if (CameraThumbImage != null && CameraThumbEmptyIcon != null)
                            {
                                CameraThumbImage.Source = bmp;
                                CameraThumbEmptyIcon.Visibility = Visibility.Collapsed;
                            }
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] CaptureThumbnail error: " + ex.Message);
            }
        }

        private void PhotoCamera_CaptureImageAvailable(object sender, ContentReadyEventArgs e)
        {
            try
            {
                byte[] rawBytes;
                using (var rawMs = new MemoryStream())
                {
                    if (e.ImageStream.CanSeek)
                    {
                        try { e.ImageStream.Position = 0; } catch { }
                    }
                    e.ImageStream.CopyTo(rawMs);
                    rawBytes = rawMs.ToArray();
                }

                int orientation = (_photoCamera != null) ? (int)Math.Round(_photoCamera.Orientation) : 90;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        byte[] buffer = rawBytes;

                        try
                        {
                            using (var decodeMs = new MemoryStream(rawBytes))
                            {
                                WriteableBitmap wb = Microsoft.Phone.PictureDecoder.DecodeJpeg(decodeMs);
                                if (wb != null)
                                {
                                    int rot = orientation;
                                    if (rot <= 0 || (rot == 0 && wb.PixelWidth > wb.PixelHeight))
                                    {
                                        rot = 90;
                                    }

                                    WriteableBitmap finalWb = RotateWriteableBitmap(wb, rot);
                                    using (var rotatedMs = new MemoryStream())
                                    {
                                        finalWb.SaveJpeg(rotatedMs, finalWb.PixelWidth, finalWb.PixelHeight, 0, 95);
                                        buffer = rotatedMs.ToArray();
                                    }
                                }
                            }
                        }
                        catch (Exception exRotate)
                        {
                            System.Diagnostics.Debug.WriteLine("[LockView] Rotate captured image error: " + exRotate.Message);
                        }

                        string fileName = string.Format("WP_{0:yyyyMMdd_HHmmssfff}_Pro.jpg", DateTime.Now);

                        // Lưu ảnh vào Camera Roll thông qua XNA MediaLibrary với việc giải phóng đầy đủ tài nguyên COM
                        try
                        {
                            using (var library = new Microsoft.Xna.Framework.Media.MediaLibrary())
                            using (var ms = new MemoryStream(buffer))
                            {
                                var pic = library.SavePictureToCameraRoll(fileName, ms);
                                if (pic != null)
                                {
                                    pic.Dispose();
                                }
                            }
                        }
                        catch (Exception exLib)
                        {
                            System.Diagnostics.Debug.WriteLine("[LockView] MediaLibrary.SavePictureToCameraRoll error: " + exLib.Message);
                        }

                        _isCapturing = false;
                        if (CameraShutterFlash != null)
                        {
                            CameraShutterFlash.Opacity = 0.0;
                            CameraShutterFlash.Visibility = Visibility.Collapsed;
                        }
                        ShowCameraToast("Đã lưu vào Bộ sưu tập");

                        // Giải phóng tức thì các mảng byte lớn để giảm đỉnh RAM dưới ngưỡng 38MB
                        buffer = null;
                        rawBytes = null;
                        GC.Collect();
                    }
                    catch (Exception exSave)
                    {
                        System.Diagnostics.Debug.WriteLine("[LockView] SaveImage error: " + exSave.Message);
                        _isCapturing = false;
                        if (CameraShutterFlash != null)
                        {
                            CameraShutterFlash.Opacity = 0.0;
                            CameraShutterFlash.Visibility = Visibility.Collapsed;
                        }
                        ShowCameraToast("Lỗi khi lưu ảnh: " + exSave.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] CaptureImageAvailable error: " + ex.Message);
            }
        }

        private static WriteableBitmap RotateWriteableBitmap(WriteableBitmap wb, int orientation)
        {
            int w = wb.PixelWidth;
            int h = wb.PixelHeight;

            if (w > h && (orientation != 90 && orientation != 180 && orientation != 270))
            {
                orientation = 90;
            }

            if (orientation == 90)
            {
                var rotated = new WriteableBitmap(h, w);
                int[] src = wb.Pixels;
                int[] dst = rotated.Pixels;

                for (int y = 0; y < h; y++)
                {
                    int yOffset = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        dst[x * h + y] = src[yOffset + x];
                    }
                }
                return rotated;
            }
            else if (orientation == 270)
            {
                var rotated = new WriteableBitmap(h, w);
                int[] src = wb.Pixels;
                int[] dst = rotated.Pixels;

                for (int y = 0; y < h; y++)
                {
                    int yOffset = y * w;
                    int destX = h - 1 - y;
                    for (int x = 0; x < w; x++)
                    {
                        dst[(w - 1 - x) * h + destX] = src[yOffset + x];
                    }
                }
                return rotated;
            }
            else if (orientation == 180)
            {
                var rotated = new WriteableBitmap(w, h);
                int[] src = wb.Pixels;
                int[] dst = rotated.Pixels;
                int total = w * h;

                for (int i = 0; i < total; i++)
                {
                    dst[total - 1 - i] = src[i];
                }
                return rotated;
            }

            return wb;
        }

        private void HookHardwareCameraButtons()
        {
            try
            {
                CameraButtons.ShutterKeyPressed += CameraButtons_ShutterKeyPressed;
                CameraButtons.ShutterKeyHalfPressed += CameraButtons_ShutterKeyHalfPressed;
            }
            catch { }
        }

        private void UnhookHardwareCameraButtons()
        {
            try
            {
                CameraButtons.ShutterKeyPressed -= CameraButtons_ShutterKeyPressed;
                CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;
            }
            catch { }
        }

        private void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (CameraOverlay.Visibility == Visibility.Visible)
                {
                    CameraShutter_Tap(this, null);
                }
            });
        }

        private void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (CameraOverlay.Visibility == Visibility.Visible && _photoCamera != null && _photoCamera.IsFocusSupported && !_isFocusing)
                {
                    _isFocusing = true;
                    try { _photoCamera.Focus(); } catch { _isFocusing = false; }
                }
            });
        }

        private void CameraViewfinder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_photoCamera == null) return;

            Point pt = e.GetPosition(CameraViewfinderCanvas);

            // Hiển thị vòng tròn lấy nét vàng One UI 6.1 tại điểm chạm
            if (CameraFocusRing != null && CameraFocusTransform != null)
            {
                Canvas.SetLeft(CameraFocusRing, pt.X - 34);
                Canvas.SetTop(CameraFocusRing, pt.Y - 34);
                CameraFocusRing.Visibility = Visibility.Visible;
                CameraFocusRing.Opacity = 1.0;
                CameraFocusTransform.ScaleX = 1.4;
                CameraFocusTransform.ScaleY = 1.4;

                var scaleAnimX = new DoubleAnimation
                {
                    From = 1.4,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleAnimX, CameraFocusTransform);
                Storyboard.SetTargetProperty(scaleAnimX, new PropertyPath("ScaleX"));

                var scaleAnimY = new DoubleAnimation
                {
                    From = 1.4,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleAnimY, CameraFocusTransform);
                Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath("ScaleY"));

                Storyboard sb = new Storyboard();
                sb.Children.Add(scaleAnimX);
                sb.Children.Add(scaleAnimY);
                sb.Begin();
            }

            _isFocusing = true;
            if (_photoCamera.IsFocusAtPointSupported)
            {
                try
                {
                    double normX = Math.Max(0.0, Math.Min(1.0, pt.X / Math.Max(1.0, CameraViewfinderCanvas.ActualWidth)));
                    double normY = Math.Max(0.0, Math.Min(1.0, pt.Y / Math.Max(1.0, CameraViewfinderCanvas.ActualHeight)));
                    _photoCamera.FocusAtPoint(normX, normY);
                }
                catch
                {
                    try { _photoCamera.Focus(); } catch { _isFocusing = false; }
                }
            }
            else if (_photoCamera.IsFocusSupported)
            {
                try { _photoCamera.Focus(); } catch { _isFocusing = false; }
            }
            else
            {
                _isFocusing = false;
            }
        }

        private void CameraShutter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_isCapturing || _photoCamera == null) return;
            _isCapturing = true;

            // Phản hồi rung 40ms chuẩn nút bấm camera One UI
            try { Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(40)); } catch { }

            // Hiệu ứng co giãn nút chụp One UI 6.1 (Scale 1.0 -> 0.88 -> 1.0)
            if (CameraShutterTransform != null)
            {
                var shrinkAnimX = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.88,
                    AutoReverse = true,
                    Duration = TimeSpan.FromMilliseconds(90),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(shrinkAnimX, CameraShutterTransform);
                Storyboard.SetTargetProperty(shrinkAnimX, new PropertyPath("ScaleX"));

                var shrinkAnimY = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.88,
                    AutoReverse = true,
                    Duration = TimeSpan.FromMilliseconds(90),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(shrinkAnimY, CameraShutterTransform);
                Storyboard.SetTargetProperty(shrinkAnimY, new PropertyPath("ScaleY"));

                Storyboard sb = new Storyboard();
                sb.Children.Add(shrinkAnimX);
                sb.Children.Add(shrinkAnimY);
                sb.Begin();
            }

            // Hiệu ứng chớp màn hình trắng (Shutter Flash) One UI 6.1
            PlayShutterFlashAnimation();

            try
            {
                _photoCamera.CaptureImage();
            }
            catch (Exception ex)
            {
                _isCapturing = false;
                ShowCameraToast("Lỗi khi chụp: " + ex.Message);
            }
        }

        private void CameraFlash_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_photoCamera == null) return;

            if (_currentFlashMode == FlashMode.Auto)
            {
                _currentFlashMode = FlashMode.On;
            }
            else if (_currentFlashMode == FlashMode.On)
            {
                _currentFlashMode = FlashMode.Off;
            }
            else
            {
                _currentFlashMode = FlashMode.Auto;
            }

            try
            {
                if (_photoCamera.IsFlashModeSupported(_currentFlashMode))
                {
                    _photoCamera.FlashMode = _currentFlashMode;
                }
            }
            catch { }

            UpdateFlashUI();
        }

        private void UpdateFlashUI()
        {
            if (CameraFlashText == null || CameraFlashIcon == null) return;

            switch (_currentFlashMode)
            {
                case FlashMode.Auto:
                    CameraFlashText.Text = "Tự động";
                    CameraFlashIcon.Text = "⚡";
                    CameraFlashBtn.Opacity = 1.0;
                    break;
                case FlashMode.On:
                    CameraFlashText.Text = "Bật";
                    CameraFlashIcon.Text = "⚡";
                    CameraFlashBtn.Opacity = 1.0;
                    break;
                case FlashMode.Off:
                    CameraFlashText.Text = "Tắt";
                    CameraFlashIcon.Text = "⚡";
                    CameraFlashBtn.Opacity = 0.5;
                    break;
            }
        }

        private void CameraFlip_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing))
            {
                ShowCameraToast("Không tìm thấy máy ảnh trước");
                return;
            }

            var nextType = (_currentCameraType == CameraType.Primary)
                ? CameraType.FrontFacing
                : CameraType.Primary;

            InitializeCamera(nextType);
        }

        private void CameraThumb_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowCameraToast("Đã lưu vào Album Ảnh");
        }

        private void ShowCameraToast(string message)
        {
            if (CameraStatusPill == null || CameraStatusText == null) return;

            CameraStatusText.Text = message;
            CameraStatusPill.Visibility = Visibility.Visible;
            CameraStatusPill.Opacity = 1.0;

            if (_cameraToastTimer != null)
            {
                _cameraToastTimer.Stop();
            }
            else
            {
                _cameraToastTimer = new DispatcherTimer();
                _cameraToastTimer.Interval = TimeSpan.FromSeconds(2.5);
                _cameraToastTimer.Tick += (s, e) =>
                {
                    _cameraToastTimer.Stop();
                    if (CameraStatusPill != null)
                    {
                        var fade = new DoubleAnimation
                        {
                            To = 0.0,
                            Duration = TimeSpan.FromMilliseconds(300)
                        };
                        Storyboard sb = new Storyboard();
                        Storyboard.SetTarget(fade, CameraStatusPill);
                        Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
                        sb.Completed += (snd, ev) =>
                        {
                            CameraStatusPill.Visibility = Visibility.Collapsed;
                        };
                        sb.Begin();
                    }
                };
            }
            _cameraToastTimer.Start();
        }

        private void PlayShutterFlashAnimation()
        {
            if (CameraShutterFlash == null) return;

            if (_shutterFlashStory != null)
            {
                try { _shutterFlashStory.Stop(); } catch { }
                _shutterFlashStory = null;
            }

            CameraShutterFlash.Visibility = Visibility.Visible;
            CameraShutterFlash.Opacity = 0.85;

            var flashAnim = new DoubleAnimation
            {
                From = 0.85,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var sb = new Storyboard();
            Storyboard.SetTarget(flashAnim, CameraShutterFlash);
            Storyboard.SetTargetProperty(flashAnim, new PropertyPath("Opacity"));

            sb.Completed += (s, e) =>
            {
                if (CameraShutterFlash != null)
                {
                    CameraShutterFlash.Opacity = 0.0;
                    CameraShutterFlash.Visibility = Visibility.Collapsed;
                }
                _shutterFlashStory = null;
            };

            _shutterFlashStory = sb;
            sb.Begin();
        }

        #endregion

        #region One UI 6.1 Music Widget (Listen Together)

        private bool isMusicExpanded = false;
        private bool isFavorite = false;
        private bool isShuffle = false;
        private const double MusicCollapsedCardHeight = 82.0;
        private const double MusicExpandedCardHeight = 184.0;

        private void ApplyMusicWidgetTransparency()
        {
            try
            {
                var settings = SettingsManager.Current;
                int trans = (settings != null) ? settings.MusicWidgetTransparency : 60;
                if (trans < 0) trans = 0;
                if (trans > 100) trans = 100;

                // 0% transparency = đục hoàn toàn (alpha = 255)
                // 100% transparency = trong suốt gần như hoàn toàn (alpha = 20)
                // Chuẩn One UI 6.1 (60% trong suốt): alpha ~ 102
                byte alpha = (byte)Math.Max(20, Math.Min(255, (int)(255 * (100 - trans) / 100.0)));
                MusicWidgetCard.Background = new SolidColorBrush(Color.FromArgb(alpha, 24, 34, 42));

                if (MusicExpandedBgOverlay != null)
                {
                    byte overlayAlpha = (byte)Math.Max(25, Math.Min(220, (int)(133 * (100 - trans) / 100.0 + 25)));
                    MusicExpandedBgOverlay.Background = new SolidColorBrush(Color.FromArgb(overlayAlpha, 18, 24, 32));
                }
            }
            catch { }
        }

        private void InitMusicWidget()
        {
            var settings = SettingsManager.Current;
            if (!settings.ShowMusicWidget)
            {
                MusicWidgetCard.Visibility = Visibility.Collapsed;
                return;
            }

            ApplyMusicWidgetTransparency();

            MusicWidgetService.Instance.PlaybackStateChanged -= OnMusicPlaybackStateChanged;
            MusicWidgetService.Instance.PlaybackStateChanged += OnMusicPlaybackStateChanged;
            MusicWidgetService.Instance.PositionTick -= OnMusicPositionTick;
            MusicWidgetService.Instance.PositionTick += OnMusicPositionTick;

            UpdateMusicUi();

            // Khởi động lắng nghe máy chủ phát nhạc nội bộ YTMusicWP
            MusicWidgetService.Instance.Start(settings.MusicServerEndpoint);
        }

        private void DisposeMusicWidget()
        {
            try
            {
                MusicWidgetService.Instance.PlaybackStateChanged -= OnMusicPlaybackStateChanged;
                MusicWidgetService.Instance.PositionTick -= OnMusicPositionTick;
                MusicWidgetService.Instance.Stop();
            }
            catch { }

            // Giải phóng ImageSource để tối ưu RAM cho máy 512MB
            if (MusicCoverBrushCollapsed != null) MusicCoverBrushCollapsed.ImageSource = null;
            if (MusicBgBrush != null) MusicBgBrush.ImageSource = null;
            if (MusicAppIconCollapsed != null) MusicAppIconCollapsed.Source = null;
            if (MusicLeftActionImage != null) MusicLeftActionImage.Source = null;
            if (MusicRightActionImage != null) MusicRightActionImage.Source = null;
            _currentAppIconKey = null;
            ResetMusicSeekPalette();
            StopWaveAnimation();
            _waveAmplitudeScale = 0.0;
            _targetWaveAmplitudeScale = 0.0;
            _isWaveTransitioning = false;
            if (_waveTimer != null)
            {
                _waveTimer.Tick -= WaveTimer_Tick;
                _waveTimer = null;
            }
            if (MusicSeekWaveBackPath != null) MusicSeekWaveBackPath.Data = null;
            if (MusicSeekWaveFrontPath != null) MusicSeekWaveFrontPath.Data = null;
            if (_waveBackPoints != null) _waveBackPoints.Clear();
            if (_waveFrontPoints != null) _waveFrontPoints.Clear();
            _waveBackPoints = null;
            _waveFrontPoints = null;
            _waveBackGeometry = null;
            _waveFrontGeometry = null;
        }

        private void OnMusicPlaybackStateChanged()
        {
            UpdateMusicUi();
        }

        private void OnMusicPositionTick(long positionMs, long durationMs)
        {
            MusicCurrentTimeText.Text = FormatMs(positionMs);
            if (durationMs > 0)
            {
                MusicTotalTimeText.Text = FormatMs(durationMs);
                UpdateSeekbar(positionMs, durationMs);
            }
        }

        private void UpdateSeekbar(long positionMs, long durationMs)
        {
            if (durationMs <= 0 || MusicSeekbarContainer == null) return;
            double totalWidth = MusicSeekbarContainer.ActualWidth > 0 ? MusicSeekbarContainer.ActualWidth : 400.0;

            // 1. Cập nhật thanh tải đệm (Buffer Progress) chuẩn AOSP PlaybackState.getBufferedPosition
            var svc = MusicWidgetService.Instance;
            if (MusicSeekBufferedTrack != null)
            {
                double bufRatio = Math.Max(0.0, Math.Min(1.0, (double)svc.BufferedPositionMs / durationMs));
                MusicSeekBufferedTrack.Width = bufRatio * totalWidth;
            }

            // 2. Cập nhật thanh tiến độ phát và con chạy One UI Thumb tròn
            double ratio = Math.Max(0.0, Math.Min(1.0, (double)positionMs / durationMs));
            double playedWidth = ratio * totalWidth;
            MusicSeekPlayedTrack.Width = playedWidth;

            // 3. Cập nhật rãnh sóng gợn One UI 6.1 (Wavy Track trôi từ trái qua phải khi đang phát)
            UpdateWaveProgress(playedWidth, svc.IsPlaying);

            double thumbX = Math.Max(0.0, Math.Min(totalWidth - 18.0, playedWidth - 9.0));
            MusicSeekThumb.Margin = new Thickness(thumbX, 14, 0, 0);
        }

        private DispatcherTimer _waveTimer = null;
        private double _wavePhaseSmall = 0.0;
        private double _wavePhaseLarge = 0.0;
        private double _currentPlayedWidth = 0.0;
        private bool _isWaveAnimating = false;
        private const int WAVE_POINTS_COUNT = 36;

        // Hiệu ứng dâng trào / hạ phẳng sóng biển khi Phát / Tạm dừng chuẩn Samsung One UI 6.1
        private double _waveAmplitudeScale = 0.0;
        private double _targetWaveAmplitudeScale = 0.0;
        private double _waveTransitionStartScale = 0.0;
        private DateTime _waveTransitionStartTime = DateTime.UtcNow;
        private bool _isWaveTransitioning = false;
        private const double WAVE_TRANSITION_DURATION_MS = 320.0; // 320ms mượt mà chuẩn One UI

        private void UpdateWaveProgress(double playedWidth, bool isPlaying)
        {
            if (MusicSeekWaveCanvas == null) return;

            _currentPlayedWidth = playedWidth;

            bool shouldWave = isPlaying && isMusicExpanded && playedWidth >= 20.0;
            double target = shouldWave ? 1.0 : 0.0;

            if (target != _targetWaveAmplitudeScale)
            {
                _targetWaveAmplitudeScale = target;
                _waveTransitionStartScale = _waveAmplitudeScale;
                _waveTransitionStartTime = DateTime.UtcNow;
                _isWaveTransitioning = true;
            }

            if (isMusicExpanded && playedWidth >= 20.0)
            {
                if (MusicSeekPlayedTrack != null) MusicSeekPlayedTrack.Visibility = Visibility.Collapsed;
                MusicSeekWaveCanvas.Visibility = Visibility.Visible;
                MusicSeekWaveCanvas.Width = playedWidth;

                if (shouldWave || _isWaveTransitioning)
                {
                    StartWaveAnimation();
                }
                else
                {
                    StopWaveAnimation();
                }
                UpdateWaveGeometry(playedWidth);
            }
            else
            {
                MusicSeekWaveCanvas.Visibility = Visibility.Collapsed;
                if (MusicSeekPlayedTrack != null)
                {
                    MusicSeekPlayedTrack.Visibility = Visibility.Visible;
                    MusicSeekPlayedTrack.Width = playedWidth;
                }
                StopWaveAnimation();
            }
        }

        private void StartWaveAnimation()
        {
            if (_isWaveAnimating) return;

            if (_waveTimer == null)
            {
                _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30 FPS mượt mà và tiết kiệm pin
                _waveTimer.Tick += WaveTimer_Tick;
            }

            _isWaveAnimating = true;
            _waveTimer.Start();
        }

        private void StopWaveAnimation()
        {
            if (!_isWaveAnimating) return;

            _isWaveAnimating = false;
            if (_waveTimer != null)
            {
                _waveTimer.Stop();
            }
        }

        private void WaveTimer_Tick(object sender, EventArgs e)
        {
            if (!_isWaveAnimating) return;

            try
            {
                // 1. Cập nhật hệ số biên độ sóng khi chuyển đổi Phát / Tạm dừng (Morphing Animation)
                if (_isWaveTransitioning)
                {
                    double elapsed = (DateTime.UtcNow - _waveTransitionStartTime).TotalMilliseconds;
                    double t = Math.Max(0.0, Math.Min(1.0, elapsed / WAVE_TRANSITION_DURATION_MS));
                    // Cubic Ease Out (mềm mại, êm dịu)
                    double ease = 1.0 - Math.Pow(1.0 - t, 3.0);
                    _waveAmplitudeScale = _waveTransitionStartScale + (_targetWaveAmplitudeScale - _waveTransitionStartScale) * ease;

                    if (t >= 1.0)
                    {
                        _waveAmplitudeScale = _targetWaveAmplitudeScale;
                        _isWaveTransitioning = false;

                        // Khi đã hạ phẳng hoàn toàn về 0 khi tạm dừng:
                        // Dừng timer để giải phóng 100% CPU, nhưng GIỮ NGUYÊN canvas sóng ở trạng thái phẳng để trọn vẹn màu sắc sóng trước không bị mất
                        if (_targetWaveAmplitudeScale <= 0.0)
                        {
                            StopWaveAnimation();
                            UpdateWaveGeometry(_currentPlayedWidth);
                            return;
                        }
                    }
                }

                if (_currentPlayedWidth < 20.0) return;

                // 2. Tốc độ trôi pha: giảm dần nhịp nhàng khi sóng đang hạ phẳng xuống
                double speedFactor = Math.Max(0.15, _waveAmplitudeScale);

                // Sóng biển nhỏ (ở phía sau): v ≈ 81.6 px/s
                _wavePhaseSmall += 0.180 * speedFactor;
                if (_wavePhaseSmall > 6283.185) _wavePhaseSmall -= 6283.185;

                // Sóng biển lớn (ở phía trước): v ≈ 59.3 px/s
                _wavePhaseLarge += 0.092 * speedFactor;
                if (_wavePhaseLarge > 6283.185) _wavePhaseLarge -= 6283.185;

                UpdateWaveGeometry(_currentPlayedWidth);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] WaveTimer_Tick error: " + ex.Message);
            }
        }

        private PathGeometry _waveBackGeometry = null;
        private PointCollection _waveBackPoints = null;
        private PathGeometry _waveFrontGeometry = null;
        private PointCollection _waveFrontPoints = null;

        private void EnsureWaveGeometries()
        {
            if (_waveBackGeometry == null)
            {
                _waveBackPoints = new PointCollection();
                var seg = new PolyLineSegment { Points = _waveBackPoints };
                var fig = new PathFigure
                {
                    IsClosed = true,
                    IsFilled = true,
                    StartPoint = new Point(5.0, 28.0)
                };
                fig.Segments.Add(seg);
                _waveBackGeometry = new PathGeometry();
                _waveBackGeometry.Figures.Add(fig);
            }

            if (_waveFrontGeometry == null)
            {
                _waveFrontPoints = new PointCollection();
                var seg = new PolyLineSegment { Points = _waveFrontPoints };
                var fig = new PathFigure
                {
                    IsClosed = true,
                    IsFilled = true,
                    StartPoint = new Point(5.0, 28.0)
                };
                fig.Segments.Add(seg);
                _waveFrontGeometry = new PathGeometry();
                _waveFrontGeometry.Figures.Add(fig);
            }

            if (MusicSeekWaveBackPath != null && MusicSeekWaveBackPath.Data != _waveBackGeometry)
            {
                MusicSeekWaveBackPath.Data = _waveBackGeometry;
            }
            if (MusicSeekWaveFrontPath != null && MusicSeekWaveFrontPath.Data != _waveFrontGeometry)
            {
                MusicSeekWaveFrontPath.Data = _waveFrontGeometry;
            }
        }

        private void UpdateSingleWaveGeometry(PointCollection points, double W, double hMax, double wavelength, double phase, double power)
        {
            if (points == null) return;
            points.Clear();

            double yBaseTop = 18.0;
            double yBottom = 28.0;
            double twoPiOverLambda = 6.283185307179586 / wavelength;

            // Chiều rộng hiệu dụng từ mép bo tròn x = 5.0 đến x = W
            double spanW = Math.Max(1.0, W - 5.0);
            // Khoảng cách bắt đầu nổi lên xa dần theo thời gian/độ dài seek (~24% chiều dài, tối thiểu 20px)
            double taperLen = Math.Max(20.0, spanW * 0.24);

            // 1. Cung tròn mép trái R=5px (từ Y=28 qua X=0, Y=23 đến Y=18)
            // Hoàn toàn mượt mà, không phụ thuộc ArcSegment, triệt tiêu 100% lỗi Direct2D/NaN
            points.Add(new Point(2.50, 27.33));
            points.Add(new Point(0.67, 25.50));
            points.Add(new Point(0.00, 23.00));
            points.Add(new Point(0.67, 20.50));
            points.Add(new Point(2.50, 18.67));
            points.Add(new Point(5.00, 18.00));

            // 2. Các điểm gợn sóng từ x = 5.0 đến x = W
            for (int i = 1; i <= WAVE_POINTS_COUNT; i++)
            {
                double u = (double)i / WAVE_POINTS_COUNT;
                double x = 5.0 + u * spanW;

                // Vát êm đềm ở 2 đầu: bình phương hàm sin (Raised Cosine) giúp tiếp tuyến tại 2 đầu phẳng hoàn toàn (slope = 0)
                double uLeft = Math.Min(1.0, (x - 5.0) / taperLen);
                double uRight = Math.Min(1.0, (W - x) / taperLen);
                double sinLeft = Math.Sin(uLeft * (Math.PI / 2.0));
                double sinRight = Math.Sin(uRight * (Math.PI / 2.0));
                double env = (sinLeft * sinLeft) * (sinRight * sinRight);

                double phi = (x * twoPiOverLambda) - phase;
                double wave = Math.Pow((1.0 + Math.Sin(phi)) * 0.5, power);

                double h = hMax * env * wave;
                double y = yBaseTop - h;

                points.Add(new Point(x, y));
            }

            // Điểm khép góc phải dưới tại nút seek thumb (W, yBottom)
            points.Add(new Point(W, yBottom));
            // Điểm khép góc trái dưới đáy (5.0, yBottom) nối liền vào StartPoint (5.0, 28.0)
            points.Add(new Point(5.0, yBottom));
        }

        private void UpdateWaveGeometry(double playedWidth)
        {
            try
            {
                if (MusicSeekWaveFrontPath == null && MusicSeekWaveBackPath == null) return;
                double W = playedWidth;
                if (W < 20.0) return;

                EnsureWaveGeometries();

                // 1. Sóng biển lớn (ở phía sau): sóng lớn hơn (11.5px), bước sóng dài hơn (135px), trôi chậm hơn (v ≈ 59.3 px/s), màu nền sáng nguyên khối
                double hLarge = 11.5 * Math.Min(1.0, W / 60.0) * _waveAmplitudeScale;
                UpdateSingleWaveGeometry(_waveBackPoints, W, hLarge, 135.0, _wavePhaseLarge, 1.6);

                // 2. Sóng biển nhỏ (ở phía trước): sóng nhỏ hơn (7.5px), bước sóng ngắn hơn (95px), trôi nhanh hơn (v ≈ 81.6 px/s), bán trong suốt tinh tế
                double hSmall = 7.5 * Math.Min(1.0, W / 50.0) * _waveAmplitudeScale;
                UpdateSingleWaveGeometry(_waveFrontPoints, W, hSmall, 95.0, _wavePhaseSmall, 1.6);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LockView] UpdateWaveGeometry error: " + ex.Message);
            }
        }

        #region Music Seekbar Color Extraction (Samsung One UI 6.1 Palette)

        private string _lastPaletteCoverUrl = null;
        private WebClient _paletteWebClient = null;
        private static readonly Color DefaultSeekFrontColor = Color.FromArgb(175, 195, 220, 240);
        private static readonly Color DefaultSeekBackColor = Color.FromArgb(245, 255, 255, 255);

        private void ApplyMusicSeekPalette(Color frontColor, Color backColor)
        {
            // Sóng nhỏ phía trước: tăng độ bán trong suốt chuẩn One UI 6.1 (Alpha 175 ~68%)
            // Sóng lớn phía sau: màu sáng nguyên khối (Alpha 245 ~96%)
            byte frontAlpha = 175;
            byte backAlpha = 245;
            byte thumbAlpha = 250;

            var frontBrush = new SolidColorBrush(Color.FromArgb(frontAlpha, frontColor.R, frontColor.G, frontColor.B));
            var backBrush = new SolidColorBrush(Color.FromArgb(backAlpha, backColor.R, backColor.G, backColor.B));
            var thumbBrush = new SolidColorBrush(Color.FromArgb(thumbAlpha, backColor.R, backColor.G, backColor.B));

            if (MusicSeekWaveBackPath != null) MusicSeekWaveBackPath.Fill = backBrush;
            if (MusicSeekWaveFrontPath != null) MusicSeekWaveFrontPath.Fill = frontBrush;
            if (MusicSeekWaveLeftCap != null) MusicSeekWaveLeftCap.Background = backBrush;
            if (MusicSeekThumb != null) MusicSeekThumb.Background = thumbBrush;
            if (MusicSeekPlayedTrack != null) MusicSeekPlayedTrack.Background = frontBrush;
        }

        private void ResetMusicSeekPalette()
        {
            _lastPaletteCoverUrl = null;
            if (_paletteWebClient != null)
            {
                try { _paletteWebClient.CancelAsync(); } catch { }
                _paletteWebClient = null;
            }
            ApplyMusicSeekPalette(DefaultSeekFrontColor, DefaultSeekBackColor);
        }

        private void ExtractAlbumArtColors(string coverUrl)
        {
            if (string.IsNullOrEmpty(coverUrl))
            {
                ResetMusicSeekPalette();
                return;
            }

            if (coverUrl == _lastPaletteCoverUrl)
            {
                return;
            }

            _lastPaletteCoverUrl = coverUrl;

            if (_paletteWebClient != null)
            {
                try { _paletteWebClient.CancelAsync(); } catch { }
                _paletteWebClient = null;
            }

            string resolvedUrl = MusicWidgetService.ResolveNetworkUrl(coverUrl);

            try
            {
                if (resolvedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    resolvedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var client = new WebClient();
                    _paletteWebClient = client;

                    client.OpenReadCompleted += (s, e) =>
                    {
                        if (client != _paletteWebClient) return;
                        _paletteWebClient = null;

                        if (e.Error == null && !e.Cancelled && e.Result != null)
                        {
                            try
                            {
                                using (var stream = e.Result)
                                {
                                    ProcessPaletteStream(stream);
                                }
                            }
                            catch
                            {
                                ApplyMusicSeekPalette(DefaultSeekFrontColor, DefaultSeekBackColor);
                            }
                        }
                    };

                    client.OpenReadAsync(new Uri(resolvedUrl, UriKind.Absolute));
                }
                else
                {
                    try
                    {
                        var resInfo = Application.GetResourceStream(new Uri(resolvedUrl, UriKind.Relative));
                        if (resInfo != null && resInfo.Stream != null)
                        {
                            using (var stream = resInfo.Stream)
                            {
                                ProcessPaletteStream(stream);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                ApplyMusicSeekPalette(DefaultSeekFrontColor, DefaultSeekBackColor);
            }
        }

        private void ProcessPaletteStream(Stream stream)
        {
            if (stream == null) return;

            var bmp = new BitmapImage();
            bmp.DecodePixelWidth = 32;
            bmp.DecodePixelHeight = 32;
            bmp.SetSource(stream);

            var wb = new WriteableBitmap(bmp);
            int[] pixels = wb.Pixels;
            if (pixels == null || pixels.Length == 0)
            {
                ApplyMusicSeekPalette(DefaultSeekFrontColor, DefaultSeekBackColor);
                return;
            }

            Color frontColor;
            Color backColor;
            ExtractVibrantSeekColors(pixels, out frontColor, out backColor);
            ApplyMusicSeekPalette(frontColor, backColor);
        }

        private static void ExtractVibrantSeekColors(int[] pixels, out Color frontColor, out Color backColor)
        {
            if (pixels == null || pixels.Length == 0)
            {
                frontColor = DefaultSeekFrontColor;
                backColor = DefaultSeekBackColor;
                return;
            }

            const int BINS = 36;
            double[] binWeights = new double[BINS];
            double[] binSats = new double[BINS];
            int[] binCounts = new int[BINS];

            int validCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int pixel = pixels[i];
                int a = (pixel >> 24) & 0xFF;
                if (a < 128) continue;

                int r = (pixel >> 16) & 0xFF;
                int g = (pixel >> 8) & 0xFF;
                int b = pixel & 0xFF;

                double h, s, l;
                RgbToHsl(r, g, b, out h, out s, out l);

                // Lọc bỏ pixel đen, trắng chói hoặc xám mờ không sắc tố
                if (s > 0.15 && l > 0.15 && l < 0.92)
                {
                    int bin = (int)(h / 10.0);
                    if (bin >= BINS) bin = BINS - 1;
                    if (bin < 0) bin = 0;

                    double weight = s * (1.0 - Math.Abs(l - 0.5));
                    binWeights[bin] += weight;
                    binSats[bin] += s;
                    binCounts[bin]++;
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                frontColor = DefaultSeekFrontColor;
                backColor = DefaultSeekBackColor;
                return;
            }

            int bestBin = 0;
            double maxWeight = -1.0;
            for (int b = 0; b < BINS; b++)
            {
                if (binWeights[b] > maxWeight)
                {
                    maxWeight = binWeights[b];
                    bestBin = b;
                }
            }

            double domH = bestBin * 10.0 + 5.0;
            double avgS = binSats[bestBin] / Math.Max(1, binCounts[bestBin]);

            // 1. Sóng lớn ở phía sau: Trắng sáng pastel tinh khôi chuẩn One UI 6.1
            double sBack = Math.Max(0.18, Math.Min(0.40, avgS * 0.60));
            double lBack = 0.93;
            backColor = HslToColor(domH, sBack, lBack);

            // 2. Sóng nhỏ ở phía trước: Cùng sắc thái nhưng màu đậm hơn một chút để khi phủ bán trong suốt tạo độ tương phản nước đẹp mắt
            double sFront = Math.Max(0.28, Math.Min(0.58, avgS * 0.80));
            double lFront = 0.86;
            frontColor = HslToColor(domH, sFront, lFront);
        }

        private static void RgbToHsl(int r, int g, int b, out double h, out double s, out double l)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (delta <= 0.0001)
            {
                h = 0.0;
                s = 0.0;
                return;
            }

            s = (l > 0.5) ? (delta / (2.0 - max - min)) : (delta / (max + min));

            if (rd >= max)
            {
                h = (gd - bd) / delta + (gd < bd ? 6.0 : 0.0);
            }
            else if (gd >= max)
            {
                h = (bd - rd) / delta + 2.0;
            }
            else
            {
                h = (rd - gd) / delta + 4.0;
            }

            h = (h / 6.0) * 360.0;
            if (h < 0.0) h += 360.0;
            if (h >= 360.0) h -= 360.0;
        }

        private static Color HslToColor(double h, double s, double l)
        {
            double r, g, b;

            if (s <= 0.0001)
            {
                r = g = b = l;
            }
            else
            {
                double q = (l < 0.5) ? (l * (1.0 + s)) : (l + s - l * s);
                double p = 2.0 * l - q;
                double hk = h / 360.0;

                r = HueToRgb(p, q, hk + (1.0 / 3.0));
                g = HueToRgb(p, q, hk);
                b = HueToRgb(p, q, hk - (1.0 / 3.0));
            }

            byte rb = (byte)Math.Max(0, Math.Min(255, (int)(r * 255.0 + 0.5)));
            byte gb = (byte)Math.Max(0, Math.Min(255, (int)(g * 255.0 + 0.5)));
            byte bb = (byte)Math.Max(0, Math.Min(255, (int)(b * 255.0 + 0.5)));

            return Color.FromArgb(255, rb, gb, bb);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0.0) t += 1.0;
            if (t > 1.0) t -= 1.0;
            if (t < (1.0 / 6.0)) return p + (q - p) * 6.0 * t;
            if (t < 0.5) return q;
            if (t < (2.0 / 3.0)) return p + (q - p) * ((2.0 / 3.0) - t) * 6.0;
            return p;
        }

        #endregion

        private void MusicSeekbarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var svc = MusicWidgetService.Instance;
            UpdateSeekbar(svc.PositionMs, svc.DurationMs);
        }

        private async void MusicSeekbarContainer_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            var svc = MusicWidgetService.Instance;
            if (!svc.CanSeek || svc.DurationMs <= 0 || MusicSeekbarContainer == null) return;
            Point p = e.GetPosition(MusicSeekbarContainer);
            double totalWidth = MusicSeekbarContainer.ActualWidth > 0 ? MusicSeekbarContainer.ActualWidth : 400.0;
            double ratio = Math.Max(0.0, Math.Min(1.0, p.X / totalWidth));
            long seekPos = (long)(ratio * svc.DurationMs);
            await svc.SendSeekAsync(seekPos);
        }

        private string _currentAppIconKey = null;

        private void UpdateMusicAppIcon(string appIconUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(appIconUrl))
                {
                    _currentAppIconKey = null;
                    if (MusicAppIconCollapsed != null) { MusicAppIconCollapsed.Source = null; MusicAppIconCollapsed.Visibility = Visibility.Collapsed; }
                    if (MusicAppGlyphCollapsed != null) MusicAppGlyphCollapsed.Visibility = Visibility.Visible;
                    return;
                }

                if (appIconUrl == _currentAppIconKey)
                {
                    return;
                }

                _currentAppIconKey = appIconUrl;
                string resolvedUrl = MusicWidgetService.ResolveNetworkUrl(appIconUrl);
                BitmapImage bmp;
                if (resolvedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || resolvedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    bmp = new BitmapImage(new Uri(resolvedUrl, UriKind.Absolute));
                }
                else
                {
                    bmp = new BitmapImage(new Uri(resolvedUrl, UriKind.Relative));
                }

                if (MusicAppIconCollapsed != null)
                {
                    MusicAppIconCollapsed.Source = bmp;
                    MusicAppIconCollapsed.Visibility = Visibility.Visible;
                }
                if (MusicAppGlyphCollapsed != null) MusicAppGlyphCollapsed.Visibility = Visibility.Collapsed;
            }
            catch
            {
                if (MusicAppIconCollapsed != null) { MusicAppIconCollapsed.Source = null; MusicAppIconCollapsed.Visibility = Visibility.Collapsed; }
                if (MusicAppGlyphCollapsed != null) MusicAppGlyphCollapsed.Visibility = Visibility.Visible;
            }
        }

        private void UpdateMusicUi()
        {
            var svc = MusicWidgetService.Instance;
            if (!svc.HasActiveTrack)
            {
                MusicWidgetCard.Visibility = Visibility.Collapsed;
                StopWaveAnimation();
                UpdateMusicAppIcon(null);
                ResetMusicSeekPalette();
                if (MusicLeftActionImage != null) { MusicLeftActionImage.Source = null; MusicLeftActionImage.Visibility = Visibility.Collapsed; }
                if (MusicRightActionImage != null) { MusicRightActionImage.Source = null; MusicRightActionImage.Visibility = Visibility.Collapsed; }
                return;
            }

            MusicWidgetCard.Visibility = Visibility.Visible;
            ApplyMusicWidgetTransparency();
            UpdateMusicAppIcon(svc.AppIconUrl);

            if (musicTransitionStory == null)
            {
                if (isMusicExpanded)
                {
                    if (MusicThumbBorderCollapsed != null)
                    {
                        MusicThumbBorderCollapsed.Opacity = 0.0;
                        MusicThumbBorderCollapsed.Visibility = Visibility.Collapsed;
                    }
                    if (MusicCollapsedControls != null)
                    {
                        MusicCollapsedControls.Opacity = 0.0;
                        MusicCollapsedControls.Visibility = Visibility.Collapsed;
                    }
                    if (MusicCollapsedInfoTransform != null) MusicCollapsedInfoTransform.TranslateX = -68.0;
                    if (MusicTitleCollapsed != null) MusicTitleCollapsed.MaxWidth = 390;
                    if (MusicArtistCollapsed != null) MusicArtistCollapsed.MaxWidth = 390;

                    if (MusicMediaOutputPill != null)
                    {
                        MusicMediaOutputPill.Visibility = Visibility.Visible;
                        MusicMediaOutputPill.Opacity = 1.0;
                    }
                    if (MusicMediaOutputTransform != null) MusicMediaOutputTransform.TranslateX = 0.0;

                    if (MusicExpandedView != null)
                    {
                        MusicExpandedView.Visibility = Visibility.Visible;
                        MusicExpandedView.Opacity = 1.0;
                    }
                    if (MusicExpandedTransform != null) MusicExpandedTransform.TranslateY = 0.0;

                    if (MusicExpandedBg != null)
                    {
                        MusicExpandedBg.Visibility = Visibility.Visible;
                        MusicExpandedBg.Opacity = 1.0;
                    }
                    if (MusicBgTransform != null) { MusicBgTransform.ScaleX = 1.25; MusicBgTransform.ScaleY = 1.25; }

                    if (MusicArrowTransform != null) MusicArrowTransform.Rotation = 180.0;
                    MusicWidgetCard.Height = MusicExpandedCardHeight;
                }
                else
                {
                    if (MusicThumbBorderCollapsed != null)
                    {
                        MusicThumbBorderCollapsed.Visibility = Visibility.Visible;
                        MusicThumbBorderCollapsed.Opacity = 1.0;
                    }
                    if (MusicCollapsedControls != null)
                    {
                        MusicCollapsedControls.Visibility = Visibility.Visible;
                        MusicCollapsedControls.Opacity = 1.0;
                    }
                    if (MusicCollapsedControlsTransform != null) MusicCollapsedControlsTransform.TranslateX = 0.0;

                    if (MusicCollapsedInfoTransform != null) MusicCollapsedInfoTransform.TranslateX = 0.0;
                    if (MusicTitleCollapsed != null) MusicTitleCollapsed.MaxWidth = 230;
                    if (MusicArtistCollapsed != null) MusicArtistCollapsed.MaxWidth = 230;

                    if (MusicMediaOutputPill != null)
                    {
                        MusicMediaOutputPill.Visibility = Visibility.Collapsed;
                        MusicMediaOutputPill.Opacity = 0.0;
                    }
                    if (MusicMediaOutputTransform != null) MusicMediaOutputTransform.TranslateX = 25.0;

                    if (MusicExpandedView != null)
                    {
                        MusicExpandedView.Visibility = Visibility.Collapsed;
                        MusicExpandedView.Opacity = 0.0;
                    }
                    if (MusicExpandedTransform != null) MusicExpandedTransform.TranslateY = 0.0;

                    if (MusicExpandedBg != null)
                    {
                        MusicExpandedBg.Visibility = Visibility.Collapsed;
                        MusicExpandedBg.Opacity = 0.0;
                    }
                    if (MusicBgTransform != null) { MusicBgTransform.ScaleX = 1.25; MusicBgTransform.ScaleY = 1.25; }

                    if (MusicArrowTransform != null) MusicArrowTransform.Rotation = 0.0;
                    MusicWidgetCard.Height = MusicCollapsedCardHeight;
                }
            }

            // Cập nhật thiết bị phát âm thanh (Audio Output)
            string audioOut = string.IsNullOrEmpty(svc.AudioOutput) ? "Điện thoại này" : svc.AudioOutput;
            if (MusicAudioOutputCollapsed != null) MusicAudioOutputCollapsed.Text = audioOut;

            // Cập nhật tên bài hát và ca sĩ / trạng thái lỗi / tiến trình tua / kết nối
            MusicTitleCollapsed.Text = svc.Title;
            string baseArtist = string.IsNullOrEmpty(svc.Artist) 
                ? (string.IsNullOrEmpty(svc.AppName) ? "Music Player" : svc.AppName) 
                : svc.Artist;

            if (!string.IsNullOrEmpty(svc.Subtitle) && !svc.Subtitle.Equals(svc.Title, StringComparison.OrdinalIgnoreCase) && !svc.Subtitle.Equals(baseArtist, StringComparison.OrdinalIgnoreCase))
            {
                baseArtist = svc.Subtitle + " • " + baseArtist;
            }

            if (svc.TrackNumber > 0)
            {
                if (svc.NumTracks > 0)
                {
                    baseArtist = string.Format("{0} ({1}/{2})", baseArtist, svc.TrackNumber, svc.NumTracks);
                }
                else
                {
                    baseArtist = string.Format("{0} (#{1})", baseArtist, svc.TrackNumber);
                }
            }

            string artistDisplay = baseArtist;
            if (svc.IsError)
            {
                artistDisplay = string.IsNullOrEmpty(svc.ErrorMessage) ? "Lỗi phát lại" : svc.ErrorMessage;
            }
            else if (svc.IsConnecting)
            {
                artistDisplay = baseArtist + " • Đang kết nối...";
            }
            else if (svc.IsBuffering)
            {
                artistDisplay = baseArtist + " • Đang tải...";
            }
            else if (svc.IsFastForwarding)
            {
                artistDisplay = baseArtist + " • Đang tua nhanh >>";
            }
            else if (svc.IsRewinding)
            {
                artistDisplay = baseArtist + " • Đang tua lùi <<";
            }
            else if (svc.IsSkippingToPrev)
            {
                artistDisplay = baseArtist + " • Đang chuyển bài trước...";
            }
            else if (svc.IsSkippingToNext)
            {
                artistDisplay = baseArtist + " • Đang chuyển bài tiếp...";
            }
            else if (svc.IsSkippingToQueueItem)
            {
                artistDisplay = baseArtist + " • Đang nạp danh sách...";
            }

            MusicArtistCollapsed.Text = artistDisplay;

            // Nút Play / Pause: \uE103 (Pause), \uE102 (Play)
            string playIcon = svc.IsPlaying ? "\uE103" : "\uE102";
            MusicPlayPauseIconCollapsed.Text = playIcon;
            MusicPlayPauseIconExpanded.Text = playIcon;

            // Cập nhật độ mờ nút Play / Pause theo cờ năng lực chuẩn PlaybackState.Actions
            bool canTogglePlay = svc.IsPlaying ? svc.CanPause : svc.CanPlay;
            double playPauseOpacity = canTogglePlay ? 1.0 : 0.35;
            if (MusicPlayPauseBtnCollapsed != null) MusicPlayPauseBtnCollapsed.Opacity = playPauseOpacity;
            if (MusicPlayPauseBtnExpanded != null) MusicPlayPauseBtnExpanded.Opacity = playPauseOpacity;

            // Cập nhật độ mờ nút Lùi / Chuyển bài theo cờ năng lực chuẩn PlaybackState.Actions
            double prevOpacity = svc.CanSkipPrev ? 1.0 : 0.35;
            double nextOpacity = svc.CanSkipNext ? 1.0 : 0.35;
            if (MusicPrevBtnCollapsed != null) MusicPrevBtnCollapsed.Opacity = prevOpacity;
            if (MusicPrevBtnExpanded != null) MusicPrevBtnExpanded.Opacity = prevOpacity;
            if (MusicNextBtnCollapsed != null) MusicNextBtnCollapsed.Opacity = nextOpacity;
            if (MusicNextBtnExpanded != null) MusicNextBtnExpanded.Opacity = nextOpacity;

            // Cập nhật các nút tùy biến (CustomActions) chuẩn Android PlaybackState.CustomAction
            ApplyCustomActionButton(MusicHeartIcon, MusicLeftActionImage, svc.LeftAction, true);
            ApplyCustomActionButton(MusicShuffleIcon, MusicRightActionImage, svc.RightAction, false);
            isFavorite = svc.IsFavorite;
            isShuffle = svc.IsShuffle;

            // Thời gian và thanh tiến trình (nội suy thời gian thực chuẩn Android AOSP)
            if (MusicSeekbarContainer != null)
            {
                MusicSeekbarContainer.Opacity = svc.CanSeek ? 1.0 : 0.45;
            }

            long currentEst = svc.CurrentEstimatedPositionMs;
            MusicCurrentTimeText.Text = FormatMs(currentEst);
            if (svc.DurationMs > 0)
            {
                MusicTotalTimeText.Text = FormatMs(svc.DurationMs);
                UpdateSeekbar(currentEst, svc.DurationMs);
                if (MusicSeekThumb != null) MusicSeekThumb.Visibility = Visibility.Visible;
            }
            else
            {
                MusicTotalTimeText.Text = "Trực tiếp";
                if (MusicSeekPlayedTrack != null) MusicSeekPlayedTrack.Width = 0;
                if (MusicSeekBufferedTrack != null) MusicSeekBufferedTrack.Width = 0;
                if (MusicSeekWaveCanvas != null) MusicSeekWaveCanvas.Visibility = Visibility.Collapsed;
                StopWaveAnimation();
                if (MusicSeekThumb != null) MusicSeekThumb.Visibility = Visibility.Collapsed;
            }

            // Tải ảnh bìa album (Căn giữa chuẩn xác cả ảnh 16:9 và 1:1)
            if (!string.IsNullOrEmpty(svc.ThumbnailUrl))
            {
                try
                {
                    string resolvedThumb = MusicWidgetService.ResolveNetworkUrl(svc.ThumbnailUrl);
                    Uri thumbUri;
                    if (Uri.TryCreate(resolvedThumb, UriKind.Absolute, out thumbUri))
                    {
                        var bmp = new BitmapImage(thumbUri);
                        if (MusicCoverBrushCollapsed != null) MusicCoverBrushCollapsed.ImageSource = bmp;
                        if (MusicBgBrush != null) MusicBgBrush.ImageSource = bmp;
                    }
                }
                catch { }

                ExtractAlbumArtColors(svc.ThumbnailUrl);
            }
            else
            {
                if (MusicCoverBrushCollapsed != null) MusicCoverBrushCollapsed.ImageSource = null;
                if (MusicBgBrush != null) MusicBgBrush.ImageSource = null;
                ResetMusicSeekPalette();
            }
        }

        private string FormatMs(long ms)
        {
            if (ms <= 0) return "00:00";
            var ts = TimeSpan.FromMilliseconds(ms);
            return (ts.Hours > 0)
                ? string.Format("{0}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds)
                : string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
        }

        private void MusicArrow_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (isMusicExpanded)
            {
                MusicCollapse_Tap(sender, e);
            }
            else
            {
                MusicExpand_Tap(sender, e);
            }
        }

        private void MusicWidgetCard_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (isMusicExpanded)
            {
                MusicCollapse_Tap(sender, e);
            }
            else
            {
                MusicExpand_Tap(sender, e);
            }
        }

        private void MusicExpand_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (isMusicExpanded && musicTransitionStory == null) return;
            isMusicExpanded = true;

            var svc = MusicWidgetService.Instance;
            UpdateSeekbar(svc.PositionMs, svc.DurationMs);

            if (musicTransitionStory != null)
            {
                try { musicTransitionStory.Stop(); } catch { }
                musicTransitionStory = null;
            }

            // Chuẩn bị trạng thái hiển thị trước khi chạy storyboard
            if (MusicTitleCollapsed != null) MusicTitleCollapsed.MaxWidth = 390;
            if (MusicArtistCollapsed != null) MusicArtistCollapsed.MaxWidth = 390;

            if (MusicThumbBorderCollapsed != null)
            {
                MusicThumbBorderCollapsed.Visibility = Visibility.Visible;
                MusicThumbBorderCollapsed.Opacity = 1.0;
            }

            if (MusicCollapsedControls != null)
            {
                MusicCollapsedControls.Visibility = Visibility.Visible;
                MusicCollapsedControls.Opacity = 1.0;
            }
            if (MusicCollapsedControlsTransform != null) MusicCollapsedControlsTransform.TranslateX = 0.0;

            if (MusicMediaOutputPill != null)
            {
                MusicMediaOutputPill.Opacity = 0.0;
                MusicMediaOutputPill.Visibility = Visibility.Visible;
            }
            if (MusicMediaOutputTransform != null) MusicMediaOutputTransform.TranslateX = 25.0;

            if (MusicExpandedView != null)
            {
                MusicExpandedView.Opacity = 0.0;
                MusicExpandedView.Visibility = Visibility.Visible;
            }
            if (MusicExpandedTransform != null) MusicExpandedTransform.TranslateY = 0.0;

            if (MusicExpandedBg != null)
            {
                MusicExpandedBg.Opacity = 0.0;
                MusicExpandedBg.Visibility = Visibility.Visible;
            }

            double fromHeight = MusicWidgetCard.ActualHeight > 0 ? MusicWidgetCard.ActualHeight : MusicCollapsedCardHeight;
            double fromRotation = MusicArrowTransform.Rotation;
            double fromInfoX = (MusicCollapsedInfoTransform != null) ? MusicCollapsedInfoTransform.TranslateX : 0.0;

            var sb = new Storyboard();
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 1. Mở rộng chiều cao card (82px -> 184px trong 330ms)
            var cardHeightAnim = new DoubleAnimation
            {
                From = fromHeight,
                To = MusicExpandedCardHeight,
                Duration = TimeSpan.FromMilliseconds(330),
                EasingFunction = easeOut
            };
            Storyboard.SetTarget(cardHeightAnim, MusicWidgetCard);
            Storyboard.SetTargetProperty(cardHeightAnim, new PropertyPath("Height"));
            sb.Children.Add(cardHeightAnim);

            // 2. Mũi tên điều hướng xoay 180 độ quanh tâm giữa (0 -> 180 trong 330ms)
            var arrowAnim = new DoubleAnimation
            {
                From = fromRotation,
                To = 180.0,
                Duration = TimeSpan.FromMilliseconds(330),
                EasingFunction = easeOut
            };
            Storyboard.SetTarget(arrowAnim, MusicArrowTransform);
            Storyboard.SetTargetProperty(arrowAnim, new PropertyPath("Rotation"));
            sb.Children.Add(arrowAnim);

            // 3. Nền ảnh bìa mờ tràn viền fade in (300ms)
            if (MusicExpandedBg != null)
            {
                var bgFade = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(bgFade, MusicExpandedBg);
                Storyboard.SetTargetProperty(bgFade, new PropertyPath("Opacity"));
                sb.Children.Add(bgFade);
            }

            // 4. Tiêu đề + nghệ sĩ và 'Điện thoại này' di chuyển sang trái đè lên ảnh bìa mini (TranslateX 0 -> -68 trong 300ms)
            if (MusicCollapsedInfoTransform != null)
            {
                var infoSlide = new DoubleAnimation
                {
                    From = fromInfoX,
                    To = -68.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(infoSlide, MusicCollapsedInfoTransform);
                Storyboard.SetTargetProperty(infoSlide, new PropertyPath("TranslateX"));
                sb.Children.Add(infoSlide);
            }

            // 5. Ảnh bìa mini fade out đơn thuần, không phóng to thu nhỏ (180ms)
            if (MusicThumbBorderCollapsed != null)
            {
                var thumbFade = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(thumbFade, MusicThumbBorderCollapsed);
                Storyboard.SetTargetProperty(thumbFade, new PropertyPath("Opacity"));
                sb.Children.Add(thumbFade);
            }

            // 6. 3 nút điều khiển ở minimize fade out và cố định không di chuyển (150ms)
            if (MusicCollapsedControls != null)
            {
                var collControlsFade = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(collControlsFade, MusicCollapsedControls);
                Storyboard.SetTargetProperty(collControlsFade, new PropertyPath("Opacity"));
                sb.Children.Add(collControlsFade);
            }

            // 7. 'Đầu ra media' di chuyển sang trái (TranslateX 25 -> 0) và fade in (Opacity 0 -> 1)
            if (MusicMediaOutputPill != null && MusicMediaOutputTransform != null)
            {
                var pillSlide = new DoubleAnimation
                {
                    From = 25.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(pillSlide, MusicMediaOutputTransform);
                Storyboard.SetTargetProperty(pillSlide, new PropertyPath("TranslateX"));
                sb.Children.Add(pillSlide);

                var pillFade = new DoubleAnimation
                {
                    BeginTime = TimeSpan.FromMilliseconds(40),
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(260),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(pillFade, MusicMediaOutputPill);
                Storyboard.SetTargetProperty(pillFade, new PropertyPath("Opacity"));
                sb.Children.Add(pillFade);
            }

            // 8. Thanh seek với 5 nút điều khiển giữ cố định không di chuyển, chỉ fade in (Opacity 0 -> 1 trong 270ms sau 50ms)
            if (MusicExpandedView != null)
            {
                var expFade = new DoubleAnimation
                {
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(270),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(expFade, MusicExpandedView);
                Storyboard.SetTargetProperty(expFade, new PropertyPath("Opacity"));
                sb.Children.Add(expFade);
            }

            sb.Completed += (s, ev) =>
            {
                musicTransitionStory = null;
                MusicWidgetCard.Height = MusicExpandedCardHeight;
                if (MusicArrowTransform != null) MusicArrowTransform.Rotation = 180.0;
                if (MusicCollapsedInfoTransform != null) MusicCollapsedInfoTransform.TranslateX = -68.0;

                if (MusicThumbBorderCollapsed != null)
                {
                    MusicThumbBorderCollapsed.Opacity = 0.0;
                    MusicThumbBorderCollapsed.Visibility = Visibility.Collapsed;
                }
                if (MusicCollapsedControls != null)
                {
                    MusicCollapsedControls.Opacity = 0.0;
                    MusicCollapsedControls.Visibility = Visibility.Collapsed;
                }
                if (MusicMediaOutputPill != null)
                {
                    MusicMediaOutputPill.Opacity = 1.0;
                    MusicMediaOutputPill.Visibility = Visibility.Visible;
                }
                if (MusicMediaOutputTransform != null) MusicMediaOutputTransform.TranslateX = 0.0;

                if (MusicExpandedView != null)
                {
                    MusicExpandedView.Opacity = 1.0;
                    MusicExpandedView.Visibility = Visibility.Visible;
                }
                if (MusicExpandedTransform != null) MusicExpandedTransform.TranslateY = 0.0;

                if (MusicExpandedBg != null)
                {
                    MusicExpandedBg.Opacity = 1.0;
                    MusicExpandedBg.Visibility = Visibility.Visible;
                }

                var svcExp = MusicWidgetService.Instance;
                if (svcExp.IsPlaying && svcExp.DurationMs > 0)
                {
                    UpdateSeekbar(svcExp.CurrentEstimatedPositionMs, svcExp.DurationMs);
                }
            };

            musicTransitionStory = sb;
            sb.Begin();
        }

        private void MusicCollapse_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (!isMusicExpanded && musicTransitionStory == null) return;
            isMusicExpanded = false;

            if (musicTransitionStory != null)
            {
                try { musicTransitionStory.Stop(); } catch { }
                musicTransitionStory = null;
            }

            StopWaveAnimation();
            _waveAmplitudeScale = 0.0;
            _targetWaveAmplitudeScale = 0.0;
            _isWaveTransitioning = false;

            // Chuẩn bị trạng thái hiển thị trước khi chạy storyboard
            if (MusicThumbBorderCollapsed != null)
            {
                MusicThumbBorderCollapsed.Opacity = 0.0;
                MusicThumbBorderCollapsed.Visibility = Visibility.Visible;
            }
            if (MusicCollapsedControls != null)
            {
                MusicCollapsedControls.Opacity = 0.0;
                MusicCollapsedControls.Visibility = Visibility.Visible;
            }
            if (MusicCollapsedControlsTransform != null) MusicCollapsedControlsTransform.TranslateX = 0.0;
            if (MusicExpandedTransform != null) MusicExpandedTransform.TranslateY = 0.0;

            double fromHeight = MusicWidgetCard.ActualHeight > 0 ? MusicWidgetCard.ActualHeight : MusicExpandedCardHeight;
            double fromRotation = MusicArrowTransform.Rotation;
            double fromInfoX = (MusicCollapsedInfoTransform != null) ? MusicCollapsedInfoTransform.TranslateX : -68.0;

            var sb = new Storyboard();
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 1. Thu gọn chiều cao thẻ MusicWidgetCard (184px -> 82px trong 280ms)
            var cardHeightAnim = new DoubleAnimation
            {
                From = fromHeight,
                To = MusicCollapsedCardHeight,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = easeOut
            };
            Storyboard.SetTarget(cardHeightAnim, MusicWidgetCard);
            Storyboard.SetTargetProperty(cardHeightAnim, new PropertyPath("Height"));
            sb.Children.Add(cardHeightAnim);

            // 2. Mũi tên điều hướng xoay ngược lại quanh tâm giữa (180 -> 0 trong 280ms)
            var arrowAnim = new DoubleAnimation
            {
                From = fromRotation,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = easeOut
            };
            Storyboard.SetTarget(arrowAnim, MusicArrowTransform);
            Storyboard.SetTargetProperty(arrowAnim, new PropertyPath("Rotation"));
            sb.Children.Add(arrowAnim);

            // 3. Tiêu đề + nghệ sĩ và 'Điện thoại này' di chuyển từ trái về vị trí cũ (TranslateX -68 -> 0 trong 270ms)
            if (MusicCollapsedInfoTransform != null)
            {
                var infoSlide = new DoubleAnimation
                {
                    From = fromInfoX,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(270),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(infoSlide, MusicCollapsedInfoTransform);
                Storyboard.SetTargetProperty(infoSlide, new PropertyPath("TranslateX"));
                sb.Children.Add(infoSlide);
            }

            // 4. Ảnh bìa mini fade in (0 -> 1 trong 220ms sau 50ms, không scale)
            if (MusicThumbBorderCollapsed != null)
            {
                var thumbFade = new DoubleAnimation
                {
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(220),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(thumbFade, MusicThumbBorderCollapsed);
                Storyboard.SetTargetProperty(thumbFade, new PropertyPath("Opacity"));
                sb.Children.Add(thumbFade);
            }

            // 5. 3 nút điều khiển ở minimize fade in và cố định không di chuyển (0 -> 1 trong 220ms sau 50ms)
            if (MusicCollapsedControls != null)
            {
                var collControlsFade = new DoubleAnimation
                {
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(220),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(collControlsFade, MusicCollapsedControls);
                Storyboard.SetTargetProperty(collControlsFade, new PropertyPath("Opacity"));
                sb.Children.Add(collControlsFade);
            }

            // 6. 'Đầu ra media' di chuyển sang phải (TranslateX 0 -> 25) và fade out (Opacity 1 -> 0 trong 160ms)
            if (MusicMediaOutputPill != null && MusicMediaOutputTransform != null)
            {
                var pillSlide = new DoubleAnimation
                {
                    From = 0.0,
                    To = 25.0,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(pillSlide, MusicMediaOutputTransform);
                Storyboard.SetTargetProperty(pillSlide, new PropertyPath("TranslateX"));
                sb.Children.Add(pillSlide);

                var pillFade = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(pillFade, MusicMediaOutputPill);
                Storyboard.SetTargetProperty(pillFade, new PropertyPath("Opacity"));
                sb.Children.Add(pillFade);
            }

            // 7. Thanh seek với 5 nút điều khiển giữ cố định không di chuyển, fade out (Opacity 1 -> 0 trong 130ms)
            if (MusicExpandedView != null)
            {
                var expFade = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(130),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(expFade, MusicExpandedView);
                Storyboard.SetTargetProperty(expFade, new PropertyPath("Opacity"));
                sb.Children.Add(expFade);
            }

            // 8. Nền ảnh bìa mờ tràn viền fade out (180ms)
            if (MusicExpandedBg != null)
            {
                var bgFade = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = easeOut
                };
                Storyboard.SetTarget(bgFade, MusicExpandedBg);
                Storyboard.SetTargetProperty(bgFade, new PropertyPath("Opacity"));
                sb.Children.Add(bgFade);
            }

            sb.Completed += (s, ev) =>
            {
                musicTransitionStory = null;
                MusicWidgetCard.Height = MusicCollapsedCardHeight;
                if (MusicArrowTransform != null) MusicArrowTransform.Rotation = 0.0;
                if (MusicCollapsedInfoTransform != null) MusicCollapsedInfoTransform.TranslateX = 0.0;
                if (MusicTitleCollapsed != null) MusicTitleCollapsed.MaxWidth = 230;
                if (MusicArtistCollapsed != null) MusicArtistCollapsed.MaxWidth = 230;

                if (MusicThumbBorderCollapsed != null)
                {
                    MusicThumbBorderCollapsed.Opacity = 1.0;
                    MusicThumbBorderCollapsed.Visibility = Visibility.Visible;
                }
                if (MusicCollapsedControls != null)
                {
                    MusicCollapsedControls.Opacity = 1.0;
                    MusicCollapsedControls.Visibility = Visibility.Visible;
                }
                if (MusicMediaOutputPill != null)
                {
                    MusicMediaOutputPill.Opacity = 0.0;
                    MusicMediaOutputPill.Visibility = Visibility.Collapsed;
                }
                if (MusicMediaOutputTransform != null) MusicMediaOutputTransform.TranslateX = 25.0;

                if (MusicExpandedView != null)
                {
                    MusicExpandedView.Opacity = 0.0;
                    MusicExpandedView.Visibility = Visibility.Collapsed;
                }
                if (MusicExpandedTransform != null) MusicExpandedTransform.TranslateY = 0.0;

                if (MusicExpandedBg != null)
                {
                    MusicExpandedBg.Opacity = 0.0;
                    MusicExpandedBg.Visibility = Visibility.Collapsed;
                }
            };

            musicTransitionStory = sb;
            sb.Begin();
        }

        private async void MusicPlayPause_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            var svc = MusicWidgetService.Instance;
            if (svc.IsPlaying)
            {
                if (!svc.CanPause) return;
                // Phản hồi thị giác lập tức: kích hoạt animation hạ phẳng sóng ngay khi chạm nút
                UpdateWaveProgress(_currentPlayedWidth, false);
                await svc.SendPauseAsync();
            }
            else
            {
                if (!svc.CanPlay) return;
                // Phản hồi thị giác lập tức: kích hoạt animation dâng trào sóng ngay khi chạm nút
                UpdateWaveProgress(_currentPlayedWidth, true);
                await svc.SendPlayAsync();
            }
        }

        private async void MusicNext_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (!MusicWidgetService.Instance.CanSkipNext) return;
            await MusicWidgetService.Instance.SendSkipNextAsync();
        }

        private async void MusicPrev_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (!MusicWidgetService.Instance.CanSkipPrev) return;
            await MusicWidgetService.Instance.SendSkipPrevAsync();
        }

        private void ApplyCustomActionButton(TextBlock textBlock, Image imageControl, CustomActionModel action, bool isLeftDefault)
        {
            if (action == null)
            {
                if (textBlock != null) textBlock.Visibility = Visibility.Collapsed;
                if (imageControl != null) { imageControl.Source = null; imageControl.Visibility = Visibility.Collapsed; }
                return;
            }

            string icon = (action.Icon ?? "").Trim();
            string actionName = (action.Action ?? "").ToLowerInvariant().Trim();

            // Nếu icon là một URL HTTP hoặc đường dẫn ảnh PNG nhị phân từ app nhạc
            if (icon.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                icon.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                icon.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                icon.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                if (imageControl != null)
                {
                    try
                    {
                        Uri imgUri;
                        if (Uri.TryCreate(icon, UriKind.Absolute, out imgUri))
                        {
                            imageControl.Source = new BitmapImage(imgUri);
                            imageControl.Visibility = Visibility.Visible;
                            imageControl.Opacity = action.IsActive ? 1.0 : 0.7;
                            if (textBlock != null) textBlock.Visibility = Visibility.Collapsed;
                            return;
                        }
                    }
                    catch { }
                }
            }

            // Nếu không phải ảnh URL, ẩn Image và dùng TextBlock Segoe UI Symbol
            if (imageControl != null)
            {
                imageControl.Visibility = Visibility.Collapsed;
                imageControl.Source = null;
            }

            if (textBlock == null) return;
            textBlock.Visibility = Visibility.Visible;

            string iconLower = icon.ToLowerInvariant();

            // 1. Favorite / Like / Heart
            if (iconLower == "heart" || iconLower == "favorite" || iconLower == "like" || actionName.Contains("favorite"))
            {
                textBlock.Text = action.IsActive ? "\uE00B" : "\uE006";
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 255, 75, 75)) // Đỏ One UI
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 2. Shuffle (Trộn bài)
            else if (iconLower == "shuffle" || actionName.Contains("shuffle"))
            {
                textBlock.Text = "\uE148";
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 112, 166, 222)) // Xanh One UI
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 3. Repeat (Lặp lại danh sách / Lặp 1 bài theo chuẩn 3 nấc AOSP AndroidRepeatMode)
            else if (iconLower == "repeat" || iconLower == "repeat_one" || iconLower == "repeat1" || actionName.Contains("repeat"))
            {
                var svc = MusicWidgetService.Instance;
                if (svc.RepeatMode == AndroidRepeatMode.REPEAT_MODE_ONE || iconLower == "repeat_one" || iconLower == "repeat1")
                {
                    textBlock.Text = "\uE1CC"; // Loop arrow with 1
                    textBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 112, 166, 222)); // Xanh One UI
                }
                else if (svc.RepeatMode == AndroidRepeatMode.REPEAT_MODE_ALL || action.IsActive)
                {
                    textBlock.Text = "\uE1CD"; // Loop arrows
                    textBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 112, 166, 222)); // Xanh One UI
                }
                else
                {
                    textBlock.Text = "\uE1CD"; // Loop arrows
                    textBlock.Foreground = new SolidColorBrush(Color.FromArgb(208, 255, 255, 255)); // Mờ
                }
            }
            // 5. Thumbs Up
            else if (iconLower == "thumbs_up" || iconLower == "thumb_up" || actionName.Contains("thumbs_up"))
            {
                textBlock.Text = "\uE19F";
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 112, 166, 222)) 
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 6. Thumbs Down
            else if (iconLower == "thumbs_down" || iconLower == "thumb_down" || actionName.Contains("thumbs_down"))
            {
                textBlock.Text = "\uE19E";
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 255, 75, 75)) 
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 7. Rewind (Tua lùi)
            else if (iconLower.StartsWith("rewind") || iconLower.StartsWith("replay") || actionName.Contains("rewind"))
            {
                textBlock.Text = "\uE110";
                textBlock.Foreground = new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 8. Fast Forward (Tua tới)
            else if (iconLower.StartsWith("forward") || iconLower.StartsWith("fast_forward") || actionName.Contains("fast_forward"))
            {
                textBlock.Text = "\uE111";
                textBlock.Foreground = new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 9. Stop
            else if (iconLower == "stop" || actionName.Contains("stop"))
            {
                textBlock.Text = "\uE15B";
                textBlock.Foreground = new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 10. Queue / Playlist
            else if (iconLower == "queue" || iconLower == "playlist" || iconLower == "list")
            {
                textBlock.Text = "\uE14C";
                textBlock.Foreground = new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 11. Mute / Volume
            else if (iconLower == "mute" || iconLower == "volume")
            {
                textBlock.Text = "\uE15D";
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 255, 75, 75)) 
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 12. Star Rating (Chuẩn AOSP Rating: RATING_3_STARS, RATING_4_STARS, RATING_5_STARS)
            else if (iconLower == "star" || iconLower.StartsWith("star") || actionName.Contains("star"))
            {
                textBlock.Text = action.IsActive ? "\uE1CF" : "\uE224"; // Segoe UI Symbol: Star filled / outline
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)) // Vàng sao One UI (#FFD700)
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 13. Ký tự Unicode đơn lẻ
            else if (!string.IsNullOrEmpty(icon) && icon.Length == 1)
            {
                textBlock.Text = icon;
                textBlock.Foreground = action.IsActive 
                    ? new SolidColorBrush(Color.FromArgb(255, 112, 166, 222)) 
                    : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
            }
            // 14. Mặc định theo vị trí
            else
            {
                if (isLeftDefault)
                {
                    textBlock.Text = action.IsActive ? "\uE00B" : "\uE006";
                    textBlock.Foreground = action.IsActive 
                        ? new SolidColorBrush(Color.FromArgb(255, 255, 75, 75)) 
                        : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
                }
                else
                {
                    textBlock.Text = "\uE148";
                    textBlock.Foreground = action.IsActive 
                        ? new SolidColorBrush(Color.FromArgb(255, 112, 166, 222)) 
                        : new SolidColorBrush(Color.FromArgb(208, 255, 255, 255));
                }
            }
        }

        private async void MusicFavorite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            var svc = MusicWidgetService.Instance;
            var act = svc.LeftAction;
            if (act == null) return;

            if (act.Action == AndroidCustomActions.ACTION_REPEAT || act.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await svc.CycleRepeatModeAsync();
                ApplyCustomActionButton(MusicHeartIcon, MusicLeftActionImage, act, true);
                return;
            }

            if (act.Action == AndroidCustomActions.ACTION_SHUFFLE || act.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await svc.CycleShuffleModeAsync();
                ApplyCustomActionButton(MusicHeartIcon, MusicLeftActionImage, act, true);
                return;
            }

            bool nextState = !act.IsActive;
            act.IsActive = nextState;
            if (act.Action == AndroidCustomActions.ACTION_FAVORITE) isFavorite = nextState;

            ApplyCustomActionButton(MusicHeartIcon, MusicLeftActionImage, act, true);
            await svc.ExecuteCustomActionAsync(act, nextState);
        }

        private async void MusicShuffle_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            var svc = MusicWidgetService.Instance;
            var act = svc.RightAction;
            if (act == null) return;

            if (act.Action == AndroidCustomActions.ACTION_REPEAT || act.Action.IndexOf("repeat", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await svc.CycleRepeatModeAsync();
                ApplyCustomActionButton(MusicShuffleIcon, MusicRightActionImage, act, false);
                return;
            }

            if (act.Action == AndroidCustomActions.ACTION_SHUFFLE || act.Action.IndexOf("shuffle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                await svc.CycleShuffleModeAsync();
                ApplyCustomActionButton(MusicShuffleIcon, MusicRightActionImage, act, false);
                return;
            }

            bool nextState = !act.IsActive;
            act.IsActive = nextState;
            if (act.Action == AndroidCustomActions.ACTION_SHUFFLE) isShuffle = nextState;

            ApplyCustomActionButton(MusicShuffleIcon, MusicRightActionImage, act, false);
            await svc.ExecuteCustomActionAsync(act, nextState);
        }

        #endregion
    }
}
