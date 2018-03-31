using System;
using System.Collections.Generic;

namespace AbMath.Discrete.Apportionment
{
    public class Jefferson<T> : Apportionment<T>, IApportionment<T>
    {
        public double Divisor { get; private set; }

        public Jefferson(Dictionary<T, double> dictionary, double _Allocation)
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
                Quota.Add(kv.Key, 0);
            }

            int Iterations = 0;
            Divisor = Math.Floor(StandardDivisor);
            double Tolerence = 0.1;
            while (Math.Floor(Quota.Sum()) != Allocation)
            {
                if (Quota.Sum() > Allocation)
                {
                    Divisor += Tolerence;
                }
                else { Divisor -= Tolerence; }

                if (Divisor != 0)
                {
                    Quota = _Input.StandardQuota(Divisor).Floor();
                }

                Iterations += 1;

                if (Iterations > 100000)
                {
                    throw new TimeoutException($"Sum({Quota.Sum()}) Divisor:{Divisor}");
                }
            }
            _Output = Quota;
            return Quota;
        }
    }
}
