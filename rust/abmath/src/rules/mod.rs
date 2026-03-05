pub mod addition;
pub mod derive;
pub mod division;
pub mod exponent;
pub mod integrate;
pub mod log;
pub mod misc;
pub mod multiplication;
pub mod sqrt;
pub mod subtraction;
pub mod sum;
pub mod trig;

use crate::ast::Node;
use std::collections::HashSet;
use std::hash::{DefaultHasher, Hash, Hasher};

/// Represents a history state of an applied rule
#[derive(Debug, Clone)]
pub struct RuleLog {
    pub rule_name: &'static str,
    pub before: Node,
    pub after: Node,
}

pub trait Rule {
    fn name(&self) -> &'static str;

    /// Returns Some(Node) if the node can be simplified/rewritten.
    /// Otherwise returns None.
    fn apply(&self, node: &Node) -> Option<Node>;
}

pub fn standard_rules() -> Vec<Box<dyn Rule>> {
    vec![
        Box::new(derive::DerivativeRule),
        // Addition
        Box::new(addition::AdditionToMultiplicationRule),
        Box::new(addition::ZeroAdditionRule),
        Box::new(addition::AdditionSwapRule),
        Box::new(addition::SimpleCoefficientRule),
        Box::new(addition::ComplexCoefficientRule),
        Box::new(addition::AdditionToSubtractionRuleOne),
        Box::new(addition::AdditionToSubtractionRuleTwo),
        Box::new(addition::ComplexNodeAdditionRule),
        Box::new(addition::DivisionAdditionRule),
        // Subtraction
        Box::new(subtraction::SameFunctionRule),
        Box::new(subtraction::SameFunctionObstructedRule),
        Box::new(subtraction::CoefficientOneReductionRule),
        Box::new(subtraction::SubtractionByZeroRule),
        Box::new(subtraction::ZeroSubtractedByFunctionRule),
        Box::new(subtraction::SubtractionDivisionCommonDenominatorRule),
        Box::new(subtraction::CoefficientReductionRule),
        Box::new(subtraction::ConstantToAdditionRule),
        Box::new(subtraction::FunctionToAdditionRule),
        Box::new(subtraction::DistributiveSimpleRule),
        // Exponent
        Box::new(exponent::FunctionRaisedToOneRule),
        Box::new(exponent::FunctionRaisedToZeroRule),
        Box::new(exponent::ZeroRaisedToConstantRule),
        Box::new(exponent::OneRaisedToFunctionRule),
        Box::new(exponent::ToDivisionRule),
        Box::new(exponent::ToSqrtRule),
        Box::new(exponent::ExponentToExponentRule),
        Box::new(exponent::ConstantRaisedToConstantRule),
        Box::new(exponent::NegativeConstantRaisedToAPowerOfTwoRule),
        Box::new(exponent::AbsRaisedToPowerofTwoRule),
        // Multiplication
        Box::new(multiplication::MultiplicationToExponentRule),
        Box::new(multiplication::MultiplicationByOneRule),
        Box::new(multiplication::MultiplicationByZeroRule),
        Box::new(multiplication::IncreaseExponentRule),
        Box::new(multiplication::IncreaseExponentTwoRule),
        Box::new(multiplication::IncreaseExponentThreeRule),
        Box::new(multiplication::DualNodeMultiplicationRule),
        Box::new(multiplication::ExpressionTimesDivisionRule),
        Box::new(multiplication::DivisionTimesDivisionRule),
        Box::new(multiplication::NegativeTimesNegativeRule),
        Box::new(multiplication::ComplexNegativeNegativeRule),
        Box::new(multiplication::NegativeTimesConstantRule),
        Box::new(multiplication::NegativeOneDistributedRule),
        Box::new(multiplication::DistributeFunctionRule),
        // Division
        Box::new(division::DivisionByZeroRule),
        Box::new(division::DivisionByOneRule),
        Box::new(division::GCDRule),
        Box::new(division::DivisionFlipRule),
        Box::new(division::DivisionFlipTwoRule),
        Box::new(division::DivisionCancelingRule),
        Box::new(division::PowerReductionRule),
        // Logarithm
        Box::new(log::LogOneRule),
        Box::new(log::LogIdenticalRule),
        Box::new(log::LnIdenticalRule),
        Box::new(log::LogPowerRule),
        Box::new(log::LnPowerRule),
        Box::new(log::LogExponentExpansionRule),
        Box::new(log::LogToLnRule),
        Box::new(log::LnToLogRule),
        Box::new(log::LogSummationRule),
        Box::new(log::LogSubtractionRule),
        Box::new(log::LnSummationRule),
        Box::new(log::LnSubtractionRule),
        Box::new(log::LnPowerRuleRule),
        // Sqrt
        Box::new(sqrt::SqrtNegativeNumbersRule),
        Box::new(sqrt::SqrtToFuncRule),
        Box::new(sqrt::SqrtToAbsRule),
        Box::new(sqrt::SqrtPowerFourRule),
        // Sum
        Box::new(sum::PropagationRule),
        Box::new(sum::VariableRule),
        Box::new(sum::ConstantComplexRule),
        Box::new(sum::CoefficientRule),
        Box::new(sum::CoefficientDivisionRule),
        Box::new(sum::PowerRule),
        // Trig
        Box::new(trig::CosOverSinToCotRule),
        Box::new(trig::SinOverCosRule),
        Box::new(trig::CosOverSinComplexRule),
        Box::new(trig::SecUnderToCosRule),
        Box::new(trig::CscUnderToSinRule),
        Box::new(trig::CotUnderToTanRule),
        Box::new(trig::CosUnderToSecRule),
        Box::new(trig::SinUnderToCscRule),
        Box::new(trig::TanUnderToCotRule),
        Box::new(trig::CosEvenIdentityRule),
        Box::new(trig::SecEvenIdentityRule),
        Box::new(trig::SinOddIdentityRule),
        Box::new(trig::TanOddIdentityRule),
        Box::new(trig::CotOddIdentityRule),
        Box::new(trig::CscOddIdentityRule),
        Box::new(trig::TrigIdentitySinToCosRule),
        Box::new(trig::TrigIdentityCosToSinRule),
        Box::new(trig::TrigIdentitySinPlusCosRule),
        Box::new(trig::CosOverSinToCotComplexComplexRule),
        // Integrate
        Box::new(integrate::PropagationRule),
        Box::new(integrate::ConstantsRule),
        Box::new(integrate::CoefficientRule),
        Box::new(integrate::SingleVariableRule),
        // Misc
        Box::new(misc::ZeroFactorialRule),
    ]
}

/// Applies rules to an AST bottom-up until a fixed point is reached or a loop is detected.
pub struct RuleEngine {
    pub rules: Vec<Box<dyn Rule>>,
    pub logs: Vec<RuleLog>,
    seen_states: HashSet<u64>,
}

impl RuleEngine {
    pub fn new() -> Self {
        Self {
            rules: Vec::new(),
            logs: Vec::new(),
            seen_states: HashSet::new(),
        }
    }

    pub fn add_rule(&mut self, rule: Box<dyn Rule>) {
        self.rules.push(rule);
    }

    fn hash_node(node: &Node) -> u64 {
        let mut hasher = DefaultHasher::new();
        node.hash(&mut hasher);
        hasher.finish()
    }

    /// Recursively apply rules to the AST bottom-up.
    pub fn simplify(&mut self, mut root: Node) -> Node {
        self.seen_states.clear();
        self.logs.clear();

        loop {
            let hash = Self::hash_node(&root);
            if !self.seen_states.insert(hash) {
                // We've seen this exact AST state before, meaning we're stuck in a loop.
                // Stop applying rules.
                break;
            }

            let (new_root, changed) = self.apply_bottom_up(root);
            root = new_root;

            if !changed {
                // Reached a fixed point
                break;
            }
        }

        root
    }

    fn apply_bottom_up(&mut self, mut node: Node) -> (Node, bool) {
        let mut child_changed = false;

        // 1. Traverse children first
        match &mut node {
            Node::UnaryOp(_, inner) => {
                let (new_inner, changed) = self.apply_bottom_up(*inner.clone());
                if changed {
                    *inner = Box::new(new_inner);
                    child_changed = true;
                }
            }
            Node::BinaryOp(_, left, right) => {
                let (new_left, l_changed) = self.apply_bottom_up(*left.clone());
                let (new_right, r_changed) = self.apply_bottom_up(*right.clone());
                if l_changed {
                    *left = Box::new(new_left);
                    child_changed = true;
                }
                if r_changed {
                    *right = Box::new(new_right);
                    child_changed = true;
                }
            }
            Node::Function(_, args) | Node::List(args) => {
                for arg in args.iter_mut() {
                    let (new_arg, changed) = self.apply_bottom_up(arg.clone());
                    if changed {
                        *arg = new_arg;
                        child_changed = true;
                    }
                }
            }
            Node::Matrix(rows) => {
                for row in rows.iter_mut() {
                    for arg in row.iter_mut() {
                        let (new_arg, changed) = self.apply_bottom_up(arg.clone());
                        if changed {
                            *arg = new_arg;
                            child_changed = true;
                        }
                    }
                }
            }
            Node::Number(_) | Node::Variable(_) | Node::Constant(_) => {}
        }

        if child_changed {
            return (node, true);
        }

        // 2. Apply rules to the current node
        for rule in &self.rules {
            if let Some(new_node) = rule.apply(&node) {
                self.logs.push(RuleLog {
                    rule_name: rule.name(),
                    before: node.clone(),
                    after: new_node.clone(),
                });

                // Return immediately upon first successful rewrite (we'll catch it in the loop)
                return (new_node, true);
            }
        }

        (node, false)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::ast::build_ast;
    use crate::tokenizer::{tokenize, DataStore};
    use crate::Shunter;

    fn get_ast_node(input: &str) -> Node {
        let ds = DataStore::default();
        let tokens = tokenize(input, &ds);
        let shunter = Shunter::new(&ds);
        let rpn = shunter.shunt(tokens);
        build_ast(rpn).unwrap()
    }

    #[test]
    fn test_zero_addition_simplification() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(crate::rules::addition::ZeroAdditionRule));

        let ast = get_ast_node("0 + x + 0");
        let simplified = engine.simplify(ast);

        assert_eq!(simplified, Node::Variable("x".to_string()));
        assert_eq!(engine.logs.len(), 2);
        assert_eq!(engine.logs[0].rule_name, "ZeroAddition");
    }

    // A cyclic rule designed purely for testing loop-detection
    pub struct PingPongRule;
    impl Rule for PingPongRule {
        fn name(&self) -> &'static str {
            "PingPong"
        }

        fn apply(&self, node: &Node) -> Option<Node> {
            // ping -> pong
            if let Node::Variable(s) = node {
                if s == "ping" {
                    return Some(Node::Variable("pong".to_string()));
                } else if s == "pong" {
                    return Some(Node::Variable("ping".to_string()));
                }
            }
            None
        }
    }

    #[test]
    fn test_loop_detection() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(PingPongRule));

        // Start with ping. It will turn to pong, then ping, then loop.
        let ast = Node::Variable("ping".to_string());
        let _ = engine.simplify(ast.clone());

        // The engine should break gracefully and have seen both states.
        assert_eq!(engine.seen_states.len(), 2);
        assert_eq!(engine.logs.len(), 2);
    }
}
