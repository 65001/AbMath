using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class SimplificationTest
    {
        [Test]
        public void VariableSimplification()
        {
            RPN rpn = new RPN("x - x + 2 - 2");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("0 + 2 - 2" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableSubtraction()
        {
            RPN rpn = new RPN("2x - 3x");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("-1 x" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableSubtraction2()
        {
            RPN rpn = new RPN("2x - x");
            rpn.Compute();

            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("x" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAddition()
        {
            RPN rpn = new RPN("2x + 3x");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("5 x" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdditionExponent()
        {
            RPN rpn = new RPN("x - x^2");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);

            string tokens = SI.Apply(rpn.Tokens).ToArray().Print();
            Console.WriteLine(tokens);

            if ("x - x ^ 2" != tokens && "- x ^ 2 + x" != tokens)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdditionComplexExponent()
        {
            RPN rpn = new RPN("2x + 3x^2");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            string tokens = SI.Apply(rpn.Tokens).ToArray().Print();
            Console.WriteLine(tokens);

            if ("2 x + 3 x ^ 2" != tokens && "3 x ^ 2 + 2 x" != tokens )
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableParanthesisReduction()
        {
            RPN rpn = new RPN("3(x^2 - x^2)");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("3 ( 0 )" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableExponentVariable()
        {
            RPN rpn = new RPN("x^@ - x^@");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("0" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableRaisedToZero()
        {
            RPN rpn = new RPN("x^0");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("1" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Swap()
        {
            RPN rpn = new RPN("x^@ + x - x^@ - x");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("0 + 0" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void IllegalSwap()
        {
            RPN rpn = new RPN("x^2 + x^3/x");
            rpn.Compute();
            RPN.PreSimplify SI = new RPN.PreSimplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("x ^ 2 + x ^ 3 / x" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }

        
        public void Power()
        {
            RPN rpn = new RPN("2x(3x^2)");
            rpn.Compute();

            if ("6 x 3 ^ *" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }
    }

    [TestFixture]
    public class PostSimplification
    {
        
        public void Swap()
        {
            RPN rpn = new RPN("(x^4) + (x^6) + (x^2) + (x^5) + x + (x^3)");
            rpn.Compute();

            if ("x 6 ^ x 5 ^ + x 4 ^ + x 3 ^ + x 2 ^ + x +" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }
    }
}
