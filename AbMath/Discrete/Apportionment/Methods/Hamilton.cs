using System;
using System.Collections.Generic;

namespace AbMath.Discrete.Apportionment
{
    public class Hamilton<T> : Apportionment<T>, IApportionment<T>
    {
        public Hamilton(Dictionary<T, double> dictionary, double _Allocation)
        {
            Allocation = _Allocation;
            _Input = dictionary;

            StandardDivisor = _Input.Sum() / Allocation;
            _STDQuota = _Input.StandardQuota(StandardDivisor);
        }

        public Dictionary<T, double> Run()
        {
            Dictionary<T, double> Quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in STDQuota)
            {
                Quota.Add(kv.Key, Math.Floor(kv.Value));
            }

            while (Quota.Sum() < Allocation)
            {
                KeyValuePair<T, double> AddKey = new KeyValuePair<T, double>();
                double Highest = 0;
                foreach (KeyValuePair<T, double> kv in Quota)
                {
                    double Delta = STDQuota[kv.Key] - kv.Value;
                    if (Delta > Highest)
                    {
                        AddKey = kv;
                        Highest = Delta;
                    }
                }
                Quota[AddKey.Key] += 1;
            }
            _Output = Quota;
            return Quota;
        }
    }
}