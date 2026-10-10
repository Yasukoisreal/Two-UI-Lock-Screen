using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Windows.Phone.System;

namespace Two_UI_Lock_Screen
{
    public partial class LockRouter : PhoneApplicationPage
    {
        public LockRouter()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                if (SystemProtection.ScreenLocked)
                {
                    // Thiết bị đang khóa -> Điều hướng ngay vào Live Lock Screen
                    NavigationService.Navigate(new Uri("/LockView.xaml", UriKind.Relative));
                }
                else
                {
                    // Thiết bị đang mở -> Điều hướng vào giao diện Cài đặt (MainPage)
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                }
            }
            catch
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
        }
    }
}
