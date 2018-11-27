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
            test.Logger += Write;
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
            test.Logger += Write;
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
            test.Logger += Write;
            test.Compute();

            if ("x 2 *" != test.Polish.Print() && "2 x *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftVariable()
        {
            RPN test = new RPN("x2");
            test.Logger += Write;
            test.Compute();

            if ("x 2 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Mix()
        {
            RPN test = new RPN("12(3) + 8(1.01)");
            test.Logger += Write;
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
            test.Logger += Write;
            test.Compute();

            if ("2 sin 4 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void RightBracket()
        {
            RPN test = new RPN("(2)4");
            test.Logger += Write;
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
            test.Logger += Write;
            test.Compute();

            if ("y x *" != test.Polish.Print() && "x y *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableRight()
        {
            RPN test = new RPN("(x)(y)");
            test.Logger += Write;
            test.Compute();

            if ("x y *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultipleFunctions()
        {
            RPN test = new RPN("sin(x)sin(x)");
            test.Logger += Write;
            test.Compute();

            if ("x sin x sin *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Unary()
        {
            RPN test = new RPN("-(3^2)");
            test.Logger += Write;
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

    [TestFixture]
    public class ImplicitPostFix
    {
        public void Write(object sender, string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
