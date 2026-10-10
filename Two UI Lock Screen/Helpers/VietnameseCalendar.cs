using System;

namespace Two_UI_Lock_Screen.Helpers
{
    /// <summary>
    /// Thuật toán chuyển đổi Dương lịch sang Âm lịch Việt Nam (Múi giờ GMT+7)
    /// Dựa trên công thức thiên văn của Hồ Ngọc Đức.
    /// </summary>
    public static class VietnameseCalendar
    {
        private const double PI = Math.PI;

        public class LunarDate
        {
            public int Day { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public bool IsLeapMonth { get; set; }

            public override string ToString()
            {
                return string.Format("{0} Th {1}{2} ÂL", Day, Month, IsLeapMonth ? " (Nhuận)" : "");
            }
        }

        private static int JdFromDate(int dd, int mm, int yy)
        {
            int a = (14 - mm) / 12;
            int y = yy + 4800 - a;
            int m = mm + 12 * a - 3;
            int jd = dd + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;
            if (jd < 2299161)
            {
                jd = dd + (153 * m + 2) / 5 + 365 * y + y / 4 - 32083;
            }
            return jd;
        }

        private static double NewMoon(int k)
        {
            double t = k / 1236.85;
            double t2 = t * t;
            double t3 = t2 * t;
            double dr = PI / 180.0;
            double jd1 = 2415020.75933 + 29.53058868 * k + 0.0001178 * t2 - 0.000000155 * t3;
            jd1 += 0.00033 * Math.Sin((166.56 + 132.87 * t - 0.009173 * t2) * dr);

            double m = 359.2242 + 29.10535608 * k - 0.0000333 * t2 - 0.00000347 * t3;
            double mprime = 306.0253 + 385.81691806 * k + 0.0107306 * t2 + 0.00001236 * t3;
            double f = 21.2964 + 390.67050646 * k - 0.0016528 * t2 - 0.00000239 * t3;

            double c1 = (0.1734 - 0.000393 * t) * Math.Sin(m * dr) + 0.0021 * Math.Sin(2 * m * dr);
            c1 -= 0.4068 * Math.Sin(mprime * dr);
            c1 += 0.0161 * Math.Sin(2 * mprime * dr);
            c1 -= 0.0004 * Math.Sin(3 * mprime * dr);
            c1 += 0.0104 * Math.Sin(2 * f * dr);
            c1 -= 0.0051 * Math.Sin((m + mprime) * dr);
            c1 -= 0.0074 * Math.Sin((m - mprime) * dr);
            c1 += 0.0004 * Math.Sin((2 * f + m) * dr);
            c1 -= 0.0004 * Math.Sin((2 * f - m) * dr);
            c1 -= 0.0006 * Math.Sin((2 * f + mprime) * dr);
            c1 += 0.0010 * Math.Sin((2 * f - mprime) * dr);
            c1 += 0.0005 * Math.Sin((2 * mprime + m) * dr);

            return jd1 + c1;
        }

        private static double SunLongitude(double jdn)
        {
            double t = (jdn - 2451545.0) / 36525.0;
            double t2 = t * t;
            double dr = PI / 180.0;
            double l0 = 280.46645 + 36000.76983 * t + 0.0003032 * t2;
            double m = 357.52910 + 35999.05030 * t - 0.0001559 * t2 - 0.00000048 * t * t2;
            double c = (1.914600 - 0.004817 * t - 0.000014 * t2) * Math.Sin(m * dr);
            c += (0.019993 - 0.000101 * t) * Math.Sin(2 * m * dr);
            c += 0.000290 * Math.Sin(3 * m * dr);
            double theta = l0 + c;
            theta = theta - 360.0 * Math.Floor(theta / 360.0);
            return theta * dr;
        }

        private static int GetSunLongitude(int dayNumber, double timeZone)
        {
            return (int)(SunLongitude(dayNumber - 0.5 - timeZone / 24.0) / (PI / 6.0));
        }

        private static int GetNewMoonDay(int k, double timeZone)
        {
            return (int)Math.Floor(NewMoon(k) + 0.5 + timeZone / 24.0);
        }

        private static int GetLunarMonth11(int yy, double timeZone)
        {
            double off = JdFromDate(31, 12, yy) - 2415021.0769986;
            int k = (int)Math.Floor(off / 29.530588853);
            int nm = GetNewMoonDay(k, timeZone);
            int sunLong = GetSunLongitude(nm, timeZone);
            if (sunLong >= 9)
            {
                nm = GetNewMoonDay(k - 1, timeZone);
            }
            return nm;
        }

        public static LunarDate ConvertSolarToLunar(DateTime solarDate)
        {
            const double timeZone = 7.0; // Vietnam GMT+7
            int dd = solarDate.Day;
            int mm = solarDate.Month;
            int yy = solarDate.Year;

            int dayNumber = JdFromDate(dd, mm, yy);
            int k = (int)Math.Floor((dayNumber - 2415021.0769986) / 29.530588853);
            int monthStart = GetNewMoonDay(k + 1, timeZone);
            if (monthStart > dayNumber)
            {
                monthStart = GetNewMoonDay(k, timeZone);
            }

            int a11 = GetLunarMonth11(yy, timeZone);
            int b11 = a11;
            int lunarYear = yy;

            if (a11 >= monthStart)
            {
                lunarYear = yy - 1;
                a11 = GetLunarMonth11(yy - 1, timeZone);
            }
            else
            {
                b11 = GetLunarMonth11(yy + 1, timeZone);
            }

            int lunarDay = dayNumber - monthStart + 1;
            int diff = (int)Math.Floor((monthStart - a11) / 29.0);
            int lunarMonth = diff + 11;
            bool isLeap = false;

            if (b11 - a11 > 365)
            {
                // Leap year in lunar calendar
                int leapMonthDiff = GetLeapMonthOffset(a11, timeZone);
                if (diff >= leapMonthDiff)
                {
                    lunarMonth = diff + 10;
                    if (diff == leapMonthDiff)
                    {
                        isLeap = true;
                    }
                }
            }

            if (lunarMonth > 12)
            {
                lunarMonth -= 12;
            }
            if (lunarMonth >= 11 && diff < 4)
            {
                lunarYear -= 1;
            }

            return new LunarDate
            {
                Day = lunarDay,
                Month = lunarMonth,
                Year = lunarYear,
                IsLeapMonth = isLeap
            };
        }

        private static int GetLeapMonthOffset(int a11, double timeZone)
        {
            int k = (int)Math.Floor((a11 - 2415021.0769986) / 29.530588853 + 0.5);
            int last = 0;
            int i = 1;
            int arc = GetSunLongitude(GetNewMoonDay(k + i, timeZone), timeZone);
            do
            {
                last = arc;
                i++;
                arc = GetSunLongitude(GetNewMoonDay(k + i, timeZone), timeZone);
            } while (arc != last && i < 14);
            return i - 1;
        }

        public static string FormatLunarDateShort(DateTime solarDate)
        {
            var lunar = ConvertSolarToLunar(solarDate);
            return string.Format("({0} Th {1}{2} ÂL)", lunar.Day, lunar.Month, lunar.IsLeapMonth ? "N" : "");
        }
    }
}
