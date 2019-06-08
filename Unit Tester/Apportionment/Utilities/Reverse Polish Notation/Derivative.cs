﻿using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class Derivative
    {
        [Test]
        public void Constant()
        {
            RPN test = new RPN("derivative(1,x)");
            test.Compute();
            if (test.Polish.Print() != "0")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Variable()
        {
            RPN test = new RPN("derivative(x,x)");
            test.Compute();
            if (test.Polish.Print() != "1")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DualNumberMultiplication()
        {
            RPN test = new RPN("derivative(2pi,x)");
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
            RPN test = new RPN("derivative(2x,x)");
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
            RPN test = new RPN("derivative(sin(x)cos(x),x)");
            test.Compute();
            if (test.Polish.Print() != "-1 x sin 2 ^ * x cos 2 ^ +")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void QuotientRule()
        {
            RPN test = new RPN("derivative(sin(x)/x^2,x)");
            test.Compute();
            if (test.Polish.Print() != "x 2 ^ x cos * x sin 2 x * * - x 2 ^ 2 ^ /")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void PowerRule()
        {
            RPN test = new RPN("derivative(x^3,x)");
            test.Compute();
            if (test.Polish.Print() != "3 x 2 ^ *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void PowerChainRule()
        {
            RPN test = new RPN("derivative(sec(x)^2,x)");
            test.Compute();
            if (test.Polish.Print() != "2 x sec * x tan x sec * *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Sqrt()
        {
            RPN test = new RPN("derivative(sqrt(x),x)");
            test.Compute();
            if (test.Polish.Print() != "0.5 x -0.5 ^ *")
            {
                Assert.Fail();
            }

            test.SetEquation("derivative(sqrt(x + 3),x)");
            test.Compute();

            if (test.Polish.Print() != "0.5 3 x + -0.5 ^ *")
            {
                Assert.Fail();
            }
        }


        [Test]
        public void EulerExponentSimple()
        {
            RPN test = new RPN("derivative(e^x,x)");
            test.Compute();
            if (test.Polish.Print() != "e x ^")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DoubleDerivative()
        {
            RPN test = new RPN("derivative( derivative(x^3,x),x)");
            test.Compute();
            if (test.Polish.Print() != "6 x *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Sin()
        {
            RPN test = new RPN("derivative(sin(x),x)");
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
            RPN test = new RPN("derivative(cos(x^2),x)");
            test.Compute();

            if (test.Polish.Print() != "2 x * -1 x 2 ^ sin * *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Tan()
        {
            RPN test = new RPN("derivative(tan(x^2),x)");
            test.Compute();
            if (test.Polish.Print() != "2 x * x 2 ^ sec 2 ^ *")
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Sec()
        {
            RPN test = new RPN("derivative(sec(2x),x)");
            test.Compute();
            if (test.Polish.Print() != "2 2 x * tan 2 x * sec * *")
            {
                Assert.Fail();
            }
        }
    }
}
