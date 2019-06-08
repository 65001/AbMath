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
            if (test.Polish.Print() != "2")
            {
                Assert.Fail();
            }

            test.SetEquation("derivative(x2,x)");
            test.Compute();
            if (test.Polish.Print() != "2")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ProductRule()
        {
            test.SetEquation("derivative(sin(x)cos(x),x)");
            test.Compute();
            if (test.Polish.Print() != "-1 x sin 2 ^ * x cos 2 ^ +")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void QuotientRule()
        {
            test.SetEquation("derivative(sin(x)/x^2,x)");
            test.Compute();
            if (test.Polish.Print() != "x 2 ^ x cos * x sin 2 x * * - x 2 ^ 2 ^ /")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void PowerRule()
        {
            test.SetEquation("derivative(x^3,x)");
            test.Compute();
            if (test.Polish.Print() != "3 x 2 ^ *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void EulerExponentSimple()
        {
            test.SetEquation("derivative(e^x,x)");
            test.Compute();
            if (test.Polish.Print() != "e x ^")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DoubleDerivative()
        {
            test.SetEquation("derivative( derivative(x^3,x),x)");
            test.Compute();
            if (test.Polish.Print() != "6 x *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Sin()
        {
            test.SetEquation("derivative(sin(x),x)");
            test.Compute();

            if (test.Polish.Print() != "x cos")
            {
                Assert.Fail();
            }

            test.SetEquation("derivative(sin(x^2),x)");
            test.Compute();
            if (test.Polish.Print() != "2 x * x 2 ^ cos *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Cos()
        {
            test.SetEquation("derivative(cos(x^2),x)");
            test.Compute();

            if (test.Polish.Print() != "2 x * -1 x 2 ^ sin * *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Tan()
        {
            test.SetEquation("derivative(tan(x^2),x)");
            test.Compute();
            if (test.Polish.Print() != "2 x * x 2 ^ sec 2 ^ *")
            {
                Assert.Fail();
            }
        }
    }
}
