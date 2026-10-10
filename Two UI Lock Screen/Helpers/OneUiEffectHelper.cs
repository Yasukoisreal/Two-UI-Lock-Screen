using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Two_UI_Lock_Screen.Helpers
{
    /// <summary>
    /// Bộ hỗ trợ hiệu ứng giao diện chuẩn Samsung One UI 6.1 cho Windows Phone 8.1 Silverlight:
    /// 1. Hiệu ứng chạm lan tỏa 2 bên (Horizontal Ripple Expansion từ điểm ngón tay chạm).
    /// 2. Hiệu ứng chuyển tiếp trang One UI mượt mà (Trượt dọc 55px -> 0px + Mờ dần hiện rõ, KHÔNG phóng to).
    /// </summary>
    public static class OneUiEffectHelper
    {
        /// <summary>
        /// Kích hoạt hiệu ứng chuyển tiếp vào trang One UI 6.1 (Trượt dọc TranslateY từ 55px lên 0px kết hợp mờ dần hiện rõ trong 230ms).
        /// Tuyệt đối KHÔNG có phóng to / thu nhỏ (No Scale).
        /// </summary>
        public static Storyboard PlayPageEntranceAnimation(UIElement layoutRoot, CompositeTransform pageTransform)
        {
            if (layoutRoot == null || pageTransform == null)
            {
                return null;
            }

            pageTransform.TranslateY = 55.0;
            pageTransform.TranslateX = 0.0;
            pageTransform.ScaleX = 1.0;
            pageTransform.ScaleY = 1.0;
            layoutRoot.Opacity = 0.0;

            var sb = new Storyboard();

            // 1. Trượt dọc từ dưới lên (TranslateY 55 -> 0)
            var slideAnim = new DoubleAnimation
            {
                From = 55.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(230),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideAnim, pageTransform);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("TranslateY"));
            sb.Children.Add(slideAnim);

            // 2. Mờ dần hiện rõ (Opacity 0.0 -> 1.0)
            var fadeAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeAnim, layoutRoot);
            Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
            sb.Children.Add(fadeAnim);

            sb.Begin();
            return sb;
        }

        /// <summary>
        /// Kích hoạt hiệu ứng chuyển tiếp thoát trang One UI 6.1 (Trượt dọc TranslateY từ 0px xuống 55px kết hợp mờ dần biến mất trong 190ms).
        /// Khi hoàn tất animation, gọi hàm onCompleted để thực hiện điều hướng hoặc thoát.
        /// </summary>
        public static Storyboard PlayPageExitAnimation(UIElement layoutRoot, CompositeTransform pageTransform, Action onCompleted)
        {
            if (layoutRoot == null || pageTransform == null)
            {
                if (onCompleted != null)
                {
                    onCompleted();
                }
                return null;
            }

            pageTransform.TranslateY = 0.0;
            pageTransform.TranslateX = 0.0;
            pageTransform.ScaleX = 1.0;
            pageTransform.ScaleY = 1.0;
            layoutRoot.Opacity = 1.0;

            var sb = new Storyboard();

            // 1. Trượt dọc từ trên xuống dưới (TranslateY 0.0 -> 55.0)
            var slideAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 55.0,
                Duration = TimeSpan.FromMilliseconds(190),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(slideAnim, pageTransform);
            Storyboard.SetTargetProperty(slideAnim, new PropertyPath("TranslateY"));
            sb.Children.Add(slideAnim);

            // 2. Mờ dần biến mất (Opacity 1.0 -> 0.0)
            var fadeAnim = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(170),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeAnim, layoutRoot);
            Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
            sb.Children.Add(fadeAnim);

            sb.Completed += (s, e) =>
            {
                if (onCompleted != null)
                {
                    onCompleted();
                }
            };

            sb.Begin();
            return sb;
        }

        /// <summary>
        /// Gắn hiệu ứng chạm lan tỏa 2 bên One UI 6.1 vào một phần tử giao diện.
        /// Khi chạm ngón tay: màu xám nổi bật mở rộng sang 2 bên từ vị trí chạm (ScaleX: 0.1 -> 1.0).
        /// Khi nhấc tay ra: mờ dần biến mất (Opacity: current -> 0.0).
        /// Khi cuộn danh sách (Scroll): hủy highlight tức thì để tránh dính màu.
        /// </summary>
        public static void AttachRipple(FrameworkElement touchTarget, FrameworkElement rippleOverlay)
        {
            if (touchTarget == null || rippleOverlay == null)
            {
                return;
            }

            var handler = new RippleHandler(touchTarget, rippleOverlay);
            handler.Attach();
        }

        private class RippleHandler
        {
            private readonly FrameworkElement _touchTarget;
            private readonly FrameworkElement _rippleOverlay;
            private CompositeTransform _transform;
            private Storyboard _expandStory;
            private Storyboard _fadeStory;
            private DateTime _touchDownTime;
            private bool _isCancelled;

            public RippleHandler(FrameworkElement touchTarget, FrameworkElement rippleOverlay)
            {
                _touchTarget = touchTarget;
                _rippleOverlay = rippleOverlay;
            }

            public void Attach()
            {
                _transform = _rippleOverlay.RenderTransform as CompositeTransform;
                if (_transform == null)
                {
                    _transform = new CompositeTransform();
                    _rippleOverlay.RenderTransform = _transform;
                }

                _transform.ScaleX = 0.0;
                _transform.ScaleY = 1.0;
                _rippleOverlay.Opacity = 0.0;

                _touchTarget.ManipulationStarted += OnManipulationStarted;
                _touchTarget.ManipulationDelta += OnManipulationDelta;
                _touchTarget.ManipulationCompleted += OnManipulationCompleted;
                _touchTarget.Unloaded += OnUnloaded;
            }

            private void OnUnloaded(object sender, RoutedEventArgs e)
            {
                _touchTarget.ManipulationStarted -= OnManipulationStarted;
                _touchTarget.ManipulationDelta -= OnManipulationDelta;
                _touchTarget.ManipulationCompleted -= OnManipulationCompleted;
                _touchTarget.Unloaded -= OnUnloaded;

                StopAnimation(ref _expandStory);
                StopAnimation(ref _fadeStory);
            }

            private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
            {
                _isCancelled = false;
                _touchDownTime = DateTime.UtcNow;

                StopAnimation(ref _fadeStory);
                StopAnimation(ref _expandStory);

                // Tính tọa độ chạm ngang tương đối để đặt điểm bắt đầu lan tỏa (RenderTransformOrigin)
                double targetWidth = _touchTarget.ActualWidth;
                if (targetWidth <= 0)
                {
                    targetWidth = _rippleOverlay.ActualWidth;
                }

                double originX = 0.5;
                if (targetWidth > 0)
                {
                    double touchX = e.ManipulationOrigin.X;
                    originX = Math.Max(0.0, Math.Min(1.0, touchX / targetWidth));
                }

                _rippleOverlay.RenderTransformOrigin = new Point(originX, 0.5);

                // Khởi tạo Storyboard lan tỏa ra 2 bên
                _expandStory = new Storyboard();

                var scaleAnim = new DoubleAnimation
                {
                    From = 0.05,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(140),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleAnim, _transform);
                Storyboard.SetTargetProperty(scaleAnim, new PropertyPath("ScaleX"));
                _expandStory.Children.Add(scaleAnim);

                var fadeAnim = new DoubleAnimation
                {
                    From = _rippleOverlay.Opacity,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(70),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnim, _rippleOverlay);
                Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
                _expandStory.Children.Add(fadeAnim);

                _expandStory.Begin();
            }

            private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
            {
                // Nếu người dùng cuộn danh sách (vuốt dọc > 7px), lập tức hủy highlight để tránh dính màu khi scroll
                if (!_isCancelled && (Math.Abs(e.DeltaManipulation.Translation.Y) > 5.0 || Math.Abs(e.CumulativeManipulation.Translation.Y) > 7.0))
                {
                    _isCancelled = true;
                    CancelHighlightQuickly();
                }
            }

            private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
            {
                if (_isCancelled)
                {
                    return;
                }

                // Đảm bảo highlight hiển thị đủ lâu nếu người dùng chỉ chạm rất nhanh (tap)
                double elapsedMs = (DateTime.UtcNow - _touchDownTime).TotalMilliseconds;
                int delayMs = 0;
                if (elapsedMs < 80.0)
                {
                    delayMs = (int)(80.0 - elapsedMs);
                }

                FadeOutHighlight(delayMs);
            }

            private void CancelHighlightQuickly()
            {
                StopAnimation(ref _expandStory);
                StopAnimation(ref _fadeStory);

                _fadeStory = new Storyboard();

                var fadeAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(60),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnim, _rippleOverlay);
                Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
                _fadeStory.Children.Add(fadeAnim);

                _fadeStory.Completed += (s, args) =>
                {
                    _transform.ScaleX = 0.0;
                };

                _fadeStory.Begin();
            }

            private void FadeOutHighlight(int delayMs)
            {
                StopAnimation(ref _expandStory);
                StopAnimation(ref _fadeStory);

                _fadeStory = new Storyboard();

                if (delayMs > 0)
                {
                    _fadeStory.BeginTime = TimeSpan.FromMilliseconds(delayMs);
                }

                var fadeAnim = new DoubleAnimation
                {
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(130),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnim, _rippleOverlay);
                Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));
                _fadeStory.Children.Add(fadeAnim);

                _fadeStory.Completed += (s, args) =>
                {
                    _transform.ScaleX = 0.0;
                };

                _fadeStory.Begin();
            }

            private void StopAnimation(ref Storyboard story)
            {
                if (story != null)
                {
                    try
                    {
                        story.Stop();
                    }
                    catch
                    {
                    }
                    story = null;
                }
            }
        }
    }
}
