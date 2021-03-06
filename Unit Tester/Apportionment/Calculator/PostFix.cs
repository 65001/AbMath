﻿using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    [Parallelizable]
    public class PostFixTest
    {
        [Test]
        public void Add()
        {
            RPN test = new RPN("2 + 2 + 2").Compute();

            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(6, math.Compute());
        }

        [Test]
        public void ComplexIncrement()
        {
            RPN test = new RPN("2++ + 2 + 2").Compute();

            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(7, math.Compute());
        }

        [Test]
        public void Increment()
        {
            RPN test = new RPN("7++").Compute();

            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(8, math.Compute());
        }

        [Test]
        public void Mod()
        {
            RPN test = new RPN("5 % 2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void UnarySubtract()
        {
            RPN test = new RPN("-2 + 4").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(2, math.Compute());
        }

        [Test]
        public void UnarySubtract2()
        {
            RPN test = new RPN("5 + -2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void UnaryDecimal()
        {
            RPN test = new RPN("-.5 + .5").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void UnaryEOS()
        {
            RPN test = new RPN("-.5 + -.5").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());

            test.SetEquation("-.5 + -.5 ").Compute();
            math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());
        }

        [Test]
        public void Sin()
        {
            RPN test = new RPN("sin(pi/2)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());

            test.SetEquation("sin(pi)").Compute();

            math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void SinOfe()
        {
            RPN test = new RPN("sin(e/2)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.Sin(Math.E/2), math.Compute());
        }

        [Test]
        public void Cos()
        {
            RPN test = new RPN("cos(pi)").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());
        }

        [Test]
        public void Functions()
        {
            RPN test = new RPN("sin( max( 2 , 3 )/3 * 3.1415 )").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.Sin(3.1415), math.Compute());
        }

        [Test]
        public void CompositeFunctions()
        {
            RPN test = new RPN("max( sqrt( 16 ) , 100)").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(100, math.Compute());
        }

        [Test]
        public void Factorial()
        {
            RPN test = new RPN("5!").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(120, math.Compute());
        }

        [Test]
        public void Round()
        {
            RPN test = new RPN("round(pi,2)").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(3.14, math.Compute());
        }

        [Test]
        public void NotEqual()
        {
            RPN test = new RPN("5 != 2").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Log()
        {
            RPN test = new RPN("log(16,2)").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(0.25, math.Compute());

            test.SetEquation("log(1)").Compute();

            math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());

        }

        [Test]
        public void Reset()
        {
            RPN test = new RPN("x^2").Compute();
            
            PostFix math = new PostFix(test);
            Assert.AreEqual(4, math.SetVariable("x", 2).Compute());

            math.Reset();
            Assert.AreEqual(9, math.SetVariable("x", 3).Compute());

            math.Reset();
            Assert.AreEqual(16, math.SetVariable("x", 4).Compute());
        }

        [Test]
        public void ComplexReset()
        {
            RPN test = new RPN("x^2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(4, math.SetVariable("x", 2).Compute());

            test.SetEquation("2x");
            test.Compute();

            math = new PostFix(test);
            Assert.AreEqual(6, math.SetVariable("x", 3).Compute());
        }

        [Test]
        public void VardiacMax()
        {
            RPN test = new RPN("max(1, 2, 3)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void VardiacMin()
        {
            RPN test = new RPN("min(1, 2, 3)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void VardiacComposite()
        {
            RPN test = new RPN("sin(min (0, 1) )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void VardiacCompositeConstants()
        {
            RPN test = new RPN("sin( max(2,3,4) * pi )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void VardiacConstantAdd()
        {
            var foo = new RPN("pi(2) + e(2)").Compute();

            PostFix math = new PostFix(foo);
            Assert.AreEqual(11.7197489640976, math.Compute(), 0.00001);
        }

        [Test]
        public void VardiacStressTest()
        {
            RPN test = new RPN("total( sqrt(16), min(0,1), max(1,2,3), avg(10,5,7,9) )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(14.75, math.Compute());
        }

        [Test]
        public void Max()
        {
            RPN test = new RPN("max(0, 1)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Min()
        {
            RPN test = new RPN("min(0, 1)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void Arcsin()
        {
            RPN test = new RPN("arcsin( sin(pi/2) )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.PI / 2, math.Compute());
        }

        [Test]
        public void Arccos()
        {
            RPN test = new RPN("arccos( cos(pi/2) )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.PI / 2, math.Compute());
        }

        [Test]
        public void Arctan()
        {
            RPN test = new RPN("arctan( tan(pi/4) )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.PI / 4, math.Compute());
        }

        [Test]
        public void Gamma()
        {
            RPN test = new RPN("gamma(3.7)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(4.1706, math.Compute(), .001 );

            test.SetEquation("gamma(4)").Compute();

            math = new PostFix(test);
            Assert.AreEqual(6, math.Compute());
        }

        [Test]
        public void DivideByZero()
        {
            RPN test = new RPN("1/0").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(double.NaN, math.Compute());
        }

        [Test]
        public void ExponentianOperator()
        {
            RPN test = new RPN("1E3").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1000, math.Compute());
        }

        [Test]
        public void powerOperator()
        {
            RPN test = new RPN("(2/2)^1").Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Substract()
        {
            RPN test = new RPN("5 - 2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void SqrtSubtraction()
        {
            RPN test = new RPN("sqrt(-1) - sqrt(-1)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(double.NaN, math.Compute());
        }


        [Test]
        public void GCD()
        {
            RPN test = new RPN("gcd(123,277)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void LCM()
        {
            RPN test = new RPN("lcm(1000,625)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(5000, math.Compute());
        }

        [Test]
        public void Abs()
        {
            RPN test = new RPN("abs(-1)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void LN()
        {
            RPN test = new RPN("ln(e)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void SwapStackOverflow()
        {
            RPN test = new RPN("x^p + x - x^p - x + x^2 + x - x^2 - x").Compute();

            PostFix math = new PostFix(test);
            math.SetVariable("x", 1);
            math.SetVariable("p", 2);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void SqrtReduction()
        {
            RPN test = new RPN("sqrt(-1)^2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());
        }
        
        [Test]
        public void Distance()
        {
            RPN test = new RPN("sqrt(2^2 + 8^2)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual( Math.Sqrt( 68 ), math.Compute());
        }

        [Test]
        public void VardiacImplicitMultiplication()
        {
            RPN test = new RPN("3total(1,4,5)").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(30, math.Compute());
        }

        [Test]
        public void VardiacUltimateStressTest()
        {
            RPN test = new RPN("total( sqrt(16), min(0,1), pi, sin(2pi), max(1,2,3), avg(10,5,7,9) ) - total( sqrt(16), min(0,1), pi, sin(2pi), max(1,2,3), avg(10,5,7,9) )").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void VardiacImplicitMultiplication2()
        {
            RPN test = new RPN("total(1,4,5)3").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(30, math.Compute());
        }

        #region Logic 

        [Test]
        public void Equals()
        {
            RPN test = new RPN("30 = 30").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());

            test.SetEquation("30 = 29").Compute();

            math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void GreaterThan()
        {
            RPN test = new RPN("5 > 2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());

            test.SetEquation("5 > 10").Compute();

            math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());

            test.SetEquation("5 >= 5").Compute();

            math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());

            test.SetEquation("15 >= 5").Compute();

            math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());

            test.SetEquation("5 >= 15").Compute();

            math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void LessThan()
        {
            RPN test = new RPN("1 < 2").Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute() );

            test.SetEquation("5 < 2").Compute();

            math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());

            //TODO:
            //5 <= 5
            //5 <= 6
            //6 <= 5
        }

        #endregion
    }
}
