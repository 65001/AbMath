use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathOperator;

pub struct ZeroFactorialRule;
impl Rule for ZeroFactorialRule {
    fn name(&self) -> &'static str {
        "ZeroFactorial"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // 0! or 1! -> 1
        // We'll match our fallback `BinaryOp` with dummy `1.0` mapped in tokenizer/Sum rule.
        if let Node::BinaryOp(MathOperator::Factorial, left, _dummy_right) = node {
            if left.is_number(0.0) || left.is_number(1.0) {
                return Some(Node::Number(1.0));
            }
        }
        None
    }
}
