using System;
using System.Collections.Generic;

namespace AbMath.Calculator
{
    public enum Assoc { Left, Right };
    public interface IShunt<T>
    {
        T[] ShuntYard(List<T> tokens);
    }

    public interface ITokenizer<T>
    {
        List<T> Tokenize();
    }

    public interface IEvaluator<T>
    {
        T Compute();
        event EventHandler<string> Logger;
    }
}
