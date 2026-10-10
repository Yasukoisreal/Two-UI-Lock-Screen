using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace Two_UI_Lock_Screen
{
    public partial class OpenSourceLicensesPage : PhoneApplicationPage
    {
        private static readonly SolidColorBrush MonospaceBrush = new SolidColorBrush(Color.FromArgb(255, 232, 232, 232)); // #E8E8E8
        private static readonly FontFamily MonoFont = new FontFamily("Consolas, Courier New");
        private System.Windows.Media.Animation.Storyboard _entranceStory;
        private System.Windows.Media.Animation.Storyboard _exitStory;
        private bool _isExiting;

        public OpenSourceLicensesPage()
        {
            InitializeComponent();
            Two_UI_Lock_Screen.Helpers.OneUiEffectHelper.AttachRipple(BackNavButton, BackNav_Ripple);
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
                _entranceStory = Two_UI_Lock_Screen.Helpers.OneUiEffectHelper.PlayPageEntranceAnimation(LayoutRoot, PageRootTransform);
            }

            LoadLicenses();
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

        private void LoadLicenses()
        {
            LicenseContainer.Children.Clear();

            // 1. Giấy phép Android Open Source Project (AOSP) - Apache 2.0
            string aospText = ReadLicenseFile("Assets/Licenses/Apache-2.0.txt");
            if (!string.IsNullOrEmpty(aospText))
            {
                AddLicenseToPanel(aospText);
            }

            // 2. Dòng phân cách chuẩn One UI 6.1
            AddDividerToPanel();

            // 3. Giấy phép Open-Meteo Weather API - CC BY 4.0
            string openMeteoText = ReadLicenseFile("Assets/Licenses/CC-BY-4.0.txt");
            if (!string.IsNullOrEmpty(openMeteoText))
            {
                AddLicenseToPanel(openMeteoText);
            }
        }

        private string ReadLicenseFile(string relativePath)
        {
            try
            {
                var uri = new Uri(relativePath, UriKind.Relative);
                var sri = Application.GetResourceStream(uri);
                if (sri != null && sri.Stream != null)
                {
                    using (var reader = new StreamReader(sri.Stream, System.Text.Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return "Lỗi khi đọc giấy phép (" + relativePath + "): " + ex.Message;
            }
            return null;
        }

        private void AddLicenseToPanel(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Chuẩn hóa xuống dòng
            string normalized = text.Replace("\r\n", "\n");
            // Tách theo đoạn văn (\n\n) để giữ các khối văn bản nguyên vẹn và tránh giới hạn 2048px chiều cao của Silverlight
            string[] paragraphs = normalized.Split(new string[] { "\n\n" }, StringSplitOptions.None);

            for (int i = 0; i < paragraphs.Length; i++)
            {
                string p = paragraphs[i].TrimEnd('\r', '\n');
                if (string.IsNullOrWhiteSpace(p))
                {
                    continue;
                }

                var tb = new TextBlock
                {
                    Text = p,
                    FontFamily = MonoFont,
                    FontSize = 10.0,
                    LineHeight = 14.5,
                    Foreground = MonospaceBrush,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12)
                };

                LicenseContainer.Children.Add(tb);
            }
        }

        private void AddDividerToPanel()
        {
            var tb = new TextBlock
            {
                Text = "----------------------------------------------------------------------",
                FontFamily = MonoFont,
                FontSize = 10.0,
                LineHeight = 14.5,
                Foreground = MonospaceBrush,
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(0, 8, 0, 18)
            };

            LicenseContainer.Children.Add(tb);
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

        private void BackNav_Tap(object sender, GestureEventArgs e)
        {
            NavigateBackWithAnimation();
        }

        private void NavigateBackWithAnimation()
        {
            if (_isExiting) return;
            _isExiting = true;

            _exitStory = Two_UI_Lock_Screen.Helpers.OneUiEffectHelper.PlayPageExitAnimation(LayoutRoot, PageRootTransform, () =>
            {
                try
                {
                    if (NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack();
                    }
                    else
                    {
                        NavigationService.Navigate(new Uri("/AboutLockScreenPage.xaml", UriKind.Relative));
                    }
                }
                catch { }
            });
        }
    }
}
