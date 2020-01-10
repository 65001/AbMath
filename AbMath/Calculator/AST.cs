using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AbMath.Calculator.Simplifications;
using AbMath.Utilities;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

        /// <summary>
        ///  Sqrt -
        ///  Log -
        ///  Division -
        ///  Exponent -
        ///  Subtraction -
        ///  Addition -
        ///  Multiplication -
        ///  Swap - 
        ///  Trig - All other trig conversions. [Currently encomposes all the below trig rules]
        ///  Trig Half Angle - Converts fractions to trig functions when appropriate
        ///  Trig Half Angle Expansion - Converts trig functions to fractions
        ///  Power Reduction -  Converts trig functions to fractions
        ///  Power Expansion - Converts fractions to trig functions
        ///  Double Angle - Converts double angles to trig functions
        ///  Double Angle Reduction - Converts trig functions to double angles
        ///  Sum - deals with all sum functions etc. 
        ///  Constants - 
        /// </summary>
        public enum SimplificationMode
        {
            Sqrt, Log, Division, Exponent, Subtraction, Addition, Multiplication, Swap,
            Trig, TrigHalfAngle, TrigHalfAngleExpansion,
            TrigPowerReduction, TrigPowerExpansion,
            TrigDoubleAngle, TrigDoubleAngleReduction,
            Sum,
            Constants,
            Misc,
            Compress, COUNT
        }

        private RPN _rpn;
        private RPN.DataStore _data;

        private bool debug => _data.DebugMode;

        private readonly RPN.Token _derive = new RPN.Token("derive", 1, RPN.Type.Function);
        private readonly RPN.Token _sum = new RPN.Token("sum", 5, RPN.Type.Function);

        public event EventHandler<string> Logger;
        public event EventHandler<string> Output;

        private OptimizerRuleSet ruleManager;

        private Logger logger;

        public AST(RPN rpn)
        {
            _rpn = rpn;
            _data = rpn.Data;
            logger = _data.Logger;
            RPN.Node.ResetCounter();
        }

        /// <summary>
        /// Generates the rule set for all simplifications
        /// </summary>
        private void GenerateRuleSetSimplifications()
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
            GenerateMiscSimplifications();
        }

        private void GenerateSqrtSimplifications()
        {
            Rule negative = new Rule(Sqrt.SqrtNegativeNumbersRunnable, Sqrt.SqrtNegativeNumbers, "sqrt(-c) -> i Imaginary Number to Root");
            Rule sqrt = new Rule(Sqrt.SqrtToFuncRunnable, Sqrt.SqrtToFunc, "sqrt(g(x))^2 - > g(x)");
            Rule abs = new Rule(Sqrt.SqrtToAbsRunnable, Sqrt.SqrtToAbs, "sqrt(g(x)^2) -> abs(g(x))");
            Rule sqrtPower = new Rule(Sqrt.SqrtPowerFourRunnable, Sqrt.SqrtPowerFour, "sqrt(g(x)^n) where n is a multiple of 4. -> g(x)^n/2");

            ruleManager.Add(SimplificationMode.Sqrt, negative);
            ruleManager.Add(SimplificationMode.Sqrt, sqrt);
            ruleManager.Add(SimplificationMode.Sqrt, abs);
            ruleManager.Add(SimplificationMode.Sqrt, sqrtPower);
        }

        private void GenerateLogSimplifications()
        {
            Rule logToLn = new Rule(Log.LogToLnRunnable, Log.LogToLn, "log(e,f(x)) - > ln(f(x))");
            
            //This rule only can be a preprocessor rule and therefore should not be added to the rule manager!
            Rule LnToLog = new Rule(Log.LnToLogRunnable, Log.LnToLog, "ln(f(x)) -> log(e,f(x))");

            //These are candidates for preprocessing and post processing:
            Rule logOne = new Rule(Log.LogOneRunnable,Log.LogOne,"log(b,1) -> 0");
            Rule logIdentical = new Rule(Log.LogIdentitcalRunnable, Log.LogIdentitcal, "log(b,b) -> 1");
            Rule logPowerExpansion = new Rule(Log.LogExponentExpansionRunnable, Log.LogExponentExpansion, "log(b,R^c) -> c * log(b,R)");

            logOne.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);
            logIdentical.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);
            logPowerExpansion.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);

            Rule logPower = new Rule(Log.LogPowerRunnable, Log.LogPower, "b^log(b,x) -> x");
            Rule logSummation = new Rule(Log.LogSummationRunnable, Log.LogSummation, "log(b,R) + log(b,S) -> log(b,R*S)");
            Rule logSubtraction = new Rule(Log.LogSubtractionRunnable, Log.LogSubtraction, "log(b,R) - log(b,S) -> log(b,R/S)");

            //TODO: lnPower e^ln(f(x)) -> f(x)
            //TODO: log(b,R^c) -> c * log(b,R)
            //TODO: ln(e) -> 1

            Rule lnSummation = new Rule(Log.LnSummationRunnable, Log.LnSummation, "ln(R) + ln(S) -> log(e,R) + log(e,S) -> ln(R*S)");
            Rule lnSubtraction = new Rule(Log.LnSubtractionRunnable, Log.LnSubtraction, "ln(R) - ln(S) -> log(e,R) - log(e,S) -> ln(R/S)");
            Rule lnPowerExpansion = new Rule(Log.LnPowerRuleRunnable, Log.LnPowerRule, "ln(R^c) -> c*ln(R)");

            ruleManager.Add(SimplificationMode.Log, logOne);
            ruleManager.Add(SimplificationMode.Log, logIdentical);
            ruleManager.Add(SimplificationMode.Log, logPower);
            ruleManager.Add(SimplificationMode.Log, logPowerExpansion);

            ruleManager.Add(SimplificationMode.Log, logSummation);
            ruleManager.Add(SimplificationMode.Log, logSubtraction);
            ruleManager.Add(SimplificationMode.Log, lnSummation);
            ruleManager.Add(SimplificationMode.Log, lnSubtraction);

            ruleManager.Add(SimplificationMode.Log, lnPowerExpansion);

            ruleManager.Add(SimplificationMode.Log, logToLn);
        }

        private void GenerateSubtractionSimplifications()
        {
            Rule setRule = new Rule(Subtraction.setRule, null, "Subtraction Set Rule");
            ruleManager.AddSetRule(SimplificationMode.Subtraction, setRule);

            Rule sameFunction = new Rule(Subtraction.SameFunctionRunnable, Subtraction.SameFunction, "f(x) - f(x) -> 0");
            Rule coefficientOneReduction = new Rule(Subtraction.CoefficientOneReductionRunnable, Subtraction.CoefficientOneReduction, "cf(x) - f(x) -> (c - 1)f(x)");
            Rule subtractionByZero = new Rule(Subtraction.SubtractionByZeroRunnable, Subtraction.SubtractionByZero, "f(x) - 0 -> f(x)");
            Rule ZeroSubtractedFunction = new Rule(Subtraction.ZeroSubtractedByFunctionRunnable, Subtraction.ZeroSubtractedByFunction,"0 - f(x) -> -f(x)");
            Rule subtractionDivisionCommmonDenominator = new Rule(Subtraction.SubtractionDivisionCommonDenominatorRunnable,
                Subtraction.SubtractionDivisionCommonDenominator, "f(x)/g(x) - h(x)/g(x) -> [f(x) - h(x)]/g(x)");
            Rule coefficientReduction = new Rule(Subtraction.CoefficientReductionRunnable, Subtraction.CoefficientReduction, "Cf(x) - cf(x) -> (C - c)f(x)");

            Rule constantToAddition = new Rule(Subtraction.ConstantToAdditionRunnable, Subtraction.ConstantToAddition, "f(x) - (-c) -> f(x) + c");
            Rule functionToAddition = new Rule(Subtraction.FunctionToAdditionRunnable, Subtraction.FunctionToAddition, "f(x) - (-c * g(x)) -> f(x) + c *g(x)");
            Rule distributive = new Rule(Subtraction.DistributiveSimpleRunnable, Subtraction.DistributiveSimple, "f(x) - (g(x) - h(x)) -> f(x) - g(x) + h(x) -> (f(x) + h(x)) - g(x)");

            ruleManager.Add(SimplificationMode.Subtraction, sameFunction);
            ruleManager.Add(SimplificationMode.Subtraction, coefficientOneReduction);
            ruleManager.Add(SimplificationMode.Subtraction, subtractionByZero);
            ruleManager.Add(SimplificationMode.Subtraction, ZeroSubtractedFunction);
            ruleManager.Add(SimplificationMode.Subtraction, subtractionDivisionCommmonDenominator);
            ruleManager.Add(SimplificationMode.Subtraction, coefficientReduction);
            ruleManager.Add(SimplificationMode.Subtraction, constantToAddition);
            ruleManager.Add(SimplificationMode.Subtraction, functionToAddition);
            ruleManager.Add(SimplificationMode.Subtraction, distributive);

            //TODO: f(x)/g(x) /pm i(x)/j(x) -> [f(x)j(x)]/g(x)j(x) /p, i(x)g(x)/g(x)j(x) -> [f(x)j(x) /pm g(x)i(x)]/[g(x)j(x)]
        }

        private void GenerateDivisionSimplifications()
        {
            Rule setRule = new Rule(Division.setRule, null, "Division Set Rule");

            Rule divisionByZero = new Rule(Division.DivisionByZeroRunnable, Division.DivisionByZero, "f(x)/0 -> NaN");
            Rule divisionByOne = new Rule(Division.DivisionByOneRunnable, Division.DivisionByOne, "f(x)/1 -> f(x)");
            Rule gcd = new Rule(Division.GCDRunnable, Division.GCD, "(cC)/(cX) -> C/X");
            Rule divisionFlip = new Rule(Division.DivisionFlipRunnable, Division.DivisionFlip, "(f(x)/g(x))/(h(x)/j(x)) - > (f(x)j(x))/(g(x)h(x))");
            Rule constantCancelation = new Rule(Division.DivisionCancelingRunnable, Division.DivisionCanceling, "(c * f(x))/c -> f(x) where c is not 0");
            Rule powerReduction = new Rule(Division.PowerReductionRunnable, Division.PowerReduction, "Power Reduction");
            Rule divisionFlipTwo = new Rule(Division.DivisionFlipTwoRunnable, Division.DivisionFlipTwo, "[f(x)/g(x)]/ h(x) -> [f(x)/g(x)]/[h(x)/1] - > f(x)/[g(x) * h(x)]");
            ruleManager.AddSetRule(SimplificationMode.Division, setRule);

            ruleManager.Add(SimplificationMode.Division, divisionByZero);
            ruleManager.Add(SimplificationMode.Division, divisionByOne);
            ruleManager.Add(SimplificationMode.Division, gcd);
            ruleManager.Add(SimplificationMode.Division, divisionFlip);
            ruleManager.Add(SimplificationMode.Division, constantCancelation);
            ruleManager.Add(SimplificationMode.Division, powerReduction);
            ruleManager.Add(SimplificationMode.Division, divisionFlipTwo);

            //TODO: 0/c when c is a constant or an expression that on solving will be a constant. 

            //TODO: (c_0 * f(x))/c_1 where c_0, c_1 share a gcd that is not 1 and c_0 and c_1 are integers 
            //TODO: (c_0 * f(x))/(c_1 * g(x)) where ...
        }

        private void GenerateMultiplicationSimplifications()
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
            Rule increaseExponentThree = new Rule(Multiplication.increaseExponentThreeRunnable, Multiplication.increaseExponentThree, "R3: f(x)^n * f(x) -> f(x)^(n + 1");

            Rule expressionDivision = new Rule(Multiplication.expressionTimesDivisionRunnable, Multiplication.expressionTimesDivision, "f(x) * [g(x)/h(x)] -> [f(x) * g(x)]/h(x)");
            Rule DivisionDivision = new Rule(Multiplication.divisionTimesDivisionRunnable, Multiplication.divisionTimesDivision, "[f(x)/g(x)] * [h(x)/j(x)] -> [f(x) * h(x)]/[g(x) * j(x)]");
            Rule negativeTimesNegative = new Rule(Multiplication.negativeTimesnegativeRunnable, Multiplication.negativeTimesnegative, "(-c)(-C) -> (c)(C)");
            Rule complexNegativeTimesNegative = new Rule(Multiplication.complexNegativeNegativeRunnable, Multiplication.complexNegativeNegative, "Complex: A negative times a negative is always positive.");
            Rule negativeByConstant = new Rule(Multiplication.negativeTimesConstantRunnable, Multiplication.negativeTimesConstant, "-1 * c -> -c");
            Rule constantByNegative = new Rule(Multiplication.constantTimesNegativeRunnable, Multiplication.constantTimesNegative, "c * -1 -> -c");
            Rule negativeOneDistributed = new Rule(Multiplication.negativeOneDistributedRunnable, Multiplication.negativeOneDistributed, "-1[f(x) - g(x)] -> -f(x) + g(x) -> g(x) - f(x)");
            Rule dualNode = new Rule(Multiplication.dualNodeMultiplicationRunnable,
                Multiplication.dualNodeMultiplication, "Dual Node");


            ruleManager.AddSetRule(SimplificationMode.Multiplication, setRule);
            ruleManager.Add(SimplificationMode.Multiplication, toExponent);
            ruleManager.Add(SimplificationMode.Multiplication, simplificationByOne);
            ruleManager.Add(SimplificationMode.Multiplication, simplificationByOneComplex);
            ruleManager.Add(SimplificationMode.Multiplication, multiplyByZero);

            ruleManager.Add(SimplificationMode.Multiplication, increaseExponent);
            ruleManager.Add(SimplificationMode.Multiplication, increaseExponentTwo);
            ruleManager.Add(SimplificationMode.Multiplication, increaseExponentThree);

            ruleManager.Add(SimplificationMode.Multiplication, dualNode);
            ruleManager.Add(SimplificationMode.Multiplication, expressionDivision);
            ruleManager.Add(SimplificationMode.Multiplication, DivisionDivision);
            ruleManager.Add(SimplificationMode.Multiplication, negativeTimesNegative);
            ruleManager.Add(SimplificationMode.Multiplication, complexNegativeTimesNegative);
            ruleManager.Add(SimplificationMode.Multiplication, negativeByConstant);
            ruleManager.Add(SimplificationMode.Multiplication, constantByNegative);
            ruleManager.Add(SimplificationMode.Multiplication, negativeOneDistributed);
        }

        private void GenerateAdditionSimplifications()
        {
            Rule setRule = new Rule(Addition.setRule, null, "Addition Set Rule");
            ruleManager.AddSetRule(SimplificationMode.Addition,setRule);

            Rule additionToMultiplication = new Rule(Addition.AdditionToMultiplicationRunnable, Addition.AdditionToMultiplication, "f(x) + f(x) -> 2 * f(x)");
            Rule zeroAddition = new Rule(Addition.ZeroAdditionRunnable, Addition.ZeroAddition, "f(x) + 0 -> f(x)");
            Rule simpleCoefficient = new Rule(Addition.SimpleCoefficientRunnable, Addition.SimpleCoefficient, "cf(x) + f(x) -> (c + 1)f(x) + 0");
            Rule complexCoefficient = new Rule(Addition.ComplexCoefficientRunnable, Addition.ComplexCoefficient, "cf(x) + Cf(x) -> (c + C)f(x) + 0");
            Rule additionSwap = new Rule(Addition.AdditionSwapRunnable, Addition.AdditionSwap, "-f(x) + g(x) -> g(x) - f(x)");
            Rule toSubtractionRuleOne = new Rule(Addition.AdditionToSubtractionRuleOneRunnable, Addition.AdditionToSubtractionRuleOne, "Addition can be converted to subtraction R1");
            Rule toSubtractionRuleTwo = new Rule(Addition.AdditionToSubtractionRuleTwoRunnable, Addition.AdditionToSubtractionRuleTwo, "Addition can be converted to subtraction R2");
            Rule complex = new Rule(Addition.ComplexNodeAdditionRunnable, Addition.ComplexNodeAddition, "f(x) + f(x) - g(x) -> 2 * f(x) - g(x)");

            ruleManager.Add(SimplificationMode.Addition, additionToMultiplication);
            ruleManager.Add(SimplificationMode.Addition, zeroAddition);
            ruleManager.Add(SimplificationMode.Addition, simpleCoefficient);
            ruleManager.Add(SimplificationMode.Addition, complexCoefficient);
            ruleManager.Add(SimplificationMode.Addition, additionSwap);
            ruleManager.Add(SimplificationMode.Addition, toSubtractionRuleOne);
            ruleManager.Add(SimplificationMode.Addition, toSubtractionRuleTwo);
            ruleManager.Add(SimplificationMode.Addition, complex);
            //TODO: -c * f(x) + g(x) -> g(x) - c * f(x)
            //TODO f(x)/g(x) + h(x)/g(x) -> [f(x) + h(x)]/g(x)
            //TODO: f(x)/g(x) + i(x)/j(x) -> [f(x)j(x)]/g(x)j(x) + i(x)g(x)/g(x)j(x) -> [f(x)j(x) + g(x)i(x)]/[g(x)j(x)]
        }

        private void GenerateExponentSimplifications()
        {
            Rule setRule = new Rule(Exponent.setRule, null, "Exponent Set Rule");
            ruleManager.AddSetRule(SimplificationMode.Exponent, setRule);

            Rule functionRaisedToOne = new Rule(Exponent.functionRaisedToOneRunnable, Exponent.functionRaisedToOne, "f(x)^1 -> f(x)");
            Rule functionRaisedToZero = new Rule(Exponent.functionRaisedToZeroRunnable, Exponent.functionRaisedToZero, "f(x)^0 -> 1");
            Rule zeroRaisedToConstant = new Rule(Exponent.zeroRaisedToConstantRunnable, Exponent.zeroRaisedToConstant, "0^c where c > 0 -> 0");
            Rule oneRaisedToFunction = new Rule(Exponent.oneRaisedToFunctionRunnable, Exponent.oneRaisedToFunction, "1^(fx) -> 1");
            Rule toDivision = new Rule(Exponent.toDivisionRunnable, Exponent.toDivision, "f(x)^-c -> 1/f(x)^c");
            Rule toSqrt = new Rule(Exponent.toSqrtRunnable, Exponent.toSqrt, "f(x)^0.5 -> sqrt( f(x) )");

            Rule exponentToExponent = new Rule(Exponent.ExponentToExponentRunnable, Exponent.ExponentToExponent,
                "(f(x)^c)^a -> f(x)^[c * a]");
            Rule negativeConstantRaisedToAPowerOfTwo = new Rule(Exponent.NegativeConstantRaisedToAPowerOfTwoRunnable, 
                Exponent.NegativeConstantRaisedToAPowerOfTwo, "c_1^c_2 where c_2 % 2 = 0 and c_1 < 0 -> [-1 * c_1]^c_2");
            Rule constantRaisedToConstant = new Rule(Exponent.ConstantRaisedToConstantRunnable, Exponent.ConstantRaisedToConstant, "c^k -> a");

            ruleManager.Add(SimplificationMode.Exponent, functionRaisedToOne);
            ruleManager.Add(SimplificationMode.Exponent, functionRaisedToZero);
            ruleManager.Add(SimplificationMode.Exponent, zeroRaisedToConstant);
            ruleManager.Add(SimplificationMode.Exponent, oneRaisedToFunction);
            ruleManager.Add(SimplificationMode.Exponent, toDivision);
            ruleManager.Add(SimplificationMode.Exponent, toSqrt);
            ruleManager.Add(SimplificationMode.Exponent, exponentToExponent);
            ruleManager.Add(SimplificationMode.Exponent, negativeConstantRaisedToAPowerOfTwo);
            ruleManager.Add(SimplificationMode.Exponent, constantRaisedToConstant);
        }

        private void GenerateTrigSimplifications()
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

            //TODO: Double Angle (I wonder if double angle could be done through power reduction instead...)
            //[cos(x)^2 - sin(x)^2] = cos(2x)
            //1 - 2sin(x)^2 = cos(2x)
            //2cos(x)^2 - 1 = cos(2x) 
            //2sin(x)cos(x) = sin(2x)
            //[2tan(x)]/1 - tan(x)^2] = tan(2x) 

            //TODO: Power Reducing 
            //[1 - cos(2x)]/2 <- sin(x)^2
            //[1 + cos(2x)]/2 <- cos(x)^2
            //[1 - cos(2x)]/[1 + cos(2x)] <- tan(x)^2 

            //TODO: Power Expansion 
            //[1 - cos(2x)]/2 -> sin(x)^2
            //[1 + cos(2x)]/2 -> cos(x)^2
            //[1 - cos(2x)]/[1 + cos(2x)] -> tan(x)^2 

            //Complex Conversions
            Rule CosOverSinToCotComplex = new Rule(Trig.CosOverSinComplexRunnable, Trig.CosOverSinComplex, "[f(x) * cos(x)]/sin(x) -> f(x) * cot(x)");
            Rule CosOverSinToCotComplexTwo = new Rule(Trig.CosOverSinToCotComplexRunnable, Trig.CosOverSinToCotComplex, "cos(x)/[f(x) * sin(x)] -> cot(x)/f(x)");
            
            ruleManager.Add(SimplificationMode.Trig, CosOverSinToCotComplex);
            ruleManager.Add(SimplificationMode.Trig, CosOverSinToCotComplexTwo);

            //Simple Conversions 
            Rule CosOverSinToCot = new Rule(Trig.CosOverSinToCotRunnable, Trig.CosOverSinToCot, "cos(x)/sin(x) -> cot(x)");
            Rule SinOverCosToTan = new Rule(Trig.SinOverCosRunnable, Trig.SinOverCos, "sin(x)/cos(x) -> tan(x)");
            Rule SecUnderToCos = new Rule(Trig.SecUnderToCosRunnable, Trig.SecUnderToCos, "f(x)/sec(g(x)) -> f(x)cos(g(x))");
            Rule CscUnderToSin = new Rule(Trig.CscUnderToSinRunnable, Trig.CscUnderToSin, "f(x)/csc(g(x)) -> f(x)sin(g(x))");
            Rule CotUnderToTan = new Rule(Trig.CotUnderToTanRunnable, Trig.CotUnderToTan, "f(x)/cot(g(x)) -> f(x)tan(g(x))");
            Rule CosUnderToSec = new Rule(Trig.CosUnderToSecRunnable, Trig.CosUnderToSec, "f(x)/cos(g(x)) -> f(x)sec(g(x))");
            Rule SinUnderToCsc = new Rule(Trig.SinUnderToCscRunnable, Trig.SinUnderToCsc, "f(x)/sin(g(x)) -> f(x)csc(g(x))");
            Rule TanUnderToCot = new Rule(Trig.TanUnderToCotRunnable, Trig.TanUnderToCot, "f(x)/tan(g(x)) -> f(x)cot(g(x))");

            ruleManager.Add(SimplificationMode.Trig, CosOverSinToCot);
            ruleManager.Add(SimplificationMode.Trig, SinOverCosToTan);
            ruleManager.Add(SimplificationMode.Trig, SecUnderToCos);
            ruleManager.Add(SimplificationMode.Trig, CscUnderToSin);
            ruleManager.Add(SimplificationMode.Trig, CotUnderToTan);
            ruleManager.Add(SimplificationMode.Trig, CosUnderToSec);
            ruleManager.Add(SimplificationMode.Trig, SinUnderToCsc);
            ruleManager.Add(SimplificationMode.Trig, TanUnderToCot);

            //Even Identity 
            Rule CosEven = new Rule(Trig.CosEvenIdentityRunnable, Trig.CosEvenIdentity, "cos(-f(x)) -> cos(f(x))");
            Rule SecEven = new Rule(Trig.SecEvenIdentityRunnable, Trig.SecEvenIdentity, "sec(-f(x)) -> sec(f(x))");

            ruleManager.Add(SimplificationMode.Trig, CosEven);
            ruleManager.Add(SimplificationMode.Trig, SecEven);

            //Odd Identity 
            Rule SinOdd = new Rule(Trig.SinOddIdentityRunnable, Trig.SinOddIdentity, "sin(-f(x)) -> -1 * sin(f(x))");
            Rule TanOdd = new Rule(Trig.TanOddIdentityRunnable, Trig.TanOddIdentity, "tan(-f(x)) -> -1 * tan(f(x))");
            Rule CotOdd = new Rule(Trig.CotOddIdentityRunnable, Trig.CotOddIdentity, "cot(-f(x)) -> -1 * cot(f(x))");
            Rule CscOdd = new Rule(Trig.CscOddIdentityRunnable, Trig.CscOddIdentity, "csc(-f(x)) -> -1 * csc(f(x))");

            ruleManager.Add(SimplificationMode.Trig, SinOdd);
            ruleManager.Add(SimplificationMode.Trig, TanOdd);
            ruleManager.Add(SimplificationMode.Trig, CotOdd);
            ruleManager.Add(SimplificationMode.Trig, CscOdd);

            Rule TrigIdentitySinToCos = new Rule(Trig.TrigIdentitySinToCosRunnable, Trig.TrigIdentitySinToCos, "1 - sin(x)^2 -> cos(x)^2");
            Rule TrigIdentityCosToSin = new Rule(Trig.TrigIdentityCosToSinRunnable, Trig.TrigIdentityCosToSin, "1 - cos(x)^2 -> sin(x)^2");
            Rule TrigIdentitySinPlusCos = new Rule(Trig.TrigIdentitySinPlusCosRunnable, Trig.TrigIdentitySinPlusCos, "sin²(x) + cos²(x) -> 1");
            
            ruleManager.Add(SimplificationMode.Trig, TrigIdentitySinPlusCos);
            ruleManager.Add(SimplificationMode.Trig, TrigIdentitySinToCos);
            ruleManager.Add(SimplificationMode.Trig, TrigIdentityCosToSin);
            
        }

        private void GenerateSumSimplifications()
        {
            Rule setRule = new Rule(Sum.setUp, null, "Sum Set Rule");

            ruleManager.AddSetRule(SimplificationMode.Sum, setRule);

            Rule propagation = new Rule(Sum.PropagationRunnable, Sum.Propagation, "sum(f(x) + g(x),x,a,b) -> sum(f(x),x,a,b) + sum(g(x),x,a,b)"); //No bounds change
            Rule coefficient = new Rule(Sum.CoefficientRunnable, Sum.Coefficient, "sum(k * f(x),x,a,b) -> k * sum(f(x),x,a,b)"); //No bounds change



            Rule variable = new Rule(Sum.VariableRunnable, Sum.Variable, "sum(x,x,0,a) -> [a(a + 1)]/2");
            Rule constantComplex = new Rule(Sum.ConstantComplexRunnable, Sum.ConstantComplex, "sum(k,x,a,b) -> k(b - a + 1)");
            Rule power = new Rule(Sum.PowerRunnable, Sum.Power, "sum(x^p,x,1,n) -> [1/(p + 1)] * sum( (-1)^j * [(p + 1)!]/ [j! * (p - j + 1)!] * B_j * n^(p - j + 1),j,0,p)");

            ruleManager.Add(SimplificationMode.Sum, propagation);
            ruleManager.Add(SimplificationMode.Sum, coefficient);

            ruleManager.Add(SimplificationMode.Sum, variable);
            ruleManager.Add(SimplificationMode.Sum, power);
            ruleManager.Add(SimplificationMode.Sum, constantComplex);

            //TODO: 
            // sum(f(x),x,a,b) -> sum(f(x),x,0,b) - sum(f(x),x,0,a - 1)
            // sum(x^p,x,0,n) -> 0^p + sum(x^p,x,1,n) 
        }

        private void GenerateMiscSimplifications()
        {
            
        }

        public RPN.Node Generate(RPN.Token[] input)
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();
            RPN.Node node = RPN.Node.Generate(input);

            //This prevents the reassignment of the root node
            if (Root is null)
            {
                Root = node;
            }

            SW.Stop();
            _rpn.Data.AddTimeRecord("AST Generate", SW);
            return node;
        }

        /// <summary>
        /// Simplifies the current tree.
        /// </summary>
        /// <returns></returns>
        public AST Simplify()
        {
            Normalize();

            Stopwatch sw = new Stopwatch();

            ruleManager = new OptimizerRuleSet(logger);
            //Let us generate the rules here if not already creates 
            GenerateRuleSetSimplifications();
            ruleManager.debug = false;
            if (debug)
            {
                ruleManager.debug = true;
            }

            sw.Start();
            int pass = 0;
            string hash = string.Empty;
            Dictionary<string, OptimizationTracker> tracker = new Dictionary<string, OptimizationTracker>();

            while (hash != Root.GetHash())
            {
                Stopwatch sw1 = new Stopwatch();
                sw1.Start();
                hash = Root.GetHash();
                if (tracker.ContainsKey(hash))
                {
                    if (tracker[hash].count > 10)
                    {
                        stdout("An infinite optimization loop may be occuring. Terminating optimization.");
                        return this;
                    }

                    var foo = tracker[hash];
                    foo.count++;
                    tracker[hash] = foo;
                }
                else
                {
                    tracker.Add(hash, new OptimizationTracker() { count = 1, Hash = hash });
                }

                _data.AddTimeRecord("AST.GetHash", sw1);

                if (debug)
                {
                    Write($"{pass}. {Root.ToInfix()}.");
                }

                Simplify(Root);
                pass++;
            }
            sw.Stop();
            _data.AddTimeRecord("AST Simplify", sw);

            if (debug)
            {
                Write("Before being normalized the tree looks like:");
                Write(Root.ToInfix());
                Write(Root.Print());
            }

            Normalize(); //This distorts the tree :(

            Write("");
            Write(ruleManager.ToString());

            return this;
        }

        private void Normalize()
        {
            //This should in theory normalize the tree
            //so that exponents etc come first etc
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _rpn.Data.AddFunction("internal_product", new RPN.Function());
            _rpn.Data.AddFunction("internal_sum", new RPN.Function());

            expand(Root);
            InternalSwap(Root);
            compress(Root);

            _rpn.Data.RemoveFunction("internal_product");
            _rpn.Data.RemoveFunction("internal_sum");
            sw.Stop();
            this._rpn.Data.AddTimeRecord("AST.Normalize :: AST Simplify", sw);
        }

        private void Simplify(RPN.Node node)
        {
        #if DEBUG
                Write(Root.ToInfix());       
        #endif
            //We want to reduce this! 

            Simplify(node, SimplificationMode.Sqrt);
            Simplify(node, SimplificationMode.Log);
            Simplify(node, SimplificationMode.Division);

            Simplify(node, SimplificationMode.Exponent); //This will make all negative exponennts into divisions
            Simplify(node, SimplificationMode.Subtraction);
            Simplify(node, SimplificationMode.Addition);
            Simplify(node, SimplificationMode.Trig);
            Simplify(node, SimplificationMode.Multiplication);

            Simplify(node, SimplificationMode.Swap);
            Simplify(node, SimplificationMode.Misc);
            Simplify(node, SimplificationMode.Sum);
            
            

            Swap(node);
#if DEBUG
                Write(Root.ToInfix());
#endif
        }

        private void Simplify(RPN.Node node, SimplificationMode mode)
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            Stack<RPN.Node> stack = new Stack<RPN.Node>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                node = stack.Pop();

                //If Root is a number abort. 
                if (Root.IsNumber())
                {
                    return;
                }

                if (node.IsNumber() || node.IsConstant())
                {
                    continue;
                }

                //This is the rule manager execution code
                if (ruleManager.CanRunSet(mode, node))
                {
                    RPN.Node assignment = ruleManager.Execute(mode, node);
                    if (assignment != null)
                    {
                        if (assignment.IsNumber() && assignment.Token.Value == "NaN")
                        {
                            SetRoot(assignment);
                        }
                        else
                        {
                            Assign(node, assignment);
                        }
                    }
                }

                if (mode == SimplificationMode.Swap)
                {
                    //We can do complex swapping in here
                    if (node.IsMultiplication() && node.Children[0].IsMultiplication() &&
                        node.Children[0].Children[0].Matches(node.Children[1]))
                    {
                        Write($"\tComplex Swap: Dual Node Multiplication Swap");
                        RPN.Node temp = node.Children[0].Children[1];


                        node.Children[0].Children[1] = node.Children[1];
                        node.Children[1] = temp;
                    }
                    else if (node.IsMultiplication() && node.Children[0].IsMultiplication() &&
                             node.Children[1].IsMultiplication())
                    {
                        if (node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber())
                        {
                            Write($"\tComplex Swap: Tri Node Multiplication Swap");
                            RPN.Node multiply =
                                new RPN.Node(
                                    new[] { Clone(node.Children[0].Children[1]), Clone(node.Children[1].Children[1]) },
                                    new RPN.Token("*", 2, RPN.Type.Operator));
                            node.Children[1].Children[1].Remove(multiply);
                            node.Children[0].Children[1].Remove(new RPN.Node(1));
                        }
                    }
                }
                else if (mode == SimplificationMode.Constants)
                {
                    if (node.IsMultiplication())
                    {
                        if (node[0].IsInteger() && node[1].IsInteger())
                        {
                            Solve(node);
                        }
                        else if (node[0].IsNumber() && node[1].IsNumber())
                        {
                            if ((int)(node[0].GetNumber() * node[1].GetNumber()) ==
                                node[0].GetNumber() * node[1].GetNumber())
                            {
                                Solve(node);
                            }
                        }

                    }
                    else if (node.IsExponent() && node[0].IsInteger() && node[1].IsInteger())
                    {
                        Solve(node);
                    }
                    else if (node.IsOperator("!"))
                    {
                        if (node[0].IsNumber(0) || node[0].IsNumber(1))
                        {
                            Solve(node);
                        }
                    }
                }
                else if (mode == SimplificationMode.Misc)
                {
                    if (node.IsOperator() && node.Children.Any(t => t.IsGreaterThanNumber(-1) && t.IsLessThanNumber(1) ))
                    {
                        Write("\tDecimal to Fraction");
                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            if (node[i].IsGreaterThanNumber(-1) && node[i].IsLessThanNumber(1))
                            {
                                var f = Extensions.getDecimalFormatToNode(node[i].GetNumber());
                                if (f != null)
                                {
                                    node.Replace(node[i], f);
                                }
                            }
                        }
                    }
                }

                //Propagate down the tree IF there is a root 
                //which value is not NaN or a number
                if (Root == null || Root.IsNumber() || Root.IsNumber(double.NaN))
                {
                    return;
                }


                SW.Stop();
                _data.AddTimeRecord("AST.Simplify:Compute", SW);
                SW.Restart();
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }

                _data.AddTimeRecord("AST.Simplify:Propogate", SW);
            }

        }

        private void Swap(RPN.Node node)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();


            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();

                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                //Addition operator
                if (node.IsAddition())
                {
                    //Write($"\t{node[0].CompareTo(node[1])}  {node[1].CompareTo(node[0])}");
                    //Write($"\t{node[0].ToInfix()} {node[1].ToInfix()}\n");
                    node.Children.Sort();
                    if (node[1].IsMultiplication() && node[0].IsMultiplication() && !node.Children[1].Children.Any(n => n.IsExponent()) && node.Children[0].Children.Any(n => n.IsExponent()))
                    {
                        Write("\tNode Swap:Straight multiplication gives way to multiplication with an exponent");
                        node.Swap(0, 1);
                    }

                    
                }
                //Multiplication operator
                else if (node.IsMultiplication())
                {
                    //Numbers and constants take way
                    if (!node.Children[1].IsNumberOrConstant() && node.Children[0].IsNumberOrConstant())
                    {
                        node.Swap(0, 1);
                        Write("\tNode Swap: Numbers and constants take way.");
                    }
                    //Sort functions alphabetically
                    else if (node[1].IsFunction() && node[0].IsFunction() && !node[1].IsConstant() &&
                             !node[0].IsConstant())
                    {
                        StringComparison foo = StringComparison.CurrentCulture;

                        int comparison = string.Compare(node.Children[0].Token.Value, node.Children[1].Token.Value
                            , foo);
                        if (comparison == -1)
                        {
                            node.Swap(0, 1);
                        }
                    }
                    else if (!(node.Children[1].IsExponent() || node.Children[1].IsNumberOrConstant() ||
                               node.Children[1].IsSolveable()) && node.Children[0].IsExponent())
                    {
                        Write("\tNode Swap: Exponents take way");
                        node.Swap(0, 1);
                    }
                    else if (node[1].IsVariable() && node[0].IsExponent())
                    {
                        Write("\tNode Swap: Variable yields to Exponent");
                        node.Swap(0, 1);
                    }

                    //a number and a expression
                    else if (node.Children[0].IsNumber() &&
                             !(node.Children[1].IsNumber() || node.Children[1].IsVariable()))
                    {
                        Write($"\tMultiplication Swap.");
                        node.Swap(1, 0);
                    }
                }
            }

            sw.Stop();
            this._rpn.Data.AddTimeRecord("AST.Swap :: AST Simplify", sw);
        }

        private void InternalSwap(RPN.Node node)
        {
            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                if (node.IsFunction("internal_sum") || node.IsFunction("total"))
                {
                    node.Children.Sort(); 
                    Write($"After Auto Sort: {node.ToInfix()}");
                }
                else if (node.IsFunction("internal_product") || node.IsFunction("product"))
                {
                    node.Children.Reverse();
                    string hash = string.Empty;
                    //Simplification
                    Dictionary<string, List<RPN.Node>> hashDictionary = new Dictionary<string, List<RPN.Node>>();
                    hashDictionary.Clear();

                    //This tracks everything
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        hash = node.Children[i].GetHash();
                        if (!hashDictionary.ContainsKey(hash))
                        {
                            List<RPN.Node> temp = new List<RPN.Node>();
                            temp.Add(node.Children[i]);
                            hashDictionary.Add(hash, temp);
                        }
                        else
                        {
                            hashDictionary[hash].Add(node.Children[i]);
                        }
                    }

                    //This simplifies everything
                    foreach (var kv in hashDictionary)
                    {
                        if (kv.Value.Count > 1)
                        {
                            Write("\t" + kv.Key + " with a count of " + kv.Value.Count + " and infix of " + kv.Value[0].ToInfix());

                            RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(kv.Value.Count), kv.Value[0] },
                                new RPN.Token("^", 2, RPN.Type.Operator));

                            foreach (var nv in kv.Value)
                            {
                                Write($"\t\t Replacing {nv.ID} with 1");
                                node.Replace(nv, new RPN.Node(1));
                            }

                            node.AddChild(exponent);
                        }
                    }


                    while (node.GetHash() != hash)
                    {
                        hash = node.GetHash();
                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            if (i - 1 < 0)
                            {
                                continue;
                            }

                            //Numbers and constants take way
                            if (!node.Children[i - 1].IsNumberOrConstant() && node.Children[i].IsNumberOrConstant())
                            {
                                node.Children.Swap(i - 1, i);
                                Write("\tNumbers and constants take way.");
                            }
                            //Sort functions alphabetically
                            else if (node[i - 1].IsFunction() && node[i].IsFunction() && !node[i - 1].IsConstant() &&
                                     !node[i].IsConstant())
                            {
                                StringComparison foo = StringComparison.CurrentCulture;

                                int comparison = string.Compare(node.Children[i].Token.Value,
                                    node.Children[i - 1].Token.Value
                                    , foo);
                                if (comparison == -1)
                                {
                                    node.Children.Swap(i - 1, i);
                                }
                            }
                            else if (!(node.Children[i - 1].IsExponent() || node.Children[i - 1].IsNumberOrConstant() ||
                                       node.Children[i - 1].IsSolveable()) && node.Children[i].IsExponent())
                            {
                                Write("\tExponents take way");
                                node.Children.Swap(i - 1, i);
                            }
                            else if (node[i - 1].IsVariable() && node[i].IsExponent())
                            {
                                Write("\tVariable yields to Exponent");
                                node.Children.Swap(i - 1, i);
                            }

                            //TODO: Exponents and other expressions right of way
                        }
                    }



                    Write(node.Print());
                }
            }
        }

        /// <summary>
        /// Simplifies or evaluates meta functions that
        /// cannot be easily represented or understood by PostFix.
        /// </summary>
        public AST MetaFunctions()
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            //This makes derive an internal only function
            //when called from the outside of this function
            //it will not appear to be a function
            if (!_rpn.Data.Functions.ContainsKey("derive"))
            {
                _rpn.Data.AddFunction("derive", new RPN.Function { Arguments = 1, MaxArguments = 1, MinArguments = 1 });
            }
            MetaFunctions(Root);

            if (_rpn.Data.Functions.ContainsKey("derive"))
            {
                _rpn.Data.RemoveFunction("derive");
            }

            SW.Stop();

            _data.AddTimeRecord("AST MetaFunctions", SW);


            Simplify();

            return this;
        }

        private bool MetaFunctions(RPN.Node node)
        {
            //Propagate down the tree
            bool fooBar = false;
            for (int i = 0; i < node.Children.Count; i++)
            {
                MetaFunctions(node.Children[i]);
            }

            if (!node.IsFunction() || !_data.MetaFunctions.Contains(node.Token.Value))
            {
                return false;
            }

            if (node.IsFunction("integrate"))
            {
                double answer = double.NaN;
                if (node.Children.Count == 4)
                {
                    node.Children.Insert(0, new RPN.Node(0.001));
                }

                if (node.Children.Count == 5)
                {
                    answer = MetaCommands.Integrate(_rpn,
                        node.Children[4],
                        node.Children[3],
                        node.Children[2],
                        node.Children[1],
                        node.Children[0]);
                }

                RPN.Node temp = new RPN.Node(answer);
                Assign(node, temp);
            }
            else if (node.IsFunction("table"))
            {
                string table;

                if (node.Children.Count == 4)
                {
                    node.Children.Insert(0, new RPN.Node(0.001));
                }

                table = MetaCommands.Table(_rpn,
                    node.Children[4],
                    node.Children[3],
                    node.Children[2],
                    node.Children[1],
                    node.Children[0]);

                stdout(table);
                SetRoot(new RPN.Node(double.NaN));
            }
            else if (node.IsFunction("derivative"))
            {
                if (node.Children.Count == 2)
                {
                    GenerateDerivativeAndReplace(node.Children[1]);
                    Derive(node.Children[0]);
                    Assign(node, node.Children[1]);
                    node.Delete();
                }
                else if (node.Children.Count == 3)
                {
                    if (!node[0].IsNumberOrConstant() && (int)node[0].GetNumber() == node[0].GetNumber())
                    {
                        throw new Exception("Expected a number or constant");
                    }

                    //This code is suspect!
                    int count = (int)node[0].GetNumber();

                    node.RemoveChild(node[0]);


                    for (int i = 0; i < count; i++)
                    {
                        GenerateDerivativeAndReplace(node.Children[1]);
                        Derive(node.Children[0]);
                        Simplify(node);
                    }
                    Assign(node, node.Children[1]);
                    node.Delete();


                }
            }
            else if (node.IsFunction("solve"))
            {
                if (node.Children.Count == 2)
                {
                    node.AddChild(new RPN.Node(new RPN.Token("=", 2, RPN.Type.Operator)));
                }
                else
                {
                    node.Swap(0, 2);
                    node[2].Children.Clear();
                }

                Write(node.Print());

                RPN.Node temp = node[2];
                node.RemoveChild(temp);

                Algebra(node, ref temp);

                temp.AddChild(new[] { node[0], node[1] });

                //This is done to fix a bug
                if (!temp.IsOperator("="))
                {
                    temp.Swap(0, 1);
                }

                node.AddChild(temp);
                Write($"{node[1].ToInfix()} {node[2].ToInfix()} {node[0].ToInfix()}");
                node.RemoveChild(temp);

                Write(temp.ToInfix());
                Assign(node, temp);
            }
            else if (node.IsFunction("sum"))
            {
                Write($"\tSolving the sum! : {node[3].ToInfix()}");
                PostFix math = new PostFix(_rpn);
                double start = math.Compute(node[1].ToPostFix().ToArray());
                double end = math.Compute(node[0].ToPostFix().ToArray());
                double DeltaX = end - start;
                int max = (int)Math.Ceiling(DeltaX);

                double PrevAnswer = 0;

                math.SetPolish(node[3].ToPostFix().ToArray());
                double sum = 0;

                for (int x = 0; x <= max; x++)
                {
                    double RealX = start + x;
                    math.SetVariable("ans", PrevAnswer);
                    math.SetVariable(node[2].Token.Value, RealX);
                    double answer = math.Compute();
                    PrevAnswer = answer;
                    sum += answer;
                    math.Reset();
                }
                Assign(node, new RPN.Node(sum));
            }

            return true;
        }

        private void Solve(RPN.Node node)
        {
            if (node == null)
            {
                return;
            }

            //All children of a node must be either numbers of constant functions
            bool isSolveable = node.IsSolveable();

            //Functions that are not constants and/or meta functions 
            if ((node.IsFunction() && !(node.IsConstant() || _data.MetaFunctions.Contains(node.Token.Value)) || node.IsOperator()) && isSolveable)
            {
                PostFix math = new PostFix(_rpn);
                double answer = math.Compute(node.ToPostFix().ToArray());
                Assign(node, new RPN.Node(answer));
                //Since we solved something lower in the tree we may be now able 
                //to solve something higher up in the tree!
                Solve(node.Parent);
            }

            //Propagate down the tree
            for (int i = 0; i < node.Children.Count; i++)
            {
                Solve(node.Children[i]);
            }
        }


        private AST Derive(RPN.Node variable)
        {
            if (!variable.IsVariable())
            {
                throw new ArgumentException("The variable of deriviation is not a variable!", nameof(variable));
            }

            Simplify(Root);
            //Simplify(Root, SimplificationMode.Constants);

            Write($"Starting to derive ROOT: {Root.ToInfix()}");
            Derive(Root, variable);

            Write("\tSimplifying Post!\n");
            Simplify(Root);
            //Simplify(Root, SimplificationMode.Constants);
            Write("");

            return this;
        }

        private void Derive(RPN.Node foo, RPN.Node variable)
        {
            //We do not know in advance the depth of a tree 
            //and given a big enough expression the current recursion 
            //is more likley to fail compared to an itterative approach.

            Stack<RPN.Node> stack = new Stack<RPN.Node>();
            stack.Push(foo);
            //Write(foo.ToInfix());
            string v = variable.ToInfix();
            RPN.Node node = null;
            while (stack.Count > 0)
            {
                node = stack.Pop();
                //Write($"Current Node: {node.ToInfix()}");
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                    //Write($"Pushing {node.Children[i]} {stack.Count}");
                }


                if (node.Token.Value != "derive")
                {
                    //Write($"Skipping Node: {node.ToInfix()}");
                    continue;
                }

                if (node.Children[0].IsAddition() || node.Children[0].IsSubtraction())
                {
                    if (debug)
                    {
                        string f_x = node.Children[0].Children[0].ToInfix();
                        string g_x = node.Children[0].Children[1].ToInfix();
                        Write($"\td/d{v}[ {f_x} ± {g_x} ] -> d/d{v}( {f_x} ) ± d/d{v}( {g_x} )");
                    }
                    else
                    {
                        Write("\td/dx[ f(x) ± g(x) ] -> d/dx( f(x) ) ± d/dx( g(x) )");
                    }

                    GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                    GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                    //Recurse explicitly down these branches
                    stack.Push(node.Children[0].Children[0]);
                    stack.Push(node.Children[0].Children[1]);
                    //Delete myself from the tree
                    node.Remove();
                }
                else if (node.Children[0].IsNumber() || node.Children[0].IsConstant() ||
                         (node.Children[0].IsVariable() && node.Children[0].Token.Value != variable.Token.Value) ||
                         node.IsSolveable())
                {
                    if (debug)
                    {
                        Write($"\td/d{v}[ {node.Children[0].ToInfix()} ] -> 0");
                    }
                    else
                    {
                        Write("\td/dx[ c ] -> 0");
                    }

                    node.Children[0].Parent = null;
                    RPN.Node temp = new RPN.Node(0);
                    //Remove myself from the tree
                    node.Remove(temp);
                }
                else if (node.Children[0].IsVariable() && node.Children[0].Token.Value == variable.Token.Value)
                {
                    if (debug)
                    {
                        Write($"\td/d{v}[ {node.Children[0].ToInfix()} ] -> 1");
                    }
                    else
                    {
                        Write("\td/dx[ x ] -> 1");
                    }

                    node.Children[0].Parent = null;
                    RPN.Node temp = new RPN.Node(1);
                    //Remove myself from the tree
                    node.Remove(temp);
                }
                else if (node.Children[0].IsMultiplication())
                {
                    //Both numbers
                    if (node.Children[0].Children[0].IsNumberOrConstant() &&
                        node.Children[0].Children[1].IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            Write(
                                $"\td/d{v}[ {node.Children[0].Children[0].ToInfix()} * {node.Children[0].Children[1].ToInfix()} ] -> 0");
                        }
                        else
                        {
                            Write("\td/dx[ c_0 * c_1 ] -> 0");
                        }

                        RPN.Node temp = new RPN.Node(0);
                        //Remove myself from the tree
                        node.Remove(temp);
                    }
                    //Constant multiplication - 0
                    else if (node.Children[0].Children[0].IsNumberOrConstant() &&
                             node.Children[0].Children[1].IsExpression())
                    {
                        if (debug)
                        {
                            Write(
                                $"\td/d{v}[ {node.Children[0].Children[1].ToInfix()} * {node.Children[0].Children[0].ToInfix()}] -> d/d{v}[ {node.Children[0].Children[1].ToInfix()} ] * {node.Children[0].Children[0].ToInfix()}");
                        }
                        else
                        {
                            Write("\td/dx[ f(x) * c] -> d/dx[ f(x) ] * c");
                        }

                        GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                        //Recurse explicitly down these branches
                        stack.Push(node.Children[0].Children[1]);
                        //Remove myself from the tree
                        node.Remove();
                    }
                    //Constant multiplication - 1
                    else if (node.Children[0].Children[1].IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            string constant = node.Children[0].Children[1].ToInfix();
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ {constant} * {expr}] -> {constant} * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ c * f(x)] -> c * d/dx[ f(x) ]");
                        }

                        GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                        //Recurse explicitly down these branches
                        stack.Push(node.Children[0].Children[0]);

                        //Remove myself from the tree
                        node.Remove();
                    }
                    //Product Rule [Two expressions] 
                    else
                    {
                        RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                        RPN.Node fNode = node.Children[0].Children[0];
                        RPN.Node gNode = node.Children[0].Children[1];

                        if (debug)
                        {
                            string f = fNode.ToInfix();
                            string g = gNode.ToInfix();
                            Write($"\td/d{v}[ {f} * {g} ] -> {f} * d/d{v}[ {g} ] + d/d{v}[ {f} ] * {g}");
                        }
                        else
                        {
                            Write($"\td/dx[ f(x) * g(x) ] -> f(x) * d/dx[ g(x) ] + d/dx[ f(x) ] * g(x)");
                        }

                        RPN.Node fDerivative = new RPN.Node(new[] { Clone(fNode) }, _derive);
                        RPN.Node gDerivative = new RPN.Node(new[] { Clone(gNode) }, _derive);

                        RPN.Node multiply1 = new RPN.Node(new[] { gDerivative, fNode }, multiply);
                        RPN.Node multiply2 = new RPN.Node(new[] { fDerivative, gNode }, multiply);

                        RPN.Node add = new RPN.Node(new[] { multiply1, multiply2 },
                            new RPN.Token("+", 2, RPN.Type.Operator));

                        //Remove myself from the tree
                        node.Remove(add);

                        //Explicit recursion
                        stack.Push(fDerivative);
                        stack.Push(gDerivative);
                    }
                }
                else if (node.Children[0].IsDivision())
                {
                    //Quotient Rule
                    RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node numerator = node.Children[0].Children[1];
                    RPN.Node denominator = node.Children[0].Children[0];

                    RPN.Node numeratorDerivative = new RPN.Node(new[] { Clone(numerator) }, _derive);
                    RPN.Node denominatorDerivative = new RPN.Node(new[] { Clone(denominator) }, _derive);

                    RPN.Node multiplicationOne = new RPN.Node(new[] { numeratorDerivative, denominator }, multiply);
                    RPN.Node multiplicationTwo = new RPN.Node(new[] { denominatorDerivative, numerator }, multiply);

                    RPN.Node subtraction = new RPN.Node(new[] { multiplicationTwo, multiplicationOne },
                        new RPN.Token("-", 2, RPN.Type.Operator));

                    RPN.Node denominatorSquared = new RPN.Node(new[] { new RPN.Node(2), Clone(denominator) },
                        new RPN.Token("^", 2, RPN.Type.Operator));

                    if (debug)
                    {
                        string n = numerator.ToInfix();
                        string d = denominator.ToInfix();
                        Write($"\td/d{v}[ {n} / {d} ] -> [ d/d{v}( {n} ) * {d} - {n} * d/d{v}( {d} ) ]/{d}^2");
                    }
                    else
                    {
                        //d/dx[ f(x)/g(x) ] = [ g(x) * d/dx( f(x)) - f(x) * d/dx( g(x) )]/ g(x)^2
                        Write($"\td/dx[ f(x) / g(x) ] -> [ d/dx( f(x) ) * g(x) - f(x) * d/dx( g(x) ) ]/g(x)^2");
                    }

                    //Replace in tree
                    node.Children[0].Replace(numerator, subtraction);
                    node.Children[0].Replace(denominator, denominatorSquared);
                    //Delete myself from the tree
                    node.Remove();

                    //Explicitly recurse down these branches
                    stack.Push(subtraction);
                }
                //Exponents! 
                else if (node.Children[0].IsExponent())
                {
                    RPN.Node baseNode = node.Children[0].Children[1];
                    RPN.Node power = node.Children[0].Children[0];
                    if ((baseNode.IsVariable() || baseNode.IsFunction() || baseNode.IsExpression()) &&
                        power.IsNumberOrConstant() && baseNode.Token.Value == variable.Token.Value)
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> {p} * {b}^({p} - 1)");
                        }
                        else
                        {
                            Write("\td/dx[ x^n ] -> n * x^(n - 1)");
                        }

                        RPN.Node powerClone = Clone(power);
                        RPN.Node exponent;

                        if (!powerClone.IsNumber())
                        {
                            //1
                            RPN.Node one = new RPN.Node(1);

                            //(n - 1)
                            RPN.Node subtraction = new RPN.Node(new[] { one, powerClone },
                                new RPN.Token("-", 2, RPN.Type.Operator));

                            //x^(n - 1) 
                            exponent = new RPN.Node(new RPN.Node[] { subtraction, baseNode },
                                new RPN.Token("^", 2, RPN.Type.Operator));
                        }
                        else
                        {
                            exponent = new RPN.Node(new RPN.Node[] { new RPN.Node(powerClone.GetNumber() - 1), baseNode },
                                new RPN.Token("^", 2, RPN.Type.Operator));
                        }

                        RPN.Node multiplication = new RPN.Node(new[] { exponent, power },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiplication);

                        //Delete self from the tree
                        node.Remove();
                    }
                    else if ((baseNode.IsFunction() || baseNode.IsExpression()) && power.IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> {p} * {b}^({p} - 1) * d/d{v}[ {b} ]");
                        }
                        else
                        {
                            Write("\td/dx[ f(x)^n ] -> n * f(x)^(n - 1) * d/dx[ f(x) ]");
                        }

                        RPN.Node bodyDerive = new RPN.Node(new[] { Clone(baseNode) }, _derive);

                        RPN.Node powerClone = Clone(power);

                        RPN.Node subtraction;
                        if (power.IsConstant())
                        {
                            RPN.Node one = new RPN.Node(1);
                            subtraction = new RPN.Node(new[] { one, powerClone },
                                new RPN.Token("-", 2, RPN.Type.Operator));
                        }
                        else
                        {
                            subtraction = new RPN.Node(power.GetNumber() - 1);
                        }

                        //Replace n with (n - 1) 
                        RPN.Node exponent = new RPN.Node(new RPN.Node[] { subtraction, baseNode },
                            new RPN.Token("^", 2, RPN.Type.Operator));

                        RPN.Node temp = new RPN.Node(new[] { exponent, power }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiply = new RPN.Node(new[] { bodyDerive, temp },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiply);

                        //Delete self from the tree
                        node.Remove();
                        stack.Push(bodyDerive);
                    }
                    else if (baseNode.IsConstant("e"))
                    {
                        if (debug)
                        {
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ e^{p} ] -> d/d{v}[ {p} ] * e^{p}");
                        }
                        else
                        {
                            Write("\td/dx[ e^g(x) ] -> d/dx[ g(x) ] * e^g(x)");
                        }

                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node powerDerivative = new RPN.Node(new[] { Clone(power) }, _derive);
                        RPN.Node multiply = new RPN.Node(new[] { powerDerivative, exponent },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();
                        stack.Push(powerDerivative);
                    }
                    else if (baseNode.IsNumberOrConstant() && (power.IsExpression() || power.IsVariable()))
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> ln({b}) * {b}^{p} * d/d{v}[ {p} ]");
                        }
                        else
                        {
                            Write($"\td/dx[ b^g(x) ] -> ln(b) * b^g(x) * d/dx[ g(x) ]");
                        }

                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node ln = new RPN.Node(new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                        RPN.Node powerDerivative = new RPN.Node(new[] { Clone(power) }, _derive);
                        RPN.Node temp = new RPN.Node(new[] { exponent, ln }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiply = new RPN.Node(new[] { temp, powerDerivative },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();
                        stack.Push(powerDerivative);
                    }
                    else
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> {b}^{p} * d/d{v}[ {b} * ln( {p} ) ]");
                        }
                        else
                        {
                            Write("\td/dx[ f(x)^g(x) ] -> f(x)^g(x) * d/dx[ g(x) * ln( f(x) ) ]");
                        }

                        RPN.Node exponent = Clone(baseNode.Parent);
                        RPN.Node ln = new RPN.Node(new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                        RPN.Node temp = new RPN.Node(new[] { Clone(power), ln },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node derive = new RPN.Node(new[] { temp }, _derive);
                        RPN.Node multiply = new RPN.Node(new[] { exponent, derive },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();

                        stack.Push(derive);
                    }
                }

                #region Trig

                else if (node.Children[0].IsFunction("sin"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ sin({expr}) ] -> cos({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ sin(g(x)) ] -> cos(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];

                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node cos = new RPN.Node(new[] { body }, new RPN.Token("cos", 1, RPN.Type.Function));

                    RPN.Node multiply = new RPN.Node(new[] { cos, bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("cos"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ cos({expr}) ] -> -sin({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ cos(g(x)) ] -> -sin(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sin = new RPN.Node(new[] { body }, new RPN.Token("sin", 1, RPN.Type.Function));
                    RPN.Node negativeOneMultiply = new RPN.Node(new[] { new RPN.Node(-1), sin },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiply = new RPN.Node(new[] { negativeOneMultiply, bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("tan"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ tan({expr}) ] -> sec({expr})^2 * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ tan(g(x)) ] -> sec(g(x))^2 * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sec = new RPN.Node(new[] { body }, new RPN.Token("sec", 1, RPN.Type.Function));
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), sec },
                        new RPN.Token("^", 2, RPN.Type.Operator));

                    RPN.Node multiply = new RPN.Node(new[] { exponent, bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("sec"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ sec({expr}) ] -> tan({expr}) * sec({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ sec(g(x)) ] -> tan(g(x)) * sec(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Token multiplyToken = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sec = node.Children[0];
                    RPN.Node tan = new RPN.Node(new[] { Clone(body) }, new RPN.Token("tan", 1, RPN.Type.Function));
                    RPN.Node temp = new RPN.Node(new[] { sec, tan }, multiplyToken);
                    RPN.Node multiply = new RPN.Node(new[] { bodyDerive, temp }, multiplyToken);

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("csc"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ csc({expr}) ] -> - cot({expr}) * csc({expr}) * d/d{v}[ {expr} ] ");
                    }
                    else
                    {
                        Write("\td/dx[ csc(g(x)) ] -> - cot(g(x)) * csc(g(x)) * d/dx[ g(x) ] ");
                    }

                    RPN.Token multiplyToken = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node csc = node.Children[0];
                    RPN.Node cot = new RPN.Node(new[] { Clone(body) }, new RPN.Token("cot", 1, RPN.Type.Function));

                    RPN.Node temp = new RPN.Node(new[] { csc, cot }, multiplyToken);
                    RPN.Node multiply = new RPN.Node(new[] { temp, bodyDerive }, multiplyToken);

                    node.Replace(node.Children[0], new RPN.Node(new[] { new RPN.Node(-1), multiply }, multiplyToken));
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("cot"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ cot({expr}) ] -> -csc({expr})^2 * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ cot(g(x)) ] -> -csc(g(x))^2 * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node csc = new RPN.Node(new[] { body }, new RPN.Token("csc", 1, RPN.Type.Function));
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), csc },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node temp = new RPN.Node(new[] { new RPN.Node(-1), exponent },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiply =
                        new RPN.Node(new[] { bodyDerive, temp }, new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arcsin"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arcsin({expr}) ] -> d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arcsin(g(x)) ] -> d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { exponent, new RPN.Node(1) },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node division =
                        new RPN.Node(new[] { sqrt, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arccos"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arccos({expr}) ] -> -1 * d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arccos(g(x)) ] -> -1 * d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { exponent, new RPN.Node(1) },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node division =
                        new RPN.Node(new[] { sqrt, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    RPN.Node multiplication = new RPN.Node(new[] { new RPN.Node(-1), division },
                        new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiplication);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arctan"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arctan({expr}) ] -> d/d{v}[ {expr} ]/(1 + {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arctan(g(x)) ] -> d/dx[ g(x) ]/(1 + g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node add = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("+", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { add, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arccot"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arccot({expr}) ] -> -1 * d/d{v}[ {expr} ]/(1 + {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arccot(g(x)) ] -> -1 * d/dx[ g(x) ]/(1 + g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node add = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("+", 2, RPN.Type.Operator));
                    RPN.Node multiplication = new RPN.Node(new[] { new RPN.Node(-1), bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { add, multiplication },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arcsec"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arcsec({expr}) ] -> d/d{v}[ {expr} ]/( {expr} * sqrt({expr}^2 - 1 ) )");
                    }
                    else
                    {
                        Write("\td/dx[ arcsec(g(x)) ] -> d/dx[ g(x) ]/( g(x) * sqrt(g(x)^2 - 1 ) )");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node abs = new RPN.Node(new[] { body.Clone() }, new RPN.Token("abs", 1, RPN.Type.Function));
                    RPN.Node denominator =
                        new RPN.Node(new[] { sqrt, abs }, new RPN.Token("*", 2, RPN.Type.Operator));

                    RPN.Node division = new RPN.Node(new[] { denominator, bodyDerive },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arccsc"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arccsc({expr}) ] -> -1 * d/d{v}[ {expr} ]/( {expr} * sqrt({expr}^2 - 1 ) )");
                    }
                    else
                    {
                        Write("\td/dx[ arccsc(g(x)) ] -> -1 * d/dx[ g(x) ]/( g(x) * sqrt(g(x)^2 - 1 ) )");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node abs = new RPN.Node(new[] { body.Clone() }, new RPN.Token("abs", 1, RPN.Type.Function));
                    RPN.Node denominator =
                        new RPN.Node(new[] { sqrt, abs }, new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiplication = new RPN.Node(new[] { new RPN.Node(-1), bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { denominator, multiplication },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }

                #endregion

                else if (node.Children[0].IsSqrt())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\tsqrt({expr}) -> {expr}^0.5");
                    }
                    else
                    {
                        Write("\tsqrt(g(x)) -> g(x)^0.5");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node OneHalf = new RPN.Node(0.5);
                    RPN.Node exponent = new RPN.Node(new[] { OneHalf, body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    node.Replace(node.Children[0], exponent);
                    stack.Push(node);
                }
                else if (node.Children[0].IsLn())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ ln({expr}) ] -> d/d{v}[ {expr} ]/{expr}");
                    }
                    else
                    {
                        Write("\td/dx[ ln(g(x)) ] -> d/dx[ g(x) ]/g(x)");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node division =
                        new RPN.Node(new[] { body, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsLog())
                {
                    RPN.Token ln = new RPN.Token("ln", 1, RPN.Type.Function);

                    RPN.Node power = node.Children[0].Children[1];
                    RPN.Node body = node.Children[0].Children[0];

                    if (debug)
                    {
                        string b = body.ToInfix();
                        string p = power.ToInfix();
                        Write($"\td/d{v}[ log({b},{p}) ] -> d/d{v}[ {p} ]/({p} * ln({b}))");
                    }
                    else
                    {
                        Write("\td/dx[ log(b,g(x)) ] -> d/dx[ g(x) ]/(g(x) * ln(b))");
                    }

                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node multiply = new RPN.Node(new[] { body, new RPN.Node(new[] { power }, ln) },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { multiply, bodyDerive },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsAbs())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\tabs({expr}) -> sqrt( {expr}^2 )");
                    }
                    else
                    {
                        Write("\tabs(g(x)) -> sqrt( g(x)^2 )");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { exponent }, new RPN.Token("sqrt", 1, RPN.Type.Function));

                    node.Replace(node.Children[0], sqrt);
                    stack.Push(node);
                }
                else if (node.Children[0].IsFunction("total"))
                {
                    Write("\tExploding total");
                    explode(node.Children[0]);
                    stack.Push(node);
                    stack.Push(node);
                }
                else if (node.Children[0].IsFunction("avg"))
                {
                    Write("\tExploding avg");
                    explode(node.Children[0]);
                    stack.Push(node);
                }
                else
                {
                    throw new NotImplementedException(
                        $"Derivative of {node.Children[0].ToInfix()} not known at this time.");
                }
            }
        }

        private void Algebra(RPN.Node node, ref RPN.Node equality)
        {
            if (!node.IsFunction("solve")) return;

            string hash = string.Empty;

            while (hash != node.GetHash()) {
                hash = node.GetHash();

                if (!node[0].IsNumberOrConstant() && node[0].IsSolveable())
                {
                    Write("\tSolving for one Side.");
                    Solve(node[0]);
                }

                if (!node[1].IsNumberOrConstant() && node[1].IsSolveable())
                {
                    Write("\tSolving for one Side.");
                    Solve(node[1]);
                }

                //TODO: Quadratic Formula


                if (node[0].IsNumberOrConstant() || node[0].IsSolveable())
                {
                    if (node[1].IsAddition())
                    {
                        RPN.Node temp = null;

                        if (node[1, 0].IsNumberOrConstant())
                        {
                            Write("\tf(x) + c = c_1 -> f(x) = c_1 - c");
                            temp = node[1, 0];
                        }
                        else if (node[1, 1].IsNumberOrConstant())
                        {
                            Write("\tc + f(x) = c_1 -> f(x) = c_1 - c");
                            temp = node[1, 1];
                        }

                        if (temp is null)
                        {
                            return;
                        }

                        node[1].Replace(temp, new RPN.Node(0));
                        RPN.Node subtraction = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("-", 2, RPN.Type.Operator));
                        node.Replace(node[0], subtraction);

                        Simplify(node[1], SimplificationMode.Addition);
                    }
                    else if (node[1].IsSubtraction())
                    {
                        RPN.Node temp = null;

                        if (node[1, 0].IsNumberOrConstant())
                        {
                            Write("\t[f(x) - c = c_1] -> [f(x) = c_1 - c]");
                            temp = node[1, 0];

                            node[1].Replace(temp, new RPN.Node(0));
                            RPN.Node addition = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("+", 2, RPN.Type.Operator));
                            node.Replace(node[0], addition);
                        }
                        else if (node[1, 1].IsNumberOrConstant())
                        {
                            Write("\t[c - f(x) = c_1] -> [- f(x) = c_1 - c]");
                            temp = node[1, 1];

                            node[1].Replace(temp, new RPN.Node(0));
                            RPN.Node subtraction = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("-", 2, RPN.Type.Operator));
                            node.Replace(node[0], subtraction);
                        }

                        Simplify(node, SimplificationMode.Subtraction);
                    }
                    else if (node[1].IsMultiplication())
                    {
                        RPN.Node temp = null;
                        if (node[1, 0].IsNumberOrConstant())
                        {
                            temp = node[1, 0];
                            Write("\tf(x)c = g(x) -> f(x) = g(x)/c");
                        }
                        else if (node[1, 1].IsNumberOrConstant())
                        {
                            temp = node[1, 1];
                            Write("\tcf(x) = g(x) -> f(x) = g(x)/c");
                            ; }

                        if (temp != null)
                        {
                            node[1].Replace(temp, new RPN.Node(1));
                            RPN.Node division = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("/", 2, RPN.Type.Operator));
                            node.Replace(node[0], division);
                        }
                    }
                    else if (node[1].IsDivision())
                    {
                        //TODO: f(x)/c = g(x) -> f(x) = c * g(x)
                        if (node[1, 0].IsNumberOrConstant())
                        {
                            RPN.Node temp = node[1, 0];

                            node[1].Replace(temp, new RPN.Node(1));
                            RPN.Node multiplication = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("*", 2, RPN.Type.Operator));
                            node.Replace(node[0], multiplication);
                            Simplify(node, SimplificationMode.Division);
                        }
                    }
                }

                //TODO: Make this equality 
                //This might mean swapping after the equality is completed! 

                //f(x)^2 = g(x)
                if (node[1].IsExponent() && node[1, 0].IsNumber(2))
                {
                    Write("\tf(x)^2 = g(x) -> abs(f(x)) = sqrt(g(x))");

                    RPN.Node abs = new RPN.Node(new[] { node[1, 1] }, new RPN.Token("abs", 1, RPN.Type.Function));
                    RPN.Node sqrt = new RPN.Node(new[] { node[0] }, new RPN.Token("sqrt", 1, RPN.Type.Function));

                    node.Replace(node[1], abs);
                    node.Replace(node[0], sqrt);
                }

                //TODO: ln(f(x)) = g(x) -> e^ln(f(x)) = e^g(x) -> f(x) = e^g(x)
            }
        }


        /// <summary>
        /// converts a vardiac function into a simpler AST
        /// </summary>
        /// <param name="node"></param>
        private void explode(RPN.Node node)
        {
            RPN.Token add = new RPN.Token("+", 2, RPN.Type.Operator);
            RPN.Token division = new RPN.Token("/", 2, RPN.Type.Operator);
            RPN.Token multiplication = new RPN.Token("*", 2, RPN.Type.Operator);

            //convert a sum to a series of additions
            if (node.IsFunction("internal_sum") || node.IsFunction("total"))
            {
                if (node.Children.Count == 2)
                {
                    node.Replace(new RPN.Token("+", 2, RPN.Type.Operator));
                    node.Children.Reverse();
                }
                else
                {
                    Assign(node, gen(add));
                }
            }
            else if (node.IsFunction("internal_product"))
            {
                if (node.Children.Count == 2)
                {
                    node.Replace(new RPN.Token("*", 2, RPN.Type.Operator));
                    node.Children.Reverse();
                }
                else
                {
                    Assign(node, gen(multiplication));
                }
            }
            //convert an avg to a series of additions and a division
            else if (node.IsFunction("avg"))
            {
                if (node.Children.Count == 1)
                {
                    node.Remove();
                    return;
                }

                List<RPN.Token> results = new List<RPN.Token>(node.Children.Count);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    results.AddRange(node.Children[i].ToPostFix());
                }

                results.Add(new RPN.Token("total", node.Children.Count, RPN.Type.Function));
                results.Add(new RPN.Token(node.Children.Count));
                results.Add(division);
                RPN.Node temp = Generate(results.ToArray());
                explode(temp.Children[1]);
                Assign(node, temp);
            }

            RPN.Node gen(RPN.Token token)
            {
                
                if (node.Children.Count == 1)
                {
                    node.Remove();
                    return null;
                }

                //TODO: Convert this from using ToPostFix to automatically generating a new correct tree!

                //Prep stage
                if (token.IsAddition())
                {
                    node.Children.Reverse();
                }

                Queue<RPN.Node> additions = new Queue<RPN.Node>(node.Children.Count);
                additions.Enqueue(new RPN.Node(new[] { node[0], node[1] }, token));
                for (int i = 2; i + 1 < node.Children.Count; i += 2)
                {
                    additions.Enqueue(new RPN.Node(new[] { node[i], node[i + 1] }, token));
                }

                if (node.Children.Count % 2 == 1 && node.Children.Count > 2)
                {
                    additions.Enqueue(node.Children[node.Children.Count - 1]);
                }

                while (additions.Count > 1)
                {

                    RPN.Node[] temp = new[] { additions.Dequeue(), additions.Dequeue() };
                    temp.Reverse();
                    additions.Enqueue(new RPN.Node(temp, token));
                }

                //This should nearly always result in a return 
                if (additions.Count == 1)
                {
                    additions.Peek().Children.Reverse();
                    Swap(additions.Peek());
                    return additions.Dequeue();
                }

                //This is fall back code! 
                

                List<RPN.Token> results = new List<RPN.Token>(node.Children.Count);
                results.AddRange(node.Children[0].ToPostFix());
                results.AddRange(node.Children[1].ToPostFix());
                results.Add(token);

                for (int i = 2; i < node.Children.Count; i += 2)
                {
                    results.AddRange(node.Children[i].ToPostFix());
                    if ((i + 1) < node.Children.Count)
                    {
                        results.AddRange(node.Children[i + 1].ToPostFix());
                        results.Add(token);
                    }
                    results.Add(token);
                }

                return Generate(results.ToArray());

            }
        }


        /// <summary>
        /// Converts a series of multiplications, additions, or subtractions 
        /// into a new node to see if there are additional simplifications that can be made
        /// </summary>
        /// <param name="node"></param>
        private void expand(RPN.Node node)
        {
            //TODO:
            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();

                //Propagate
                for (int i = 0; i < node.Children.Count; i++)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                if (node.IsAddition())
                {
                    if (node.isRoot || !node.Parent.IsFunction("internal_sum"))
                    {
                        //This prevents a stupid allocation and expansion and compression cycle
                        if (node[0].IsAddition() || node[1].IsAddition())
                        {
                            RPN.Node sum = new RPN.Node(node.Children.ToArray(),
                                new RPN.Token("internal_sum", node.Children.Count, RPN.Type.Function));
                            Assign(node, sum);
                        }
                    }
                    else if (node.Parent.IsFunction("internal_sum"))
                    {
                        node.Parent.RemoveChild(node);
                        node.Parent.AddChild(node.Children[0]);
                        node.Parent.AddChild(node.Children[1]);
                    }
                }
                else if (node.IsMultiplication())
                {
                    if (node.isRoot || !node.Parent.IsFunction("internal_product"))
                    {
                        //This prevents a stupid allocation and expansion and compression cycle
                        if (node[0].IsMultiplication() || node[1].IsMultiplication())
                        {
                            RPN.Node product = new RPN.Node(node.Children.ToArray(),
                                new RPN.Token("internal_product", node.Children.Count, RPN.Type.Function));
                            Assign(node, product);
                        }
                    }
                    else if (node.Parent.IsFunction("internal_product"))
                    {
                        node.Parent.RemoveChild(node);
                        node.Parent.AddChild(node.Children[0]);
                        node.Parent.AddChild(node.Children[1]);
                    }
                }
                //Convert a subtraction into an addition with multiplication by negative one ????
                //We would also need to add a corelating thing in the simplify method
            }
        }

        private void compress(RPN.Node node)
        {
            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();
                for (int i = 0; i < node.Children.Count; i++)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                if (node.IsFunction("internal_sum") || node.IsFunction("internal_product"))
                {
                    explode(node);
                }
            }
        }

        private void GenerateDerivativeAndReplace(RPN.Node child)
        {
            RPN.Node temp = new RPN.Node(new[] { child.Clone() }, _derive);
            temp.Parent = child.Parent;

            child.Parent?.Replace(child, temp);

            child.Parent = temp;
        }

        private RPN.Node Clone(RPN.Node node)
        {
            return node.Clone();
        }

        /// <summary>
        /// This is the preferred method of setting the Root
        /// when you are simplifying and not creating a NaN root. 
        /// </summary>
        /// <param name="node"></param>
        private void SetRoot(RPN.Node node)
        {
            node.Parent = null;
            Root = node;
        }

        private void Assign(RPN.Node node, RPN.Node assign)
        {
            try
            {
                if (node is null || assign is null)
                {
                    return;
                }

                if (node.isRoot)
                {
                    SetRoot(assign);
                    return;
                }

                node.Parent.Replace(node, assign);
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new Exception($"node: {node.ToInfix()}, assign: {assign.ToInfix()}", ex);
            }
        }

        private void Write(string message)
        {
            logger.Log(Channels.Debug, message);
        }

        private void stdout(string message)
        {
            logger.Log(Channels.Output, message);
        }
    }


    public struct OptimizationTracker
    {
        public string Hash;
        public int count;
    }

    public class OptimizerRuleSet
    {
        public static Dictionary<AST.SimplificationMode, List<Rule>> ruleSet { get; private set; }
        public static Dictionary<AST.SimplificationMode, Rule> setRule { get; private set; }

        private Dictionary<AST.SimplificationMode, Stopwatch> ruleSetTracker;
        private Dictionary<AST.SimplificationMode, Stopwatch> canExecuteTracker;
        private Dictionary<AST.SimplificationMode, int> hits; 

        public bool debug;

        private Logger logger;
        private static HashSet<Rule> contains;

        public OptimizerRuleSet(Logger logger, bool debugMode = false)
        {
            if (ruleSet == null)
            {
                ruleSet = new Dictionary<AST.SimplificationMode, List<Rule>>();
            }

            if (contains == null)
            {
                contains = new HashSet<Rule>();
            }

            if (setRule == null)
            {
                setRule = new Dictionary<AST.SimplificationMode, Rule>();
            }

            hits = new Dictionary<AST.SimplificationMode, int>();
            debug = debugMode;
            this.logger = logger;

            if (debug)
            {
                ruleSetTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
                canExecuteTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
            }
        }

        public void Add(AST.SimplificationMode mode, Rule rule)
        {
            //If the rule already exists we should not
            //add it!
            if (contains.Contains(rule))
            {
                return;
            }

            contains.Add(rule);
            rule.Logger += Write;

            //When the key already exists we just add onto the existing list 
            if (ruleSet.ContainsKey(mode))
            {
                List<Rule> rules = ruleSet[mode];
                rules.Add(rule);
                ruleSet[mode] = rules;
                return;
            }
            //When the key does not exist we should create a list and add the rule onto it
            ruleSet.Add(mode, new List<Rule> {rule});
            hits.Add(mode, 0);

            if (debug)
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                ruleSetTracker.Add(mode, sw);

                Stopwatch sw2 = new Stopwatch();
                sw2.Reset();
                canExecuteTracker.Add(mode, sw2);
            }
        }

        public void AddSetRule(AST.SimplificationMode mode, Rule rule)
        {
            if (!this.HasSetRule(mode))
            {
                setRule.Add(mode, rule);
            }
        }

        public List<Rule> Get(AST.SimplificationMode mode)
        {
            return ruleSet[mode];
        }

        public Rule GetSetRule(AST.SimplificationMode mode)
        {
            return setRule[mode];
        }

        /// <summary>
        /// This executes any possible simplification in the appropriate
        /// set.
        /// </summary>
        /// <param name="mode">The set to look in</param>
        /// <param name="node">The node to apply over</param>
        /// <returns>A new node that is the result of the application of the rule or null
        /// when no rule could be run</returns>
        public RPN.Node Execute(AST.SimplificationMode mode, RPN.Node node)
        {
            if (!ruleSet.ContainsKey(mode))
            {
                throw new KeyNotFoundException("The optimization set was not found");
            }

            List<Rule> rules = ruleSet[mode];
            Stopwatch sw = null;

            if (debug)
            {
                if (ruleSetTracker == null)
                {
                    ruleSetTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
                }

                if (ruleSetTracker.ContainsKey(mode))
                {
                    sw = ruleSetTracker[mode];
                }
                else
                {
                    sw = new Stopwatch();
                    ruleSetTracker.Add(mode, sw);
                }

                sw.Start();
            }

            if (!hits.ContainsKey(mode))
            {
                hits.Add(mode, 0);
            }

            for (int i = 0; i < rules.Count; i++)
            {
                Rule rule = rules[i];
                if (rule.CanExecute(node))
                {
                    RPN.Node temp = rule.Execute(node);
                    if (debug)
                    {
                        sw.Stop();
                    }

                    if (rule.DebugMode())
                    {
                        Write("The output of : " + temp.ToInfix());
                    }

                    hits[mode] = hits[mode] + 1;
                    return temp;
                }
            }

            if (debug)
            {
                sw.Stop();
            }

            return null;
        }

        public bool ContainsSet(AST.SimplificationMode mode)
        {
            return ruleSet.ContainsKey(mode);
        }

        public bool HasSetRule(AST.SimplificationMode mode)
        {
            return setRule.ContainsKey(mode);
        }

        public bool CanRunSet(AST.SimplificationMode mode, RPN.Node node)
        {
            if (debug)
            {
                if (canExecuteTracker == null)
                {
                    canExecuteTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
                }

                if (!canExecuteTracker.ContainsKey(mode))
                {
                    Stopwatch temp = new Stopwatch();
                    temp.Reset();
                    canExecuteTracker.Add(mode, temp);
                }
            }

            bool result = false;
            Stopwatch sw = null;
            if (debug)
            {
                sw = canExecuteTracker[mode];
                sw.Start();
            }

            result = ContainsSet(mode) && (!HasSetRule(mode) || GetSetRule(mode).CanExecute(node));
            if (debug)
            {
                sw.Stop();
            }
            return result;
        }

        private void Write(object obj, string message)
        {
            Write(message);
        }

        private void Write(string message)
        {
            logger.Log(Channels.Debug, message);
        }

        public override string ToString()
        {
            return this.ToString(null);
        }

        public string ToString(string format)
        {
            Config config = new Config() { Format = Format.Default, Title = "Simplification Rule Set Overview" };
            if (format == "%M")
            {
                config.Format = Format.MarkDown;
            }

            Tables<string> ruleTables = new Tables<string>(config);
            ruleTables.Add(new Schema("Name"));
            ruleTables.Add(new Schema("Count")); 
            ruleTables.Add(new Schema("Set Rule"));
            ruleTables.Add(new Schema("Execution Time (ms | Ticks)"));
            ruleTables.Add(new Schema("Check Time (ms | Ticks)"));
            ruleTables.Add(new Schema("Hits"));

            int totalRules = 0;
            long totalExecutionElapsedMilliseconds = 0;
            long totalExecutionElappsedTicks = 0;
            long totalCheckElapsedMilliseconds = 0;
            long totalCheckElapsedTicks = 0;
            int totalHits = 0;
            foreach(var KV in ruleSet)
            {
                string executionTime = "-";
                string checkTime = "-";
                string hit = "-";

                if (debug && ruleSetTracker != null && ruleSetTracker.ContainsKey(KV.Key))
                {
                    executionTime = ruleSetTracker[KV.Key].ElapsedMilliseconds.ToString() + " | " + ruleSetTracker[KV.Key].ElapsedTicks.ToString("N0");
                    checkTime = canExecuteTracker[KV.Key].ElapsedMilliseconds.ToString() + " | " + canExecuteTracker[KV.Key].ElapsedTicks.ToString("N0");

                    totalExecutionElapsedMilliseconds += ruleSetTracker[KV.Key].ElapsedMilliseconds;
                    totalExecutionElappsedTicks += ruleSetTracker[KV.Key].ElapsedTicks;

                    totalCheckElapsedMilliseconds += canExecuteTracker[KV.Key].ElapsedMilliseconds;
                    totalCheckElapsedTicks += canExecuteTracker[KV.Key].ElapsedTicks;
                }

                if (hits.ContainsKey(KV.Key))
                {
                    hit = hits[KV.Key].ToString();
                    totalHits += hits[KV.Key];
                }

                totalRules += KV.Value.Count;

                string[] row = new string[] { KV.Key.ToString(), KV.Value.Count.ToString(), setRule.ContainsKey(KV.Key) ? "✓" : "X", executionTime, checkTime, hit };
                ruleTables.Add(row);
            }
            string[] total = new string[] {"Total", totalRules.ToString(), "", $"{totalExecutionElapsedMilliseconds} | {totalExecutionElappsedTicks}", $"{totalCheckElapsedMilliseconds} | {totalCheckElapsedTicks}", totalHits.ToString()};
            ruleTables.Add(total);

            return ruleTables.ToString();
        }
    }

}