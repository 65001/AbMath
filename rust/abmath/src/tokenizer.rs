use nom::{
    branch::alt,
    bytes::complete::tag,
    character::complete::{alpha1, char, digit1},
    combinator::{map, opt, recognize},
    sequence::pair,
    IResult,
};
use std::fmt;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Assoc {
    Left,
    Right,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum MathOperator {
    Pow,
    Factorial,
    Divide,
    Multiply,
    Add,
    Subtract,
    GreaterThanOrEqual,
    LessThanOrEqual,
    GreaterThan,
    LessThan,
    Equal,
    NotEqual,
    And,
    Or,
    Mod,
    Scientific,
}

impl MathOperator {
    pub fn from_str(s: &str) -> Option<Self> {
        match s {
            "^" => Some(Self::Pow),
            "!" => Some(Self::Factorial),
            "/" | "÷" => Some(Self::Divide),
            "*" => Some(Self::Multiply),
            "+" => Some(Self::Add),
            "-" | "−" => Some(Self::Subtract),
            ">=" | "≥" => Some(Self::GreaterThanOrEqual),
            "<=" | "≤" => Some(Self::LessThanOrEqual),
            ">" => Some(Self::GreaterThan),
            "<" => Some(Self::LessThan),
            "==" | "=" => Some(Self::Equal),
            "!=" | "≠" | "ne" => Some(Self::NotEqual),
            "&&" | "and" => Some(Self::And),
            "||" | "or" => Some(Self::Or),
            "%" => Some(Self::Mod),
            "E" => Some(Self::Scientific),
            _ => None,
        }
    }

    pub fn weight(&self) -> i32 {
        match self {
            Self::Pow | Self::Factorial | Self::Scientific => 5,
            Self::Divide | Self::Multiply | Self::Mod => 4,
            Self::Add | Self::Subtract => 3,
            Self::GreaterThanOrEqual
            | Self::LessThanOrEqual
            | Self::GreaterThan
            | Self::LessThan
            | Self::Equal => 2,
            Self::NotEqual | Self::And | Self::Or => 1,
        }
    }

    pub fn assoc(&self) -> Assoc {
        match self {
            Self::Pow | Self::Scientific => Assoc::Right,
            _ => Assoc::Left,
        }
    }

    pub fn name(&self) -> &'static str {
        match self {
            Self::Pow => "^",
            Self::Factorial => "!",
            Self::Divide => "/",
            Self::Multiply => "*",
            Self::Add => "+",
            Self::Subtract => "-",
            Self::GreaterThanOrEqual => ">=",
            Self::LessThanOrEqual => "<=",
            Self::GreaterThan => ">",
            Self::LessThan => "<",
            Self::Equal => "==",
            Self::NotEqual => "!=",
            Self::And => "&&",
            Self::Or => "||",
            Self::Mod => "%",
            Self::Scientific => "E",
        }
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum MathFunction {
    Sin,
    Cos,
    Tan,
    Sec,
    Csc,
    Cot,
    Arcsin,
    Arccos,
    Arctan,
    Arcsec,
    Arccsc,
    Arccot,
    Max,
    Min,
    Sqrt,
    Abs,
    Log,
    Ln,
    Pi,
    E,
    List,
    Matrix,
    Round,
    Gcd,
    Lcm,
    Bounded,
    Total,
    Sum,
    Avg,
    Random,
    Rand,
    Seed,
    Binomial,
    Gamma,
    Rad,
    Deg,
    Derivative,
    Integrate,
    Table,
    Solve,
    Plot,
}

impl MathFunction {
    pub fn from_str(s: &str) -> Option<Self> {
        match s.to_lowercase().as_str() {
            "sin" => Some(Self::Sin),
            "cos" => Some(Self::Cos),
            "tan" => Some(Self::Tan),
            "sec" => Some(Self::Sec),
            "csc" => Some(Self::Csc),
            "cot" => Some(Self::Cot),
            "arcsin" => Some(Self::Arcsin),
            "arccos" => Some(Self::Arccos),
            "arctan" => Some(Self::Arctan),
            "arcsec" => Some(Self::Arcsec),
            "arccsc" => Some(Self::Arccsc),
            "arccot" => Some(Self::Arccot),
            "max" => Some(Self::Max),
            "min" => Some(Self::Min),
            "sqrt" => Some(Self::Sqrt),
            "abs" => Some(Self::Abs),
            "log" => Some(Self::Log),
            "ln" => Some(Self::Ln),
            "π" | "pi" => Some(Self::Pi),
            "e" => Some(Self::E),
            "list" => Some(Self::List),
            "matrix" => Some(Self::Matrix),
            "round" => Some(Self::Round),
            "gcd" => Some(Self::Gcd),
            "lcm" => Some(Self::Lcm),
            "bounded" => Some(Self::Bounded),
            "total" => Some(Self::Total),
            "sum" | "Σ" => Some(Self::Sum),
            "avg" => Some(Self::Avg),
            "random" => Some(Self::Random),
            "rand" => Some(Self::Rand),
            "seed" => Some(Self::Seed),
            "binomial" => Some(Self::Binomial),
            "γ" | "gamma" | "Γ" => Some(Self::Gamma),
            "rad" => Some(Self::Rad),
            "deg" => Some(Self::Deg),
            "derivative" => Some(Self::Derivative),
            "integrate" => Some(Self::Integrate),
            "table" => Some(Self::Table),
            "solve" => Some(Self::Solve),
            "plot" => Some(Self::Plot),
            _ => None,
        }
    }

    pub fn name(&self) -> &'static str {
        match self {
            Self::Sin => "sin",
            Self::Cos => "cos",
            Self::Tan => "tan",
            Self::Sec => "sec",
            Self::Csc => "csc",
            Self::Cot => "cot",
            Self::Arcsin => "arcsin",
            Self::Arccos => "arccos",
            Self::Arctan => "arctan",
            Self::Arcsec => "arcsec",
            Self::Arccsc => "arccsc",
            Self::Arccot => "arccot",
            Self::Max => "max",
            Self::Min => "min",
            Self::Sqrt => "sqrt",
            Self::Abs => "abs",
            Self::Log => "log",
            Self::Ln => "ln",
            Self::Pi => "π",
            Self::E => "e",
            Self::List => "list",
            Self::Matrix => "matrix",
            Self::Round => "round",
            Self::Gcd => "gcd",
            Self::Lcm => "lcm",
            Self::Bounded => "bounded",
            Self::Total => "total",
            Self::Sum => "sum",
            Self::Avg => "avg",
            Self::Random => "random",
            Self::Rand => "rand",
            Self::Seed => "seed",
            Self::Binomial => "binomial",
            Self::Gamma => "Γ",
            Self::Rad => "rad",
            Self::Deg => "deg",
            Self::Derivative => "derivative",
            Self::Integrate => "integrate",
            Self::Table => "table",
            Self::Solve => "solve",
            Self::Plot => "plot",
        }
    }

    pub fn is_constant(&self) -> bool {
        matches!(self, Self::Pi | Self::E)
    }
}

#[derive(Debug, PartialEq, Clone)]
pub enum Token {
    Number(String),
    Variable(String),
    Function(MathFunction, usize),
    Operator(MathOperator, usize),
    LParen(String),
    RParen(String),
    Comma,
}

impl Token {
    pub fn is_number(&self) -> bool {
        matches!(self, Token::Number(_))
    }
    pub fn is_variable(&self) -> bool {
        matches!(self, Token::Variable(_))
    }
    pub fn is_function(&self) -> bool {
        matches!(self, Token::Function(_, _))
    }
    pub fn is_operator(&self) -> bool {
        matches!(self, Token::Operator(_, _))
    }
    pub fn is_lp(&self) -> bool {
        matches!(self, Token::LParen(_))
    }
    pub fn is_rp(&self) -> bool {
        matches!(self, Token::RParen(_))
    }
    pub fn is_comma(&self) -> bool {
        matches!(self, Token::Comma)
    }

    pub fn value(&self) -> String {
        match self {
            Token::Number(v) | Token::Variable(v) | Token::LParen(v) | Token::RParen(v) => {
                v.clone()
            }
            Token::Function(func, _) => func.name().to_string(),
            Token::Operator(op, _) => op.name().to_string(),
            Token::Comma => ",".to_string(),
        }
    }

    pub fn arity(&self) -> usize {
        match self {
            Token::Function(_, a) | Token::Operator(_, a) => *a,
            _ => 0,
        }
    }

    pub fn set_arity(&mut self, arity: usize) {
        match self {
            Token::Function(_, a) | Token::Operator(_, a) => *a = arity,
            _ => {}
        }
    }
}

impl fmt::Display for Token {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{}", self.value())
    }
}

pub struct DataStore {
    pub implicit_multiplication_priority: bool,
}

impl Default for DataStore {
    fn default() -> Self {
        DataStore {
            implicit_multiplication_priority: false,
        }
    }
}

pub fn tokenize<'a>(input: &'a str, _ds: &'a DataStore) -> Vec<Token> {
    let mut tokens = Vec::new();
    let mut remaining = input;

    while !remaining.is_empty() {
        remaining = remaining.trim_start();
        if remaining.is_empty() {
            break;
        }

        if let Ok((rem, token)) = parse_token(remaining) {
            tokens.push(token);
            remaining = rem;
        } else {
            remaining = &remaining[1..];
        }
    }
    tokens
}

fn parse_token(input: &str) -> IResult<&str, Token> {
    alt((
        parse_number,
        parse_operator,
        parse_identifier,
        parse_parens,
        parse_comma,
    ))(input)
}

fn parse_number(input: &str) -> IResult<&str, Token> {
    let (input, num_str) = recognize(pair(digit1, opt(pair(char('.'), digit1))))(input)?;
    Ok((input, Token::Number(num_str.to_string())))
}

fn parse_operator(input: &str) -> IResult<&str, Token> {
    let operators = vec![
        ">=", "<=", "==", "!=", "&&", "||", "^", "!", "/", "*", "+", "-", ">", "<", "÷", "−", "≥",
        "≤", "%", "=",
    ];
    for op_str in operators {
        if let Ok((rem, _)) = tag::<_, _, ()>(op_str)(input) {
            let op = MathOperator::from_str(op_str).expect("Operator must be defined");
            let arity = if op == MathOperator::Factorial { 1 } else { 2 };
            return Ok((rem, Token::Operator(op, arity)));
        }
    }
    Err(nom::Err::Error(nom::error::Error::new(
        input,
        nom::error::ErrorKind::Tag,
    )))
}

fn parse_identifier(input: &str) -> IResult<&str, Token> {
    let (input, id) = alt((alpha1, tag("π"), tag("Γ"), tag("∞"), tag("Σ")))(input)?;

    if let Some(func) = MathFunction::from_str(id) {
        Ok((input, Token::Function(func, 0)))
    } else {
        Ok((input, Token::Variable(id.to_string())))
    }
}

fn parse_parens(input: &str) -> IResult<&str, Token> {
    alt((
        map(alt((tag("("), tag("{"), tag("["))), |s: &str| {
            Token::LParen(s.to_string())
        }),
        map(alt((tag(")"), tag("}"), tag("]"))), |s: &str| {
            Token::RParen(s.to_string())
        }),
    ))(input)
}

fn parse_comma(input: &str) -> IResult<&str, Token> {
    map(tag(","), |_| Token::Comma)(input)
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::Shunter;

    fn get_rpn(input: &str, ds: &DataStore) -> String {
        let tokens = tokenize(input, ds);
        let shunter = Shunter::new(ds);
        let rpn = shunter.shunt(tokens);
        let result = rpn
            .into_iter()
            .map(|t| t.to_string())
            .collect::<Vec<_>>()
            .join(" ");
        println!("Input: {}, RPN: {}", input, result);
        result
    }

    #[test]
    fn unary_function() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("-pi", &ds), "-1 π *");
    }

    #[test]
    fn complex_function() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("sin(16pi)", &ds), "16 π * sin");
    }

    #[test]
    fn constant_function() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("2e", &ds), "2 e *");
    }

    #[test]
    fn constant_function_right() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("pi(2)", &ds), "π 2 *");
    }

    #[test]
    fn multi_term_multiply() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("(30.1)2.5(278)", &ds), "30.1 2.5 * 278 *");
    }

    #[test]
    fn variable_add() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("2+x", &ds), "2 x +");
    }

    #[test]
    fn simple_add() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("3 + 2", &ds), "3 2 +");
    }

    #[test]
    fn multi_term_add() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("2 + 3 + 2", &ds), "2 3 + 2 +");
    }

    #[test]
    fn simple_subtract() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("4 - 2", &ds), "4 2 -");
    }

    #[test]
    fn wikipedia() {
        let ds = DataStore::default();
        assert_eq!(
            get_rpn("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3", &ds),
            "3 4 2 * 1 5 - 2 3 ^ ^ / +"
        );
    }

    #[test]
    fn functions() {
        let ds = DataStore::default();
        assert_eq!(
            get_rpn("sin ( max ( 2 , 3 ) / 3 * 3.1415 )", &ds),
            "2 3 max 3 / 3.1415 * sin"
        );
    }

    #[test]
    fn variables() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("2 * x", &ds), "2 x *");
    }

    #[test]
    fn composite_max() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("max(sqrt(16),100)", &ds), "16 sqrt 100 max");
    }

    #[test]
    fn variable_multiplication() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("v + a * t", &ds), "v a t * +");
    }

    #[test]
    fn arity_constant_max() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("max(1, pi)", &ds), "1 π max");
    }

    #[test]
    fn variable_exponents() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("x^2", &ds), "x 2 ^");
    }

    #[test]
    fn variable_chain_multiplication() {
        let ds = DataStore::default();
        assert_eq!(
            get_rpn("x2sin(x) + x3sin(x)", &ds),
            "x 2 * x sin * x 3 * x sin * +"
        );
    }

    #[test]
    fn aliasing() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("7÷2", &ds), "7 2 /");
    }

    #[test]
    fn unary_start() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("-2 + 4", &ds), "-1 2 * 4 +");
    }

    #[test]
    fn complex_expression() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("x >= 0 && x <= 5", &ds), "x 0 >= x 5 <= &&");
    }

    #[test]
    fn mixed_division_multiplication() {
        let mut ds = DataStore::default();
        ds.implicit_multiplication_priority = false;
        assert_eq!(get_rpn("1/2x", &ds), "1 2 / x *");
    }

    #[test]
    fn list() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("{5,2}", &ds), "5 2 list");
    }

    #[test]
    fn matrix() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("{{a,b},{c,d}}", &ds), "a b list c d list list");
    }

    // --- Implicit Multiplication Tests ---

    #[test]
    fn implicit_left() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("4sin(2)", &ds), "4 2 sin *");
    }

    #[test]
    fn implicit_left_bracket() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("4(2)", &ds), "4 2 *");
    }

    #[test]
    fn implicit_left_eos() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("2x", &ds), "2 x *");
    }

    #[test]
    fn implicit_left_variable() {
        let ds = DataStore::default();
        // Since we don't swap, x followed by 2 is x * 2.
        assert_eq!(get_rpn("x2", &ds), "x 2 *");
    }

    #[test]
    fn implicit_mix() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("12(3) + 8(1.01)", &ds), "12 3 * 8 1.01 * +");
    }

    #[test]
    fn implicit_right() {
        let ds = DataStore::default();
        // We don't swap, so sin(2) followed by 4 is sin(2) * 4 in original order
        assert_eq!(get_rpn("sin(2)4", &ds), "2 sin 4 *");
    }

    #[test]
    fn implicit_right_bracket() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("(2)4", &ds), "2 4 *");
    }

    #[test]
    fn implicit_variable_left() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("x(y)", &ds), "x y *");
    }

    #[test]
    fn implicit_variable_right() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("(x)(y)", &ds), "x y *");
    }

    #[test]
    fn implicit_multiple_functions() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("sin(x)cos(x)", &ds), "x sin x cos *");
    }

    #[test]
    fn implicit_unary() {
        let ds = DataStore::default();
        assert_eq!(get_rpn("-(3^2)", &ds), "-1 3 2 ^ *");
    }
}
