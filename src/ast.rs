use crate::tokenizer::{MathFunction, MathOperator, Token};
use std::hash::{Hash, Hasher};

#[derive(Debug, Clone, PartialEq)]
pub enum Node {
    Number(f64),
    Variable(String),
    Constant(MathFunction), // For Pi, E, etc.
    UnaryOp(MathOperator, Box<Node>),
    BinaryOp(MathOperator, Box<Node>, Box<Node>),
    Function(MathFunction, Vec<Node>),
    List(Vec<Node>),
    Matrix(Vec<Vec<Node>>),
}

impl Eq for Node {}

impl Hash for Node {
    fn hash<H: Hasher>(&self, state: &mut H) {
        // We incorporate a discriminator to avoid collisions between variants
        // that happen to contain the same inner values.
        match self {
            Node::Number(f) => {
                0_u8.hash(state);
                // Hash the byte representation for f64
                f.to_bits().hash(state);
            }
            Node::Variable(s) => {
                1_u8.hash(state);
                s.hash(state);
            }
            Node::Constant(c) => {
                2_u8.hash(state);
                c.hash(state);
            }
            Node::UnaryOp(op, n) => {
                3_u8.hash(state);
                op.hash(state);
                n.hash(state);
            }
            Node::BinaryOp(op, l, r) => {
                4_u8.hash(state);
                op.hash(state);
                l.hash(state);
                r.hash(state);
            }
            Node::Function(f, args) => {
                5_u8.hash(state);
                f.hash(state);
                args.hash(state);
            }
            Node::List(elements) => {
                6_u8.hash(state);
                elements.hash(state);
            }
            Node::Matrix(rows) => {
                7_u8.hash(state);
                rows.hash(state);
            }
        }
    }
}

impl std::fmt::Display for Node {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Node::Number(val) => write!(f, "{}", val),
            Node::Variable(name) => write!(f, "{}", name),
            Node::Constant(c) => write!(f, "{}", c.name()),
            Node::UnaryOp(op, inner) => {
                if *op == MathOperator::Factorial {
                    write!(f, "{}!", inner)
                } else {
                    write!(f, "{}{}", op.name(), inner)
                }
            }
            Node::BinaryOp(op, left, right) => {
                if *op == MathOperator::Multiply {
                    if let Node::Number(val) = &**left {
                        if *val == -1.0 {
                            let r_needs_parens = match &**right {
                                Node::BinaryOp(r_op, ..) => r_op.weight() <= op.weight(),
                                _ => false,
                            };
                            write!(f, "-")?;
                            if r_needs_parens {
                                return write!(f, "({})", right);
                            } else {
                                return write!(f, "{}", right);
                            }
                        }
                    }
                }

                let l_needs_parens = match &**left {
                    Node::BinaryOp(l_op, ..) => l_op.weight() < op.weight(),
                    _ => false,
                };
                let r_needs_parens = match &**right {
                    Node::BinaryOp(r_op, ..) => r_op.weight() <= op.weight(),
                    _ => false,
                };

                if l_needs_parens {
                    write!(f, "({})", left)?;
                } else {
                    write!(f, "{}", left)?;
                }

                if *op == MathOperator::Multiply {
                    write!(f, " * ")?;
                } else if *op == MathOperator::Pow {
                    write!(f, "^")?;
                } else {
                    write!(f, " {} ", op.name())?;
                }

                if r_needs_parens {
                    write!(f, "({})", right)
                } else {
                    write!(f, "{}", right)
                }
            }
            Node::Function(func, args) => {
                write!(f, "{}(", func.name())?;
                for (i, arg) in args.iter().enumerate() {
                    if i > 0 {
                        write!(f, ", ")?;
                    }
                    write!(f, "{}", arg)?;
                }
                write!(f, ")")
            }
            Node::List(args) => {
                write!(f, "{{")?;
                for (i, arg) in args.iter().enumerate() {
                    if i > 0 {
                        write!(f, ", ")?;
                    }
                    write!(f, "{}", arg)?;
                }
                write!(f, "}}")
            }
            Node::Matrix(rows) => {
                write!(f, "{{")?;
                for (i, row) in rows.iter().enumerate() {
                    if i > 0 {
                        write!(f, ", ")?;
                    }
                    write!(f, "{{")?;
                    for (j, arg) in row.iter().enumerate() {
                        if j > 0 {
                            write!(f, ", ")?;
                        }
                        write!(f, "{}", arg)?;
                    }
                    write!(f, "}}")?;
                }
                write!(f, "}}")
            }
        }
    }
}

impl Node {
    pub fn is_number(&self, val: f64) -> bool {
        if let Node::Number(v) = self {
            (v - val).abs() < f64::EPSILON
        } else {
            false
        }
    }

    pub fn get_number(&self) -> Option<f64> {
        if let Node::Number(v) = self {
            Some(*v)
        } else {
            None
        }
    }

    pub fn is_addition(&self) -> bool {
        matches!(self, Node::BinaryOp(MathOperator::Add, _, _))
    }

    pub fn is_subtraction(&self) -> bool {
        matches!(self, Node::BinaryOp(MathOperator::Subtract, _, _))
    }

    pub fn is_multiplication(&self) -> bool {
        matches!(self, Node::BinaryOp(MathOperator::Multiply, _, _))
    }

    pub fn is_division(&self) -> bool {
        matches!(self, Node::BinaryOp(MathOperator::Divide, _, _))
    }

    pub fn is_power(&self) -> bool {
        matches!(self, Node::BinaryOp(MathOperator::Pow, _, _))
    }

    pub fn is_log(&self) -> bool {
        matches!(self, Node::Function(crate::tokenizer::MathFunction::Log, _))
    }

    pub fn is_ln(&self) -> bool {
        matches!(self, Node::Function(crate::tokenizer::MathFunction::Ln, _))
    }

    pub fn is_constant(&self, s: &str) -> bool {
        if let Node::Variable(v) = self {
            v == s
        } else {
            false
        }
    }

    pub fn matches_node(&self, other: &Node) -> bool {
        self == other
    }

    pub fn less_than_number(&self, val: f64) -> bool {
        if let Node::Number(v) = self {
            *v < val
        } else {
            false
        }
    }
}

#[derive(Debug, PartialEq)]
pub enum AstError {
    EmptyInput,
    InvalidToken(Token),
    NotEnoughArguments(Token),
    TooManyValuesOnStack,
    ParseFloatError(String),
}

pub fn build_ast(postfix: Vec<Token>) -> Result<Node, AstError> {
    let mut stack: Vec<Node> = Vec::new();

    for token in postfix {
        match token {
            Token::Number(ref s) => {
                let val = s
                    .parse::<f64>()
                    .map_err(|_| AstError::ParseFloatError(s.clone()))?;
                stack.push(Node::Number(val));
            }
            Token::Variable(ref s) => {
                stack.push(Node::Variable(s.clone()));
            }
            Token::Operator(op, arity) => {
                if arity == 1 {
                    if let Some(operand) = stack.pop() {
                        stack.push(Node::UnaryOp(op, Box::new(operand)));
                    } else {
                        return Err(AstError::NotEnoughArguments(token));
                    }
                } else if arity == 2 {
                    let right = stack
                        .pop()
                        .ok_or_else(|| AstError::NotEnoughArguments(token.clone()))?;
                    let left = stack
                        .pop()
                        .ok_or_else(|| AstError::NotEnoughArguments(token.clone()))?;
                    stack.push(Node::BinaryOp(op, Box::new(left), Box::new(right)));
                } else {
                    return Err(AstError::InvalidToken(token));
                }
            }
            Token::Function(func, arity) => {
                if func.is_constant() {
                    stack.push(Node::Constant(func));
                } else {
                    let mut args = Vec::with_capacity(arity);
                    for _ in 0..arity {
                        if let Some(arg) = stack.pop() {
                            args.push(arg);
                        } else {
                            return Err(AstError::NotEnoughArguments(token.clone()));
                        }
                    }
                    args.reverse();

                    // Map `list` and detect `matrix` representations
                    if func == MathFunction::List {
                        let is_matrix =
                            !args.is_empty() && args.iter().all(|arg| matches!(arg, Node::List(_)));
                        if is_matrix {
                            let matrix_data = args
                                .into_iter()
                                .map(|arg| {
                                    if let Node::List(inner) = arg {
                                        inner
                                    } else {
                                        unreachable!()
                                    }
                                })
                                .collect();
                            stack.push(Node::Matrix(matrix_data));
                        } else {
                            stack.push(Node::List(args));
                        }
                    } else if func == MathFunction::Matrix {
                        // Explicit `matrix()` function calls could also be converted, assuming they behave similarly
                        let is_matrix =
                            !args.is_empty() && args.iter().all(|arg| matches!(arg, Node::List(_)));
                        if is_matrix {
                            let matrix_data = args
                                .into_iter()
                                .map(|arg| {
                                    if let Node::List(inner) = arg {
                                        inner
                                    } else {
                                        unreachable!()
                                    }
                                })
                                .collect();
                            stack.push(Node::Matrix(matrix_data));
                        } else {
                            stack.push(Node::Function(func, args)); // Fallback if matrix(...) doesn't hold lists
                        }
                    } else {
                        stack.push(Node::Function(func, args));
                    }
                }
            }
            Token::Comma | Token::LParen(_) | Token::RParen(_) => {
                return Err(AstError::InvalidToken(token));
            }
        }
    }

    if stack.len() == 1 {
        Ok(stack.pop().unwrap())
    } else if stack.is_empty() {
        Err(AstError::EmptyInput)
    } else {
        Err(AstError::TooManyValuesOnStack)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::Shunter;
    use crate::tokenizer::{DataStore, tokenize};

    fn get_ast_node(input: &str) -> Node {
        let ds = DataStore::default();
        let tokens = tokenize(input, &ds);
        let shunter = Shunter::new(&ds);
        let rpn = shunter.shunt(tokens);
        build_ast(rpn).unwrap()
    }

    #[test]
    fn one_raised_exponent() {
        let ast = get_ast_node("1^x");
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Pow,
                Box::new(Node::Number(1.0)),
                Box::new(Node::Variable("x".to_string()))
            )
        );
    }

    #[test]
    fn zero_raised_to_constant() {
        let ast = get_ast_node("0^2");
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Pow,
                Box::new(Node::Number(0.0)),
                Box::new(Node::Number(2.0))
            )
        );
    }

    #[test]
    fn abs_raised_to_power_two() {
        let ast = get_ast_node("abs(x)^2");
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Pow,
                Box::new(Node::Function(
                    MathFunction::Abs,
                    vec![Node::Variable("x".to_string())]
                )),
                Box::new(Node::Number(2.0))
            )
        );
    }

    #[test]
    fn trig_identity() {
        let ast = get_ast_node("sin(x)sin(x) + cos(x)cos(x)");
        let sin_x = Node::Function(MathFunction::Sin, vec![Node::Variable("x".to_string())]);
        let cos_x = Node::Function(MathFunction::Cos, vec![Node::Variable("x".to_string())]);
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Add,
                Box::new(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(sin_x.clone()),
                    Box::new(sin_x)
                )),
                Box::new(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(cos_x.clone()),
                    Box::new(cos_x)
                ))
            )
        );
    }

    #[test]
    fn log_exponent_multiply() {
        let ast = get_ast_node("log(2,3^x)");
        assert_eq!(
            ast,
            Node::Function(
                MathFunction::Log,
                vec![
                    Node::Number(2.0),
                    Node::BinaryOp(
                        MathOperator::Pow,
                        Box::new(Node::Number(3.0)),
                        Box::new(Node::Variable("x".to_string()))
                    )
                ]
            )
        );
    }

    #[test]
    fn division_flip() {
        let ast = get_ast_node("(5/x)/(x/3)");
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Divide,
                Box::new(Node::BinaryOp(
                    MathOperator::Divide,
                    Box::new(Node::Number(5.0)),
                    Box::new(Node::Variable("x".to_string()))
                )),
                Box::new(Node::BinaryOp(
                    MathOperator::Divide,
                    Box::new(Node::Variable("x".to_string())),
                    Box::new(Node::Number(3.0))
                ))
            )
        );
    }

    #[test]
    fn variable_subtraction() {
        let ast = get_ast_node("2x - 3x");
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Subtract,
                Box::new(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(Node::Number(2.0)),
                    Box::new(Node::Variable("x".to_string()))
                )),
                Box::new(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(Node::Number(3.0)),
                    Box::new(Node::Variable("x".to_string()))
                ))
            )
        );
    }
}
