using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtplugIo.GalakuDevice
{
    public class MathCalScaler
    {
        public static List<double> CalScalerWithTime(double start, double end, int duration, int sleep)
        {
            var append = (start - end) * sleep / duration;
            var count = (int) Math.Round(append);
            var result = new List<double>();
            for (int i = 0; i < count; i++)
            {
                result.Add(CalScaler(start + append * i));
            }
            return result;
        }

        public static double CalScaler(double x)
        {
            x = Math.Max(0, Math.Min(1, x));
            return -0.8 * Math.Pow(x, 2) + 1.8 * x;
        }
    }
}
