using System;
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

            Assert.AreEqual(2, test.Tokens[1].Arguments);
        }
    }
}
