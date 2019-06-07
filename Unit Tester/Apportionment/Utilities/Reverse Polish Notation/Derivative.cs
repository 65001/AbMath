using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class Derivative
    {
        private RPN test;

        [OneTimeSetUp]
        public void StartUp()
        {
            test = new RPN("");
        }

        [Test]
        public void Constant()
        {
            test.SetEquation("derivative(1,x)");
            test.Compute();
            if (test.Polish.Print() != "0")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Variable()
        {
            test.SetEquation("derivative(x,x)");
            test.Compute();
            if (test.Polish.Print() != "1")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DualNumberMultiplication()
        {
            test.SetEquation("derivative(2pi,x)");
            test.Compute();
            if (test.Polish.Print() != "0")
            {
                Assert.Fail();
            }

            test.SetEquation("derivative(2(4),x)");
            test.Compute();
            if (test.Polish.Print() != "0")
            {
                Assert.Fail();
            }

            test.SetEquation("derivative(pi(2),x)");
            test.Compute();
            if (test.Polish.Print() != "0")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ConstantMultiplications()
        {
            test.SetEquation("derivative(2x,x)");
            test.Compute();
            if (test.Polish.Print() != "2 1 *")
            {
                Assert.Fail();
            }

            test.SetEquation("derivative(x2,x)");
            test.Compute();
            if (test.Polish.Print() != "2 1 *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ProductRule()
        {
            test.SetEquation("derivative(sin(x)cos(x),x)");
            test.Compute();
            if (test.Polish.Print() != "x sin x cos derive * x cos x sin derive * +")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void QuotientRule()
        {
            test.SetEquation("derivative(sin(x)/x^2,x)");
            test.Compute();
            if (test.Polish.Print() != "x 2 ^ x sin derive * x sin x 2 ^ derive * - x 2 ^ 2 ^ /")
            {
                Assert.Fail();
            }
        }
    }
}
