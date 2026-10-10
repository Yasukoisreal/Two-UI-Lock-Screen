using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Microsoft.Devices;

namespace Two_UI_Lock_Screen.Helpers
{
    /// <summary>
    /// Điều khiển Đèn pin (LED Torch) phần cứng chuẩn One UI
    /// Tích hợp rung phản hồi Haptic và cờ khóa chống nhấn lặp (re-entrancy guard)
    /// </summary>
    public static class FlashlightHelper
    {
        private static MediaCapture _mediaCapture;
        private static bool _isProcessing = false;

        public static bool IsOn { get; private set; }
        public static event Action<bool> StateChanged;

        public static async Task<bool> ToggleAsync()
        {
            if (_isProcessing) return IsOn;
            _isProcessing = true;

            try
            {
                // Rung phản hồi haptic 25ms tạo cảm giác bấm vật lý
                try
                {
                    VibrateController.Default.Start(TimeSpan.FromMilliseconds(25));
                }
                catch { }

                if (!IsOn)
                {
                    if (_mediaCapture == null)
                    {
                        var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                        var backCam = devices.FirstOrDefault(x => x.EnclosureLocation != null && 
                                      x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back) 
                                      ?? devices.FirstOrDefault();

                        if (backCam == null) return false;

                        _mediaCapture = new MediaCapture();
                        await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                        {
                            VideoDeviceId = backCam.Id,
                            AudioDeviceId = string.Empty, // Tránh yêu cầu quyền Microphone
                            StreamingCaptureMode = StreamingCaptureMode.Video,
                            PhotoCaptureSource = PhotoCaptureSource.VideoPreview
                        });
                    }

                    var torch = _mediaCapture.VideoDeviceController.TorchControl;
                    if (torch.Supported)
                    {
                        if (torch.PowerSupported) torch.PowerPercent = 100f;
                        torch.Enabled = true;
                        IsOn = true;
                    }
                }
                else
                {
                    if (_mediaCapture != null)
                    {
                        var torch = _mediaCapture.VideoDeviceController.TorchControl;
                        if (torch.Supported) torch.Enabled = false;
                        _mediaCapture.Dispose();
                        _mediaCapture = null;
                    }
                    IsOn = false;
                }

                if (StateChanged != null)
                {
                    StateChanged(IsOn);
                }
                return IsOn;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Flashlight error: " + ex.Message);
                return false;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public static void TurnOffAndDispose()
        {
            if (_mediaCapture != null)
            {
                try
                {
                    var torch = _mediaCapture.VideoDeviceController.TorchControl;
                    if (torch.Supported) torch.Enabled = false;
                    _mediaCapture.Dispose();
                }
                catch { }
                _mediaCapture = null;
                IsOn = false;
                if (StateChanged != null)
                {
                    StateChanged(false);
                }
            }
        }
    }
}
