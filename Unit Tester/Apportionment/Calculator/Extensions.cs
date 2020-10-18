using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;
using AbMath.Calculator.Simplifications;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class Extensions
    {
        [Test]
        public void Bernoulli()
        {
            Assert.AreEqual("1 1 /", bernoulli(0));
            Assert.AreEqual("-1 2 /", bernoulli(1));
            Assert.AreEqual("1 6 /", bernoulli(2));
            Assert.AreEqual("-1 30 /", bernoulli(4));
            Assert.AreEqual("1 42 /", bernoulli(6));
            Assert.AreEqual("-1 30 /", bernoulli(8));
            Assert.AreEqual("5 66 /", bernoulli(10));
            Assert.AreEqual("-691 2730 /", bernoulli(12));
            Assert.AreEqual("7 6 /", bernoulli(14));
            Assert.AreEqual("-3617 510 /", bernoulli(16));
            Assert.AreEqual("43867 798 /", bernoulli(18));
            Assert.AreEqual("-174611 330 /", bernoulli(20));
            Assert.AreEqual("854513 138 /", bernoulli(22));
            Assert.AreEqual("-236364091 2730 /", bernoulli(24));
            Assert.AreEqual("8553103 6 /", bernoulli(26));
            Assert.AreEqual("-23749461029 870 /", bernoulli(28));
            Assert.AreEqual("8615841276005 14322 /", bernoulli(30));
            Assert.AreEqual("-7709321041217 510 /", bernoulli(32));
            Assert.AreEqual("2577687858367 6 /", bernoulli(34));

            Assert.AreEqual("-2.6315271553053475E+19 1919190 /", bernoulli(36));
            Assert.AreEqual("2929993913841559 6 /", bernoulli(38));
            Assert.AreEqual("-2.610827184964491E+20 13530 /", bernoulli(40));
            Assert.AreEqual("1.5200976439180706E+21 1806 /", bernoulli(42));
            Assert.AreEqual("-2.7833269579301022E+22 690 /", bernoulli(44));
            Assert.AreEqual("5.964511115939121E+23 282 /", bernoulli(46));
            Assert.AreEqual("-5.609403368997818E+27 46410 /", bernoulli(48));
            Assert.AreEqual("4.950572052410796E+26 66 /", bernoulli(50));
            Assert.AreEqual("-8.011657181354899E+29 1590 /", bernoulli(52));
            Assert.AreEqual("2.914996363488486E+31 798 /", bernoulli(54));
            Assert.AreEqual("-2.4793929293132266E+33 870 /", bernoulli(56));
            Assert.AreEqual("8.448361334888004E+34 354 /", bernoulli(58));
            Assert.AreEqual("-1.2152331404837555E+42 56786730 /", bernoulli(60));
            Assert.AreEqual("1.2300585434086857E+37 6 /", bernoulli(62));
        }

        [Test]
        public void BernoulliOdd()
        {
            Assert.AreEqual("0 1 /",  bernoulli(3));
            Assert.AreEqual("0 1 /", bernoulli(5));
            Assert.AreEqual("0 1 /", bernoulli(7));
            Assert.AreEqual("0 1 /", bernoulli(9));
            Assert.AreEqual("0 1 /", bernoulli(11));
            Assert.AreEqual("0 1 /", bernoulli(13));
            Assert.AreEqual("0 1 /", bernoulli(15));
            Assert.AreEqual("0 1 /", bernoulli(17));
            Assert.AreEqual("0 1 /", bernoulli(19));
        }

        private string bernoulli(int n)
        {
            return Sum.getBernoulliNumber(n).ToPostFix().Print();
        }
    }
}
