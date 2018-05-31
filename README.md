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
* Primitive Implicit multiplication support
* All  of this alongside with the ability to easily add your own operators and functions at runtime!
