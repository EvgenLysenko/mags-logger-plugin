using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagsLogger
{
    public class ParseUtils
    {
        public static int toInt(float value)
        {
            return (int)(value + (value < 0 ? -0.5 : 0.5));
        }

        public static float parseFloat(string text, float defaultValue)
        {
            try {
                return Convert.ToSingle(text);
            }
            catch {
                return defaultValue;
            }
        }

        public static double parseDouble(string text, double defaultValue)
        {
            try
            {
                return Convert.ToDouble(text);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static int parseInt(string text, int defaultValue)
        {
            try
            {
                return Convert.ToInt32(text);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
