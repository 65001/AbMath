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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("0 + 2 - 2" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("-1 x" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("x" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("5 x" != tokens)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdditionExponent()
        {
            RPN rpn = new RPN("x - x^2");
            rpn.Compute();

            string tokens = rpn.Polish.Print();
            Console.WriteLine(tokens);

            if ("-1 x 2 ^ * x +" != tokens && "x 2 ^ -1 * x +" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("3 ( 0 )" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("0" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("1" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("0 + 0" != tokens && "0 * 2" != tokens)
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
            string tokens = SI.Apply(rpn.Tokens).Print();
            Console.WriteLine(tokens);

            if ("x ^ 2 + x ^ 3 / x" != tokens)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Power()
        {
            RPN rpn = new RPN("2x(3x^2)");
            rpn.Compute();

            if ("6 x 3 ^ *" != rpn.Polish.Print() && "x 3 ^ 6 *" != rpn.Polish.Print() && "2 3 * x 3 ^ *" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }
    }

    [TestFixture]
    public class PostSimplification
    {
        [Test]
        public void Log_Base_Power()
        {
            RPN rpn = new RPN("log(b,b)");
            rpn.Compute();

            if ("1" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }


        [Test]
        public void Log_Power()
        {
            RPN rpn = new RPN("log(b,1)");
            rpn.Compute();

            if ("0" != rpn.Polish.Print())
            {
                Assert.Fail();
            }

            rpn.SetEquation("log(x^2,1)");
            rpn.Compute();

            if ("0" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Exponent_Log_Power()
        {
            RPN rpn = new RPN("b^log(b,x)");
            rpn.Compute();
            if ("x" != rpn.Polish.Print())
            {
                Assert.Fail();
            }

            rpn.SetEquation("(2x)^log(2x,2)");
            rpn.Compute();
            if ("2" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ZeroSimplification()
        {
            RPN rpn = new RPN("0(x)");
            rpn.Compute();
            if ("0" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Sqrt_to_abs()
        {
            RPN rpn = new RPN("sqrt(x^2)");
            rpn.Compute();
            if ("x abs" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

    }
}
