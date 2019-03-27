using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("x - 1 x ^ 2" != SI.Apply(rpn.Tokens).ToArray().Print())
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
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("2 x + 3 x ^ 2" != SI.Apply(rpn.Tokens).ToArray().Print())
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
    }
}
