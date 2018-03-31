using System;
using System.Collections.Generic;

namespace AbMath.Discrete.Apportionment
{
    public interface IApportionment<T>
    {
        double StandardDivisor { get; }
        double Allocation { get; }
        IReadOnlyDictionary<T, double> Input { get; }
        IReadOnlyDictionary<T, double> Output { get; }
        /// <summary>
        /// Standard Quota
        /// </summary>
        IReadOnlyDictionary<T, double> STDQuota { get; }
        Dictionary<T, double> Run();
    }
}
