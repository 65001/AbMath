using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public interface IShunt<T>
    {
        Queue<T> ShuntYard(List<T> Tokens);
        event EventHandler<string> Logger;
    }

    public interface ITokenizer<T>
    {
        List<T> Tokenize();
        event EventHandler<string> Logger;
    }
}
