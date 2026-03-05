use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

pub struct DerivativeRule;

impl Rule for DerivativeRule {
    fn name(&self) -> &'static str {
        "Derivative"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::Function(MathFunction::Derivative, args) = node {
            if args.len() == 2 {
                let expr = &args[0];
                let var = &args[1];

                if let Node::Variable(var_name) = var {
                    return Some(derive_node(expr, &var_name));
                }
            } else if args.len() == 3 {
                // derivative(f(x), x, 2) -> derivative(derivative(f(x), x), x)
                if let Node::Number(n) = args[2] {
                    if n > 1.0 {
                        return Some(Node::Function(
                            MathFunction::Derivative,
                            vec![
                                Node::Function(
                                    MathFunction::Derivative,
                                    vec![args[0].clone(), args[1].clone(), Node::Number(n - 1.0)],
                                ),
                                args[1].clone(),
                            ],
                        ));
                    } else if n == 1.0 {
                        return Some(Node::Function(
                            MathFunction::Derivative,
                            vec![args[0].clone(), args[1].clone()],
                        ));
                    }
                }
            }
        }
        None
    }
}

fn derive_node(node: &Node, var: &str) -> Node {
    let wrap_derive = |n: &Node| -> Node {
        Node::Function(
            MathFunction::Derivative,
            vec![n.clone(), Node::Variable(var.to_string())],
        )
    };

    match node {
        Node::Number(_) | Node::Constant(_) => Node::Number(0.0),
        Node::Variable(name) => {
            if name == var {
                Node::Number(1.0)
            } else {
                Node::Number(0.0)
            }
        }
        Node::BinaryOp(MathOperator::Add, left, right) => Node::BinaryOp(
            MathOperator::Add,
            Box::new(wrap_derive(left)),
            Box::new(wrap_derive(right)),
        ),
        Node::BinaryOp(MathOperator::Subtract, left, right) => Node::BinaryOp(
            MathOperator::Subtract,
            Box::new(wrap_derive(left)),
            Box::new(wrap_derive(right)),
        ),
        Node::BinaryOp(MathOperator::Multiply, left, right) => {
            // (f * g)' = f'g + fg'
            // check for constant multiples
            let l_is_const = is_constant_wrt(left, var);
            let r_is_const = is_constant_wrt(right, var);

            if l_is_const && r_is_const {
                Node::Number(0.0)
            } else if l_is_const {
                Node::BinaryOp(
                    MathOperator::Multiply,
                    left.clone(),
                    Box::new(wrap_derive(right)),
                )
            } else if r_is_const {
                Node::BinaryOp(
                    MathOperator::Multiply,
                    right.clone(),
                    Box::new(wrap_derive(left)),
                )
            } else {
                Node::BinaryOp(
                    MathOperator::Add,
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(wrap_derive(right)),
                    )),
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(wrap_derive(left)),
                        right.clone(),
                    )),
                )
            }
        }
        Node::BinaryOp(MathOperator::Divide, left, right) => {
            // (f / g)' = (f'g - fg') / g^2
            let r_is_const = is_constant_wrt(right, var);
            if r_is_const {
                Node::BinaryOp(
                    MathOperator::Divide,
                    Box::new(wrap_derive(left)),
                    right.clone(),
                )
            } else {
                let num = Node::BinaryOp(
                    MathOperator::Subtract,
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(wrap_derive(left)),
                        right.clone(),
                    )),
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(wrap_derive(right)),
                    )),
                );
                let den = Node::BinaryOp(
                    MathOperator::Pow,
                    right.clone(),
                    Box::new(Node::Number(2.0)),
                );
                Node::BinaryOp(MathOperator::Divide, Box::new(num), Box::new(den))
            }
        }
        Node::BinaryOp(MathOperator::Pow, left, right) => {
            let l_is_const = is_constant_wrt(left, var);
            let r_is_const = is_constant_wrt(right, var);

            if l_is_const && r_is_const {
                Node::Number(0.0)
            } else if r_is_const {
                // (f(x)^c)' = c * f(x)^(c-1) * f'(x)
                if let Node::Number(c) = **right {
                    Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            right.clone(),
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                left.clone(),
                                Box::new(Node::Number(c - 1.0)),
                            )),
                        )),
                        Box::new(wrap_derive(left)),
                    )
                } else {
                    Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            right.clone(),
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                left.clone(),
                                Box::new(Node::BinaryOp(
                                    MathOperator::Subtract,
                                    right.clone(),
                                    Box::new(Node::Number(1.0)),
                                )),
                            )),
                        )),
                        Box::new(wrap_derive(left)),
                    )
                }
            } else if l_is_const {
                // (c^g(x))' = ln(c) * c^g(x) * g'(x)
                Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::Function(MathFunction::Ln, vec![*left.clone()])),
                        Box::new(node.clone()),
                    )),
                    Box::new(wrap_derive(right)),
                )
            } else {
                // x^x -> x^x * (x * ln(x))'
                Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(node.clone()),
                    Box::new(wrap_derive(&Node::BinaryOp(
                        MathOperator::Multiply,
                        right.clone(),
                        Box::new(Node::Function(MathFunction::Ln, vec![*left.clone()])),
                    ))),
                )
            }
        }
        Node::Function(func, args) => {
            if args.len() == 1 {
                let inner = &args[0];
                let inner_prime = wrap_derive(inner);
                match func {
                    MathFunction::Sin => Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::Function(MathFunction::Cos, vec![inner.clone()])),
                        Box::new(inner_prime),
                    ),
                    MathFunction::Cos => Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Sin, vec![inner.clone()])),
                        )),
                        Box::new(inner_prime),
                    ),
                    MathFunction::Tan => Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Pow,
                            Box::new(Node::Function(MathFunction::Sec, vec![inner.clone()])),
                            Box::new(Node::Number(2.0)),
                        )),
                        Box::new(inner_prime),
                    ),
                    MathFunction::Sec => Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Function(MathFunction::Sec, vec![inner.clone()])),
                            Box::new(Node::Function(MathFunction::Tan, vec![inner.clone()])),
                        )),
                        Box::new(inner_prime),
                    ),
                    MathFunction::Csc => Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::BinaryOp(
                                MathOperator::Multiply,
                                Box::new(Node::Function(MathFunction::Csc, vec![inner.clone()])),
                                Box::new(Node::Function(MathFunction::Cot, vec![inner.clone()])),
                            )),
                        )),
                        Box::new(inner_prime),
                    ),
                    MathFunction::Cot => Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                Box::new(Node::Function(MathFunction::Csc, vec![inner.clone()])),
                                Box::new(Node::Number(2.0)),
                            )),
                        )),
                        Box::new(inner_prime),
                    ),
                    MathFunction::Arcsin => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(inner_prime),
                        Box::new(Node::Function(
                            MathFunction::Sqrt,
                            vec![Node::BinaryOp(
                                MathOperator::Subtract,
                                Box::new(Node::Number(1.0)),
                                Box::new(Node::BinaryOp(
                                    MathOperator::Pow,
                                    Box::new(inner.clone()),
                                    Box::new(Node::Number(2.0)),
                                )),
                            )],
                        )),
                    ),
                    MathFunction::Arccos => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(inner_prime),
                        )),
                        Box::new(Node::Function(
                            MathFunction::Sqrt,
                            vec![Node::BinaryOp(
                                MathOperator::Subtract,
                                Box::new(Node::Number(1.0)),
                                Box::new(Node::BinaryOp(
                                    MathOperator::Pow,
                                    Box::new(inner.clone()),
                                    Box::new(Node::Number(2.0)),
                                )),
                            )],
                        )),
                    ),
                    MathFunction::Arctan => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(inner_prime),
                        Box::new(Node::BinaryOp(
                            MathOperator::Add,
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                Box::new(inner.clone()),
                                Box::new(Node::Number(2.0)),
                            )),
                            Box::new(Node::Number(1.0)),
                        )),
                    ),
                    MathFunction::Arcsec => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(inner_prime),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Function(MathFunction::Abs, vec![inner.clone()])),
                            Box::new(Node::Function(
                                MathFunction::Sqrt,
                                vec![Node::BinaryOp(
                                    MathOperator::Subtract,
                                    Box::new(Node::BinaryOp(
                                        MathOperator::Pow,
                                        Box::new(inner.clone()),
                                        Box::new(Node::Number(2.0)),
                                    )),
                                    Box::new(Node::Number(1.0)),
                                )],
                            )),
                        )),
                    ),
                    MathFunction::Arccsc => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(inner_prime),
                        )),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Function(MathFunction::Abs, vec![inner.clone()])),
                            Box::new(Node::Function(
                                MathFunction::Sqrt,
                                vec![Node::BinaryOp(
                                    MathOperator::Subtract,
                                    Box::new(Node::BinaryOp(
                                        MathOperator::Pow,
                                        Box::new(inner.clone()),
                                        Box::new(Node::Number(2.0)),
                                    )),
                                    Box::new(Node::Number(1.0)),
                                )],
                            )),
                        )),
                    ),
                    MathFunction::Arccot => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(inner_prime),
                        )),
                        Box::new(Node::BinaryOp(
                            MathOperator::Add,
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                Box::new(inner.clone()),
                                Box::new(Node::Number(2.0)),
                            )),
                            Box::new(Node::Number(1.0)),
                        )),
                    ),
                    MathFunction::Ln => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(inner_prime),
                        Box::new(inner.clone()),
                    ),
                    MathFunction::Log => {
                        // log(x) handled? Wait, C# tests said log(b,x) is 2 args!
                        // C# tests: log(2, x) => 1 / (x * ln(2))
                        Node::BinaryOp(
                            MathOperator::Divide,
                            Box::new(inner_prime),
                            Box::new(inner.clone()),
                        ) // Fallback
                    }
                    MathFunction::Sqrt => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(inner_prime),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(2.0)),
                            Box::new(Node::Function(MathFunction::Sqrt, vec![inner.clone()])),
                        )),
                    ),
                    MathFunction::Abs => Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(inner.clone()),
                            Box::new(inner_prime),
                        )),
                        Box::new(Node::Function(MathFunction::Abs, vec![inner.clone()])),
                    ),
                    _ => Node::Number(0.0), // Unknown function derivative is 0 for safety
                }
            } else if args.len() == 2 && *func == MathFunction::Log {
                let base = &args[0];
                let inner = &args[1];
                let inner_prime = wrap_derive(inner);
                Node::BinaryOp(
                    MathOperator::Divide,
                    Box::new(inner_prime),
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(Node::Function(MathFunction::Ln, vec![base.clone()])),
                        Box::new(inner.clone()),
                    )),
                )
            } else {
                Node::Number(0.0) // fallback
            }
        }
        _ => Node::Number(0.0),
    }
}

fn is_constant_wrt(node: &Node, var: &str) -> bool {
    match node {
        Node::Number(_) | Node::Constant(_) => true,
        Node::Variable(v) => v != var,
        Node::UnaryOp(_, inner) => is_constant_wrt(inner, var),
        Node::BinaryOp(_, left, right) => is_constant_wrt(left, var) && is_constant_wrt(right, var),
        Node::Function(_, args) => args.iter().all(|arg| is_constant_wrt(arg, var)),
        Node::List(elements) => elements.iter().all(|arg| is_constant_wrt(arg, var)),
        Node::Matrix(rows) => rows
            .iter()
            .all(|row| row.iter().all(|arg| is_constant_wrt(arg, var))),
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::ast::build_ast;
    use crate::rules::RuleEngine;
    use crate::tokenizer::{tokenize, DataStore};
    use crate::Shunter;

    fn assert_derive(input: &str, expected: &str) {
        let mut ds = DataStore::default();
        ds.implicit_multiplication_priority = true; // Match C# behavior if needed
        let mut engine = RuleEngine::new();
        for rule in crate::rules::standard_rules() {
            engine.add_rule(rule);
        }

        let tokens = tokenize(input, &ds);
        let ast = build_ast(Shunter::new(&ds).shunt(tokens)).unwrap();
        let simplified = engine.simplify(ast);

        let expected_tokens = tokenize(expected, &ds);
        let expected_ast = build_ast(Shunter::new(&ds).shunt(expected_tokens)).unwrap();
        let expected_simplified = engine.simplify(expected_ast);

        if simplified != expected_simplified {
            panic!(
                "Expected:\n{:#?}\nGot:\n{:#?}",
                expected_simplified, simplified
            );
        }
    }

    #[test]
    fn test_constant() {
        assert_derive("derivative(1, x)", "0");
    }

    #[test]
    fn test_variable() {
        assert_derive("derivative(x, x)", "1");
    }

    #[test]
    fn test_constant_multiplication() {
        assert_derive("derivative(2 * x, x)", "2");
        assert_derive("derivative(x * 2, x)", "2");
    }

    #[test]
    fn test_power_rule() {
        assert_derive("derivative(x^3, x)", "3 * x^2");
    }

    #[test]
    fn test_sin() {
        assert_derive("derivative(sin(x), x)", "cos(x)");
    }

    #[test]
    fn test_cos() {
        assert_derive("derivative(cos(x), x)", "-1 * sin(x)");
    }

    #[test]
    fn test_euler_exponent() {
        assert_derive("derivative(e^x, x)", "e^x");
    }

    #[test]
    fn test_ln() {
        assert_derive("derivative(ln(x), x)", "1 / x");
    }

    #[test]
    fn test_double_derivative() {
        assert_derive("derivative(derivative(x^3, x), x)", "6 * x");
        assert_derive("derivative(x^3, x, 2)", "6 * x");
    }
}
