using System.Collections.Generic;

namespace AbMath.Discrete.Apportionment
{
    public class Apportionment<T>
    {
        public double StandardDivisor { get; protected set; }
        public double Allocation { get; protected set; }

        protected Dictionary<T, double> _Input;
        public IReadOnlyDictionary<T,double> Input => _Input;

        protected Dictionary<T, double> _Output;
        public IReadOnlyDictionary<T,double> Output => _Output;

        protected Dictionary<T, double> _STDQuota;
        public IReadOnlyDictionary<T,double> STDQuota => _STDQuota;
    }
}
