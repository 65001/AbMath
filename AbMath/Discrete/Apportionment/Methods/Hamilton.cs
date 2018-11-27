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
            Dictionary<T, double> quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in STDQuota)
            {
                quota.Add(kv.Key, Math.Floor(kv.Value));
            }

            while (quota.Sum() < Allocation)
            {
                KeyValuePair<T, double> addKey = new KeyValuePair<T, double>();
                double highest = 0;
                foreach (KeyValuePair<T, double> kv in quota)
                {
                    double Delta = STDQuota[kv.Key] - kv.Value;
                    if (Delta > highest)
                    {
                        addKey = kv;
                        highest = Delta;
                    }
                }
                quota[addKey.Key] += 1;
            }
            _Output = quota;
            return quota;
        }
    }
}