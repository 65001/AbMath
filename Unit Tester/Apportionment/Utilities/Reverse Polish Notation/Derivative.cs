using System;
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
            RPN test = new RPN("derivative(1,x)").Compute();
            Assert.AreEqual("0", test.Polish.Print());
        }

        [Test]
        public void Variable()
        {
            RPN test = new RPN("derivative(x,x)").Compute();
            Assert.AreEqual("1", test.Polish.Print());
        }

        [Test]
        public void DualNumberMultiplication()
        {
            RPN test = new RPN("derivative(2pi,x)").Compute();
            Assert.AreEqual("0", test.Polish.Print());

            test.SetEquation("derivative(2(4),x)").Compute();
            Assert.AreEqual("0", test.Polish.Print());

            test.SetEquation("derivative(pi(2),x)").Compute();
            Assert.AreEqual("0", test.Polish.Print());
        }

        [Test]
        public void ConstantMultiplications()
        {
            RPN test = new RPN("derivative(2x,x)").Compute();
            Assert.AreEqual("2", test.Polish.Print());

            test.SetEquation("derivative(x2,x)").Compute();
            Assert.AreEqual("2", test.Polish.Print());
        }

        [Test]
        public void ProductRule()
        {
            RPN test = new RPN("derivative(sin(x)cos(x),x)").Compute();
            Assert.AreEqual("x cos 2 ^ -1 x sin 2 ^ * +", test.Polish.Print());
        }

        [Test]
        public void QuotientRule()
        {
            RPN test = new RPN("derivative(sin(x)/x^2,x)").Compute();
            Assert.AreEqual("x 2 ^ x cos * x sin 2 x * * - x 4 ^ /", test.Polish.Print());
        }

        [Test]
        public void PowerRule()
        {
            RPN test = new RPN("derivative(x^3,x)").Compute();
            Assert.AreEqual("3 x 2 ^ *", test.Polish.Print());
        }

        [Test]
        public void PowerChainRule()
        {
            RPN test = new RPN("derivative(sec(x)^2,x)").Compute();
            Assert.AreEqual("2 x sec * x tan x sec * *", test.Polish.Print());
        }

        [Test]
        public void GeneralPowerRule()
        {
            RPN test = new RPN("derivative(x^(2x),x)").Compute();
            Assert.AreEqual("2 x * x / 2 x ln * + x 2 x * ^ *", test.Polish.Print());

            test.SetEquation("derivative(x^x,x)").Compute();
            Assert.AreEqual("x x / x ln + x x ^ *", test.Polish.Print());
        }

        [Test]
        public void BaseExponentSimple()
        {
            RPN test = new RPN("derivative(2^x,x)").Compute();
            Assert.AreEqual("0.693147180559945 2 x ^ *", test.Polish.Print());
        }

        [Test]
        public void Sqrt()
        {
            RPN test = new RPN("derivative(sqrt(x),x)").Compute();
            Assert.AreEqual("0.5 x sqrt /", test.Polish.Print());

            test.SetEquation("derivative(sqrt(x + 3),x)").Compute();
            Assert.AreEqual("0.5 x 3 + sqrt /", test.Polish.Print());
        }

        [Test]
        public void Abs()
        {
            RPN test = new RPN("derivative( abs(x^2), x)").Compute();
            Assert.AreEqual("0.5 2 2 ^ x 2 ^ * x * * x 2 ^ /", test.Polish.Print());
        }

        [Test]
        public void Ln()
        {
            RPN test = new RPN("derivative(ln(x^2),x)").Compute();
            Assert.AreEqual("2 x * x 2 ^ /", test.Polish.Print());
        }

        [Test]
        public void Log()
        {
            RPN test = new RPN("derivative( log(2,x) , x)").Compute();
            Assert.AreEqual("1 0.693147180559945 x * /", test.Polish.Print());
        }


        [Test]
        public void EulerExponentSimple()
        {
            RPN test = new RPN("derivative(e^x,x)").Compute();
            Assert.AreEqual("e x ^", test.Polish.Print());
        }

        [Test]
        public void DoubleDerivative()
        {
            RPN test = new RPN("derivative( derivative(x^3,x),x)").Compute();
            Assert.AreEqual("6 x *", test.Polish.Print());
        }

        [Test]
        public void Sin()
        {
            RPN test = new RPN("derivative(sin(x),x)").Compute();
            Assert.AreEqual("x cos", test.Polish.Print());

            test.SetEquation("derivative(sin(x^2),x)").Compute();
            Assert.AreEqual("2 x * x 2 ^ cos *", test.Polish.Print());
        }

        [Test]
        public void Cos()
        {
            RPN test = new RPN("derivative(cos(x^2),x)").Compute();
            Assert.AreEqual("2 -1 * x * x 2 ^ sin *", test.Polish.Print() );
        }

        [Test]
        public void Tan()
        {
            RPN test = new RPN("derivative(tan(x^2),x)").Compute();
            Assert.AreEqual("2 x * x 2 ^ sec 2 ^ *", test.Polish.Print());
        }

        [Test]
        public void Sec()
        {
            RPN test = new RPN("derivative(sec(2x),x)").Compute();
            Assert.AreEqual("2 2 x * tan 2 x * sec * *", test.Polish.Print());
        }

        [Test]
        public void Csc()
        {
            RPN test = new RPN("derivative(csc(x^2),x)").Compute();
            Assert.AreEqual("-1 2 x * x 2 ^ cot x 2 ^ csc * * *", test.Polish.Print());
        }

        [Test]
        public void Cot()
        {
            RPN test = new RPN("derivative(cot(2x),x)").Compute();
            Assert.AreEqual("-2 2 x * csc 2 ^ *", test.Polish.Print());
        }

        [Test]
        public void Arcsin()
        {
            RPN test = new RPN("derivative(arcsin(x^2),x)").Compute();
            Assert.AreEqual("2 x * 1 x 4 ^ - sqrt /", test.Polish.Print());
        }

        [Test]
        public void Arccos()
        {
            RPN test = new RPN("derivative(arccos(x^2),x)").Compute();
            Assert.AreEqual("-2 x * 1 x 4 ^ - sqrt /", test.Polish.Print());
        }

        [Test]
        public void Arctan()
        {
            RPN test = new RPN("derivative(arctan(x^2),x)").Compute();
            Assert.AreEqual("2 x * x 4 ^ 1 + /", test.Polish.Print());
        }

        [Test]
        public void ArcCot()
        {
            RPN test = new RPN("derivative( arccot(x), x)").Compute();
            Assert.AreEqual("-1 x 2 ^ 1 + /", test.Polish.Print());

            test.SetEquation("derivative( arccot(x^2), x)").Compute();
            Assert.AreEqual("-2 x * x 4 ^ 1 + /", test.Polish.Print());
        }

        [Test]
        public void ArcSec()
        {
            RPN test = new RPN("derivative( arcsec(x), x)").Compute();
            Assert.AreEqual("1 x x 2 ^ 1 - sqrt * /", test.Polish.Print());

            test.SetEquation("derivative( arcsec(x^2), x)").Compute();
            Assert.AreEqual("2 x * x 2 ^ x 4 ^ 1 - sqrt * /", test.Polish.Print());
        }

        [Test]
        public void ArcCsc()
        {
            RPN test = new RPN("derivative( arccsc(x), x)").Compute();
            Assert.AreEqual("-1 x x 2 ^ 1 - sqrt * /", test.Polish.Print());

            test.SetEquation("derivative( arccsc(x^2), x)").Compute();
            Assert.AreEqual("-2 x * x 2 ^ x 4 ^ 1 - sqrt * /", test.Polish.Print());
        }

        [Test]
        public void ComplexEquation()
        {
            RPN test = new RPN("derivative( x(x - 1)e^(-1/(2x)), x)").Compute();
            Assert.AreEqual("e -1 2 x * / ^ x 1 - x + * x x 1 - * 2 e -1 2 x * / ^ * * 2 x * 2 ^ / +", test.Polish.Print());
        }
    }
}