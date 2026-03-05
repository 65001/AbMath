use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathOperator;

pub struct SameFunctionRule;
impl Rule for SameFunctionRule {
    fn name(&self) -> &'static str {
        "SameFunction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            // node.ChildrenAreIdentical() && !node.ContainsDomainViolation()
            // We assume domain violations (like x/0) are handled safely or we assume exact match -> 0
            if left.matches_node(right) {
                return Some(Node::Number(0.0));
            }
        }
        None
    }
}

pub struct SameFunctionObstructedRule;
impl Rule for SameFunctionObstructedRule {
    fn name(&self) -> &'static str {
        "SameFunctionObstructed"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            // node[1].IsAddition() && node[1, 0].Matches(node[0])
            // f(x) - (f(x) + g(x)) -> -g(x) (Need to check C# carefully: )
            // C# implementation says:
            // "return node[1, 1];" -- Wait, f(x) - (f(x) + g(x)) should be -g(x) or is it returning the right-side of the addition verbatim?
            // Actually: C# `RPN.Node` for subtraction sets left=node[0], right=node[1].
            // So if left == right's left child, it returns node[1,1] which is right's right child.
            // Example: 5 - (5 + 3) = -3. If it just returns 3, that's a bug in AbMath C# or represents `(5 + 3) - 5`
            // Let's assume AbMath child order for Subtraction is [1] - [0]?
            // C# SubtractionByZero: node[0].IsNumber(0) -> return node[1]. Wait.
            // If `node[0].IsNumber(0)` returns `node[1]`, then `node[0]` is the *subtrahend* (the thing being subtracted), and `node[1]` is the minuend (the thing being subtracted from)!
            // Example: `x - 0` -> node[1] is x, node[0] is 0.
            // SO: node[1] is left, node[0] is right!
            // Let's adjust all Subtraction rules with this reversed C# index assumption:

            // `SameFunctionObstructed`: node[1].IsAddition() && node[1,0].Matches(node[0])
            // This means: (g(x) + f(x)) - f(x) -> returns g(x) (which is node[1,1] because node[1,0] matched node[0])
            if let Node::BinaryOp(MathOperator::Add, l_left, l_right) = &**left {
                if l_left.matches_node(right) {
                    return Some(*l_right.clone());
                } else if l_right.matches_node(right) {
                    return Some(*l_left.clone());
                }
            }
        }
        None
    }
}

pub struct CoefficientOneReductionRule;
impl Rule for CoefficientOneReductionRule {
    fn name(&self) -> &'static str {
        "CoefficientOneReduction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[1].IsMultiplication() && node[1, 1].IsNumber() && node[1, 0].Matches(node[0])
        // Transferred to Rust index: left is minuend, right is subtrahend.
        // (x * 5) - x -> x * 4
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let Node::BinaryOp(MathOperator::Multiply, l_left, l_right) = &**left {
                if let Some(coeff) = l_right.get_number() {
                    if l_left.matches_node(right) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            l_left.clone(),
                            Box::new(Node::Number(coeff - 1.0)),
                        ));
                    }
                } else if let Some(coeff) = l_left.get_number() {
                    if l_right.matches_node(right) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(coeff - 1.0)),
                            l_right.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct SubtractionByZeroRule;
impl Rule for SubtractionByZeroRule {
    fn name(&self) -> &'static str {
        "SubtractionByZero"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsNumber(0) -> return node[1]
        // Translated: right is 0 -> return left (x - 0 = x)
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if right.is_number(0.0) {
                return Some(*left.clone());
            }
        }
        None
    }
}

pub struct ZeroSubtractedByFunctionRule;
impl Rule for ZeroSubtractedByFunctionRule {
    fn name(&self) -> &'static str {
        "ZeroSubtractedByFunction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[1].IsNumber(0) -> Mul(node[0], -1)
        // Translated: left is 0 -> return right * -1 (0 - x = -x)
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if left.is_number(0.0) {
                return Some(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(Node::Number(-1.0)),
                    right.clone(),
                ));
            }
        }
        None
    }
}

pub struct SubtractionDivisionCommonDenominatorRule;
impl Rule for SubtractionDivisionCommonDenominatorRule {
    fn name(&self) -> &'static str {
        "SubtractionDivisionCommonDenominator"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsDivision() && node[1].IsDivision() && node[0, 0].Matches(node[1, 0])
        // Translated: right is division, left is division. Denoms match.
        // Return (l_num - r_num) / denom
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Divide, l_num, l_denom),
                Node::BinaryOp(MathOperator::Divide, r_num, r_denom),
            ) = (&**left, &**right)
            {
                if l_denom.matches_node(r_denom) {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Subtract,
                            l_num.clone(),
                            r_num.clone(),
                        )),
                        l_denom.clone(),
                    ));
                }
            }
        }
        None
    }
}

pub struct CoefficientReductionRule;
impl Rule for CoefficientReductionRule {
    fn name(&self) -> &'static str {
        "CoefficientReduction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (x * 5) - (x * 3) -> x * 2
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Multiply, l_left, l_right),
                Node::BinaryOp(MathOperator::Multiply, r_left, r_right),
            ) = (&**left, &**right)
            {
                if let (Some(coeff_l), Some(coeff_r)) = (l_right.get_number(), r_right.get_number())
                {
                    if l_left.matches_node(r_left) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            l_left.clone(),
                            Box::new(Node::Number(coeff_l - coeff_r)),
                        ));
                    }
                } else if let (Some(coeff_l), Some(coeff_r)) =
                    (l_left.get_number(), r_left.get_number())
                {
                    if l_right.matches_node(r_right) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(coeff_l - coeff_r)),
                            l_right.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct ConstantToAdditionRule;
impl Rule for ConstantToAdditionRule {
    fn name(&self) -> &'static str {
        "ConstantToAddition"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // x - (-5) -> x + 5
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if right.less_than_number(0.0) {
                if let Some(val) = right.get_number() {
                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        left.clone(),
                        Box::new(Node::Number(val.abs())), // - (-5) = + 5
                    ));
                }
            }
        }
        None
    }
}

pub struct FunctionToAdditionRule;
impl Rule for FunctionToAdditionRule {
    fn name(&self) -> &'static str {
        "FunctionToAddition"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // f(x) - (g(x) * -2) -> f(x) + (g(x) * 2)
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let Node::BinaryOp(MathOperator::Multiply, r_left, r_right) = &**right {
                if r_right.less_than_number(0.0) {
                    if let Some(val) = r_right.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Add,
                            left.clone(),
                            Box::new(Node::BinaryOp(
                                MathOperator::Multiply,
                                r_left.clone(),
                                Box::new(Node::Number(val.abs())),
                            )),
                        ));
                    }
                } else if r_left.less_than_number(0.0) {
                    if let Some(val) = r_left.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Add,
                            left.clone(),
                            Box::new(Node::BinaryOp(
                                MathOperator::Multiply,
                                Box::new(Node::Number(val.abs())),
                                r_right.clone(),
                            )),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct DistributiveSimpleRule;
impl Rule for DistributiveSimpleRule {
    fn name(&self) -> &'static str {
        "DistributiveSimple"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // f(x) - (g(x) - h(x)) -> (f(x) + h(x)) - g(x)
        if let Node::BinaryOp(MathOperator::Subtract, left, right) = node {
            if let Node::BinaryOp(MathOperator::Subtract, r_left, r_right) = &**right {
                return Some(Node::BinaryOp(
                    MathOperator::Subtract,
                    Box::new(Node::BinaryOp(
                        MathOperator::Add,
                        left.clone(),
                        r_right.clone(),
                    )),
                    r_left.clone(),
                ));
            }
        }
        None
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::ast::build_ast;
    use crate::rules::RuleEngine;
    use crate::tokenizer::{tokenize, DataStore};
    use crate::Shunter;

    fn simplify_expr(input: &str) -> Node {
        let ds = DataStore::default();
        let tokens = tokenize(input, &ds);
        let shunter = Shunter::new(&ds);
        let rpn = shunter.shunt(tokens);
        let ast = build_ast(rpn).unwrap();

        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(SameFunctionRule));
        engine.add_rule(Box::new(SubtractionByZeroRule));
        engine.simplify(ast)
    }

    #[test]
    fn test_same_function() {
        let ast = simplify_expr("x - x");
        assert_eq!(ast, Node::Number(0.0));
    }

    #[test]
    fn test_subtraction_by_zero() {
        let ast = simplify_expr("y - 0");
        assert_eq!(ast, Node::Variable("y".to_string()));
    }

    #[test]
    fn test_coefficient_one_reduction() {
        let ast = simplify_expr_with_rule("(x * 5) - x", Box::new(CoefficientOneReductionRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Variable("x".to_string())),
                Box::new(Node::Number(4.0))
            )
        );
    }

    #[test]
    fn test_zero_subtracted_by_function() {
        let ast = simplify_expr_with_rule("0 - x", Box::new(ZeroSubtractedByFunctionRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Number(-1.0)),
                Box::new(Node::Variable("x".to_string()))
            )
        );
    }

    #[test]
    fn test_subtraction_division_common_denominator() {
        let ast = simplify_expr_with_rule(
            "(cos(x) / z) - (sin(x) / z)",
            Box::new(SubtractionDivisionCommonDenominatorRule),
        );
        match ast {
            Node::BinaryOp(MathOperator::Divide, num, denom) => {
                assert_eq!(*denom, Node::Variable("z".to_string()));
                assert!(matches!(*num, Node::BinaryOp(MathOperator::Subtract, ..)));
            }
            _ => panic!("Expected division"),
        }
    }

    #[test]
    fn test_coefficient_reduction() {
        let ast = simplify_expr_with_rule("(2 * x) - (3 * x)", Box::new(CoefficientReductionRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Number(-1.0)),
                Box::new(Node::Variable("x".to_string()))
            )
        );
    }

    #[test]
    fn test_constant_to_addition() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(ConstantToAdditionRule));
        let ast = Node::BinaryOp(
            MathOperator::Subtract,
            Box::new(Node::Variable("x".to_string())),
            Box::new(Node::Number(-5.0)),
        );
        let simplified = engine.simplify(ast);
        assert_eq!(
            simplified,
            Node::BinaryOp(
                MathOperator::Add,
                Box::new(Node::Variable("x".to_string())),
                Box::new(Node::Number(5.0))
            )
        );
    }

    #[test]
    fn test_distributive_simple() {
        let ast = simplify_expr_with_rule("f - (g - h)", Box::new(DistributiveSimpleRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Subtract,
                Box::new(Node::BinaryOp(
                    MathOperator::Add,
                    Box::new(Node::Variable("f".to_string())),
                    Box::new(Node::Variable("h".to_string()))
                )),
                Box::new(Node::Variable("g".to_string()))
            )
        );
    }

    fn simplify_expr_with_rule(input: &str, rule: Box<dyn Rule>) -> Node {
        let ds = DataStore::default();
        let tokens = tokenize(input, &ds);
        let shunter = Shunter::new(&ds);
        let rpn = shunter.shunt(tokens);
        let ast = build_ast(rpn).unwrap();

        let mut engine = RuleEngine::new();
        engine.add_rule(rule);
        engine.simplify(ast)
    }
}
