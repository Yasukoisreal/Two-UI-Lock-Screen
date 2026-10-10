using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Two_UI_Lock_Screen.Helpers;

namespace Two_UI_Lock_Screen
{
    public partial class MusicSettingsPage : PhoneApplicationPage
    {
        private bool _isLoaded = false;
        private System.Windows.Media.Animation.Storyboard _entranceStory;
        private System.Windows.Media.Animation.Storyboard _exitStory;
        private bool _isExiting;

        public MusicSettingsPage()
        {
            InitializeComponent();
            OneUiEffectHelper.AttachRipple(BackNavButton, BackNav_Ripple);
            OneUiEffectHelper.AttachRipple(BtnResetEndpoint, ResetEndpoint_Ripple);
            OneUiEffectHelper.AttachRipple(BtnSaveEndpoint, SaveEndpoint_Ripple);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

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
                if (_entranceStory != null)
                {
                    try { _entranceStory.Stop(); } catch { }
                    _entranceStory = null;
                }
                _entranceStory = OneUiEffectHelper.PlayPageEntranceAnimation(LayoutRoot, PageRootTransform);
            }

            LoadSettingsToUi();
            LoadWallpaperPreview();
            LoadTrackPreview();
            _isLoaded = true;
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

        private void LoadSettingsToUi()
        {
            var settings = SettingsManager.Current;
            MusicEnabledSwitch.IsChecked = settings.ShowMusicWidget;
            
            int trans = settings.MusicWidgetTransparency;
            if (trans < 0) trans = 0;
            if (trans > 100) trans = 100;

            TransparencySlider.Value = trans;
            TransparencyPercentText.Text = string.Format("{0}%", trans);
            ApplyPreviewTransparency(trans);

            MusicEndpointTextBox.Text = !string.IsNullOrEmpty(settings.MusicServerEndpoint)
                ? settings.MusicServerEndpoint
                : "http://olmp.local";
        }

        private void LoadWallpaperPreview()
        {
            try
            {
                var settings = SettingsManager.Current;
                string customFile = settings.CustomWallpaperFile;

                if (!string.IsNullOrEmpty(customFile))
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (store.FileExists(customFile))
                        {
                            using (var stream = store.OpenFile(customFile, FileMode.Open, FileAccess.Read))
                            {
                                var bmp = new BitmapImage();
                                bmp.SetSource(stream);
                                PreviewWallpaperBrush.ImageSource = bmp;
                                return;
                            }
                        }
                    }
                }

                // Fallback về ảnh nền mặc định
                PreviewWallpaperBrush.ImageSource = new BitmapImage(new Uri("/Assets/DefaultLockImage.png", UriKind.Relative));
            }
            catch
            {
                PreviewWallpaperBrush.ImageSource = new BitmapImage(new Uri("/Assets/DefaultLockImage.png", UriKind.Relative));
            }
        }

        private void LoadTrackPreview()
        {
            var svc = MusicWidgetService.Instance;
            if (svc != null && svc.HasActiveTrack)
            {
                PreviewTitleText.Text = !string.IsNullOrEmpty(svc.Title) ? svc.Title : "Track 1";
                PreviewArtistText.Text = !string.IsNullOrEmpty(svc.Artist) ? svc.Artist : "Samsung";

                if (!string.IsNullOrEmpty(svc.ThumbnailUrl))
                {
                    try
                    {
                        Uri thumbUri;
                        if (Uri.TryCreate(svc.ThumbnailUrl, UriKind.Absolute, out thumbUri))
                        {
                            PreviewAlbumCover.Source = new BitmapImage(thumbUri);
                        }
                    }
                    catch { }
                }
            }
            else
            {
                PreviewTitleText.Text = "Track 1";
                PreviewArtistText.Text = "Samsung";
            }
        }

        private void ApplyPreviewTransparency(int trans)
        {
            if (PreviewMusicCard == null) return;
            if (trans < 0) trans = 0;
            if (trans > 100) trans = 100;

            // 0% transparency = đục hoàn toàn (alpha = 255)
            // 100% transparency = trong suốt gần như hoàn toàn (alpha = 20)
            // Chuẩn One UI 6.1 (60% trong suốt): alpha ~ 102
            byte alpha = (byte)Math.Max(20, Math.Min(255, (int)(255 * (100 - trans) / 100.0)));
            PreviewMusicCard.Background = new SolidColorBrush(Color.FromArgb(alpha, 24, 34, 42));
        }

        private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            int val = (int)Math.Round(e.NewValue);
            if (val < 0) val = 0;
            if (val > 100) val = 100;

            if (TransparencyPercentText != null)
            {
                TransparencyPercentText.Text = string.Format("{0}%", val);
            }

            ApplyPreviewTransparency(val);

            var settings = SettingsManager.Current;
            settings.MusicWidgetTransparency = val;
            SettingsManager.Save(settings);
        }

        private void MusicEnabledSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowMusicWidget = MusicEnabledSwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void SaveEndpoint_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var settings = SettingsManager.Current;
            string ep = MusicEndpointTextBox.Text != null ? MusicEndpointTextBox.Text.Trim() : "";
            if (string.IsNullOrEmpty(ep)) ep = "http://olmp.local";
            settings.MusicServerEndpoint = ep;
            SettingsManager.Save(settings);

            MessageBox.Show("Đã lưu địa chỉ máy chủ phát nhạc!");
        }

        private void ResetEndpoint_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.MusicServerEndpoint = "http://olmp.local";
            SettingsManager.Save(settings);

            MusicEndpointTextBox.Text = "http://olmp.local";
            MessageBox.Show("Đã khôi phục địa chỉ mặc định!");
        }

        private void SaveEndpoint_Click(object sender, RoutedEventArgs e)
        {
            SaveEndpoint_Tap(sender, null);
        }

        private void ResetEndpoint_Click(object sender, RoutedEventArgs e)
        {
            ResetEndpoint_Tap(sender, null);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (_isExiting)
            {
                e.Cancel = true;
                return;
            }
            e.Cancel = true;
            NavigateBackWithAnimation();
        }

        private void BackNav_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigateBackWithAnimation();
        }

        private void NavigateBackWithAnimation()
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
                        NavigationService.Navigate(new Uri("/WidgetsPage.xaml", UriKind.Relative));
                    }
                }
                catch { }
            });
        }
    }
}
