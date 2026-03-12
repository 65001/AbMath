use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

pub struct PropagationRule;
impl Rule for PropagationRule {
    fn name(&self) -> &'static str {
        "Propagation"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // integrate(A + B, start, end, x) -> integrate(A, start, end, x) + integrate(B, start, end, x)
        // args = [expression, start, end, var_x]
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Add, left, right) = &args[0] {
                    let mut int_a = args.clone();
                    int_a[0] = *left.clone();

                    let mut int_b = args.clone();
                    int_b[0] = *right.clone();

                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        Box::new(Node::Function(MathFunction::Integrate, int_a)),
                        Box::new(Node::Function(MathFunction::Integrate, int_b)),
                    ));
                } else if let Node::BinaryOp(MathOperator::Subtract, left, right) = &args[0] {
                    let mut int_a = args.clone();
                    int_a[0] = *left.clone();

                    let mut int_b = args.clone();
                    int_b[0] = *right.clone();

                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        Box::new(Node::Function(MathFunction::Integrate, int_a)),
                        Box::new(Node::Function(MathFunction::Integrate, int_b)),
                    ));
                }
            }
        }
        None
    }
}

pub struct ConstantsRule;
impl Rule for ConstantsRule {
    fn name(&self) -> &'static str {
        "Constants"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // integrate(C, start, end, x) -> C * (end - start)
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                let is_const = match &args[0] {
                    Node::Variable(_) => !args[0].matches_node(&args[3]),
                    Node::Number(_) => true,
                    _ => false, // simplistic containment check for now, matching C#'s basic logic
                };

                if is_const {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(args[0].clone()),
                        Box::new(Node::BinaryOp(
                            MathOperator::Subtract,
                            Box::new(args[2].clone()), // end
                            Box::new(args[1].clone()), // start
                        )),
                    ));
                }
            }
        }
        None
    }
}

pub struct CoefficientRule;
impl Rule for CoefficientRule {
    fn name(&self) -> &'static str {
        "Coefficient"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // integrate(C * f(x), start, end, x) -> C * integrate(f(x), start, end, x)
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = &args[0] {
                    let left_is_const = match &**left {
                        Node::Variable(_) => !left.matches_node(&args[3]),
                        Node::Number(_) => true,
                        _ => false,
                    };
                    let right_is_const = match &**right {
                        Node::Variable(_) => !right.matches_node(&args[3]),
                        Node::Number(_) => true,
                        _ => false,
                    };

                    if left_is_const {
                        let mut int_args = args.clone();
                        int_args[0] = *right.clone();
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            left.clone(),
                            Box::new(Node::Function(MathFunction::Integrate, int_args)),
                        ));
                    } else if right_is_const {
                        let mut int_args = args.clone();
                        int_args[0] = *left.clone();
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            right.clone(),
                            Box::new(Node::Function(MathFunction::Integrate, int_args)),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct SingleVariableRule;
impl Rule for SingleVariableRule {
    fn name(&self) -> &'static str {
        "SingleVariable"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // integrate(x, start, end, x) -> (end^2 - start^2) / 2
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                if matches!(&args[0], Node::Variable(_)) && args[0].matches_node(&args[3]) {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Subtract,
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                Box::new(args[2].clone()),
                                Box::new(Node::Number(2.0)),
                            )),
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                Box::new(args[1].clone()),
                                Box::new(Node::Number(2.0)),
                            )),
                        )),
                        Box::new(Node::Number(2.0)),
                    ));
                }
            }
        }
        None
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::Shunter;
    use crate::ast::build_ast;
    use crate::rules::RuleEngine;
    use crate::tokenizer::{DataStore, tokenize};

    fn simplify_expr(input: &str) -> Node {
        let mut ds = DataStore::default();
        ds.implicit_multiplication_priority = true;
        let tokens = tokenize(input, &ds);
        let shunter = Shunter::new(&ds);
        let rpn = shunter.shunt(tokens);
        let ast = build_ast(rpn).unwrap();

        let mut engine = RuleEngine::new();
        // Add all rules so integrals actually evaluate completely (evaluating exponents, subtraction, evaluation etc)
        for rule in crate::rules::standard_rules() {
            engine.add_rule(rule);
        }
        engine.simplify(ast)
    }

    #[test]
    fn test_single_variable_integrate() {
        let ast = simplify_expr("integrate(x, 0, 5, x)");
        // Returns (5^2 - 0^2) / 2 = 12.5 (EvaluateNumber should calculate it down to 12.5)
        assert_eq!(ast, Node::Number(12.5));
    }
}
