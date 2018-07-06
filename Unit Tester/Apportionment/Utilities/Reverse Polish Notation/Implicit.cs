using System;
using AbMath.Utilities;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class ImplicitShunting
    {
        [Test]
        public void Left()
        {
            RPN Test = new RPN("4sin(2)");
            Test.Logger += Write;
            Test.Compute();

            if ("2 sin 4 *" != Test.Polish.Print() && "4 2 sin *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftBracket()
        {
            RPN Test = new RPN("4(2)");
            Test.Logger += Write;
            Test.Compute();

            if ("2 4 *" != Test.Polish.Print() && "4 2 *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftEOS()
        {
            RPN Test = new RPN("2x");
            Test.Logger += Write;
            Test.Compute();

            if ("x 2 *" != Test.Polish.Print() && "2 x *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LeftVariable()
        {
            RPN Test = new RPN("x2");
            Test.Logger += Write;
            Test.Compute();

            if ("x 2 *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Mix()
        {
            RPN Test = new RPN("12(3) + 8(1.01)");
            Test.Logger += Write;
            Test.Compute();

            if ("12 3 * 8 1.01 * +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Right()
        {
            RPN Test = new RPN("sin(2)4");
            Test.Logger += Write;
            Test.Compute();

            if ("2 sin 4 *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void RightBracket()
        {
            RPN Test = new RPN("(2)4");
            Test.Logger += Write;
            Test.Compute();

            if ("2 4 *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableLeft()
        {
            RPN Test = new RPN("x(y)");
            Test.Logger += Write;
            Test.Compute();

            if ("y x *" != Test.Polish.Print() && "x y *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableRight()
        {
            RPN Test = new RPN("(x)(y)");
            Test.Logger += Write;
            Test.Compute();

            if ("x y *" != Test.Polish.Print())
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
