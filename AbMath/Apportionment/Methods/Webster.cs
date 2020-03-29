using System;
using System.Collections.Generic;

namespace AbMath.Discrete.Apportionment
{
    public class Webster<T> : Apportionment<T>, IApportionment<T>
    {
        public double Divisor { get; private set; }
        public double MaxIterations { get; private set; }

        public Webster(Dictionary<T, double> dictionary, double allocation,int maxIterations = 1000000)
        {
            Allocation = allocation;
            _Input = dictionary;
            MaxIterations = maxIterations;

            StandardDivisor = _Input.Sum() / Allocation;
            _STDQuota = _Input.StandardQuota(StandardDivisor);
        }

        public Dictionary<T, double> Run()
        {
            Dictionary<T, double> Quota = new Dictionary<T, double>();
            foreach (KeyValuePair<T, double> kv in STDQuota)
            {
                Quota.Add(kv.Key, 0);
            }

            int iterations = 0;
            Divisor = Math.Floor(StandardDivisor);
            double Tolerence = 0.1;
            while (Math.Floor(Quota.Sum()) != Allocation)
            {
                if (Quota.Sum() > Allocation)
                {
                    Divisor += Tolerence;
                }
                else
                {
                    Divisor -= Tolerence;
                }

                if (Divisor != 0)
                {
                    Quota = _Input.StandardQuota(Divisor).Round();
                }

                iterations += 1;

                if (iterations > MaxIterations)
                {
                    throw new TimeoutException($"Sum({Quota.Sum()}) Divisor:{Divisor}");
                }
            }

            _Output = Quota;
            return _Output;
        }
    }
}
