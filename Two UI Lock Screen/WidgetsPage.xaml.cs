using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Two_UI_Lock_Screen.Helpers;

namespace Two_UI_Lock_Screen
{
    public partial class WidgetsPage : PhoneApplicationPage
    {
        private System.Windows.Media.Animation.Storyboard _entranceStory;
        private System.Windows.Media.Animation.Storyboard _exitStory;
        private bool _isExiting;

        public WidgetsPage()
        {
            InitializeComponent();
            OneUiEffectHelper.AttachRipple(BackNavButton, BackNav_Ripple);
            OneUiEffectHelper.AttachRipple(RowMusicLeft, Music_Ripple);
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

        private void LoadSettingsToUI()
        {
            var settings = SettingsManager.Current;

            MusicSwitch.IsChecked = settings.ShowMusicWidget;
            WeatherSwitch.IsChecked = settings.ShowWeather;
            BatterySwitch.IsChecked = settings.ShowBatteryWidget;
            CalendarSwitch.IsChecked = settings.ShowCalendarWidget;
            AlarmSwitch.IsChecked = settings.ShowAlarmWidget;

            if (MusicSubtitle != null)
            {
                string ep = !string.IsNullOrEmpty(settings.MusicServerEndpoint) ? settings.MusicServerEndpoint : "http://olmp.local";
                MusicSubtitle.Text = string.Format("Độ trong suốt: {0}% • {1}", settings.MusicWidgetTransparency, ep);
            }
        }

        private void MusicSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowMusicWidget = MusicSwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void WeatherSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowWeather = WeatherSwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void BatterySwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowBatteryWidget = BatterySwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void CalendarSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowCalendarWidget = CalendarSwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void AlarmSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowAlarmWidget = AlarmSwitch.IsChecked == true;
            SettingsManager.Save(settings);
        }

        private void MusicDetails_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/MusicSettingsPage.xaml", UriKind.Relative));
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
                        NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                    }
                }
                catch { }
            });
        }
    }
}
