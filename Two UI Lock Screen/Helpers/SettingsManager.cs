using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Threading;
using Two_UI_Lock_Screen.Models;

namespace Two_UI_Lock_Screen.Helpers
{
    /// <summary>
    /// Đồng bộ dữ liệu cài đặt đa tiến trình (Cross-Process Settings Synchronization)
    /// Sử dụng System-wide Named Mutex và cơ chế sao lưu 2 pha (datas.xml / datas.xml.bak)
    /// Tránh lỗi IsolatedStorageException sharing violation giữa LockView và MainPage.
    /// </summary>
    public static class SettingsManager
    {
        public const string DATAFILE = "two_ui_settings.xml";
        public const string DATAFILE_BAK = "two_ui_settings.xml.bak";

        private static readonly Mutex _mutex = new Mutex(false, "TwoUILockScreenMutex");
        private static TwoUISettings _currentSettings;

        public static TwoUISettings Current
        {
            get
            {
                if (_currentSettings == null)
                {
                    Load();
                }
                return _currentSettings;
            }
        }

        public static void Load()
        {
            try
            {
                _mutex.WaitOne(TimeSpan.FromSeconds(2));
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(DATAFILE))
                    {
                        try
                        {
                            using (var stream = store.OpenFile(DATAFILE, FileMode.Open, FileAccess.Read))
                            {
                                var serializer = new DataContractSerializer(typeof(TwoUISettings));
                                _currentSettings = serializer.ReadObject(stream) as TwoUISettings;
                            }

                            if (_currentSettings != null)
                            {
                                // Tạo file backup nguyên tử
                                using (var stream = store.OpenFile(DATAFILE, FileMode.Open, FileAccess.Read))
                                using (var backupStream = store.OpenFile(DATAFILE_BAK, FileMode.Create))
                                {
                                    stream.CopyTo(backupStream);
                                }
                                return;
                            }
                        }
                        catch
                        {
                            // File chính bị hỏng hoặc đang ghi dở -> phục hồi từ backup
                        }
                    }

                    // Fallback sang file backup nếu file chính có vấn đề
                    if (store.FileExists(DATAFILE_BAK))
                    {
                        try
                        {
                            using (var backupStream = store.OpenFile(DATAFILE_BAK, FileMode.Open, FileAccess.Read))
                            {
                                var serializer = new DataContractSerializer(typeof(TwoUISettings));
                                _currentSettings = serializer.ReadObject(backupStream) as TwoUISettings;
                                if (_currentSettings != null) return;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SettingsManager.Load error: " + ex.Message);
            }
            finally
            {
                if (_currentSettings == null)
                {
                    _currentSettings = new TwoUISettings();
                }
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch { }
            }
        }

        public static void Save(TwoUISettings settings = null)
        {
            if (settings != null)
            {
                _currentSettings = settings;
            }
            if (_currentSettings == null)
            {
                _currentSettings = new TwoUISettings();
            }

            try
            {
                _mutex.WaitOne(TimeSpan.FromSeconds(2));
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = store.OpenFile(DATAFILE, FileMode.Create, FileAccess.Write))
                    {
                        var serializer = new DataContractSerializer(typeof(TwoUISettings));
                        serializer.WriteObject(stream, _currentSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SettingsManager.Save error: " + ex.Message);
            }
            finally
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch { }
            }
        }
    }
}
