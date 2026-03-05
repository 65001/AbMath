use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

pub struct PropagationRule;
impl Rule for PropagationRule {
    fn name(&self) -> &'static str {
        "Propagation"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // integrate(x, start, end, A + B) -> integrate(x, start, end, A) + integrate(x, start, end, B)
        // Assume args = [end, start, var_x, expression] based on AbMath standard 4-arg function
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Add, left, right) = &args[3] {
                    let mut int_a = args.clone();
                    int_a[3] = *left.clone();

                    let mut int_b = args.clone();
                    int_b[3] = *right.clone();

                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        Box::new(Node::Function(MathFunction::Integrate, int_a)),
                        Box::new(Node::Function(MathFunction::Integrate, int_b)),
                    ));
                } else if let Node::BinaryOp(MathOperator::Subtract, left, right) = &args[3] {
                    let mut int_a = args.clone();
                    int_a[3] = *left.clone();

                    let mut int_b = args.clone();
                    int_b[3] = *right.clone();

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
        // integrate(x, start, end, C) -> C * (end - start)
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                let is_const = match &args[3] {
                    Node::Variable(_) => !args[3].matches_node(&args[2]),
                    Node::Number(_) => true,
                    _ => false, // simplistic containment check for now, matching C#'s basic logic
                };

                if is_const {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(args[3].clone()),
                        Box::new(Node::BinaryOp(
                            MathOperator::Subtract,
                            Box::new(args[0].clone()), // end
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
        // integrate(x, start, end, C * f(x)) -> C * integrate(x, start, end, f(x))
        if let Node::Function(MathFunction::Integrate, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = &args[3] {
                    // In C#: node[3, 1].IsNumberOrConstant() || !node[3, 1].Contains(node[2])
                    // Assume `right` is `node[3, 1]`
                    let right_is_const = match &**right {
                        Node::Variable(_) => !right.matches_node(&args[2]),
                        Node::Number(_) => true,
                        _ => false,
                    };

                    if right_is_const {
                        let mut int_args = args.clone();
                        int_args[3] = *left.clone();

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
                if matches!(&args[3], Node::Variable(_)) && args[3].matches_node(&args[2]) {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Subtract,
                            Box::new(Node::BinaryOp(
                                MathOperator::Pow,
                                Box::new(args[0].clone()),
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
