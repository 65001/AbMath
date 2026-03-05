use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

pub struct CosOverSinToCotRule;
impl Rule for CosOverSinToCotRule {
    fn name(&self) -> &'static str {
        "CosOverSinToCot"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // C#: node.IsDivision() && node[0].IsFunction("sin") && node[1].IsFunction("cos") && node[0,0].Matches(node[1,0])
        // C# node[1] = numerator, node[0] = denominator
        // cos(x) / sin(x) -> cot(x)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let (
                Node::Function(MathFunction::Cos, cos_args),
                Node::Function(MathFunction::Sin, sin_args),
            ) = (&**left, &**right)
            {
                if let (Some(cos_inner), Some(sin_inner)) = (cos_args.first(), sin_args.first()) {
                    if cos_inner.matches_node(sin_inner) {
                        return Some(Node::Function(MathFunction::Cot, vec![cos_inner.clone()]));
                    }
                }
            }
        }
        None
    }
}

pub struct SinOverCosRule;
impl Rule for SinOverCosRule {
    fn name(&self) -> &'static str {
        "SinOverCos"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sin(x) / cos(x) -> tan(x)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let (
                Node::Function(MathFunction::Sin, sin_args),
                Node::Function(MathFunction::Cos, cos_args),
            ) = (&**left, &**right)
            {
                if let (Some(sin_inner), Some(cos_inner)) = (sin_args.first(), cos_args.first()) {
                    if sin_inner.matches_node(cos_inner) {
                        return Some(Node::Function(MathFunction::Tan, vec![sin_inner.clone()]));
                    }
                }
            }
        }
        None
    }
}

pub struct CosOverSinComplexRule;
impl Rule for CosOverSinComplexRule {
    fn name(&self) -> &'static str {
        "CosOverSinComplex"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // [cos(x) * f(x)] / sin(x) -> cot(x) * f(x)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::BinaryOp(MathOperator::Multiply, mul_left, mul_right) = &**left {
                if let Node::Function(MathFunction::Sin, sin_args) = &**right {
                    if let Some(sin_inner) = sin_args.first() {
                        // Check if mul_left is cos(x)
                        if let Node::Function(MathFunction::Cos, cos_args) = &**mul_left {
                            if let Some(cos_inner) = cos_args.first() {
                                if cos_inner.matches_node(sin_inner) {
                                    return Some(Node::BinaryOp(
                                        MathOperator::Multiply,
                                        mul_right.clone(),
                                        Box::new(Node::Function(
                                            MathFunction::Cot,
                                            vec![cos_inner.clone()],
                                        )),
                                    ));
                                }
                            }
                        }

                        // Check if mul_right is cos(x)
                        if let Node::Function(MathFunction::Cos, cos_args) = &**mul_right {
                            if let Some(cos_inner) = cos_args.first() {
                                if cos_inner.matches_node(sin_inner) {
                                    return Some(Node::BinaryOp(
                                        MathOperator::Multiply,
                                        mul_left.clone(), // Swap order to match C# logic (mostly arbitrary)
                                        Box::new(Node::Function(
                                            MathFunction::Cot,
                                            vec![cos_inner.clone()],
                                        )),
                                    ));
                                }
                            }
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct SecUnderToCosRule;
impl Rule for SecUnderToCosRule {
    fn name(&self) -> &'static str {
        "SecUnderToCos"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / sec(y) -> x * cos(y)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Sec, args) = &**right {
                if let Some(inner) = args.first() {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(Node::Function(MathFunction::Cos, vec![inner.clone()])),
                    ));
                }
            }
        }
        None
    }
}

pub struct CscUnderToSinRule;
impl Rule for CscUnderToSinRule {
    fn name(&self) -> &'static str {
        "CscUnderToSin"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / csc(y) -> x * sin(y)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Csc, args) = &**right {
                if let Some(inner) = args.first() {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(Node::Function(MathFunction::Sin, vec![inner.clone()])),
                    ));
                }
            }
        }
        None
    }
}

pub struct CotUnderToTanRule;
impl Rule for CotUnderToTanRule {
    fn name(&self) -> &'static str {
        "CotUnderToTan"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / cot(y) -> x * tan(y)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Cot, args) = &**right {
                if let Some(inner) = args.first() {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(Node::Function(MathFunction::Tan, vec![inner.clone()])),
                    ));
                }
            }
        }
        None
    }
}

pub struct CosUnderToSecRule;
impl Rule for CosUnderToSecRule {
    fn name(&self) -> &'static str {
        "CosUnderToSec"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / cos(y) -> x * sec(y)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Cos, args) = &**right {
                if let Some(inner) = args.first() {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(Node::Function(MathFunction::Sec, vec![inner.clone()])),
                    ));
                }
            }
        }
        None
    }
}

pub struct SinUnderToCscRule;
impl Rule for SinUnderToCscRule {
    fn name(&self) -> &'static str {
        "SinUnderToCsc"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / sin(y) -> x * csc(y)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Sin, args) = &**right {
                if let Some(inner) = args.first() {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(Node::Function(MathFunction::Csc, vec![inner.clone()])),
                    ));
                }
            }
        }
        None
    }
}

pub struct TanUnderToCotRule;
impl Rule for TanUnderToCotRule {
    fn name(&self) -> &'static str {
        "TanUnderToCot"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x / tan(y) -> x * cot(y)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Tan, args) = &**right {
                if let Some(inner) = args.first() {
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        left.clone(),
                        Box::new(Node::Function(MathFunction::Cot, vec![inner.clone()])),
                    ));
                }
            }
        }
        None
    }
}

// Even Identities: f(-x) = f(x)
// For cos, sec
// Odd Identities: f(-x) = -f(x)
// For sin, csc, tan, cot
pub struct CosEvenIdentityRule;
impl Rule for CosEvenIdentityRule {
    fn name(&self) -> &'static str {
        "CosEvenIdentity"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // cos(-1 * x) -> cos(x)
        if let Node::Function(MathFunction::Cos, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = arg0 {
                    if right.is_number(-1.0) {
                        return Some(Node::Function(MathFunction::Cos, vec![*left.clone()]));
                    } else if left.is_number(-1.0) {
                        return Some(Node::Function(MathFunction::Cos, vec![*right.clone()]));
                    }
                }
            }
        }
        None
    }
}

pub struct SecEvenIdentityRule;
impl Rule for SecEvenIdentityRule {
    fn name(&self) -> &'static str {
        "SecEvenIdentity"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sec(-1 * x) -> sec(x)
        if let Node::Function(MathFunction::Sec, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = arg0 {
                    if right.is_number(-1.0) {
                        return Some(Node::Function(MathFunction::Sec, vec![*left.clone()]));
                    } else if left.is_number(-1.0) {
                        return Some(Node::Function(MathFunction::Sec, vec![*right.clone()]));
                    }
                }
            }
        }
        None
    }
}

pub struct SinOddIdentityRule;
impl Rule for SinOddIdentityRule {
    fn name(&self) -> &'static str {
        "SinOddIdentity"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sin(-1 * x) -> -1 * sin(x)
        if let Node::Function(MathFunction::Sin, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = arg0 {
                    if right.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Sin, vec![*left.clone()])),
                        ));
                    } else if left.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Sin, vec![*right.clone()])),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct TanOddIdentityRule;
impl Rule for TanOddIdentityRule {
    fn name(&self) -> &'static str {
        "TanOddIdentity"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // tan(-1 * x) -> -1 * tan(x)
        if let Node::Function(MathFunction::Tan, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = arg0 {
                    if right.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Tan, vec![*left.clone()])),
                        ));
                    } else if left.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Tan, vec![*right.clone()])),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct CotOddIdentityRule;
impl Rule for CotOddIdentityRule {
    fn name(&self) -> &'static str {
        "CotOddIdentity"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // cot(-1 * x) -> -1 * cot(x)
        if let Node::Function(MathFunction::Cot, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = arg0 {
                    if right.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Cot, vec![*left.clone()])),
                        ));
                    } else if left.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Cot, vec![*right.clone()])),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct CscOddIdentityRule;
impl Rule for CscOddIdentityRule {
    fn name(&self) -> &'static str {
        "CscOddIdentity"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // csc(-1 * x) -> -1 * csc(x)
        if let Node::Function(MathFunction::Csc, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = arg0 {
                    if right.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Csc, vec![*left.clone()])),
                        ));
                    } else if left.is_number(-1.0) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(-1.0)),
                            Box::new(Node::Function(MathFunction::Csc, vec![*right.clone()])),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct TrigIdentitySinToCosRule;
impl Rule for TrigIdentitySinToCosRule {
    fn name(&self) -> &'static str {
        "TrigIdentitySinToCos"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // 1 - sin(x)^2 -> cos(x)^2
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if left.is_number(1.0) {
                if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = &**right {
                    if pow_exp.is_number(2.0) {
                        if let Node::Function(MathFunction::Sin, sin_args) = &**pow_base {
                            if let Some(sin_inner) = sin_args.first() {
                                return Some(Node::BinaryOp(
                                    MathOperator::Pow,
                                    Box::new(Node::Function(
                                        MathFunction::Cos,
                                        vec![sin_inner.clone()],
                                    )),
                                    Box::new(Node::Number(2.0)),
                                ));
                            }
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct TrigIdentityCosToSinRule;
impl Rule for TrigIdentityCosToSinRule {
    fn name(&self) -> &'static str {
        "TrigIdentityCosToSin"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // 1 - cos(x)^2 -> sin(x)^2
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if left.is_number(1.0) {
                if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = &**right {
                    if pow_exp.is_number(2.0) {
                        if let Node::Function(MathFunction::Cos, cos_args) = &**pow_base {
                            if let Some(cos_inner) = cos_args.first() {
                                return Some(Node::BinaryOp(
                                    MathOperator::Pow,
                                    Box::new(Node::Function(
                                        MathFunction::Sin,
                                        vec![cos_inner.clone()],
                                    )),
                                    Box::new(Node::Number(2.0)),
                                ));
                            }
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct TrigIdentitySinPlusCosRule;
impl Rule for TrigIdentitySinPlusCosRule {
    fn name(&self) -> &'static str {
        "TrigIdentitySinPlusCos"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sin(x)^2 + cos(x)^2 -> 1
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Pow, l_base, l_exp),
                Node::BinaryOp(MathOperator::Pow, r_base, r_exp),
            ) = (&**left, &**right)
            {
                if l_exp.is_number(2.0) && r_exp.is_number(2.0) {
                    let mut found_sin = false;
                    let mut found_cos = false;
                    let mut matcher_obj = None;

                    if let Node::Function(MathFunction::Sin, args) = &**l_base {
                        found_sin = true;
                        matcher_obj = args.first().cloned();
                    } else if let Node::Function(MathFunction::Cos, args) = &**l_base {
                        found_cos = true;
                        matcher_obj = args.first().cloned();
                    }

                    if let Node::Function(MathFunction::Sin, args) = &**r_base {
                        if found_cos {
                            if let Some(arg) = args.first() {
                                if arg.matches_node(matcher_obj.as_ref().unwrap()) {
                                    return Some(Node::Number(1.0));
                                }
                            }
                        }
                    } else if let Node::Function(MathFunction::Cos, args) = &**r_base {
                        if found_sin {
                            if let Some(arg) = args.first() {
                                if arg.matches_node(matcher_obj.as_ref().unwrap()) {
                                    return Some(Node::Number(1.0));
                                }
                            }
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct CosOverSinToCotComplexComplexRule;
impl Rule for CosOverSinToCotComplexComplexRule {
    fn name(&self) -> &'static str {
        "CosOverSinToCotComplexComplex"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // cos(x) / (sin(x) * f(x)) -> cot(x) / f(x)
        if let Node::BinaryOp(MathOperator::Divide, left, right) = node {
            if let Node::Function(MathFunction::Cos, cos_args) = &**left {
                if let Some(cos_inner) = cos_args.first() {
                    if let Node::BinaryOp(MathOperator::Multiply, r_left, r_right) = &**right {
                        if let Node::Function(MathFunction::Sin, sin_args) = &**r_left {
                            if let Some(sin_inner) = sin_args.first() {
                                if cos_inner.matches_node(sin_inner) {
                                    return Some(Node::BinaryOp(
                                        MathOperator::Divide,
                                        Box::new(Node::Function(
                                            MathFunction::Cot,
                                            vec![cos_inner.clone()],
                                        )),
                                        r_right.clone(),
                                    ));
                                }
                            }
                        } else if let Node::Function(MathFunction::Sin, sin_args) = &**r_right {
                            if let Some(sin_inner) = sin_args.first() {
                                if cos_inner.matches_node(sin_inner) {
                                    return Some(Node::BinaryOp(
                                        MathOperator::Divide,
                                        Box::new(Node::Function(
                                            MathFunction::Cot,
                                            vec![cos_inner.clone()],
                                        )),
                                        r_left.clone(),
                                    ));
                                }
                            }
                        }
                    }
                }
            }
        }
        None
    }
}
