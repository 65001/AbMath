use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathOperator;

pub struct AdditionToMultiplicationRule;
impl Rule for AdditionToMultiplicationRule {
    fn name(&self) -> &'static str {
        "AdditionToMultiplication"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if left.matches_node(right) {
                // x + x -> 2 * x
                return Some(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(Node::Number(2.0)),
                    left.clone(),
                ));
            }
        }
        None
    }
}

pub struct ZeroAdditionRule;
impl Rule for ZeroAdditionRule {
    fn name(&self) -> &'static str {
        "ZeroAddition"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if !(left.is_multiplication() && right.is_multiplication()) {
                if left.is_number(0.0) {
                    return Some(*right.clone());
                } else if right.is_number(0.0) {
                    return Some(*left.clone());
                }
            }
        }
        None
    }
}

pub struct AdditionSwapRule;
impl Rule for AdditionSwapRule {
    fn name(&self) -> &'static str {
        "AdditionSwap"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if !(left.is_multiplication() && right.is_multiplication()) && right.is_multiplication()
            {
                if let Node::BinaryOp(MathOperator::Multiply, r_left, r_right) = &**right {
                    if r_right.is_number(-1.0) || r_left.is_number(-1.0) {
                        // For example: f(x) + (g(x) * -1) -> f(x) - g(x)
                        let sub_term = if r_right.is_number(-1.0) {
                            r_left.clone()
                        } else {
                            r_right.clone()
                        };
                        return Some(Node::BinaryOp(
                            MathOperator::Subtract,
                            left.clone(),
                            sub_term,
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct SimpleCoefficientRule;
impl Rule for SimpleCoefficientRule {
    fn name(&self) -> &'static str {
        "SimpleCoefficient"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if !(left.is_multiplication() && right.is_multiplication()) && right.is_multiplication()
            {
                if let Node::BinaryOp(MathOperator::Multiply, r_left, r_right) = &**right {
                    if r_right.is_number(1.0) || r_left.is_number(1.0) {
                        // wait, c# rule says node[1,1].IsNumber() && node[1,0].Matches(node[0])
                    }

                    // The C# rule: node[1,1].IsNumber() && node[1,0].Matches(node[0])
                    // Means: x + (x * 3) -> 4 * x
                    if let Some(coeff) = r_right.get_number() {
                        if r_left.matches_node(left) {
                            return Some(Node::BinaryOp(
                                MathOperator::Multiply,
                                r_left.clone(),                      // x
                                Box::new(Node::Number(coeff + 1.0)), // (coeff + 1)
                            ));
                        }
                    } else if let Some(coeff) = r_left.get_number() {
                        if r_right.matches_node(left) {
                            return Some(Node::BinaryOp(
                                MathOperator::Multiply,
                                r_right.clone(),
                                Box::new(Node::Number(coeff + 1.0)),
                            ));
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct ComplexCoefficientRule;
impl Rule for ComplexCoefficientRule {
    fn name(&self) -> &'static str {
        "ComplexCoefficient"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Multiply, l_left, l_right),
                Node::BinaryOp(MathOperator::Multiply, r_left, r_right),
            ) = (&**left, &**right)
            {
                // (x * 2) + (x * 3) -> x * 5
                if let (Some(coeff_l), Some(coeff_r)) = (l_right.get_number(), r_right.get_number())
                {
                    if l_left.matches_node(r_left) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            l_left.clone(),
                            Box::new(Node::Number(coeff_l + coeff_r)),
                        ));
                    }
                } else if let (Some(coeff_l), Some(coeff_r)) =
                    (l_left.get_number(), r_left.get_number())
                {
                    // (2 * x) + (3 * x)
                    if l_right.matches_node(r_right) {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(coeff_l + coeff_r)),
                            l_right.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct AdditionToSubtractionRuleOne;
impl Rule for AdditionToSubtractionRuleOne {
    fn name(&self) -> &'static str {
        "AdditionToSubtractionRuleOne"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsMultiplication() && node[0, 1].IsNumber(-1) implies (-1 * f) + g
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if let Node::BinaryOp(MathOperator::Multiply, l_left, l_right) = &**left {
                if l_right.is_number(-1.0) {
                    // (-1 * x) + y -> y - x
                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        right.clone(),
                        l_left.clone(),
                    ));
                } else if l_left.is_number(-1.0) {
                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        right.clone(),
                        l_right.clone(),
                    ));
                }
            }
        }
        None
    }
}

pub struct AdditionToSubtractionRuleTwo;
impl Rule for AdditionToSubtractionRuleTwo {
    fn name(&self) -> &'static str {
        "AdditionToSubtractionRuleTwo"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsLessThanNumber(0) && node[1].IsMultiplication()
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if !(left.is_multiplication() && right.is_multiplication())
                && left.less_than_number(0.0)
                && right.is_multiplication()
            {
                // (-5) + (x * 2) -> (x * 2) - 5
                if let Some(val) = left.get_number() {
                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        right.clone(),
                        Box::new(Node::Number(val.abs())),
                    ));
                }
            }
        }
        None
    }
}

pub struct ComplexNodeAdditionRule;
impl Rule for ComplexNodeAdditionRule {
    fn name(&self) -> &'static str {
        "ComplexNodeAddition"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsSubtraction() && node[1].Matches(node[0, 1])
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if !(left.is_multiplication() && right.is_multiplication()) {
                if let Node::BinaryOp(MathOperator::Subtract, l_left, l_right) = &**left {
                    if right.matches_node(l_right) {
                        // (x - y) + y  -> x
                        return Some(*l_left.clone());
                    }
                }
            }
        }
        None
    }
}

pub struct DivisionAdditionRule;
impl Rule for DivisionAdditionRule {
    fn name(&self) -> &'static str {
        "DivisionAddition"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsDivision() && node[1].IsDivision() && node[0, 0].Matches(node[1, 0])
        // Note: the C# uses node[0, 0] which is the denom or numerator depending on how parser sets it up.
        // Assuming Division is (Numerator / Denominator) -> left = Num, right = Denom
        // C# matches: node[0,0] == node[1,0]. Since Abmath typically stores (denom, num) visually? Let's check Division.
        // Usually division is l_left / l_right. If denom is right:
        if let Node::BinaryOp(MathOperator::Add, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Divide, l_num, l_denom),
                Node::BinaryOp(MathOperator::Divide, r_num, r_denom),
            ) = (&**left, &**right)
            {
                // Assuming l_denom matches r_denom
                if l_denom.matches_node(r_denom) {
                    // (x/z) + (y/z) -> (x+y)/z
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Add,
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
        engine.add_rule(Box::new(ZeroAdditionRule));
        engine.add_rule(Box::new(AdditionToMultiplicationRule));
        engine.simplify(ast)
    }

    #[test]
    fn test_zero_addition() {
        let ast = simplify_expr("0 + x");
        assert_eq!(ast, Node::Variable("x".to_string()));

        let ast = simplify_expr("y + 0");
        assert_eq!(ast, Node::Variable("y".to_string()));
    }

    #[test]
    fn test_addition_to_multiplication() {
        let ast = simplify_expr("x + x");

        if let Node::BinaryOp(MathOperator::Multiply, left, right) = ast {
            assert!(left.is_number(2.0) || right.is_number(2.0));
        } else {
            panic!("Expected Multiplication node, got {:?}", ast);
        }
    }

    #[test]
    fn test_variable_addition_complex_coefficient() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(ComplexCoefficientRule));
        let ds = DataStore::default();
        let tokens = tokenize("2 * x + 3 * x", &ds);
        let ast = build_ast(Shunter::new(&ds).shunt(tokens)).unwrap();
        let simplified = engine.simplify(ast);
        assert_eq!(
            simplified,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Number(5.0)),
                Box::new(Node::Variable("x".to_string()))
            )
        );
    }

    #[test]
    fn test_simple_coefficient() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(SimpleCoefficientRule));
        let ds = DataStore::default();
        let tokens = tokenize("x + (3 * x)", &ds);
        let ast = build_ast(Shunter::new(&ds).shunt(tokens)).unwrap();
        let simplified = engine.simplify(ast);
        assert_eq!(
            simplified,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Variable("x".to_string())),
                Box::new(Node::Number(4.0))
            )
        );
    }

    #[test]
    fn test_addition_to_subtraction_one() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(AdditionToSubtractionRuleOne));
        let ast = Node::BinaryOp(
            MathOperator::Add,
            Box::new(Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Number(-1.0)),
                Box::new(Node::Variable("x".to_string())),
            )),
            Box::new(Node::Variable("y".to_string())),
        );
        let simplified = engine.simplify(ast);
        assert_eq!(
            simplified,
            Node::BinaryOp(
                MathOperator::Subtract,
                Box::new(Node::Variable("y".to_string())),
                Box::new(Node::Variable("x".to_string()))
            )
        );
    }

    #[test]
    fn test_addition_to_subtraction_two() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(AdditionToSubtractionRuleTwo));
        let ast = Node::BinaryOp(
            MathOperator::Add,
            Box::new(Node::Number(-5.0)),
            Box::new(Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Variable("x".to_string())),
                Box::new(Node::Number(2.0)),
            )),
        );
        let simplified = engine.simplify(ast);
        assert_eq!(
            simplified,
            Node::BinaryOp(
                MathOperator::Subtract,
                Box::new(Node::BinaryOp(
                    MathOperator::Multiply,
                    Box::new(Node::Variable("x".to_string())),
                    Box::new(Node::Number(2.0))
                )),
                Box::new(Node::Number(5.0))
            )
        );
    }

    #[test]
    fn test_complex_node_addition() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(ComplexNodeAdditionRule));
        let ds = DataStore::default();
        let tokens = tokenize("(x - y) + y", &ds);
        let ast = build_ast(Shunter::new(&ds).shunt(tokens)).unwrap();
        let simplified = engine.simplify(ast);
        assert_eq!(simplified, Node::Variable("x".to_string()));
    }

    #[test]
    fn test_division_addition() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(DivisionAdditionRule));
        let ds = DataStore::default();
        let tokens = tokenize("(x / z) + (y / z)", &ds);
        let ast = build_ast(Shunter::new(&ds).shunt(tokens)).unwrap();
        let simplified = engine.simplify(ast);
        assert_eq!(
            simplified,
            Node::BinaryOp(
                MathOperator::Divide,
                Box::new(Node::BinaryOp(
                    MathOperator::Add,
                    Box::new(Node::Variable("x".to_string())),
                    Box::new(Node::Variable("y".to_string()))
                )),
                Box::new(Node::Variable("z".to_string()))
            )
        );
    }
}
