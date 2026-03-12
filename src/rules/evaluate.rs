use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathOperator;

pub struct EvaluateNumberRule;

impl Rule for EvaluateNumberRule {
    fn name(&self) -> &'static str {
        "EvaluateNumber"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        match node {
            Node::BinaryOp(op, left, right) => {
                if let (Node::Number(l), Node::Number(r)) = (&**left, &**right) {
                    match op {
                        MathOperator::Add => Some(Node::Number(l + r)),
                        MathOperator::Subtract => Some(Node::Number(l - r)),
                        MathOperator::Multiply => Some(Node::Number(l * r)),
                        MathOperator::Divide => {
                            if r.abs() > std::f64::EPSILON {
                                Some(Node::Number(l / r))
                            } else {
                                None
                            }
                        }
                        MathOperator::Pow => {
                            let res = l.powf(*r);
                            if res.is_nan() {
                                None
                            } else {
                                Some(Node::Number(res))
                            }
                        }
                        MathOperator::Mod => {
                            if r.abs() > std::f64::EPSILON {
                                Some(Node::Number(l % r))
                            } else {
                                None
                            }
                        }
                        _ => None,
                    }
                } else {
                    None
                }
            }
            Node::UnaryOp(op, inner) => {
                if let Node::Number(val) = &**inner {
                    match op {
                        MathOperator::Subtract => Some(Node::Number(-val)),
                        MathOperator::Add => Some(Node::Number(*val)),
                        _ => None,
                    }
                } else {
                    None
                }
            }
            _ => None,
        }
    }
}
