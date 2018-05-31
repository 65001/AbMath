using System;
using AbMath.Discrete.Apportionment;
using System.Collections.Generic;
using NUnit.Framework;

namespace TestHarness
{
    [TestFixture]
    public class Apportionment
    {
        [Test]
        public void Hamilton()
        {
            Dictionary<string, double> Expected = new Dictionary<string, double>()
            {
                { "VA", 1 },
                { "CA", 2 },
                { "FL", 1 },
                { "WV", 1}
            };
            Dictionary<string, double> Pop = new Dictionary<string, double>() {
                { "VA", 5 },
                { "CA", 7 },
                { "FL", 4 },
                { "WV",2 } };

            double Allocation = 5;
            Run(new Hamilton<string>(Pop, Allocation), Expected);
        }

        [Test]
        public void Jefferson()
        {
            Dictionary<string, double> Expected = new Dictionary<string, double>()
            {
                { "Eng", 3 },
                { "History", 7 },
                { "Psych", 5 }
            };
            Dictionary<string, double> Pop = new Dictionary<string, double>() {
                { "Eng", 231 },
                { "History", 502 },
                { "Psych", 355 } };

            double Allocation = 15;
            Run(new Jefferson<string>(Pop, Allocation), Expected);
        }

        [Test]
        public void Webster()
        {
            Dictionary<string, double> Expected = new Dictionary<string, double>()
            {
                { "A", 3 },
                { "B", 2 },
                { "C", 2 },
            };
            Dictionary<string, double> Pop = new Dictionary<string, double>() {
               { "A", 53 },
               { "B", 24 },
               { "C", 23 }
            };

            double Allocation = 7;
            Run(new Webster<string>(Pop, Allocation), Expected);
        }

        [Test]
        public void HunningtonHill()
        {
            Dictionary<string, double> Expected = new Dictionary<string, double>()
            {
                {"A",2 },
                {"B",6 },
                {"G",3 },
                {"D",2 },
                {"E",7 },
                {"Z",5 }
            };
            Dictionary<string, double> Pop = new Dictionary<string, double>()
            {
                {"A",24000 },
                {"B",56000 },
                {"G",28000 },
                {"D",17000 },
                {"E",65000 },
                {"Z",47000 }
            };

            double Allocation = 25;
            Run(new Hunnington<string>(Pop, Allocation), Expected);
        }

        static void Run<T>(IApportionment<T> Test, Dictionary<T, double> Expected)
        {
            var Results = Test.Run();
            Verify(Expected, Results, Test);
        }

        static void Verify<T>(Dictionary<T, double> Expected, Dictionary<T, double> Results, IApportionment<T> Test)
        {
            foreach (KeyValuePair<T, double> kv in Results)
            {
                if (Expected.ContainsKey(kv.Key) == false || Expected[kv.Key] != kv.Value)
                {
                    Assert.Fail();
                }
            }

            //These numbers cannot be zero.
            //If they are zero then that means the code hasn't 
            //changed it from its default value, meaning 
            //that we are breaking out interface contract!
            if (Test.Allocation == 0) { Assert.Fail(); }
            if (Test.StandardDivisor == 0) { Assert.Fail(); }

            //Null Tests
            if (Test.Input == null) { Assert.Fail(); }
            if (Test.Output == null) { Assert.Fail(); }
            if (Test.STDQuota == null) { Assert.Fail(); }
        }
    }
}
