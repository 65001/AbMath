use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

pub struct SqrtNegativeNumbersRule;
impl Rule for SqrtNegativeNumbersRule {
    fn name(&self) -> &'static str {
        "SqrtNegativeNumbers"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sqrt(-x) -> NaN (unless handling complex numbers)
        if let Node::Function(MathFunction::Sqrt, args) = node {
            if let Some(arg0) = args.first() {
                if arg0.less_than_number(0.0) {
                    return Some(Node::Number(f64::NAN));
                }
            }
        }
        None
    }
}

pub struct SqrtToFuncRule;
impl Rule for SqrtToFuncRule {
    fn name(&self) -> &'static str {
        "SqrtToFunc"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sqrt(x)^2 -> x
        if let Node::BinaryOp(MathOperator::Pow, left_base, right_exp) = node {
            if right_exp.is_number(2.0) {
                if let Node::Function(MathFunction::Sqrt, sqrt_args) = &**left_base {
                    if let Some(inner) = sqrt_args.first() {
                        return Some(inner.clone());
                    }
                }
            }
        }
        None
    }
}

pub struct SqrtToAbsRule;
impl Rule for SqrtToAbsRule {
    fn name(&self) -> &'static str {
        "SqrtToAbs"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sqrt(x^2) -> abs(x)
        if let Node::Function(MathFunction::Sqrt, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = arg0 {
                    if pow_exp.is_number(2.0) {
                        return Some(Node::Function(MathFunction::Abs, vec![*pow_base.clone()]));
                    }
                }
            }
        }
        None
    }
}

pub struct SqrtPowerFourRule;
impl Rule for SqrtPowerFourRule {
    fn name(&self) -> &'static str {
        "SqrtPowerFour"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sqrt(x^y) where y % 4 == 0 -> x^(y/2)
        // Let's actually just do y % 2 == 0 more generally if it's even unless the C# code strictly requires % 4?
        // Ah, the C# code says `node[0,0].GetNumber() % 4 == 0` and returns `Pow(base, num/2)`.
        // We'll stick strictly to `% 4 == 0` to identically match `SqrtPowerFourRunnable`.
        if let Node::Function(MathFunction::Sqrt, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = arg0 {
                    if let Some(val) = pow_exp.get_number() {
                        if val.fract() == 0.0 && val % 4.0 == 0.0 {
                            return Some(Node::BinaryOp(
                                MathOperator::Pow,
                                pow_base.clone(),
                                Box::new(Node::Number(val / 2.0)),
                            ));
                        }
                    }
                }
            }
        }
        None
    }
}
