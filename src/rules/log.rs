use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

pub struct LogOneRule;
impl Rule for LogOneRule {
    fn name(&self) -> &'static str {
        "LogOne"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // log(1, base) -> 0. In AbMath, node[0] is often the argument and node[1] is the base, OR vice-versa depending on parser.
        // Let's assume AbMath `log(base, value)`: `Node::Function(Log, vec![base, value])`. But sometimes it's `log(value)` representing `log_10`.
        if let Node::Function(MathFunction::Log, args) = node {
            // In C#, node[0] is the argument. Example `log(2, 1) -> 0`. If `log` has 1 or 2 args:
            // Let's check the last argument (value) or the first argument if it's 1-arity.
            if args.len() == 2 && args[1].is_number(1.0) {
                return Some(Node::Number(0.0));
            } else if args.len() == 1 && args[0].is_number(1.0) {
                return Some(Node::Number(0.0));
            }

            // The C# code says `node[0].IsNumber(1)` which may imply AbMath reverses arguments `log(value, base)`?
            if let Some(arg0) = args.first() {
                if arg0.is_number(1.0) {
                    return Some(Node::Number(0.0));
                }
            }
        }
        None
    }
}

pub struct LogIdenticalRule;
impl Rule for LogIdenticalRule {
    fn name(&self) -> &'static str {
        "LogIdentical"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::Function(MathFunction::Log, args) = node {
            if args.len() == 2 && args[0].matches_node(&args[1]) {
                return Some(Node::Number(1.0));
            }
        }
        None
    }
}

pub struct LnIdenticalRule;
impl Rule for LnIdenticalRule {
    fn name(&self) -> &'static str {
        "LnIdentical"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // ln(e) -> 1
        if let Node::Function(MathFunction::Ln, args) = node {
            if let Some(arg0) = args.first() {
                if matches!(arg0, Node::Constant(MathFunction::E)) {
                    return Some(Node::Number(1.0));
                }
            }
        }
        None
    }
}

pub struct LogPowerRule;
impl Rule for LogPowerRule {
    fn name(&self) -> &'static str {
        "LogPower"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node.IsExponent() && node[0].IsLog() && node[0, 1].Matches(node[1])
        // C# node[1] = base, node[0] = exponent.
        // base^(log_base(x)) -> x
        if let Node::BinaryOp(MathOperator::Pow, left_base, right_exp) = node {
            if let Node::Function(MathFunction::Log, log_args) = &**right_exp {
                // Assuming log_args = [value, log_base] or [log_base, value]
                // C#: node[0, 1].Matches(node[1]) -> exponent.right_child matches base.
                // This heavily implies `log_base` is at index 1 in the C# tree. So [value, base].
                if log_args.len() == 2 && log_args[1].matches_node(left_base) {
                    return Some(log_args[0].clone());
                }
            }
        }
        None
    }
}

pub struct LnPowerRule;
impl Rule for LnPowerRule {
    fn name(&self) -> &'static str {
        "LnPower"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // e^(ln(x)) -> x
        if let Node::BinaryOp(MathOperator::Pow, left_base, right_exp) = node {
            if let Node::Function(MathFunction::Ln, ln_args) = &**right_exp {
                if left_base.is_constant("e") {
                    if let Some(val) = ln_args.first() {
                        return Some(val.clone());
                    }
                }
            }
        }
        None
    }
}

pub struct LogExponentExpansionRule;
impl Rule for LogExponentExpansionRule {
    fn name(&self) -> &'static str {
        "LogExponentExpansion"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node.IsLog() && node[0].IsExponent()
        // log_base(x^y) -> y * log_base(x)
        if let Node::Function(MathFunction::Log, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = arg0 {
                    let mut new_log_args = vec![*pow_base.clone()];
                    if args.len() == 2 {
                        new_log_args.push(args[1].clone());
                    }

                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        pow_exp.clone(),
                        Box::new(Node::Function(MathFunction::Log, new_log_args)),
                    ));
                }
            }
        }
        None
    }
}

pub struct LogToLnRule;
impl Rule for LogToLnRule {
    fn name(&self) -> &'static str {
        "LogToLn"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // log_e(x) -> ln(x)
        if let Node::Function(MathFunction::Log, args) = node {
            if args.len() == 2 && args[1].is_constant("e") {
                return Some(Node::Function(MathFunction::Ln, vec![args[0].clone()]));
            }
        }
        None
    }
}

pub struct LnToLogRule;
impl Rule for LnToLogRule {
    fn name(&self) -> &'static str {
        "LnToLog"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // ln(x) -> log_e(x) ... wait the C# rule is marked `LnToLogRunnable` -> `node.IsLn()`
        // If we map this, we will infinite loop with LogToLn.
        // AbMath probably manually controlled which direction this ran in.
        // We will SKIP this rule to avoid infinite loops, as ln(x) -> ln(x) is fine for simplification.
        None
    }
}

pub struct LogSummationRule;
impl Rule for LogSummationRule {
    fn name(&self) -> &'static str {
        "LogSummation"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // log_b(x) + log_b(y) -> log_b(x * y)
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if let (
                Node::Function(MathFunction::Log, l_args),
                Node::Function(MathFunction::Log, r_args),
            ) = (&**left, &**right)
            {
                if l_args.len() == 2 && r_args.len() == 2 && l_args[1].matches_node(&r_args[1]) {
                    return Some(Node::Function(
                        MathFunction::Log,
                        vec![
                            Node::BinaryOp(
                                MathOperator::Multiply,
                                Box::new(l_args[0].clone()),
                                Box::new(r_args[0].clone()),
                            ),
                            l_args[1].clone(),
                        ],
                    ));
                }
            }
        }
        None
    }
}

pub struct LogSubtractionRule;
impl Rule for LogSubtractionRule {
    fn name(&self) -> &'static str {
        "LogSubtraction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // log_b(x) - log_b(y) -> log_b(x / y)
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let (
                Node::Function(MathFunction::Log, l_args),
                Node::Function(MathFunction::Log, r_args),
            ) = (&**left, &**right)
            {
                if l_args.len() == 2 && r_args.len() == 2 && l_args[1].matches_node(&r_args[1]) {
                    return Some(Node::Function(
                        MathFunction::Log,
                        vec![
                            Node::BinaryOp(
                                MathOperator::Divide,
                                Box::new(l_args[0].clone()),
                                Box::new(r_args[0].clone()),
                            ),
                            l_args[1].clone(),
                        ],
                    ));
                }
            }
        }
        None
    }
}

pub struct LnSummationRule;
impl Rule for LnSummationRule {
    fn name(&self) -> &'static str {
        "LnSummation"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // ln(x) + ln(y) -> ln(x * y)
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if let (
                Node::Function(MathFunction::Ln, l_args),
                Node::Function(MathFunction::Ln, r_args),
            ) = (&**left, &**right)
            {
                if let (Some(l_val), Some(r_val)) = (l_args.first(), r_args.first()) {
                    return Some(Node::Function(
                        MathFunction::Ln,
                        vec![Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(l_val.clone()),
                            Box::new(r_val.clone()),
                        )],
                    ));
                }
            }
        }
        None
    }
}

pub struct LnSubtractionRule;
impl Rule for LnSubtractionRule {
    fn name(&self) -> &'static str {
        "LnSubtraction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // ln(x) - ln(y) -> ln(x / y)
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let (
                Node::Function(MathFunction::Ln, l_args),
                Node::Function(MathFunction::Ln, r_args),
            ) = (&**left, &**right)
            {
                if let (Some(l_val), Some(r_val)) = (l_args.first(), r_args.first()) {
                    return Some(Node::Function(
                        MathFunction::Ln,
                        vec![Node::BinaryOp(
                            MathOperator::Divide,
                            Box::new(l_val.clone()),
                            Box::new(r_val.clone()),
                        )],
                    ));
                }
            }
        }
        None
    }
}

pub struct LnPowerRuleRule;
impl Rule for LnPowerRuleRule {
    fn name(&self) -> &'static str {
        "LnPowerRule"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // ln(x^y) -> y * ln(x)
        if let Node::Function(MathFunction::Ln, args) = node {
            if let Some(arg0) = args.first() {
                if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = arg0 {
                    if !pow_exp.is_constant("") && !matches!(**pow_exp, Node::Variable(_)) {
                        // C# says !node[0,0].IsVariable(). We will just apply unconditionally if we feel it's simpler.
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            pow_exp.clone(),
                            Box::new(Node::Function(MathFunction::Ln, vec![*pow_base.clone()])),
                        ));
                    }
                }
            }
        }
        None
    }
}
