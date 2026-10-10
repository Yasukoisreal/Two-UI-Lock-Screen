using System;
using System.Windows;
using Microsoft.Phone.Controls;
using Two_UI_Lock_Screen.Helpers;

namespace Two_UI_Lock_Screen
{
    public partial class AboutLockScreenPage : PhoneApplicationPage
    {
        private System.Windows.Media.Animation.Storyboard _entranceStory;
        private System.Windows.Media.Animation.Storyboard _exitStory;
        private bool _isExiting;

        public AboutLockScreenPage()
        {
            InitializeComponent();
            OneUiEffectHelper.AttachRipple(BackNavButton, BackNav_Ripple);
            OneUiEffectHelper.AttachRipple(AppDetailsButton, AppDetails_Ripple);
            OneUiEffectHelper.AttachRipple(BtnOpenSourceLicenses, OpenSourceLicenses_Ripple);
            LoadVersion();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
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
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
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

        private void LoadVersion()
        {
            try
            {
                // Hiển thị phiên bản theo chuẩn One UI 6.1
                VersionText.Text = "Phiên bản 8.7.98.45";
            }
            catch
            {
                VersionText.Text = "Phiên bản 8.7.98.45";
            }
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

        private void AppDetails_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show("Two UI Lock Screen\nThiết kế màn hình khóa chuẩn Samsung One UI 6.1 dành cho Windows Phone 8.1 Silverlight.\n\nTác giả: Yasuko", "Thông tin ứng dụng", MessageBoxButton.OK);
        }

        private void OpenSourceLicenses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/OpenSourceLicensesPage.xaml", UriKind.Relative));
        }
    }
}
