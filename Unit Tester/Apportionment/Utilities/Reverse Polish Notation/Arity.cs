using System;
using System.Collections.Generic;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class Arity
    {
        [Test]
        public void Operator()
        {
            RPN test = new RPN("2+2");
            test.Compute();
            int[] arity = {0, 2, 0};

            if (validate(arity, test.Tokens))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        private bool validate(int[] arity, List<RPN.Token> Tokens)
        {
            for (int i = 0; i < Tokens.Count; i++)
            {
                if (arity[i] != Tokens[i].Arguments)
                {
                    throw new Exception($"Expected {arity[i]} but was {Tokens[i].Arguments} at {i}");
                }
            }
            return true;
        }

        public void Write(object sender, string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
