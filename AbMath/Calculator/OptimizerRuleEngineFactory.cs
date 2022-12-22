using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AbMath.Calculator;
using AbMath.Calculator.Simplifications;

namespace AbMath.Calculator
{
    /// <summary>
    /// This makes the Optimizer Rule Engine class a Singleton. 
    /// </summary>
    public class OptimizerRuleEngineFactory
    {
        private static OptimizerRuleEngine ruleManager = null;

        private OptimizerRuleEngineFactory()
        {

        }

        public static OptimizerRuleEngine generate(Utilities.Logger logger, Action<string, string, string> actionTableLogger, bool debugMode = false)
        {
            if (ruleManager == null)
            {
                ruleManager = new OptimizerRuleEngine(logger, actionTableLogger, debugMode);
                GenerateRuleSetSimplifications();
            }
            return ruleManager;

        }

        /// <summary>
        /// Generates the rule set for all simplifications
        /// </summary>
        private static void GenerateRuleSetSimplifications()
        {
            GenerateSqrtSimplifications();
            GenerateLogSimplifications();
            GenerateSubtractionSimplifications();
            GenerateDivisionSimplifications();
            GenerateMultiplicationSimplifications();
            GenerateAdditionSimplifications();
            GenerateExponentSimplifications();
            GenerateTrigSimplifications();
            GenerateSumSimplifications();
            GenerateIntegralSimplifications();
            GenerateMiscSimplifications();
            GenerateListSimplifications();
        }

        private static void GenerateSqrtSimplifications()
        {
            Rule negative = new Rule(Sqrt.SqrtNegativeNumbersRunnable, Sqrt.SqrtNegativeNumbers, "sqrt(-c) -> i Imaginary Number to Root");
            Rule sqrt = new Rule(Sqrt.SqrtToFuncRunnable, Sqrt.SqrtToFunc, "sqrt(g(x))^2 - > g(x)");
            Rule abs = new Rule(Sqrt.SqrtToAbsRunnable, Sqrt.SqrtToAbs, "sqrt(g(x)^2) -> abs(g(x))");
            Rule sqrtPower = new Rule(Sqrt.SqrtPowerFourRunnable, Sqrt.SqrtPowerFour, "sqrt(g(x)^n) where n is a multiple of 4. -> g(x)^n/2");

            ruleManager.Add(AST.SimplificationMode.Sqrt, negative);
            ruleManager.Add(AST.SimplificationMode.Sqrt, sqrt);
            ruleManager.Add(AST.SimplificationMode.Sqrt, abs);
            ruleManager.Add(AST.SimplificationMode.Sqrt, sqrtPower);
        }

        private static void GenerateLogSimplifications()
        {
            Rule logToLn = new Rule(Log.LogToLnRunnable, Log.LogToLn, "log(e,f(x)) - > ln(f(x))");

            //This rule only can be a preprocessor rule and therefore should not be added to the rule manager!
            Rule LnToLog = new Rule(Log.LnToLogRunnable, Log.LnToLog, "ln(f(x)) -> log(e,f(x))");

            //These are candidates for preprocessing and post processing:
            Rule logOne = new Rule(Log.LogOneRunnable, Log.LogOne, "log(b,1) -> 0");
            Rule logIdentical = new Rule(Log.LogIdentitcalRunnable, Log.LogIdentitcal, "log(b,b) -> 1");
            Rule logPowerExpansion = new Rule(Log.LogExponentExpansionRunnable, Log.LogExponentExpansion, "log(b,R^c) -> c * log(b,R)");

            logOne.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);
            logIdentical.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);
            logPowerExpansion.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);

            Rule logPower = new Rule(Log.LogPowerRunnable, Log.LogPower, "b^log(b,x) -> x");

            Rule logSummation = new Rule(Log.LogSummationRunnable, Log.LogSummation, "log(b,R) + log(b,S) -> log(b,R*S)");
            Rule logSubtraction = new Rule(Log.LogSubtractionRunnable, Log.LogSubtraction, "log(b,R) - log(b,S) -> log(b,R/S)");

            Rule lnPower = new Rule(Log.LnPowerRunnable, Log.LnPower, "e^ln(f(x)) -> f(x)");
            Rule lnSummation = new Rule(Log.LnSummationRunnable, Log.LnSummation, "ln(R) + ln(S) -> log(e,R) + log(e,S) -> ln(R*S)");
            Rule lnSubtraction = new Rule(Log.LnSubtractionRunnable, Log.LnSubtraction, "ln(R) - ln(S) -> log(e,R) - log(e,S) -> ln(R/S)");
            Rule lnPowerExpansion = new Rule(Log.LnPowerRuleRunnable, Log.LnPowerRule, "ln(R^c) -> c*ln(R)");

            ruleManager.Add(AST.SimplificationMode.Log, logOne);
            ruleManager.Add(AST.SimplificationMode.Log, logIdentical);
            ruleManager.Add(AST.SimplificationMode.Log, logPower);
            ruleManager.Add(AST.SimplificationMode.Log, logPowerExpansion);

            ruleManager.Add(AST.SimplificationMode.Log, logSummation);
            ruleManager.Add(AST.SimplificationMode.Log, logSubtraction);
            ruleManager.Add(AST.SimplificationMode.Log, lnSummation);
            ruleManager.Add(AST.SimplificationMode.Log, lnSubtraction);

            ruleManager.Add(AST.SimplificationMode.Log, lnPowerExpansion);
            ruleManager.Add(AST.SimplificationMode.Log, lnPower);

            ruleManager.Add(AST.SimplificationMode.Log, logToLn);
        }

        private static void GenerateSubtractionSimplifications()
        {
            Rule setRule = new Rule(Subtraction.setRule, null, "Subtraction Set Rule");
            ruleManager.AddSetRule(AST.SimplificationMode.Subtraction, setRule);

            Rule sameFunction = new Rule(Subtraction.SameFunctionRunnable, Subtraction.SameFunction, "f(x) - f(x) -> 0");
            Rule coefficientOneReduction = new Rule(Subtraction.CoefficientOneReductionRunnable, Subtraction.CoefficientOneReduction, "cf(x) - f(x) -> (c - 1)f(x)");
            Rule subtractionByZero = new Rule(Subtraction.SubtractionByZeroRunnable, Subtraction.SubtractionByZero, "f(x) - 0 -> f(x)");
            Rule ZeroSubtractedFunction = new Rule(Subtraction.ZeroSubtractedByFunctionRunnable, Subtraction.ZeroSubtractedByFunction, "0 - f(x) -> -f(x)");
            Rule subtractionDivisionCommmonDenominator = new Rule(Subtraction.SubtractionDivisionCommonDenominatorRunnable,
                Subtraction.SubtractionDivisionCommonDenominator, "f(x)/g(x) - h(x)/g(x) -> [f(x) - h(x)]/g(x)");
            Rule coefficientReduction = new Rule(Subtraction.CoefficientReductionRunnable, Subtraction.CoefficientReduction, "Cf(x) - cf(x) -> (C - c)f(x)");

            Rule constantToAddition = new Rule(Subtraction.ConstantToAdditionRunnable, Subtraction.ConstantToAddition, "f(x) - (-c) -> f(x) + c");
            Rule functionToAddition = new Rule(Subtraction.FunctionToAdditionRunnable, Subtraction.FunctionToAddition, "f(x) - (-c * g(x)) -> f(x) + c *g(x)");
            Rule distributive = new Rule(Subtraction.DistributiveSimpleRunnable, Subtraction.DistributiveSimple, "f(x) - (g(x) - h(x)) -> f(x) - g(x) + h(x) -> (f(x) + h(x)) - g(x)");

            ruleManager.Add(AST.SimplificationMode.Subtraction, sameFunction);
            ruleManager.Add(AST.SimplificationMode.Subtraction, coefficientOneReduction);
            ruleManager.Add(AST.SimplificationMode.Subtraction, subtractionByZero);
            ruleManager.Add(AST.SimplificationMode.Subtraction, ZeroSubtractedFunction);
            ruleManager.Add(AST.SimplificationMode.Subtraction, subtractionDivisionCommmonDenominator);
            ruleManager.Add(AST.SimplificationMode.Subtraction, coefficientReduction);
            ruleManager.Add(AST.SimplificationMode.Subtraction, constantToAddition);
            ruleManager.Add(AST.SimplificationMode.Subtraction, functionToAddition);
            ruleManager.Add(AST.SimplificationMode.Subtraction, distributive);

            //TODO: f(x)/g(x) /pm i(x)/j(x) -> [f(x)j(x)]/g(x)j(x) /pm i(x)g(x)/g(x)j(x) -> [f(x)j(x) /pm g(x)i(x)]/[g(x)j(x)]
        }

        private static void GenerateDivisionSimplifications()
        {
            Rule setRule = new Rule(Division.setRule, null, "Division Set Rule");

            Rule divisionByZero = new Rule(Division.DivisionByZeroRunnable, Division.DivisionByZero, "f(x)/0 -> NaN");
            Rule divisionByOne = new Rule(Division.DivisionByOneRunnable, Division.DivisionByOne, "f(x)/1 -> f(x)");
            Rule gcd = new Rule(Division.GCDRunnable, Division.GCD, "(cC)/(cX) -> C/X");
            Rule divisionFlip = new Rule(Division.DivisionFlipRunnable, Division.DivisionFlip, "(f(x)/g(x))/(h(x)/j(x)) - > (f(x)j(x))/(g(x)h(x))");

            Rule constantCancelation = new Rule(Division.DivisionCancelingRunnable, Division.DivisionCanceling, "(c * f(x))/c -> f(x) where c is not 0");

            Rule powerReduction = new Rule(Division.PowerReductionRunnable, Division.PowerReduction, "Power Reduction");
            Rule divisionFlipTwo = new Rule(Division.DivisionFlipTwoRunnable, Division.DivisionFlipTwo, "[f(x)/g(x)]/ h(x) -> [f(x)/g(x)]/[h(x)/1] - > f(x)/[g(x) * h(x)]");

            Rule factorialSimplificationToOne = new Rule(Division.FactorialCancellationRunnable, Division.FactorialCancellation, "f(x)!/f(x)! -> 1");
            Rule factorialComplexSimplification = new Rule(Division.FactorialRemovedRunnable, Division.FactorialRemoved, "[f(x)(x!)]/x! -> f(x)");
            ruleManager.AddSetRule(AST.SimplificationMode.Division, setRule);

            ruleManager.Add(AST.SimplificationMode.Division, divisionByZero);
            ruleManager.Add(AST.SimplificationMode.Division, divisionByOne);
            ruleManager.Add(AST.SimplificationMode.Division, gcd);
            ruleManager.Add(AST.SimplificationMode.Division, divisionFlip);
            ruleManager.Add(AST.SimplificationMode.Division, constantCancelation);
            ruleManager.Add(AST.SimplificationMode.Division, powerReduction);
            ruleManager.Add(AST.SimplificationMode.Division, divisionFlipTwo);
            ruleManager.Add(AST.SimplificationMode.Division, factorialSimplificationToOne);
            ruleManager.Add(AST.SimplificationMode.Division, factorialComplexSimplification);

            //TODO: 0/c when c is a constant or an expression that on solving will be a constant. 

            //TODO: (c_0 * f(x))/c_1 where c_0, c_1 share a gcd that is not 1 and c_0 and c_1 are integers 
            //TODO: (c_0 * f(x))/(c_1 * g(x)) where ...
        }

        private static void GenerateMultiplicationSimplifications()
        {
            //TODO: If one of the leafs is a division and the other a number or variable
            //TODO: Replace the requirement that we cannot do a simplification when a division is present to 
            //that we cannot do a simplification when a division has a variable in the denominator!

            Rule setRule = new Rule(Multiplication.setRule, null, "Multiplication Set Rule");
            Rule toExponent = new Rule(Multiplication.multiplicationToExponentRunnable, Multiplication.multiplicationToExponent, "f(x) * f(x) -> f(x)^2");
            Rule simplificationByOne = new Rule(Multiplication.multiplicationByOneRunnable, Multiplication.multiplicationByOne, "1 * f(x) -> f(x)");
            Rule simplificationByOneComplex = new Rule(Multiplication.multiplicationByOneComplexRunnable, Multiplication.multiplicationByOneComplex, "1 * f(x) || f(x) * 1 -> f(x)");

            Rule multiplyByZero = new Rule(Multiplication.multiplicationByZeroRunnable, Multiplication.multiplicationByZero, "0 * f(x) -> 0");

            Rule increaseExponent = new Rule(Multiplication.increaseExponentRunnable, Multiplication.increaseExponent, "R1: f(x)^n * f(x) -> f(x)^(n + 1)");
            Rule increaseExponentTwo = new Rule(Multiplication.increaseExponentTwoRunnable, Multiplication.increaseExponentTwo, "R2: f(x)^n * f(x) -> f(x)^(n + 1)");
            Rule increaseExponentThree = new Rule(Multiplication.increaseExponentThreeRunnable, Multiplication.increaseExponentThree, "R3: f(x)^n * f(x) -> f(x)^(n + 1)");

            Rule expressionDivision = new Rule(Multiplication.expressionTimesDivisionRunnable, Multiplication.expressionTimesDivision, "f(x) * [g(x)/h(x)] -> [f(x) * g(x)]/h(x)");
            Rule DivisionDivision = new Rule(Multiplication.divisionTimesDivisionRunnable, Multiplication.divisionTimesDivision, "[f(x)/g(x)] * [h(x)/j(x)] -> [f(x) * h(x)]/[g(x) * j(x)]");
            Rule negativeTimesNegative = new Rule(Multiplication.negativeTimesnegativeRunnable, Multiplication.negativeTimesnegative, "(-c)(-C) -> (c)(C)");
            Rule complexNegativeTimesNegative = new Rule(Multiplication.complexNegativeNegativeRunnable, Multiplication.complexNegativeNegative, "-c * -k -> c * k");
            Rule negativeByConstant = new Rule(Multiplication.negativeTimesConstantRunnable, Multiplication.negativeTimesConstant, "-1 * c -> -c");
            Rule constantByNegative = new Rule(Multiplication.constantTimesNegativeRunnable, Multiplication.constantTimesNegative, "c * -1 -> -c");
            Rule negativeOneDistributed = new Rule(Multiplication.negativeOneDistributedRunnable, Multiplication.negativeOneDistributed, "-1[f(x) - g(x)] -> -f(x) + g(x) -> g(x) - f(x)");
            Rule dualNode = new Rule(Multiplication.dualNodeMultiplicationRunnable,
                Multiplication.dualNodeMultiplication, "Dual Node");


            ruleManager.AddSetRule(AST.SimplificationMode.Multiplication, setRule);
            ruleManager.Add(AST.SimplificationMode.Multiplication, toExponent);
            ruleManager.Add(AST.SimplificationMode.Multiplication, simplificationByOne);
            ruleManager.Add(AST.SimplificationMode.Multiplication, simplificationByOneComplex);
            ruleManager.Add(AST.SimplificationMode.Multiplication, multiplyByZero);

            ruleManager.Add(AST.SimplificationMode.Multiplication, increaseExponent);
            ruleManager.Add(AST.SimplificationMode.Multiplication, increaseExponentTwo);
            ruleManager.Add(AST.SimplificationMode.Multiplication, increaseExponentThree);

            ruleManager.Add(AST.SimplificationMode.Multiplication, dualNode);
            ruleManager.Add(AST.SimplificationMode.Multiplication, expressionDivision);
            ruleManager.Add(AST.SimplificationMode.Multiplication, DivisionDivision);
            ruleManager.Add(AST.SimplificationMode.Multiplication, negativeTimesNegative);
            ruleManager.Add(AST.SimplificationMode.Multiplication, complexNegativeTimesNegative);
            ruleManager.Add(AST.SimplificationMode.Multiplication, negativeByConstant);
            ruleManager.Add(AST.SimplificationMode.Multiplication, constantByNegative);
            ruleManager.Add(AST.SimplificationMode.Multiplication, negativeOneDistributed);
        }

        private static void GenerateAdditionSimplifications()
        {
            Rule setRule = new Rule(Addition.setRule, null, "Addition Set Rule");
            ruleManager.AddSetRule(AST.SimplificationMode.Addition, setRule);

            Rule additionToMultiplication = new Rule(Addition.AdditionToMultiplicationRunnable, Addition.AdditionToMultiplication, "f(x) + f(x) -> 2 * f(x)");
            Rule zeroAddition = new Rule(Addition.ZeroAdditionRunnable, Addition.ZeroAddition, "f(x) + 0 -> f(x)");
            Rule simpleCoefficient = new Rule(Addition.SimpleCoefficientRunnable, Addition.SimpleCoefficient, "cf(x) + f(x) -> (c + 1)f(x) + 0");
            Rule complexCoefficient = new Rule(Addition.ComplexCoefficientRunnable, Addition.ComplexCoefficient, "cf(x) + Cf(x) -> (c + C)f(x) + 0");
            Rule additionSwap = new Rule(Addition.AdditionSwapRunnable, Addition.AdditionSwap, "-f(x) + g(x) -> g(x) - f(x)");
            Rule toSubtractionRuleOne = new Rule(Addition.AdditionToSubtractionRuleOneRunnable, Addition.AdditionToSubtractionRuleOne, "f(x) + -1 * g(x) -> f(x) - g(x)");
            Rule toSubtractionRuleTwo = new Rule(Addition.AdditionToSubtractionRuleTwoRunnable, Addition.AdditionToSubtractionRuleTwo, "Addition can be converted to subtraction R2");
            Rule complex = new Rule(Addition.ComplexNodeAdditionRunnable, Addition.ComplexNodeAddition, "f(x) + f(x) - g(x) -> 2 * f(x) - g(x)");
            Rule division = new Rule(Addition.DivisionAdditionRunnable, Addition.DivisionAddition, "f(x)/g(x) + h(x)/g(x) -> [f(x) + h(x)]/g(x)");

            ruleManager.Add(AST.SimplificationMode.Addition, additionToMultiplication);
            ruleManager.Add(AST.SimplificationMode.Addition, zeroAddition);
            ruleManager.Add(AST.SimplificationMode.Addition, simpleCoefficient);
            ruleManager.Add(AST.SimplificationMode.Addition, complexCoefficient);
            ruleManager.Add(AST.SimplificationMode.Addition, additionSwap);
            ruleManager.Add(AST.SimplificationMode.Addition, toSubtractionRuleOne);
            ruleManager.Add(AST.SimplificationMode.Addition, toSubtractionRuleTwo);
            ruleManager.Add(AST.SimplificationMode.Addition, complex);
            ruleManager.Add(AST.SimplificationMode.Addition, division);
            //TODO: -c * f(x) + g(x) -> g(x) - c * f(x)
            //TODO: f(x)/g(x) + i(x)/j(x) -> [fx/(x)j(x)]/g(x)j(x) + i(x)g(x)/g(x)j(x) -> [f(x)j(x) + g(x)i(x)]/[g(x)j(x)]
        }

        private static void GenerateExponentSimplifications()
        {
            Rule setRule = new Rule(Exponent.setRule, null, "Exponent Set Rule");
            ruleManager.AddSetRule(AST.SimplificationMode.Exponent, setRule);

            Rule functionRaisedToOne = new Rule(Exponent.functionRaisedToOneRunnable, Exponent.functionRaisedToOne, "f(x)^1 -> f(x)");
            Rule functionRaisedToZero = new Rule(Exponent.functionRaisedToZeroRunnable, Exponent.functionRaisedToZero, "f(x)^0 -> 1");
            Rule zeroRaisedToConstant = new Rule(Exponent.zeroRaisedToConstantRunnable, Exponent.zeroRaisedToConstant, "0^c where c > 0 -> 0");
            Rule oneRaisedToFunction = new Rule(Exponent.oneRaisedToFunctionRunnable, Exponent.oneRaisedToFunction, "1^(fx) -> 1");

            Rule toDivision = new Rule(Exponent.toDivisionRunnable, Exponent.toDivision, "f(x)^-c -> 1/f(x)^c");
            Rule toSqrt = new Rule(Exponent.toSqrtRunnable, Exponent.toSqrt, "f(x)^0.5 -> sqrt( f(x) )");

            Rule exponentToExponent = new Rule(Exponent.ExponentToExponentRunnable, Exponent.ExponentToExponent,
                "(f(x)^c)^a -> f(x)^[c * a]");
            Rule negativeConstantRaisedToAPowerOfTwo = new Rule(Exponent.NegativeConstantRaisedToAPowerOfTwoRunnable,
                Exponent.NegativeConstantRaisedToAPowerOfTwo, "c_1^c_2 where c_2 is even and c_1 < 0 -> [-1 * c_1]^c_2");
            Rule constantRaisedToConstant = new Rule(Exponent.ConstantRaisedToConstantRunnable, Exponent.ConstantRaisedToConstant, "c^k -> a");
            Rule absRaisedToPowerTwo = new Rule(Exponent.AbsRaisedToPowerofTwoRunnable, Exponent.AbsRaisedToPowerofTwo, "abs(f(x))^2 -> [ sqrt(f(x) ^ 2) ]^2 -> sqrt(f(x)^2)^2 -> f(x)^2");

            ruleManager.Add(AST.SimplificationMode.Exponent, functionRaisedToOne);
            ruleManager.Add(AST.SimplificationMode.Exponent, functionRaisedToZero);
            ruleManager.Add(AST.SimplificationMode.Exponent, zeroRaisedToConstant);
            ruleManager.Add(AST.SimplificationMode.Exponent, oneRaisedToFunction);
            ruleManager.Add(AST.SimplificationMode.Exponent, toDivision);
            ruleManager.Add(AST.SimplificationMode.Exponent, toSqrt);
            ruleManager.Add(AST.SimplificationMode.Exponent, exponentToExponent);

            ruleManager.Add(AST.SimplificationMode.Exponent, negativeConstantRaisedToAPowerOfTwo);
            ruleManager.Add(AST.SimplificationMode.Exponent, constantRaisedToConstant);
            ruleManager.Add(AST.SimplificationMode.Exponent, absRaisedToPowerTwo);
        }

        private static void GenerateTrigSimplifications()
        {
            //TODO:
            //[f(x) * cos(x)]/[g(x) * sin(x)] -> [f(x) * cot(x)]/g(x) 
            //[f(x) * sin(x)]/cos(x) -> f(x) * tan(x)
            //sin(x)/[f(x) * cos(x)] -> tan(x)/f(x)
            //[f(x) * sin(x)]/[g(x) * cos(x)] -> [f(x) * tan(x)]/g(x) 

            //TODO: [1 + tan(f(x))^2] -> sec(f(x))^2
            //TODO: [cot(f(x))^2 + 1] -> csc(f(x))^2

            //These will probably violate domain constraints ?
            //TODO: sec(x)^2 - tan(x)^2 = 1
            //TODO: cot(x)^2 + 1 = csc(x)^2 
            //TODO: csc(x)^2 - cot(x)^2 = 1

            //TODO: Double Angle (->) (I wonder if double angle could be done through power reduction instead...)
            //[cos(f(x))^2 - sin(f(x))^2] = cos(2f(x))
            //1 - 2sin(x)^2 = cos(2x)
            //2cos(x)^2 - 1 = cos(2x) 
            //2sin(x)cos(x) = sin(2x)
            //[2tan(x)]/1 - tan(x)^2] = tan(2x) 

            //TODO: Power Reducing (Exclusive with Power Expansion)
            //[1 - cos(2x)]/2 <- sin(x)^2
            //[1 + cos(2x)]/2 <- cos(x)^2
            //[1 - cos(2x)]/[1 + cos(2x)] <- tan(x)^2 

            //TODO: Power Expansion (Exclusive with Power Reduction)
            //[1 - cos(2x)]/2 -> sin(x)^2
            //[1 + cos(2x)]/2 -> cos(x)^2
            //[1 - cos(2x)]/[1 + cos(2x)] -> tan(x)^2 

            //TODO: Shifting 
            //cos(x - pi/2) = sin(x)
            //sin(pi/2 - x) = sin(pi/2 + x) = cos(x)

            //Complex Conversions
            Rule CosOverSinToCotComplex = new Rule(Trig.CosOverSinComplexRunnable, Trig.CosOverSinComplex, "[f(x) * cos(x)]/sin(x) -> f(x) * cot(x)");
            Rule CosOverSinToCotComplexTwo = new Rule(Trig.CosOverSinToCotComplexRunnable, Trig.CosOverSinToCotComplex, "cos(x)/[f(x) * sin(x)] -> cot(x)/f(x)");

            ruleManager.Add(AST.SimplificationMode.Trig, CosOverSinToCotComplex);
            ruleManager.Add(AST.SimplificationMode.Trig, CosOverSinToCotComplexTwo);

            //Simple Conversions 
            Rule CosOverSinToCot = new Rule(Trig.CosOverSinToCotRunnable, Trig.CosOverSinToCot, "cos(x)/sin(x) -> cot(x)");
            Rule SinOverCosToTan = new Rule(Trig.SinOverCosRunnable, Trig.SinOverCos, "sin(x)/cos(x) -> tan(x)");
            Rule SecUnderToCos = new Rule(Trig.SecUnderToCosRunnable, Trig.SecUnderToCos, "f(x)/sec(g(x)) -> f(x)cos(g(x))");
            Rule CscUnderToSin = new Rule(Trig.CscUnderToSinRunnable, Trig.CscUnderToSin, "f(x)/csc(g(x)) -> f(x)sin(g(x))");
            Rule CotUnderToTan = new Rule(Trig.CotUnderToTanRunnable, Trig.CotUnderToTan, "f(x)/cot(g(x)) -> f(x)tan(g(x))");
            Rule CosUnderToSec = new Rule(Trig.CosUnderToSecRunnable, Trig.CosUnderToSec, "f(x)/cos(g(x)) -> f(x)sec(g(x))");
            Rule SinUnderToCsc = new Rule(Trig.SinUnderToCscRunnable, Trig.SinUnderToCsc, "f(x)/sin(g(x)) -> f(x)csc(g(x))");
            Rule TanUnderToCot = new Rule(Trig.TanUnderToCotRunnable, Trig.TanUnderToCot, "f(x)/tan(g(x)) -> f(x)cot(g(x))");

            ruleManager.Add(AST.SimplificationMode.Trig, CosOverSinToCot);
            ruleManager.Add(AST.SimplificationMode.Trig, SinOverCosToTan);
            ruleManager.Add(AST.SimplificationMode.Trig, SecUnderToCos);
            ruleManager.Add(AST.SimplificationMode.Trig, CscUnderToSin);
            ruleManager.Add(AST.SimplificationMode.Trig, CotUnderToTan);
            ruleManager.Add(AST.SimplificationMode.Trig, CosUnderToSec);
            ruleManager.Add(AST.SimplificationMode.Trig, SinUnderToCsc);
            ruleManager.Add(AST.SimplificationMode.Trig, TanUnderToCot);

            //Even Identity 
            Rule CosEven = new Rule(Trig.CosEvenIdentityRunnable, Trig.CosEvenIdentity, "cos(-f(x)) -> cos(f(x))");
            Rule SecEven = new Rule(Trig.SecEvenIdentityRunnable, Trig.SecEvenIdentity, "sec(-f(x)) -> sec(f(x))");

            ruleManager.Add(AST.SimplificationMode.Trig, CosEven);
            ruleManager.Add(AST.SimplificationMode.Trig, SecEven);

            //Odd Identity 
            Rule SinOdd = new Rule(Trig.SinOddIdentityRunnable, Trig.SinOddIdentity, "sin(-f(x)) -> -1 * sin(f(x))");
            Rule TanOdd = new Rule(Trig.TanOddIdentityRunnable, Trig.TanOddIdentity, "tan(-f(x)) -> -1 * tan(f(x))");
            Rule CotOdd = new Rule(Trig.CotOddIdentityRunnable, Trig.CotOddIdentity, "cot(-f(x)) -> -1 * cot(f(x))");
            Rule CscOdd = new Rule(Trig.CscOddIdentityRunnable, Trig.CscOddIdentity, "csc(-f(x)) -> -1 * csc(f(x))");

            ruleManager.Add(AST.SimplificationMode.Trig, SinOdd);
            ruleManager.Add(AST.SimplificationMode.Trig, TanOdd);
            ruleManager.Add(AST.SimplificationMode.Trig, CotOdd);
            ruleManager.Add(AST.SimplificationMode.Trig, CscOdd);

            Rule TrigIdentitySinToCos = new Rule(Trig.TrigIdentitySinToCosRunnable, Trig.TrigIdentitySinToCos, "1 - sin(x)^2 -> cos(x)^2");
            Rule TrigIdentityCosToSin = new Rule(Trig.TrigIdentityCosToSinRunnable, Trig.TrigIdentityCosToSin, "1 - cos(x)^2 -> sin(x)^2");
            Rule TrigIdentitySinPlusCos = new Rule(Trig.TrigIdentitySinPlusCosRunnable, Trig.TrigIdentitySinPlusCos, "sin²(x) + cos²(x) -> 1");

            ruleManager.Add(AST.SimplificationMode.Trig, TrigIdentitySinPlusCos);
            ruleManager.Add(AST.SimplificationMode.Trig, TrigIdentitySinToCos);
            ruleManager.Add(AST.SimplificationMode.Trig, TrigIdentityCosToSin);

        }

        private static void GenerateSumSimplifications()
        {
            Rule setRule = new Rule(Sum.setUp, null, "Sum Set Rule");

            ruleManager.AddSetRule(AST.SimplificationMode.Sum, setRule);

            Rule propagation = new Rule(Sum.PropagationRunnable, Sum.Propagation, "sum(f(x) ± g(x),x,a,b) -> sum(f(x),x,a,b) ± sum(g(x),x,a,b)"); //No bounds change
            Rule coefficient = new Rule(Sum.CoefficientRunnable, Sum.Coefficient, "sum(k * f(x),x,a,b) -> k * sum(f(x),x,a,b)"); //No bounds change
            Rule coefficientDivision = new Rule(Sum.CoefficientDivisionRunnable, Sum.CoefficientDivision, "sum(f(x)/k,x,a,b) -> sum(f(x),x,a,b)/k"); //No bounds change!

            Rule variable = new Rule(Sum.VariableRunnable, Sum.Variable, "sum(x,x,0,a) -> [a(a + 1)]/2");
            Rule constantComplex = new Rule(Sum.ConstantComplexRunnable, Sum.ConstantComplex, "sum(k,x,a,b) -> k(b - a + 1)");
            Rule power = new Rule(Sum.PowerRunnable, Sum.Power, "sum(x^p,x,1,n) -> [1/(p + 1)] * sum( (-1)^j * [(p + 1)!]/ [j! * (p - j + 1)!] * B_j * n^(p - j + 1),j,0,p)");

            ruleManager.Add(AST.SimplificationMode.Sum, propagation);
            ruleManager.Add(AST.SimplificationMode.Sum, coefficient);
            ruleManager.Add(AST.SimplificationMode.Sum, coefficientDivision);

            ruleManager.Add(AST.SimplificationMode.Sum, variable);
            ruleManager.Add(AST.SimplificationMode.Sum, power);
            ruleManager.Add(AST.SimplificationMode.Sum, constantComplex);

            //TODO: 
            // sum(f(x),x,a,b) -> sum(f(x),x,0,b) - sum(f(x),x,0,a - 1) where f(x) is continious on (0,b)
            // sum(x^p,x,0,n) -> 0^p + sum(x^p,x,1,n) 
        }

        private static void GenerateIntegralSimplifications()
        {
            Rule setRule = new Rule(Integrate.setUp, null, "Integral Set Rule");
            Rule propagation = new Rule(Integrate.PropagationRunnable, Integrate.Propagation, "integrate(f(x) + g(x),x,a,b) -> integrate(f(x),x,a,b) + integrate(g(x),x,a,b)"); //No bounds change
            Rule constants = new Rule(Integrate.ConstantsRunnable, Integrate.Constants, "integrate(k,x,a,b) -> k(b - a)");
            Rule coefficient = new Rule(Integrate.CoefficientRunnable, Integrate.Coefficient, "integrate(cf(x),x,a,b) -> c*integrate(f(x),x,a,b)");

            Rule singleVariable = new Rule(Integrate.SingleVariableRunnable, Integrate.SingleVariable, "integrate(x,x,a,b) -> (b^2 - a^2)/2");

            ruleManager.AddSetRule(AST.SimplificationMode.Integral, setRule);

            ruleManager.Add(AST.SimplificationMode.Integral, propagation);
            ruleManager.Add(AST.SimplificationMode.Integral, constants);
            //2) Coefficient
            // integrate(c*sin(x),x,a,b) -> c * integrate(sin(x),x,a,b)
            // integrate(sin(y)*x,x,a,b)

            ruleManager.Add(AST.SimplificationMode.Integral, coefficient);

            //3) Coefficient Division
            // integrate(f(x)/c,x,a,b) -> integrate(f(x),x,a,b)/c
            //3.5) Coefficient Division Two 
            // integrate(c/f(x),x,a,b) -> c * integrate(1/f(x),x,a,b) 
            ruleManager.Add(AST.SimplificationMode.Integral, singleVariable);
            //5) Power (n = -1)
            // integrate(1/x,x,a,b) -> ln(b) - ln(a) -> ln(b/a) 
            //6) Power
            // integrate(x^n,x,a,b) -> (b^(n + 1) - a^(n + 1))/(n + 1) where n is a number that is not -1 
            //7) Euler Exponent
            // integrate(e^x,x,a,b) -> e^b - e^a
            //8) Exponent 
            // integrate(k^x,x,a,b) -> [b^x]/ln(b) - [a^x]/ln(a)
            //9) Cos 
            // integrate(cos(x),x,a,b) -> sin(b) - sin(a) 
            //10) Sin 
            // integrate(sin(x),x,a,b) -> -cos(b) - -cos(a) -> cos(a) - cos(b)
            //11) sec(x)^2
            // integrate(sec(x)^2,x,a,b) -> tan(b) - tan(a) 
            //12) sec(x)tan(x) 
            // integrate(...,x,a,b) -> sec(b) - sec(a) 
            //13) 1/sqrt{1 - x^2}
            // integrate(...,x,a,b) -> arcsin(b) - arcsin(a) 
            //14) 1/(1 + x^2)
            // integrate(...,x,a,b) -> arctan(b) - arctan(a)
            //15) tan(x) 
            // integrate(tan(x),x,a,b) -> -ln(abs(cos(b)) - -ln(abs(cos(a)) -> ln(abs(cos(a)) - ln(abs(cos(b))
            //16) cot(x)
            // integrate(cot(x),x,a,b) -> ln(abs(sin(b))) - ln(abs(sin(a)))
            //17) sec(x) -> ln( abs(sec(x) + tan(x)) )
            //18) csc(x) -> -ln( abs(csc(x) + cot(x)) )

            //U-Substitution 
            //Chain Rule Integration 

            //All other ones fall back to numerical integration
        }

        private static void GenerateMiscSimplifications()
        {
            Rule factorial = new Rule(Misc.ZeroFactorialRunnable, Misc.ZeroFactorial, "(0! || 1!) -> 1");

            ruleManager.Add(AST.SimplificationMode.Misc, factorial);
        }

        private static void GenerateListSimplifications()
        {
            Rule setRule = new Rule(List.setRule, null, "List/Matrix Set Rule");
            ruleManager.AddSetRule(AST.SimplificationMode.List, setRule);

            Rule singleElement = new Rule(List.singleElementRunnable, List.singleElement, "List Single Element");
            Rule vectorToMatrix = new Rule(List.convertVectorToMatrixRunnable, List.convertVectorToMatrix, "Vector to Matrix");

            ruleManager.Add(AST.SimplificationMode.List, singleElement);
            ruleManager.Add(AST.SimplificationMode.List, vectorToMatrix);

        }

    }
}
