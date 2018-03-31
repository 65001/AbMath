using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Discrete.Apportionment
{
    public class Hunnington<T> : Apportionment<T>, IApportionment<T>
    {
        private readonly double Tolerance = .1;
        public double Divisor { get; private set; }
        public double MaxIterations { get; private set; }

        public Hunnington(Dictionary<T, double> dictionary, double _Allocation, int _MaxIterations = 1000000)
        {
            _Input = dictionary;
            Allocation = _Allocation;
            MaxIterations = _MaxIterations;

            StandardDivisor = _Input.Sum() / Allocation;
            _Input.StandardQuota(StandardDivisor);
            _STDQuota = _Input.StandardQuota(StandardDivisor);
        }

        public Dictionary<T, double> Run()
        {
            Divisor = StandardDivisor;

            int Itterations = 0;
            Dictionary<T, double> Quota = (_Input.StandardQuota(Divisor)).Floor();
            while (Quota.Sum() != Allocation)
            {

                if (Quota.Sum() > Allocation)
                {
                    Divisor += Tolerance;
                }
                else
                {
                    Divisor -= Tolerance;
                }

                if (Divisor != 0)
                {
                    Quota = _Input.StandardQuota(Divisor).Floor();
                }
                Dictionary<T, double> GeometricMean = new Dictionary<T, double>();
                foreach (KeyValuePair<T, double> kv in Quota)
                {
                    GeometricMean.Add(kv.Key, Math.Sqrt(kv.Value * (kv.Value + 1)));
                }

                foreach (KeyValuePair<T, double> kv in Quota)
                {
                    if (kv.Value > GeometricMean[kv.Key])
                    {
                        Quota[kv.Key] = kv.Value + 1;
                    }
                }

                Itterations += 1;
                if (Itterations > MaxIterations)
                {
                    throw new TimeoutException();
                }
            }

            _Output = Quota;
            return Quota;
        }
    }
}
