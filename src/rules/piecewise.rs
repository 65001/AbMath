use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::MathFunction;

pub struct EvaluatePiecewiseRule;

impl Rule for EvaluatePiecewiseRule {
    fn name(&self) -> &'static str {
        "EvaluatePiecewise"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // piecewise(cond1, expr1, cond2, expr2, ..., else_expr)
        if let Node::Function(MathFunction::Piecewise, args) = node {
            if args.is_empty() {
                return Some(Node::Constant(MathFunction::NaN));
            }

            let mut new_args = args.clone();
            let mut i = 0;
            let mut changed = false;

            while i + 1 < new_args.len() {
                let cond = &new_args[i];
                let expr = &new_args[i + 1];

                if let Node::Number(val) = cond {
                    if *val == 1.0 {
                        // True condition
                        return Some(expr.clone());
                    } else if *val == 0.0 {
                        // False condition, remove this pair
                        new_args.remove(i);
                        new_args.remove(i); // now at same index
                        changed = true;
                        continue; // don't increment i
                    }
                }

                i += 2;
            }

            if new_args.len() == 1 {
                // Only the else_expr remains
                return Some(new_args[0].clone());
            } else if new_args.is_empty() {
                // No else expression, and all conditions were false
                return Some(Node::Constant(MathFunction::NaN));
            }

            if changed {
                return Some(Node::Function(MathFunction::Piecewise, new_args));
            }
        }
        None
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::Shunter;
    use crate::ast::build_ast;
    use crate::rules::RuleEngine;
    use crate::tokenizer::{DataStore, tokenize};

    fn simplify_expr(input: &str) -> Node {
        let mut ds = DataStore::default();
        ds.implicit_multiplication_priority = true;
        let tokens = tokenize(input, &ds);
        let shunter = Shunter::new(&ds);
        let rpn = shunter.shunt(tokens);
        let ast = build_ast(rpn).unwrap();

        let mut engine = RuleEngine::new();
        for rule in crate::rules::standard_rules() {
            engine.add_rule(rule);
        }
        engine.simplify(ast)
    }

    #[test]
    fn test_piecewise_true_cond() {
        // piecewise(1 == 1, 5, 2) -> 5
        let ast = simplify_expr("piecewise(1 == 1, 5, 2)");
        assert_eq!(ast, Node::Number(5.0));
    }

    #[test]
    fn test_piecewise_false_cond() {
        // piecewise(0 == 1, 5, 2 == 2, 10, 3) -> 10
        let ast = simplify_expr("piecewise(0 == 1, 5, 2 == 2, 10, 3)");
        assert_eq!(ast, Node::Number(10.0));
    }

    #[test]
    fn test_piecewise_else_cond() {
        // piecewise(0 == 1, 5, 0 == 2, 10, 3) -> 3
        let ast = simplify_expr("piecewise(0 == 1, 5, 0 == 2, 10, 3)");
        assert_eq!(ast, Node::Number(3.0));
    }

    #[test]
    fn test_piecewise_no_else_all_false() {
        // piecewise(0 == 1, 5, 0 == 2, 10) -> NaN
        let ast = simplify_expr("piecewise(0 == 1, 5, 0 == 2, 10)");
        assert_eq!(ast, Node::Constant(MathFunction::NaN));
    }
}
