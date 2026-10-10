using System;
using System.Runtime.Serialization;

namespace Two_UI_Lock_Screen.Models
{
    [DataContract]
    public class TwoUISettings
    {
        /// <summary>
        /// 0: Stacked (2 dòng: Giờ trên, Phút dưới - đặc trưng One UI)
        /// 1: Classic (Ngang: 22:24)
        /// 2: Bold Modern
        /// 3: Minimal Analog (Kim tối giản)
        /// 4: None
        /// </summary>
        [DataMember]
        public int ClockStyle { get; set; }

        [DataMember]
        public bool TouchAndHoldToEdit { get; set; }

        [DataMember]
        public bool ShowLunarCalendar { get; set; }

        [DataMember]
        public bool ShowWeather { get; set; }

        [DataMember]
        public string WeatherTemp { get; set; }

        [DataMember]
        public bool ShowBatteryWidget { get; set; }

        [DataMember]
        public bool ShowCalendarWidget { get; set; }

        [DataMember]
        public bool ShowAlarmWidget { get; set; }

        [DataMember]
        public string ContactInfo { get; set; }

        [DataMember]
        public string ClockColorHex { get; set; }

        [DataMember]
        public string LeftShortcut { get; set; }

        [DataMember]
        public string RightShortcut { get; set; }

        [DataMember]
        public string LocationName { get; set; }

        [DataMember]
        public string CustomWallpaperFile { get; set; }

        [DataMember]
        public bool ShowNotifications { get; set; }

        [DataMember]
        public bool ShowMusicWidget { get; set; }

        [DataMember]
        public string MusicServerEndpoint { get; set; }

        [DataMember]
        public string ListenTogetherRoomCode { get; set; }

        [DataMember]
        public bool UseVideoWallpaper { get; set; }

        [DataMember]
        public string CustomWallpaperVideoFile { get; set; }

        /// <summary>
        /// Độ trong suốt của thẻ tiện ích nhạc (Music widget transparency)
        /// Giá trị từ 0 đến 100 (%), mặc định chuẩn One UI 6.1 là 60%.
        /// </summary>
        [DataMember]
        public int MusicWidgetTransparency { get; set; }

        public TwoUISettings()
        {
            ClockStyle = 0; // 0: Classic Horizontal (12:06), 1: Stacked (12 / 06)
            TouchAndHoldToEdit = true;
            ShowLunarCalendar = true;
            ShowWeather = true;
            ShowNotifications = true;
            ShowMusicWidget = true;
            MusicWidgetTransparency = 60;
            MusicServerEndpoint = "http://olmp.local";
            ListenTogetherRoomCode = "";
            WeatherTemp = "";
            LocationName = "";
            ShowBatteryWidget = true;
            ShowCalendarWidget = true;
            ShowAlarmWidget = true;
            ContactInfo = "";
            ClockColorHex = "#FFFFFE";
            LeftShortcut = "Flashlight";
            RightShortcut = "Camera";
            CustomWallpaperFile = "";
            UseVideoWallpaper = false;
            CustomWallpaperVideoFile = "";
        }
    }
}
