using System;
using System.Collections.Generic;

namespace AbMath.Calculator
{
    public enum Assoc { Left, Right };
    public interface IShunt<T>
    {
        T[] ShuntYard(List<T> tokens);
        event EventHandler<string> Logger;
    }

    public interface ITokenizer<T>
    {
        List<T> Tokenize();
        event EventHandler<string> Logger;
    }

    public interface IEvaluator<T>
    {
        T Compute();
        event EventHandler<string> Logger;
    }
}
