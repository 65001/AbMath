using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public interface IShunt<T>
    {
        Queue<T> ShuntYard(List<T> Tokens);
    }

    public interface ITokenizer<T>
    {
        List<T> Tokenize();
    }
}
