using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public enum Assoc { Left, Right };
    public interface IShunt<T>
    {
        Queue<T> ShuntYard(List<T> tokens);
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
