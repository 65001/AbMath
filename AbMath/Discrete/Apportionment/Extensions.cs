using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Discrete.Apportionment
{
    static class Extenensions
    {
        public static Dictionary<T, double> StandardQuota<T>(this Dictionary<T, double> dictionary, double StandardDivisor)
        {
            Dictionary<T, double> Quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                Quota.Add(kv.Key, kv.Value / StandardDivisor);
            }
            return Quota;
        }

        public static Dictionary<T, double> Round<T>(this Dictionary<T, double> dictionary)
        {
            Dictionary<T, double> Quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                Quota.Add(kv.Key, Math.Round(kv.Value));
            }
            return Quota;
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
            Dictionary<T, double> Quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in dictionary)
            {
                Quota.Add(kv.Key, Math.Floor(kv.Value));
            }
            return Quota;
        }
    }
}
