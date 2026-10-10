using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Two_UI_Lock_Screen.Helpers;

namespace Two_UI_Lock_Screen
{
    public partial class RoamingClockPage : PhoneApplicationPage
    {
        private System.Windows.Media.Animation.Storyboard _entranceStory;
        private System.Windows.Media.Animation.Storyboard _exitStory;
        private bool _isExiting;

        public RoamingClockPage()
        {
            InitializeComponent();
            OneUiEffectHelper.AttachRipple(BackNavButton, BackNav_Ripple);
            OneUiEffectHelper.AttachRipple(RowRegion, Region_Ripple);
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

            var settings = SettingsManager.Current;
            RoamingClockSwitch.IsChecked = settings.ShowWeather;
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

        private void RoamingClockSwitch_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Current;
            settings.ShowWeather = RoamingClockSwitch.IsChecked == true;
            SettingsManager.Save(settings);
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
