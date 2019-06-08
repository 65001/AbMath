using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class ImplicitShunting
    {
        [Test]
        public void Left()
        {
            RPN test = new RPN("4sin(2)");
            test.Compute();

            if ("2 sin 4 *" != test.Polish.Print() && "4 2 sin *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftBracket()
        {
            RPN test = new RPN("4(2)");
            test.Compute();

            if ("2 4 *" != test.Polish.Print() && "4 2 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftEOS()
        {
            RPN test = new RPN("2x");
            test.Compute();

            if ("x 2 *" != test.Polish.Print() && "2 x *" != test.Polish.Print() && "2 x 1 ^ *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftVariable()
        {
            RPN test = new RPN("x2");
            test.Compute();

            if ("x 2 *" != test.Polish.Print() && "2 x *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Mix()
        {
            RPN test = new RPN("12(3) + 8(1.01)");
            test.Compute();

            if ("12 3 * 8 1.01 * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Right()
        {
            RPN test = new RPN("sin(2)4");
            test.Compute();

            if ("2 sin 4 *" != test.Polish.Print() && "4 2 sin *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void RightBracket()
        {
            RPN test = new RPN("(2)4");
            test.Compute();

            if ("2 4 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableLeft()
        {
            RPN test = new RPN("x(y)");
            test.Compute();

            if ("y x *" != test.Polish.Print() && "x y *" != test.Polish.Print() && "1 x ^ * 1 1 y 1 ^ * *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableRight()
        {
            RPN test = new RPN("(x)(y)");
            test.Compute();

            if ("x y *" != test.Polish.Print() && "1 x 1 ^ * 1 y 1 ^ * *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultipleFunctions()
        {
            RPN test = new RPN("sin(x)cos(x)");
            test.Compute();

            if ("x sin x cos *" != test.Polish.Print() && "1 x 1 ^ * sin 1 x 1 ^ * sin *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Unary()
        {
            RPN test = new RPN("-(3^2)");
            test.Compute();

            if ("-1 3 2 ^ *" != test.Polish.Print() && "3 2 ^ -1 *" != test.Polish.Print())
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
