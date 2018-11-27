using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Discrete.Apportionment
{
    static class Extensions
    {
        public static Dictionary<T, double> StandardQuota<T>(this Dictionary<T, double> dictionary, double standardDivisor)
        {
            Dictionary<T, double> quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                quota.Add(kv.Key, kv.Value / standardDivisor);
            }
            return quota;
        }

        public static Dictionary<T, double> Round<T>(this Dictionary<T, double> dictionary)
        {
            Dictionary<T, double> quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                quota.Add(kv.Key, Math.Round(kv.Value));
            }
            return quota;
        }

        public static double Sum<T>(this Dictionary<T, double> dictionary)
        {
            double result = 0;
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                result += kv.Value;
            }
            return result;
        }

        public static Dictionary<T, double> Floor<T>(this Dictionary<T, double> dictionary)
        {
            Dictionary<T, double> quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                quota.Add(kv.Key, Math.Floor(kv.Value));
            }
            return quota;
        }
    }
}
