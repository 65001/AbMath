![Build Status](https://github.com/65001/AbMath/workflows/.NET%20Core/badge.svg)
# AbMath
AbMath is a library that lets you compute an arbitrary mathematical expression and is designed to be 
efficient enough to be used in graphing calculators. 

It implements algorithims constructs such as Reverse Polish Notation, the shunting yard alogrithim, and apportionment. 

# Usage
Using AbMath is as simple as adding it as a library and importing it into your codebase! 
Computing any math can be done in around four lines of code. 
```cs
RPN test = new RPN("3 + 2 + 7");
test.Compute();

PostFix math = new PostFix(test)
math.Compute(); //numeric result is returned here
```

For runtime user defined variables:
```cs
RPN test = new RPN("3 + 2 + x");
test.Compute();

PostFix math = new PostFix(test);

//Set all the unknown variables
if (test.ContainsVariables) {
      for (int i = 0; i < test.Data.Variables.Count; i++)
      {
           //...
           //get a numerical constant from the user
           math.SetVariable(test.Data.Variables[i], a double value);
      }
}

math.Compute(); //numeric result is returned here
```

For generating a table for any given function with the dependent variable being ```x``` in the example:
```cs
//Code adapted from https://github.com/65001/RPN/blob/master/RPN/CLI.cs#L288
double start = 0; //Start point of our table or graph
double end = 10; //End point of our table or graph
double freq = 1; //How often we should sample the function
double DeltaX = end - start;
double n = DeltaX / freq; //The number of itterations we need given the above information

int max = (int)Math.Ceiling(n);

RPN test = new RPN("x^2 + 3");
test.Compute();

PostFix math = new PostFix(test)

 for (int i = 0; i <= max; i++) {
      double RealX = start + i * DeltaX / n; //We do this to minimize floating point drift.
      math.SetVariable("x", RealX);
      double answer = math.Compute();
      math.Reset(); //Unsets all variables
 }
```

## Apportionment 
Apportionment is a way to distribute a fixed integer amount of resources based on population or other data. 
AbMath implements the Hamilton, Hunnington-Hill, Jefferson, and Webster methods of apportionment.

## Reverse Polish Notation / Shunting Yard
[Reverse Polish Notation](https://en.wikipedia.org/wiki/Reverse_Polish_notation) and the [Shunting Yard Algorithim](https://en.wikipedia.org/wiki/Shunting-yard_algorithm) are ways to take user mathematical input and return an answer.
AbMath's implemetation of our Tokenizer and Shunting Yard Alogrithim currentley support the following:
* Unary negative and positive operands 
* Primitive composite function support
* Operators such as +,-,*,/,^,and % (modulo).
* Comparison operators such as >,<,=,and !=.
* Logic operators such as || (or) and "&&" (and).
* All the basic trig functions, max,min, sqrt, round, ln, and log functions.
* Implicit multiplication support
* Variadic function support (functions with a variable number of arguments)
* All  of this alongside with the ability to easily add your own operators and functions at runtime!

## Contributors 
Thanks to [Josh Hayward](https://github.com/josh-hayward)  for his assistance in fixing the [Arity system](https://github.com/65001/AbMath/commit/81cd306a5f5344f404b9e9a1ddb2a70d2faa4c16) which is the basis of 
variadic functions.
