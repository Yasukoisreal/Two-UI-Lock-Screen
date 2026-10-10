using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Two_UI_Lock_Screen.Helpers
{
    /// <summary>
    /// Easing lò xo vật lý chính thức của Microsoft Live Lock Screen
    /// Mô phỏng độ nảy đàn hồi tự nhiên khi cử chỉ vuốt bị hủy (snap-back)
    /// </summary>
    public class LocksScreenBounceEase : EasingFunctionBase
    {
        public static readonly DependencyProperty DistanceProperty = 
            DependencyProperty.Register("Distance", typeof(double), typeof(LocksScreenBounceEase), new PropertyMetadata(100.0));

        public static readonly DependencyProperty BouncinessProperty = 
            DependencyProperty.Register("Bounciness", typeof(double), typeof(LocksScreenBounceEase), new PropertyMetadata(2.0));

        public double Distance
        {
            get { return (double)GetValue(DistanceProperty); }
            set { SetValue(DistanceProperty, value); }
        }

        public double Bounciness
        {
            get { return (double)GetValue(BouncinessProperty); }
            set { SetValue(BouncinessProperty, value); }
        }

        protected override double EaseInCore(double normalizedTime)
        {
            double num = normalizedTime * 5.0;
            double d = Math.Log(num + 1.0, 2.0);
            double num2 = Math.Floor(d);
            double y = num2 + 1.0;
            double num3 = (1.0 - Math.Pow(2.0, num2)) / -5.0;
            double num4 = (1.0 - Math.Pow(2.0, y)) / -5.0;
            double num5 = (num3 + num4) * 0.5;
            double num6 = normalizedTime - num5;
            double num7 = num5 - num3;
            double num8 = 0.0;
            switch ((int)num2)
            {
                case 2:
                    num8 = 1.0;
                    break;
                case 1:
                    num8 = 100.0 / (Distance == 0.0 ? 100.0 : Distance);
                    break;
                case 0:
                    num8 = 50.0 / (Distance == 0.0 ? 50.0 : Distance);
                    break;
            }
            if (num7 == 0.0) return 0.0;
            return (0.0 - num8) / (num7 * num7) * (num6 - num7) * (num6 + num7);
        }
    }
}
