using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class ImplicitShunting
    {
        private RPN test;

        [OneTimeSetUp]
        public void SetUp()
        {
            test = new RPN("");
        }


        [Test]
        public void Left()
        {
            test.SetEquation("4sin(2)");
            test.Compute();

            if ("2 sin 4 *" != test.Polish.Print() && "4 2 sin *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftBracket()
        {
            test.SetEquation("4(2)");
            test.Compute();

            if ("2 4 *" != test.Polish.Print() && "4 2 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftEOS()
        {
            test.SetEquation("2x");
            test.Compute();

            if ("x 2 *" != test.Polish.Print() && "2 x *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftVariable()
        {
            test.SetEquation("x2");
            test.Compute();

            if ("x 2 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Mix()
        {
            test.SetEquation("12(3) + 8(1.01)");
            test.Compute();

            if ("12 3 * 8 1.01 * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Right()
        {
            test.SetEquation("sin(2)4");
            test.Compute();

            if ("2 sin 4 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void RightBracket()
        {
            test.SetEquation("(2)4");
            test.Compute();

            if ("2 4 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableLeft()
        {
            test.SetEquation("x(y)");
            test.Compute();

            if ("y x *" != test.Polish.Print() && "x y *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableRight()
        {
            test.SetEquation("(x)(y)");
            test.Compute();

            if ("x y *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultipleFunctions()
        {
            test.SetEquation("sin(x)sin(x)");
            test.Compute();

            if ("x sin x sin *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Unary()
        {
            test.SetEquation("-(3^2)");
            test.Compute();

            if ("-1 3 2 ^ *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        public void Write(object sender, string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
