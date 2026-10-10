using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Windows.Phone.System;
using Windows.Phone.System.LockScreenExtensibility;
using Two_UI_Lock_Screen.Helpers;
using Two_UI_Lock_Screen.Models;

namespace Two_UI_Lock_Screen
{
    public partial class MainPage : PhoneApplicationPage
    {
        private PhotoChooserTask _photoChooser;
        private Storyboard _entranceStory;
        private Storyboard _exitStory;
        private bool _isExiting;

        public MainPage()
        {
            InitializeComponent();

            _photoChooser = new PhotoChooserTask();
            _photoChooser.ShowCamera = true;
            _photoChooser.Completed += PhotoChooser_Completed;

            InitializeOneUiEffects();
        }

        private void InitializeOneUiEffects()
        {
            // Gắn hiệu ứng chạm lan tỏa 2 bên chuẩn Samsung One UI 6.1 (Horizontal Ripple Expansion)
            OneUiEffectHelper.AttachRipple(BackNavButton, BackNav_Ripple);
            OneUiEffectHelper.AttachRipple(SearchNavButton, SearchNav_Ripple);

            OneUiEffectHelper.AttachRipple(RowPreviewLock, PreviewLock_Ripple);
            OneUiEffectHelper.AttachRipple(RowSystemLockSettings, SystemLockSettings_Ripple);

            OneUiEffectHelper.AttachRipple(RowRoamingClockLeft, RoamingClock_Ripple);
            OneUiEffectHelper.AttachRipple(RowWidget, Widget_Ripple);
            OneUiEffectHelper.AttachRipple(RowClockStyle, ClockStyle_Ripple);

            OneUiEffectHelper.AttachRipple(RowSelectVideo, SelectVideo_Ripple);
            OneUiEffectHelper.AttachRipple(RowAboutLockScreen, AboutLockScreen_Ripple);

            OneUiEffectHelper.AttachRipple(RowEditLockScreen, EditLockScreen_Ripple);
            OneUiEffectHelper.AttachRipple(RowHideContent, HideContent_Ripple);
            OneUiEffectHelper.AttachRipple(RowNotificationSettings, NotificationSettings_Ripple);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Anti-Desync Guard: Nếu khóa máy lúc ở Settings, lập tức chuyển sang LockView
            if (SystemProtection.ScreenLocked)
            {
                NavigationService.Navigate(new Uri("/LockView.xaml", UriKind.Relative));
                return;
            }

            // Dọn dẹp sạch Backstack
            while (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }

            // Khi vào trang mới (New/Refresh) thì trượt lên; khi quay lại từ trang con (Back) thì giữ nguyên vị trí
            if (e.NavigationMode == NavigationMode.Back)
            {
                _isExiting = false;
                PageRootTransform.TranslateY = 0.0;
                PageRootTransform.TranslateX = 0.0;
                PageRootTransform.ScaleX = 1.0;
                PageRootTransform.ScaleY = 1.0;
                LayoutRoot.Opacity = 1.0;
            }
            else
            {
                _isExiting = false;
                PlayOneUiPageEntranceAnimation();
            }

            // Kiểm tra kết quả từ FileOpenPicker nếu vừa chọn video
            var app = Application.Current as App;
            if (app != null && app.FilePickerContinuationArgs != null)
            {
                var continuationArgs = app.FilePickerContinuationArgs;
                app.FilePickerContinuationArgs = null;
                HandleFilePickerContinuation(continuationArgs);
            }

            LoadSettingsToUI();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_entranceStory != null)
            {
                try { _entranceStory.Stop(); } catch { }
                _entranceStory = null;
            }
            if (_exitStory != null)
            {
                try { _exitStory.Stop(); } catch { }
                _exitStory = null;
            }
        }

        private void PlayOneUiPageEntranceAnimation()
        {
            if (_entranceStory != null)
            {
                try { _entranceStory.Stop(); } catch { }
                _entranceStory = null;
            }

            _entranceStory = OneUiEffectHelper.PlayPageEntranceAnimation(LayoutRoot, PageRootTransform);
        }

        private void LoadSettingsToUI()
        {
            try
            {
                ActiveLockSwitch.IsChecked = ExtensibilityApp.IsLockScreenApplicationRegistered();
            }
            catch
            {
                ActiveLockSwitch.IsChecked = false;
            }

            var settings = SettingsManager.Current;

            TouchAndHoldSwitch.IsChecked = settings.TouchAndHoldToEdit;
            LunarSwitch.IsChecked = settings.ShowLunarCalendar;
            RoamingClockSwitch.IsChecked = settings.ShowWeather;
            NotificationSwitch.IsChecked = settings.ShowNotifications;

            UseVideoWallpaperSwitch.IsChecked = settings.UseVideoWallpaper;
            UpdateVideoWallpaperSubtitle(settings.CustomWallpaperVideoFile);

            UpdateClockStyleSubtitle(settings.ClockStyle);
        }

        private void UpdateClockStyleSubtitle(int style)
        {
            ClockStyleSubtitle.Text = (style == 1)
                ? "Số 2 dòng (Giờ trên, phút dưới)"
                : "Số ngang 1 dòng (12:06 - Mặc định One UI)";
        }

        #region Core Live Lock Screen Actions

        private void ActiveLockSwitch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ActiveLockSwitch.IsChecked == true)
                {
                    if (!ExtensibilityApp.IsLockScreenApplicationRegistered())
                    {
                        ExtensibilityApp.RegisterLockScreenApplication();
                    }
                }
                else
                {
                    if (ExtensibilityApp.IsLockScreenApplicationRegistered())
                    {
                        ExtensibilityApp.UnregisterLockScreenApplication();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thay đổi đăng ký Live Lock Screen: " + ex.Message);
            }
        }

        private void PreviewLock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/LockView.xaml", UriKind.Relative));
        }

        private async void SystemLockSettings_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở Cài đặt màn hình khóa của máy: " + ex.Message);
            }
        }

        #endregion

        #region One UI Setting Toggles

        private void SettingChanged_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.TouchAndHoldToEdit = TouchAndHoldSwitch.IsChecked == true;
            settings.ShowLunarCalendar = LunarSwitch.IsChecked == true;
            settings.ShowWeather = RoamingClockSwitch.IsChecked == true;
            settings.ShowNotifications = NotificationSwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void RoamingClockDetails_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/RoamingClockPage.xaml", UriKind.Relative));
        }

        private void WidgetDetails_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/WidgetsPage.xaml", UriKind.Relative));
        }

        private void ClockStyle_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ClockStyle = (settings.ClockStyle == 0) ? 1 : 0;
            SettingsManager.Save(settings);
            UpdateClockStyleSubtitle(settings.ClockStyle);
        }

        private void AboutLockScreen_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutLockScreenPage.xaml", UriKind.Relative));
        }

        #endregion

        #region Header Navigation

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (_isExiting)
            {
                e.Cancel = true;
                return;
            }
            e.Cancel = true;
            ExitWithAnimation();
        }

        private void BackNav_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ExitWithAnimation();
        }

        private void ExitWithAnimation()
        {
            if (_isExiting) return;
            _isExiting = true;

            _exitStory = OneUiEffectHelper.PlayPageExitAnimation(LayoutRoot, PageRootTransform, () =>
            {
                try
                {
                    if (NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack();
                    }
                    else
                    {
                        Application.Current.Terminate();
                    }
                }
                catch { }
            });
        }

        private void SearchNav_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show("Tìm kiếm cài đặt Màn hình khóa One UI", "Tìm kiếm", MessageBoxButton.OK);
        }

        #endregion

        #region 'Bạn đang tìm kiếm điều gì khác?' Footer Actions

        private void EditLockScreen_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var settings = SettingsManager.Current;
            if (!string.IsNullOrEmpty(settings.CustomWallpaperFile))
            {
                var result = MessageBox.Show("Bạn muốn chọn hình nền mới hay đặt lại hình nền mặc định?\n\n- Nhấn OK: Chọn ảnh mới từ Bộ sưu tập\n- Nhấn Cancel: Đặt lại hình nền mặc định", "Sửa Màn hình khóa", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    OpenWallpaperPicker();
                }
                else
                {
                    settings.CustomWallpaperFile = "";
                    SettingsManager.Save(settings);
                    MessageBox.Show("Đã đặt lại hình nền mặc định!");
                }
            }
            else
            {
                OpenWallpaperPicker();
            }
        }

        private void OpenWallpaperPicker()
        {
            try
            {
                _photoChooser.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở bộ sưu tập: " + ex.Message);
            }
        }

        private void PhotoChooser_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK && e.ChosenPhoto != null)
            {
                try
                {
                    const string customWallpaperFileName = "custom_wallpaper.jpg";
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var fileStream = store.OpenFile(customWallpaperFileName, FileMode.Create, FileAccess.Write))
                        {
                            e.ChosenPhoto.CopyTo(fileStream);
                        }
                    }

                    var settings = SettingsManager.Current;
                    settings.CustomWallpaperFile = customWallpaperFileName;
                    SettingsManager.Save(settings);
                    MessageBox.Show("Đã lưu hình nền mới thành công!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi lưu hình nền: " + ex.Message);
                }
            }
        }

        private void HideContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show("Bảo vệ tính riêng tư: Ứng dụng chỉ hiển thị biểu tượng thông báo và số lượng, không hiển thị nội dung tin nhắn nhạy cảm trên màn hình khóa.", "Ẩn nội dung", MessageBoxButton.OK);
        }

        private void NotificationSettings_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show("Màn hình khóa One UI tự động đồng bộ huy hiệu cuộc gọi nhỡ và tin nhắn thông qua hợp đồng Live Lock Screen của Windows Phone.", "Thông báo cần hiển thị", MessageBoxButton.OK);
        }

        #endregion

        #region Video Wallpaper Actions

        private void UseVideoWallpaperSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            bool isChecked = UseVideoWallpaperSwitch.IsChecked == true;

            if (isChecked)
            {
                // Kiểm tra xem đã có video nền nào được lưu trong máy chưa
                bool hasVideo = false;
                try
                {
                    if (!string.IsNullOrEmpty(settings.CustomWallpaperVideoFile))
                    {
                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            hasVideo = store.FileExists(settings.CustomWallpaperVideoFile);
                        }
                    }
                }
                catch { }

                if (!hasVideo)
                {
                    UseVideoWallpaperSwitch.IsChecked = false;
                    var result = MessageBox.Show("Bạn chưa có tệp video nền nào. Bạn có muốn chọn video từ máy ngay bây giờ không?", "Video nền", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        OpenVideoPicker();
                    }
                    return;
                }

                settings.UseVideoWallpaper = true;
            }
            else
            {
                settings.UseVideoWallpaper = false;
            }

            SettingsManager.Save(settings);
        }

        private void SelectVideo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var settings = SettingsManager.Current;
            if (!string.IsNullOrEmpty(settings.CustomWallpaperVideoFile))
            {
                var result = MessageBox.Show("Bạn muốn chọn video mới hay đặt lại về hình nền tĩnh?\n\n- Nhấn OK: Chọn video mới từ máy\n- Nhấn Cancel: Hủy video nền", "Video nền", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    OpenVideoPicker();
                }
                else
                {
                    settings.CustomWallpaperVideoFile = "";
                    settings.UseVideoWallpaper = false;
                    SettingsManager.Save(settings);
                    UseVideoWallpaperSwitch.IsChecked = false;
                    UpdateVideoWallpaperSubtitle("");
                    MessageBox.Show("Đã hủy video nền và quay lại hình nền tĩnh!");
                }
            }
            else
            {
                OpenVideoPicker();
            }
        }

        private void OpenVideoPicker()
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                picker.FileTypeFilter.Add(".mp4");
                picker.FileTypeFilter.Add(".wmv");
                picker.FileTypeFilter.Add(".avi");
                picker.FileTypeFilter.Add(".3gp");
                picker.FileTypeFilter.Add(".mkv");
                picker.PickSingleFileAndContinue();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở trình chọn tệp: " + ex.Message);
            }
        }

        private async void HandleFilePickerContinuation(Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs args)
        {
            if (args == null || args.Files == null || args.Files.Count == 0)
            {
                return;
            }

            try
            {
                var file = args.Files[0];
                if (file == null) return;

                const string videoFileName = "custom_wallpaper_video.mp4";

                // Đọc luồng từ StorageFile và sao chép an toàn vào IsolatedStorage
                using (var winStream = await file.OpenReadAsync())
                using (var netStream = winStream.AsStreamForRead())
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                using (var targetStream = store.OpenFile(videoFileName, FileMode.Create, FileAccess.Write))
                {
                    netStream.CopyTo(targetStream);
                }

                var settings = SettingsManager.Current;
                settings.CustomWallpaperVideoFile = videoFileName;
                settings.UseVideoWallpaper = true;
                SettingsManager.Save(settings);

                UseVideoWallpaperSwitch.IsChecked = true;
                UpdateVideoWallpaperSubtitle(videoFileName);

                MessageBox.Show("Đã lưu và kích hoạt video nền thành công!", "Video nền", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu video nền: " + ex.Message, "Lỗi", MessageBoxButton.OK);
            }
        }

        private void UpdateVideoWallpaperSubtitle(string customFileName)
        {
            if (VideoWallpaperSubtitle == null) return;

            if (string.IsNullOrEmpty(customFileName))
            {
                var settings = SettingsManager.Current;
                customFileName = (settings != null) ? settings.CustomWallpaperVideoFile : "";
            }

            if (!string.IsNullOrEmpty(customFileName))
            {
                bool exists = false;
                try
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        exists = store.FileExists(customFileName);
                    }
                }
                catch { }

                if (exists)
                {
                    VideoWallpaperSubtitle.Text = "Đang dùng: " + customFileName;
                    return;
                }
            }

            VideoWallpaperSubtitle.Text = "Chưa chọn video (Hỗ trợ định dạng .mp4, .wmv)";
        }

        #endregion
    }
}