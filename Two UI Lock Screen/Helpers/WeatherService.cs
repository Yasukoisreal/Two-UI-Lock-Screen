using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Two_UI_Lock_Screen.Helpers
{
    [DataContract]
    public class WeatherData
    {
        [DataMember]
        public int Temperature { get; set; }

        [DataMember]
        public string LocationName { get; set; }

        [DataMember]
        public int WeatherCode { get; set; }

        [DataMember]
        public string WeatherIconUri { get; set; }

        [DataMember]
        public double UvIndex { get; set; }

        [DataMember]
        public DateTime LastUpdated { get; set; }

        [DataMember]
        public bool HasData { get; set; }
    }

    public static class WeatherService
    {
        private const string WEATHER_CACHE_FILE = "weather_cache.xml";
        private static readonly object _fileLock = new object();

        private static WeatherData _cachedData;
        private static DateTime _lastFetchTime = DateTime.MinValue;
        private static bool _isFetching = false;

        public static WeatherData CurrentData
        {
            get
            {
                if (_cachedData == null)
                {
                    _cachedData = LoadCacheFromDisk();
                }
                return _cachedData ?? GetEmptyFallback();
            }
        }

        public static WeatherData GetEmptyFallback()
        {
            return new WeatherData
            {
                Temperature = 0,
                LocationName = "",
                WeatherCode = 0,
                WeatherIconUri = "",
                UvIndex = 0.0,
                LastUpdated = DateTime.MinValue,
                HasData = false
            };
        }

        public static void SaveCacheToDisk(WeatherData data)
        {
            if (data == null) return;
            lock (_fileLock)
            {
                try
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var stream = store.OpenFile(WEATHER_CACHE_FILE, FileMode.Create, FileAccess.Write))
                        {
                            var serializer = new DataContractSerializer(typeof(WeatherData));
                            serializer.WriteObject(stream, data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SaveCacheToDisk error: " + ex.Message);
                }
            }
        }

        public static WeatherData LoadCacheFromDisk()
        {
            lock (_fileLock)
            {
                try
                {
                    using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (store.FileExists(WEATHER_CACHE_FILE))
                        {
                            using (var stream = store.OpenFile(WEATHER_CACHE_FILE, FileMode.Open, FileAccess.Read))
                            {
                                var serializer = new DataContractSerializer(typeof(WeatherData));
                                var data = serializer.ReadObject(stream) as WeatherData;
                                if (data != null && data.HasData)
                                {
                                    return data;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("LoadCacheFromDisk error: " + ex.Message);
                }

                // Fallback nếu chưa có file weather_cache nhưng SettingsManager đã lưu vị trí trước đó
                try
                {
                    var s = SettingsManager.Current;
                    if (s != null && !string.IsNullOrEmpty(s.LocationName))
                    {
                        int temp = 0;
                        int parsed;
                        if (!string.IsNullOrEmpty(s.WeatherTemp) && int.TryParse(s.WeatherTemp.Replace("°", "").Trim(), out parsed))
                        {
                            temp = parsed;
                        }
                        return new WeatherData
                        {
                            LocationName = s.LocationName,
                            Temperature = temp,
                            WeatherCode = 81,
                            WeatherIconUri = "/Assets/Icons/ic_weather_cloud_rain.png",
                            UvIndex = 0.0,
                            LastUpdated = DateTime.Now,
                            HasData = true
                        };
                    }
                }
                catch { }

                return null;
            }
        }

        public static async Task<WeatherData> GetWeatherAsync(bool forceRefresh = false)
        {
            if (_cachedData == null)
            {
                _cachedData = LoadCacheFromDisk();
            }

            // Cache trong vòng 20 phút để tiết kiệm pin và băng thông
            if (!forceRefresh && _cachedData != null && _cachedData.HasData && (DateTime.Now - _cachedData.LastUpdated).TotalMinutes < 20.0)
            {
                return _cachedData;
            }

            if (_isFetching)
            {
                return _cachedData ?? GetEmptyFallback();
            }

            _isFetching = true;

            try
            {
                bool isEmulator = Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator;
                double lat = 0;
                double lon = 0;
                string locationName = "";
                bool locationResolved = false;

                // 1. Dò vị trí qua IP mạng (Wi-Fi/Host PC):
                // Trên máy ảo Emulator hoặc máy thật kết nối Wi-Fi, IP mạng phản ánh chính xác vị trí địa lý thực tế của người dùng
                try
                {
                    string ipJson = await DownloadStringAsync("http://ip-api.com/json/", 4000);
                    if (!string.IsNullOrEmpty(ipJson))
                    {
                        double ipLat;
                        double ipLon;
                        if (TryExtractValue(ipJson, "lat", out ipLat) && TryExtractValue(ipJson, "lon", out ipLon))
                        {
                            lat = ipLat;
                            lon = ipLon;
                            string rawCity = ExtractString(ipJson, "city");
                            if (!string.IsNullOrEmpty(rawCity))
                            {
                                locationName = NormalizeCityName(rawCity);
                                locationResolved = true;
                            }
                        }
                    }
                }
                catch { }

                // 2. Nếu là máy thật (KHÔNG PHẢI EMULATOR) và có GPS phần cứng:
                // Dùng GPS nếu bắt được sóng vệ tinh thực tế (bỏ qua tọa độ ảo Redmond WA 47.67, -122.12 của Microsoft SDK)
                if (!isEmulator && !locationResolved)
                {
                    try
                    {
                        var geolocator = new Windows.Devices.Geolocation.Geolocator();
                        geolocator.DesiredAccuracyInMeters = 2000;
                        var pos = await geolocator.GetGeopositionAsync(TimeSpan.FromHours(2), TimeSpan.FromSeconds(5));
                        if (pos != null && pos.Coordinate != null && pos.Coordinate.Point != null)
                        {
                            double gpsLat = pos.Coordinate.Point.Position.Latitude;
                            double gpsLon = pos.Coordinate.Point.Position.Longitude;
                            bool isRedmondDummy = (Math.Abs(gpsLat - 47.67) < 0.5 && Math.Abs(gpsLon - (-122.12)) < 0.5);

                            if (!isRedmondDummy)
                            {
                                lat = gpsLat;
                                lon = gpsLon;
                                locationName = GetCityFromCoordinates(lat, lon, locationName);
                                locationResolved = true;
                            }
                        }
                    }
                    catch { }
                }

                // Nếu có cache trước đó nhưng lần này chưa dò được tên vị trí, tái sử dụng tên vị trí cũ
                if (string.IsNullOrEmpty(locationName) && _cachedData != null && !string.IsNullOrEmpty(_cachedData.LocationName))
                {
                    locationName = _cachedData.LocationName;
                }

                // Nếu chưa có tọa độ nào được tìm thấy, trả về cache hoặc fallback
                if (lat == 0 && lon == 0)
                {
                    return _cachedData ?? GetEmptyFallback();
                }

                // 3. Truy vấn API Open-Meteo REST theo tọa độ thực tế
                string url = string.Format(CultureInfo.InvariantCulture,
                    "http://api.open-meteo.com/v1/forecast?latitude={0:F4}&longitude={1:F4}&current=temperature_2m,weather_code,uv_index&timezone=auto",
                    lat, lon);

                string json = await DownloadStringAsync(url, 6000);

                if (!string.IsNullOrEmpty(json))
                {
                    int temp;
                    int code;
                    double uv;

                    if (TryParseOpenMeteo(json, out temp, out code, out uv))
                    {
                        var newData = new WeatherData
                        {
                            Temperature = temp,
                            LocationName = locationName,
                            WeatherCode = code,
                            WeatherIconUri = GetIconForCode(code),
                            UvIndex = uv,
                            LastUpdated = DateTime.Now,
                            HasData = true
                        };

                        _cachedData = newData;
                        _lastFetchTime = DateTime.Now;
                        SaveCacheToDisk(newData);

                        // Đồng bộ nhanh vào SettingsManager
                        try
                        {
                            var s = SettingsManager.Current;
                            if (s != null)
                            {
                                s.LocationName = locationName;
                                s.WeatherTemp = temp + "°";
                                SettingsManager.Save(s);
                            }
                        }
                        catch { }

                        return _cachedData;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Weather fetch error: " + ex.Message);
            }
            finally
            {
                _isFetching = false;
            }

            return _cachedData ?? GetEmptyFallback();
        }

        private static string NormalizeCityName(string city)
        {
            if (string.IsNullOrEmpty(city)) return "";
            string lower = city.Trim().ToLowerInvariant();

            if (lower.Contains("dien ban") || lower.Contains("điện bàn")) return "Điện Bàn";
            if (lower.Contains("dien minh") || lower.Contains("điện minh")) return "Điện Minh";
            if (lower.Contains("da nang") || lower.Contains("đà nẵng")) return "Đà Nẵng";
            if (lower.Contains("hoi an") || lower.Contains("hội an")) return "Hội An";
            if (lower.Contains("tam ky") || lower.Contains("tam kỳ")) return "Tam Kỳ";
            if (lower.Contains("quang nam") || lower.Contains("quảng nam")) return "Quảng Nam";
            if (lower.Contains("ha noi") || lower.Contains("hà nội") || lower.Contains("hanoi")) return "Hà Nội";
            if (lower.Contains("ho chi minh") || lower.Contains("hồ chí minh") || lower.Contains("saigon") || lower.Contains("sai gon")) return "TP. Hồ Chí Minh";
            if (lower.Contains("hai phong") || lower.Contains("hải phòng")) return "Hải Phòng";
            if (lower.Contains("can tho") || lower.Contains("cần thơ")) return "Cần Thơ";
            if (lower.Contains("hue") || lower.Contains("huế")) return "Huế";
            if (lower.Contains("nha trang")) return "Nha Trang";
            if (lower.Contains("vung tau") || lower.Contains("vũng tàu")) return "Vũng Tàu";
            if (lower.Contains("quy nhon") || lower.Contains("qui nhơn") || lower.Contains("quy nhơn")) return "Quy Nhơn";
            if (lower.Contains("buon ma thuot") || lower.Contains("buôn ma thuột")) return "Buôn Ma Thuột";
            if (lower.Contains("da lat") || lower.Contains("đà lạt")) return "Đà Lạt";

            return city.Trim();
        }

        private static string GetCityFromCoordinates(double lat, double lon, string fallbackCity)
        {
            if (Math.Abs(lat - 15.89) < 0.15 && Math.Abs(lon - 108.24) < 0.15) return "Điện Bàn";
            if (Math.Abs(lat - 15.88) < 0.25 && Math.Abs(lon - 108.28) < 0.25) return "Điện Minh";
            if (Math.Abs(lat - 16.06) < 0.25 && Math.Abs(lon - 108.22) < 0.25) return "Đà Nẵng";
            if (Math.Abs(lat - 15.88) < 0.25 && Math.Abs(lon - 108.33) < 0.25) return "Hội An";
            if (Math.Abs(lat - 21.02) < 0.35 && Math.Abs(lon - 105.83) < 0.35) return "Hà Nội";
            if (Math.Abs(lat - 10.77) < 0.35 && Math.Abs(lon - 106.70) < 0.35) return "TP. Hồ Chí Minh";
            if (Math.Abs(lat - 20.85) < 0.35 && Math.Abs(lon - 106.68) < 0.35) return "Hải Phòng";
            if (Math.Abs(lat - 10.03) < 0.35 && Math.Abs(lon - 105.78) < 0.35) return "Cần Thơ";
            if (Math.Abs(lat - 16.46) < 0.35 && Math.Abs(lon - 107.59) < 0.35) return "Huế";
            if (Math.Abs(lat - 47.67) < 0.35 && Math.Abs(lon - (-122.12)) < 0.35) return "Redmond";
            if (Math.Abs(lat - 47.60) < 0.35 && Math.Abs(lon - (-122.33)) < 0.35) return "Seattle";

            return string.IsNullOrEmpty(fallbackCity) ? "" : fallbackCity;
        }

        private static bool TryParseOpenMeteo(string json, out int temp, out int code, out double uv)
        {
            temp = 25;
            code = 0;
            uv = 0.0;
            try
            {
                int blockIdx = json.IndexOf("\"current\":{");
                if (blockIdx < 0) blockIdx = json.IndexOf("\"current_weather\":{");
                if (blockIdx < 0) return false;

                int blockEnd = json.IndexOf('}', blockIdx);
                if (blockEnd < 0) blockEnd = json.Length;
                string block = json.Substring(blockIdx, blockEnd - blockIdx + 1);

                double dTemp;
                if (TryExtractValue(block, "temperature_2m", out dTemp) || TryExtractValue(block, "temperature", out dTemp))
                {
                    temp = (int)Math.Round(dTemp);
                }

                double dCode;
                if (TryExtractValue(block, "weather_code", out dCode) || TryExtractValue(block, "weathercode", out dCode))
                {
                    code = (int)Math.Round(dCode);
                }

                double dUv;
                if (TryExtractValue(block, "uv_index", out dUv))
                {
                    uv = dUv;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Task<string> DownloadStringAsync(string url, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<string>();
            try
            {
                var client = new WebClient();
                client.Headers[HttpRequestHeader.UserAgent] = "TwoUILockScreen/1.0 (Windows Phone 8.1)";

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(timeoutMs) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    try { client.CancelAsync(); } catch { }
                    tcs.TrySetResult(null);
                };

                client.DownloadStringCompleted += (s, e) =>
                {
                    timer.Stop();
                    if (e.Error != null || e.Cancelled)
                    {
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        tcs.TrySetResult(e.Result);
                    }
                };

                timer.Start();
                client.DownloadStringAsync(new Uri(url));
            }
            catch
            {
                tcs.TrySetResult(null);
            }
            return tcs.Task;
        }

        private static bool TryExtractValue(string jsonBlock, string key, out double value)
        {
            value = 0.0;
            try
            {
                string pattern = "\"" + key + "\":";
                int idx = jsonBlock.IndexOf(pattern);
                if (idx < 0)
                {
                    pattern = key + ":";
                    idx = jsonBlock.IndexOf(pattern);
                    if (idx < 0) return false;
                }

                int start = idx + pattern.Length;
                int end = jsonBlock.IndexOfAny(new char[] { ',', '}', ' ', '\r', '\n' }, start);
                if (end < 0) end = jsonBlock.Length;

                string valStr = jsonBlock.Substring(start, end - start).Trim().Trim('"');
                return double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractString(string json, string key)
        {
            try
            {
                string pattern = "\"" + key + "\":\"";
                int idx = json.IndexOf(pattern);
                if (idx < 0) return null;
                int start = idx + pattern.Length;
                int end = json.IndexOf('"', start);
                if (end < 0) return null;
                return json.Substring(start, end - start);
            }
            catch
            {
                return null;
            }
        }

        private static string GetIconForCode(int code)
        {
            if (code <= 1) return "/Assets/Icons/ic_weather_sun.png";
            if (code <= 3 || code == 45 || code == 48) return "/Assets/Icons/ic_weather_cloud.png";
            return "/Assets/Icons/ic_weather_cloud_rain.png";
        }
    }
}
