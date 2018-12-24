[![Build Status](https://travis-ci.org/65001/AbMath.svg?branch=master)](https://travis-ci.org/65001/AbMath)

# AbMath
AbMath is a library that implements mathematical constructs such as Reverse Polish Notation, the shunting yard alogrithim, and apportionment. 

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