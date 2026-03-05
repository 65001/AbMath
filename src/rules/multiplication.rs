use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathOperator;

// node[0] is typically right, node[1] is left for binary ops in C# RPN.
// We adopt the Rust AST convention: left, right

pub struct MultiplicationToExponentRule;
impl Rule for MultiplicationToExponentRule {
    fn name(&self) -> &'static str {
        "MultiplicationToExponent"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if left.matches_node(right) {
                return Some(Node::BinaryOp(
                    MathOperator::Pow,
                    left.clone(),
                    Box::new(Node::Number(2.0)),
                ));
            }
        }
        None
    }
}

pub struct MultiplicationByOneRule;
impl Rule for MultiplicationByOneRule {
    fn name(&self) -> &'static str {
        "MultiplicationByOne"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if left.is_number(1.0) {
                return Some(*right.clone());
            } else if right.is_number(1.0) {
                return Some(*left.clone());
            }
        }
        None
    }
}

pub struct MultiplicationByZeroRule;
impl Rule for MultiplicationByZeroRule {
    fn name(&self) -> &'static str {
        "MultiplicationByZero"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            // C# checks !node.ContainsDomainViolation() but we'll assume exact matches or zero simplification
            if left.is_number(0.0) || right.is_number(0.0) {
                return Some(Node::Number(0.0));
            }
        }
        None
    }
}

pub struct IncreaseExponentRule;
impl Rule for IncreaseExponentRule {
    fn name(&self) -> &'static str {
        "IncreaseExponent"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[1].IsExponent() && node[1,0].IsNumber() && node[0].Matches(node[1,1])
        // (x^2) * x -> x^3
        // C# node[1]=left, node[0]=right. node[1,0]=left.right (exponent), node[1,1]=left.left (base)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if let Node::BinaryOp(MathOperator::Pow, l_base, l_exp) = &**left {
                if let Some(exp_val) = l_exp.get_number() {
                    if l_base.matches_node(right) {
                        return Some(Node::BinaryOp(
                            MathOperator::Pow,
                            l_base.clone(),
                            Box::new(Node::Number(exp_val + 1.0)),
                        ));
                    }
                }
            } else if let Node::BinaryOp(MathOperator::Pow, r_base, r_exp) = &**right {
                // x * (x^2) -> x^3 (symmetric case just in case)
                if let Some(exp_val) = r_exp.get_number() {
                    if r_base.matches_node(left) {
                        return Some(Node::BinaryOp(
                            MathOperator::Pow,
                            r_base.clone(),
                            Box::new(Node::Number(exp_val + 1.0)),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct IncreaseExponentTwoRule;
impl Rule for IncreaseExponentTwoRule {
    fn name(&self) -> &'static str {
        "IncreaseExponentTwo"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsExponent() && node[1].IsMultiplication() && node[0,0].IsGreaterThanNumber(0) && node[1,0].Matches(node[0,1])
        // node[1] (left) = Mul, node[0] (right) = Pow
        // left.right (node[1,0]) == right.left (base) [node[0,1]]
        // (c * f(x)) * f(x)^y -> c * f(x)^(y+1)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if let (
                Node::BinaryOp(MathOperator::Multiply, l_left, l_right),
                Node::BinaryOp(MathOperator::Pow, r_base, r_exp),
            ) = (&**left, &**right)
            {
                if let Some(y) = r_exp.get_number() {
                    if y > 0.0 {
                        if l_right.matches_node(r_base) {
                            return Some(Node::BinaryOp(
                                MathOperator::Multiply,
                                l_left.clone(),
                                Box::new(Node::BinaryOp(
                                    MathOperator::Pow,
                                    r_base.clone(),
                                    Box::new(Node::Number(y + 1.0)),
                                )),
                            ));
                        } else if l_left.matches_node(r_base) {
                            return Some(Node::BinaryOp(
                                MathOperator::Multiply,
                                l_right.clone(),
                                Box::new(Node::BinaryOp(
                                    MathOperator::Pow,
                                    r_base.clone(),
                                    Box::new(Node::Number(y + 1.0)),
                                )),
                            ));
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct IncreaseExponentThreeRule;
impl Rule for IncreaseExponentThreeRule {
    fn name(&self) -> &'static str {
        "IncreaseExponentThree"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsExponent() && node[1].IsMultiplication() && node[0,1].Matches(node[1])
        // (y * f) * (y * f)^z -> (y * f)^(z + 1)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if let Node::BinaryOp(MathOperator::Pow, r_base, r_exp) = &**right {
                if left.is_multiplication() && r_base.matches_node(left) {
                    if let Some(val) = r_exp.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Pow,
                            left.clone(),
                            Box::new(Node::Number(val + 1.0)),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct DualNodeMultiplicationRule;
impl Rule for DualNodeMultiplicationRule {
    fn name(&self) -> &'static str {
        "DualNodeMultiplication"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[1].IsNumber() && node[0].IsMultiplication() && node[0,1].IsNumber() && !node[0,0].IsNumber()
        // c * (k * f(x)) -> (c * k) * f(x)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if let Some(c) = left.get_number() {
                if let Node::BinaryOp(MathOperator::Multiply, r_left, r_right) = &**right {
                    if let Some(k) = r_left.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(c * k)),
                            r_right.clone(),
                        ));
                    } else if let Some(k) = r_right.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(c * k)),
                            r_left.clone(),
                        ));
                    }
                }
            } else if let Some(c) = right.get_number() {
                if let Node::BinaryOp(MathOperator::Multiply, l_left, l_right) = &**left {
                    if let Some(k) = l_left.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(c * k)),
                            l_right.clone(),
                        ));
                    } else if let Some(k) = l_right.get_number() {
                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(Node::Number(c * k)),
                            l_left.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct ExpressionTimesDivisionRule;
impl Rule for ExpressionTimesDivisionRule {
    fn name(&self) -> &'static str {
        "ExpressionTimesDivision"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // node[0].IsDivision() ^ node[1].IsDivision()
        // y * (x / z) -> (y * x) / z
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if let Node::BinaryOp(MathOperator::Divide, r_num, r_denom) = &**right {
                if !left.is_division() {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            left.clone(),
                            r_num.clone(),
                        )),
                        r_denom.clone(),
                    ));
                }
            } else if let Node::BinaryOp(MathOperator::Divide, l_num, l_denom) = &**left {
                if !right.is_division() {
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            right.clone(),
                            l_num.clone(),
                        )),
                        l_denom.clone(),
                    ));
                }
            }
        }
        None
    }
}

pub struct DivisionTimesDivisionRule;
impl Rule for DivisionTimesDivisionRule {
    fn name(&self) -> &'static str {
        "DivisionTimesDivision"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (a / b) * (c / d) -> (a * c) / (b * d)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
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
                        r_num.clone(),
                    )),
                    Box::new(Node::BinaryOp(
                        MathOperator::Multiply,
                        l_denom.clone(),
                        r_denom.clone(),
                    )),
                ));
            }
        }
        None
    }
}

pub struct NegativeTimesNegativeRule;
impl Rule for NegativeTimesNegativeRule {
    fn name(&self) -> &'static str {
        "NegativeTimesNegative"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // -5 * -3 -> 15
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if left.less_than_number(0.0) && right.less_than_number(0.0) {
                if let (Some(l_val), Some(r_val)) = (left.get_number(), right.get_number()) {
                    return Some(Node::Number(l_val * r_val));
                }
            }
        }
        None
    }
}

pub struct ComplexNegativeNegativeRule;
impl Rule for ComplexNegativeNegativeRule {
    fn name(&self) -> &'static str {
        "ComplexNegativeNegative"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // (x * -2) * -3 -> (x * 2) * 3
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if right.less_than_number(0.0) {
                if let Node::BinaryOp(MathOperator::Multiply, l_left, l_right) = &**left {
                    if l_right.less_than_number(0.0) {
                        if let (Some(l_val), Some(r_val)) =
                            (l_right.get_number(), right.get_number())
                        {
                            return Some(Node::BinaryOp(
                                MathOperator::Multiply,
                                Box::new(Node::BinaryOp(
                                    MathOperator::Multiply,
                                    l_left.clone(),
                                    Box::new(Node::Number(l_val.abs())),
                                )),
                                Box::new(Node::Number(r_val.abs())),
                            ));
                        }
                    }
                }
            }
        }
        None
    }
}

pub struct NegativeTimesConstantRule;
impl Rule for NegativeTimesConstantRule {
    fn name(&self) -> &'static str {
        "NegativeTimesConstant"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // -1 * 5 -> -5 (or similar explicit simplifications)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if left.is_number(-1.0) {
                if let Some(r_val) = right.get_number() {
                    return Some(Node::Number(r_val * -1.0));
                }
            } else if right.is_number(-1.0) {
                if let Some(l_val) = left.get_number() {
                    return Some(Node::Number(l_val * -1.0));
                }
            }
        }
        None
    }
}

pub struct NegativeOneDistributedRule;
impl Rule for NegativeOneDistributedRule {
    fn name(&self) -> &'static str {
        "NegativeOneDistributed"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // -1 * (f(x) - g(x)) -> g(x) - f(x)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if left.is_number(-1.0) {
                if let Node::BinaryOp(MathOperator::Subtract, r_left, r_right) = &**right {
                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        r_right.clone(),
                        r_left.clone(),
                    ));
                }
            } else if right.is_number(-1.0) {
                if let Node::BinaryOp(MathOperator::Subtract, l_left, l_right) = &**left {
                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        l_right.clone(),
                        l_left.clone(),
                    ));
                }
            }
        }
        None
    }
}

pub struct DistributeFunctionRule;
impl Rule for DistributeFunctionRule {
    fn name(&self) -> &'static str {
        "DistributeFunction"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // f(x) * (f(x) + g(x)) -> f(x)^2 + f(x)g(x)
        if let Node::BinaryOp(MathOperator::Multiply, left, right) = node {
            if let Node::BinaryOp(MathOperator::Add, r_left, r_right) = &**right {
                if left.matches_node(r_left) {
                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        Box::new(Node::BinaryOp(
                            MathOperator::Pow,
                            left.clone(),
                            Box::new(Node::Number(2.0)),
                        )),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            left.clone(),
                            r_right.clone(),
                        )),
                    ));
                } else if left.matches_node(r_right) {
                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        Box::new(Node::BinaryOp(
                            MathOperator::Pow,
                            left.clone(),
                            Box::new(Node::Number(2.0)),
                        )),
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            left.clone(),
                            r_left.clone(),
                        )),
                    ));
                }
            } else if let Node::BinaryOp(MathOperator::Add, l_left, l_right) = &**left {
                // (f(x) + g(x)) * f(x)
                if right.matches_node(l_left) || right.matches_node(l_right) {
                    let other = if right.matches_node(l_left) {
                        l_right.clone()
                    } else {
                        l_left.clone()
                    };
                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        Box::new(Node::BinaryOp(
                            MathOperator::Pow,
                            right.clone(),
                            Box::new(Node::Number(2.0)),
                        )),
                        Box::new(Node::BinaryOp(MathOperator::Multiply, right.clone(), other)),
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

    #[test]
    fn test_multiplication_to_exponent() {
        let ast = simplify_expr_with_rule("x * x", Box::new(MultiplicationToExponentRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Pow,
                Box::new(Node::Variable("x".to_string())),
                Box::new(Node::Number(2.0))
            )
        );
    }

    #[test]
    fn test_multiplication_by_one() {
        let ast = simplify_expr_with_rule("x * 1", Box::new(MultiplicationByOneRule));
        assert_eq!(ast, Node::Variable("x".to_string()));
    }

    #[test]
    fn test_multiplication_by_zero() {
        let ast = simplify_expr_with_rule("x * 0", Box::new(MultiplicationByZeroRule));
        assert_eq!(ast, Node::Number(0.0));
    }

    #[test]
    fn test_increase_exponent() {
        let ast = simplify_expr_with_rule("x * (x^2)", Box::new(IncreaseExponentRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Pow,
                Box::new(Node::Variable("x".to_string())),
                Box::new(Node::Number(3.0))
            )
        );
    }

    #[test]
    fn test_increase_exponent_two() {
        let ast = simplify_expr_with_rule("(5 * x) * (x^2)", Box::new(IncreaseExponentTwoRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Number(5.0)),
                Box::new(Node::BinaryOp(
                    MathOperator::Pow,
                    Box::new(Node::Variable("x".to_string())),
                    Box::new(Node::Number(3.0))
                ))
            )
        );
    }

    #[test]
    fn test_dual_node_multiplication() {
        let ast = simplify_expr_with_rule("2 * (3 * x)", Box::new(DualNodeMultiplicationRule));
        assert_eq!(
            ast,
            Node::BinaryOp(
                MathOperator::Multiply,
                Box::new(Node::Number(6.0)),
                Box::new(Node::Variable("x".to_string()))
            )
        );
    }

    #[test]
    fn test_negative_times_negative() {
        let mut engine = RuleEngine::new();
        engine.add_rule(Box::new(NegativeTimesNegativeRule));
        let ast = Node::BinaryOp(
            MathOperator::Multiply,
            Box::new(Node::Number(-2.0)),
            Box::new(Node::Number(-3.0)),
        );
        let simplified = engine.simplify(ast);
        assert_eq!(simplified, Node::Number(6.0));
    }

    #[test]
    fn test_negative_times_constant() {
        let ast = simplify_expr_with_rule("-1 * 5", Box::new(NegativeTimesConstantRule));
        assert_eq!(ast, Node::Number(-5.0));
    }
}
