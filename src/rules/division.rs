use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathOperator;

// node[0] is typically right/denominator, node[1] is left/numerator in C# RPN.
// We adopt the Rust AST convention: left (num), right (denom).

pub struct DivisionByZeroRule;
impl Rule for DivisionByZeroRule {
    fn name(&self) -> &'static str {
        "DivisionByZero"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Divide, _, right) = node {
            if right.is_number(0.0) {
                // Return NaN or an Error node. The C# code returned double.NaN
                return Some(Node::Number(f64::NAN));
            }
        }
        None
    }
}

pub struct DivisionByOneRule;
impl Rule for DivisionByOneRule {
    fn name(&self) -> &'static str {
        "DivisionByOne"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if right.is_number(1.0) {
                return Some(*left.clone());
            }
        }
        None
    }
}

pub struct GCDRule;
impl Rule for GCDRule {
    fn name(&self) -> &'static str {
        "GCD"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / y -> (x/gcd) / (y/gcd) if integer
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let (Some(l_val), Some(r_val)) = (left.get_number(), right.get_number()) {
                if l_val.fract() == 0.0 && r_val.fract() == 0.0 {
                    // Let's find simple gcd
                    let mut a = l_val.abs() as i64;
                    let mut b = r_val.abs() as i64;
                    while b != 0 {
                        let temp = b;
                        b = a % b;
                        a = temp;
                    }
                    let gcd = a as f64;
                    if gcd > 1.0 {
                        return Some(Node::BinaryOp(
                            MathOperator::Divide,
                            Box::new(Node::Number(l_val / gcd)),
                            Box::new(Node::Number(r_val / gcd)),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct DivisionFlipRule;
impl Rule for DivisionFlipRule {
    fn name(&self) -> &'static str {
        "DivisionFlip"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (a / b) / (c / d) -> (a * d) / (c * b)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Divide, l_num, l_denom),
                Node::BinaryOp(MathOperator::Divide, r_num, r_denom),
            ) = (&**left, &**right)
            {
                return Some(Node::BinaryOp(
                    MathOperator::Divide,
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        l_num.clone(),
                        r_denom.clone(),
                    )),
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        r_num.clone(),
                        l_denom.clone(),
                    )),
                ));
            }
        }
        None
    }
}

pub struct DivisionFlipTwoRule;
impl Rule for DivisionFlipTwoRule {
    fn name(&self) -> &'static str {
        "DivisionFlipTwo"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (a / b) / c -> a / (b * c)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::BinaryOp(MathOperator::Divide, l_num, l_denom) = &**left {
                if !right.is_division() {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        l_num.clone(),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            l_denom.clone(),
                            right.clone(),
                        )),
                    ));
                }
            }
        }
        None
    }
}

pub struct DivisionCancelingRule;
impl Rule for DivisionCancelingRule {
    fn name(&self) -> &'static str {
        "DivisionCanceling"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (x * c) / c -> x
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::BinaryOp(MathOperator::Multiply, l_left, l_right) = &**left {
                if let Some(_val) = right.get_number() {
                    // We could also enforce it's not 0 by checking right != 0, but DivisionByZero should catch it if bottom up
                    if l_right.matches_node(right) && !right.is_number(0.0) {
                        return Some(*l_left.clone());
                    } else if l_left.matches_node(right) && !right.is_number(0.0) {
                        return Some(*l_right.clone());
                    }
                }
            }
        }
        None
    }
}

pub struct PowerReductionRule;
impl Rule for PowerReductionRule {
    fn name(&self) -> &'static str {
        "PowerReduction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (x^4) / (x^2) -> (x^3) / x (Wait... the C# says node[0,0] - reduction)
        // C# Reduction: Min(pow_num, pow_denom) - 1.
        // It's a way to cancel exponents.
        // Let's implement full cancel: x^a / x^b = x^(a-b)
        // If a-b = 0, that's 1. If a-b < 0, it's 1 / x^(b-a)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Pow, l_base, l_exp),
                Node::BinaryOp(MathOperator::Pow, r_base, r_exp),
            ) = (&**left, &**right)
            {
                if l_base.matches_node(r_base) {
                    if let (Some(l_val), Some(r_val)) = (l_exp.get_number(), r_exp.get_number()) {
                        if l_val > r_val {
                            // Returns x^(l - r) ... technically divided by 1, but we can just return x^(l-r)
                            return Some(Node::BinaryOp(
                                MathOperator::Pow,
                                l_base.clone(),
                                Box::new(Node::Number(l_val - r_val)),
                            ));
                        } else if r_val > l_val {
                            // Returns 1 / x^(r - l)
                            return Some(Node::BinaryOp(
                                MathOperator::Divide,
                                Box::new(Node::Number(1.0)),
                                Box::new(Node::BinaryOp(
                                    MathOperator::Pow,
                                    l_base.clone(),
                                    Box::new(Node::Number(r_val - l_val)),
                                )),
                            ));
                        } else {
                            // Equal powers => 1 (Assuming not 0/0)
                            return Some(Node::Number(1.0));
                        }
                    }
                }
            }
        }
        None
    }
}

// TODO: FactorialCancellation and FactorialRemoved wait on Factorial operator/function support in AST.
