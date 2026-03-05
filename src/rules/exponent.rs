use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

// node[0] = exponent (right), node[1] = base (left)

pub struct FunctionRaisedToOneRule;
impl Rule for FunctionRaisedToOneRule {
    fn name(&self) -> &'static str {
        "FunctionRaisedToOne"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            if right.is_number(1.0) {
                return Some(*left.clone());
            }
        }
        None
    }
}

pub struct FunctionRaisedToZeroRule;
impl Rule for FunctionRaisedToZeroRule {
    fn name(&self) -> &'static str {
        "FunctionRaisedToZero"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, _, right) = node {
            if right.is_number(0.0) {
                return Some(Node::Number(1.0));
            }
        }
        None
    }
}

pub struct ZeroRaisedToConstantRule;
impl Rule for ZeroRaisedToConstantRule {
    fn name(&self) -> &'static str {
        "ZeroRaisedToConstant"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            if left.is_number(0.0) && right.get_number().map_or(false, |v| v > 0.0) {
                return Some(Node::Number(0.0));
            }
        }
        None
    }
}

pub struct OneRaisedToFunctionRule;
impl Rule for OneRaisedToFunctionRule {
    fn name(&self) -> &'static str {
        "OneRaisedToFunction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, left, _) = node {
            if left.is_number(1.0) {
                return Some(Node::Number(1.0));
            }
        }
        None
    }
}

pub struct ToDivisionRule;
impl Rule for ToDivisionRule {
    fn name(&self) -> &'static str {
        "ToDivision"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            if right.less_than_number(0.0) {
                // x^-2 -> 1 / (x^2)
                if let Some(val) = right.get_number() {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::Number(1.0)),
                        Box::new(Node::BinaryOp(
                            MathOperator::Pow,
                            left.clone(),
                            Box::new(Node::Number(val.abs())),
                        )),
                    ));
                }
            }
        }
        None
    }
}

pub struct ToSqrtRule;
impl Rule for ToSqrtRule {
    fn name(&self) -> &'static str {
        "ToSqrt"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            let is_half = right.is_number(0.5);
            let is_one_half_div =
                if let Node::BinaryOp(MathOperator::Divide, n_num, n_denom) = &**right {
                    n_denom.is_number(2.0) && n_num.is_number(1.0)
                } else {
                    false
                };

            if is_half || is_one_half_div {
                return Some(Node::Function(MathFunction::Sqrt, vec![*left.clone()]));
            }
        }
        None
    }
}

pub struct ExponentToExponentRule;
impl Rule for ExponentToExponentRule {
    fn name(&self) -> &'static str {
        "ExponentToExponent"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (x^y)^z -> x^(y*z)
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            if let Node::BinaryOp(MathOperator::Pow, l_left, l_right) = &**left {
                if let (Some(l_val), Some(r_val)) = (l_right.get_number(), right.get_number()) {
                    return Some(Node::BinaryOp(
                        MathOperator::Pow,
                        l_left.clone(),
                        Box::new(Node::Number(l_val * r_val)),
                    ));
                } else {
                    return Some(Node::BinaryOp(
                        MathOperator::Pow,
                        l_left.clone(),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            l_right.clone(),
                            right.clone(),
                        )),
                    ));
                }
            }
        }
        None
    }
}

pub struct ConstantRaisedToConstantRule;
impl Rule for ConstantRaisedToConstantRule {
    fn name(&self) -> &'static str {
        "ConstantRaisedToConstant"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            // Integer check in C#. We'll just check if it has a number value and if it is wholly integer
            if let (Some(l_val), Some(r_val)) = (left.get_number(), right.get_number()) {
                if l_val.fract() == 0.0 && r_val.fract() == 0.0 {
                    return Some(Node::Number(l_val.powf(r_val)));
                }
            }
        }
        None
    }
}

pub struct NegativeConstantRaisedToAPowerOfTwoRule;
impl Rule for NegativeConstantRaisedToAPowerOfTwoRule {
    fn name(&self) -> &'static str {
        "NegativeConstantRaisedToAPowerOfTwo"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (-x)^y where y is even -> x^y
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            if left.less_than_number(0.0) {
                if let (Some(l_val), Some(r_val)) = (left.get_number(), right.get_number()) {
                    if r_val.fract() == 0.0 && r_val % 2.0 == 0.0 {
                        return Some(Node::BinaryOp(
                            MathOperator::Pow,
                            Box::new(Node::Number(l_val.abs())),
                            right.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct AbsRaisedToPowerofTwoRule;
impl Rule for AbsRaisedToPowerofTwoRule {
    fn name(&self) -> &'static str {
        "AbsRaisedToPowerofTwo"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // abs(x)^2 -> x^2
        if let Node::BinaryOp(MathOperator::Pow, left, right) = node {
            if right.is_number(2.0) {
                if let Node::Function(MathFunction::Abs, args) = &**left {
                    if let Some(inner) = args.first() {
                        return Some(Node::BinaryOp(
                            MathOperator::Pow,
                            Box::new(inner.clone()),
                            right.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}
