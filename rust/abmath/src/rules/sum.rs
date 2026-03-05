use crate::ast::Node;
use crate::rules::Rule;
use crate::tokenizer::{MathFunction, MathOperator};

// The C# code uses BigInteger for Exact Bernoulli fractions.
// Here we'll map that carefully, perhaps using f64 or a basic fraction tuple
// if we don't have a BigInt library readily available in this context.
// Actually, since this is a simple AST builder, we can evaluate Bernoulli in f64.
// For n <= small numbers, f64 is exact enough.
fn get_bernoulli_number(n: i32) -> f64 {
    if n < 0 {
        return f64::NAN;
    }

    // Quick and dirty Bernoulli calculation matching RosettaCode formula in C#,
    // but using f64 since we lack BigInteger out of the box.
    // A more robust implementation would use num-bigint.
    let n_usize = n as usize;
    let mut nu: Vec<f64> = vec![0.0; n_usize + 1];
    let mut de: Vec<f64> = vec![0.0; n_usize + 1];

    for m in 0..=n_usize {
        nu[m] = 1.0;
        de[m] = (m + 1) as f64;
        for j in (1..=m).rev() {
            // formula: B_j-1 = j * (de[j]*nu[j-1] - de[j-1]*nu[j]) / (de[j]*de[j-1])
            // Simplified floating point updates:
            // Since we're doing floating point: B_val = B_val ...
            // Let's just track the float value.
            // Oh wait, RosettaCode's formula mutates arrays in place using GCD.
            // Let's just track the raw float value B[m] directly. Acknowledged loss of precision for large N.
        }
    }

    // We'll implement a simpler O(N^2) float version.
    let mut b = vec![0.0; n_usize + 1];
    b[0] = 1.0;
    if n >= 1 {
        // Note: The C# convention for Bernoulli sets B_1 = -1/2
        b[1] = -0.5;
        // ... well this gets complicated fast without BigInt exactness for the combinatorial math.
        // Let's do the floating point recursive formula:
        for m in 1..=n_usize {
            let mut sum = 0.0;
            for k in 0..m {
                // nCr(m+1, k) * B_k
                sum += nCr((m + 1) as u64, k as u64) as f64 * b[k];
            }
            b[m] = -sum / ((m + 1) as f64);
        }
        if n == 1 {
            b[1] = -0.5;
        } // The C# forces nu[0] = -1 implies B1=-1/2.
    }

    b[n_usize]
}

fn nCr(n: u64, r: u64) -> u64 {
    if r > n {
        return 0;
    }
    let mut res = 1;
    let r = if r > n - r { n - r } else { r };
    for i in 0..r {
        res = res * (n - i) / (i + 1);
    }
    res
}

// Sum convention in C#: _sum = token("sum", 4) -> sum(x, start, end, expression)
// Or sum(expression, x, start, end). The C# code uses `node[3]`, `node[0]`, `node[1]`, `node[2]`.
// Let's assume the argument order is: [x, start, end, expression]
// So `node[3]` is the expression.

pub struct PropagationRule;
impl Rule for PropagationRule {
    fn name(&self) -> &'static str {
        "Propagation"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sum(..., A + B) -> sum(..., A) + sum(..., B)
        // sum(..., A - B) -> sum(..., A) - sum(..., B)
        if let Node::Function(MathFunction::Sum, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Add, left, right) = &args[3] {
                    let mut sum_a = args.clone();
                    sum_a[3] = *left.clone();

                    let mut sum_b = args.clone();
                    sum_b[3] = *right.clone();

                    return Some(Node::BinaryOp(
                        MathOperator::Add,
                        Box::new(Node::Function(MathFunction::Sum, sum_a)),
                        Box::new(Node::Function(MathFunction::Sum, sum_b)),
                    ));
                } else if let Node::BinaryOp(MathOperator::Subtract, left, right) = &args[3] {
                    let mut sum_a = args.clone();
                    sum_a[3] = *left.clone();

                    let mut sum_b = args.clone();
                    sum_b[3] = *right.clone();

                    return Some(Node::BinaryOp(
                        MathOperator::Subtract,
                        Box::new(Node::Function(MathFunction::Sum, sum_a)),
                        Box::new(Node::Function(MathFunction::Sum, sum_b)),
                    ));
                }
            }
        }
        None
    }
}

pub struct VariableRule;
impl Rule for VariableRule {
    fn name(&self) -> &'static str {
        "Variable"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sum(x, 0 or 1, end, x) -> (end * (end + 1)) / 2
        // Assumes args = [end, start, var(x), expression] based on C# indexing logic.
        // Wait, C# logic:
        // return (node[1].IsNumber(0) || node[1].IsNumber(1)) && node[3].IsVariable() && node[3].Matches(node[2]);
        // This means node[1] = start, node[2] = var, node[3] = expression, and thus node[0] = end.
        // So args = [end, start, var_name, expression].
        if let Node::Function(MathFunction::Sum, args) = node {
            if args.len() == 4 {
                if (args[1].is_number(0.0) || args[1].is_number(1.0))
                    && matches!(&args[3], Node::Variable(_))
                    && args[3].matches_node(&args[2])
                {
                    // end = args[0]
                    // return (end * (end + 1)) / 2
                    return Some(Node::BinaryOp(
                        MathOperator::Divide,
                        Box::new(Node::BinaryOp(
                            MathOperator::Multiply,
                            Box::new(args[0].clone()),
                            Box::new(Node::BinaryOp(
                                MathOperator::Add,
                                Box::new(args[0].clone()),
                                Box::new(Node::Number(1.0)),
                            )),
                        )),
                        Box::new(Node::Number(2.0)),
                    ));
                }
            }
        }
        None
    }
}

pub struct ConstantComplexRule;
impl Rule for ConstantComplexRule {
    fn name(&self) -> &'static str {
        "ConstantComplex"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // If the expression doesn't contain the sum variable, it's a constant C.
        // sum(var, start, end, C) -> C * ((end - start) + 1)
        if let Node::Function(MathFunction::Sum, args) = node {
            if args.len() == 4 {
                // The C# logic is simplistic:
                // node[3].IsNumberOrConstant() || (node[3].IsVariable() && !node[3].Matches(node[2]))
                // It misses full node traversal. We'll stick to their exact matching.
                let is_simple_const = if let Node::Variable(_) = args[3] {
                    !args[3].matches_node(&args[2])
                } else {
                    // Number or other unhandled constant
                    args[3].is_number(0.0)
                        || args[3].get_number().is_some()
                        || args[3].is_constant("e")
                        || args[3].is_constant("π")
                };

                if is_simple_const {
                    // C * ((end - start) + 1)
                    return Some(Node::BinaryOp(
                        MathOperator::Multiply,
                        Box::new(args[3].clone()),
                        Box::new(Node::BinaryOp(
                            MathOperator::Add,
                            Box::new(Node::BinaryOp(
                                MathOperator::Subtract,
                                Box::new(args[0].clone()), // end
                                Box::new(args[1].clone()), // start
                            )),
                            Box::new(Node::Number(1.0)),
                        )),
                    ));
                }
            }
        }
        None
    }
}

pub struct CoefficientRule;
impl Rule for CoefficientRule {
    fn name(&self) -> &'static str {
        "Coefficient"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sum(var, start, end, c * expression) -> c * sum(var, start, end, expression)
        if let Node::Function(MathFunction::Sum, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Multiply, left, right) = &args[3] {
                    // Check if right is constant in C# `node[3, 1].IsNumberOrConstant()`
                    // Wait, C# `node[3, 1]` is the right operand.
                    let is_const = right.get_number().is_some()
                        || right.is_constant("e")
                        || right.is_constant("π");
                    if is_const {
                        let mut sum_args = args.clone();
                        sum_args[3] = *left.clone(); // The non-constant expression

                        return Some(Node::BinaryOp(
                            MathOperator::Multiply,
                            right.clone(),
                            Box::new(Node::Function(MathFunction::Sum, sum_args)),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct CoefficientDivisionRule;
impl Rule for CoefficientDivisionRule {
    fn name(&self) -> &'static str {
        "CoefficientDivision"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sum(var, start, end, expression / c) -> sum(var, start, end, expression) / c
        if let Node::Function(MathFunction::Sum, args) = node {
            if args.len() == 4 {
                if let Node::BinaryOp(MathOperator::Divide, left, right) = &args[3] {
                    // node[3, 0] is denominator in C# index layout
                    let is_const = right.get_number().is_some()
                        || right.is_constant("e")
                        || right.is_constant("π");
                    if is_const {
                        let mut sum_args = args.clone();
                        sum_args[3] = *left.clone();

                        return Some(Node::BinaryOp(
                            MathOperator::Divide,
                            Box::new(Node::Function(MathFunction::Sum, sum_args)),
                            right.clone(),
                        ));
                    }
                }
            }
        }
        None
    }
}

pub struct PowerRule;
impl Rule for PowerRule {
    fn name(&self) -> &'static str {
        "Power"
    }

    fn apply(&self, node: &Node) -> Option<Node> {
        // sum(var, start, end, var^p) -> long Bernoulli expansion polynomial
        if let Node::Function(MathFunction::Sum, args) = node {
            if args.len() == 4 {
                // ( node[1].IsInteger(1) || node[1].IsNumber(0) )
                // && node[3].IsExponent() && node[3, 0].IsInteger() && node[3, 0] > 0
                // && node[3, 1].Matches(node[2])
                let start_is_0_or_1 = args[1].is_number(0.0) || args[1].is_number(1.0);
                if start_is_0_or_1 {
                    if let Node::BinaryOp(MathOperator::Pow, pow_base, pow_exp) = &args[3] {
                        if pow_base.matches_node(&args[2]) {
                            if let Some(p) = pow_exp.get_number() {
                                if p > 0.0 && p.fract() == 0.0 {
                                    let end = args[0].clone();

                                    // The expansion loop logic from C#
                                    // sum_{i=0}^p [ (-1)^i * (p+1)! / ( (p-i+1)! * i! ) * B_i * n^(p-i+1) ] / (p+1)
                                    // Wait, we need the `total` function (or a long chain of Additions).
                                    // Let's create an Addition chain instead of a `Total` node for better compatibility.
                                    let max_p = p as i32;
                                    let mut additions: Option<Node> = None;
                                    let p_plus_1 = p + 1.0;

                                    for i in 0..=max_p {
                                        let p_minus_i_plus_1 = p - (i as f64) + 1.0;

                                        let bernoulli = get_bernoulli_number(i);

                                        let mut num_fact_node = Node::BinaryOp(
                                            MathOperator::Factorial,
                                            Box::new(Node::Number(p_plus_1)),
                                            Box::new(Node::Number(1.0)), // Dummy right for unary factorial as BinaryOp fallback.
                                        );
                                        // actually `Factorial` isn't fully implemented as MathFunction vs Operator in current architecture...
                                        // In tokenizer.rs it's MathOperator::Factorial.
                                        // Let's use MathOperator::Factorial which is unary but represented as BinaryOp or UnaryOp?
                                        // The AST only has `BinaryOp(MathOperator)`. Factorial isn't well handled if `BinaryOp`.
                                        // Let's use it as `Node::Operator(MathOperator::Factorial, child)` ? It doesn't exist.
                                        // We will map Factorial to a `MathFunction` if it's missing or a manual unary op.
                                        // Actually we'll just evaluate the coefficients as floats since p and i are constants!

                                        let p_fact = factorial(max_p + 1) as f64;
                                        let b_fact = factorial(max_p - i + 1) as f64;
                                        let i_fact = factorial(i) as f64;

                                        let fraction = p_fact / (b_fact * i_fact);

                                        let neg_one_pow = if i % 2 == 0 { 1.0 } else { -1.0 };

                                        let overall_coeff = neg_one_pow * fraction * bernoulli;

                                        let term = Node::BinaryOp(
                                            MathOperator::Multiply,
                                            Box::new(Node::Number(overall_coeff)),
                                            Box::new(Node::BinaryOp(
                                                MathOperator::Pow,
                                                Box::new(end.clone()),
                                                Box::new(Node::Number(p_minus_i_plus_1)),
                                            )),
                                        );

                                        if let Some(prev) = additions {
                                            additions = Some(Node::BinaryOp(
                                                MathOperator::Add,
                                                Box::new(prev),
                                                Box::new(term),
                                            ));
                                        } else {
                                            additions = Some(term);
                                        }
                                    }

                                    return Some(Node::BinaryOp(
                                        MathOperator::Divide,
                                        Box::new(additions.unwrap()),
                                        Box::new(Node::Number(p_plus_1)),
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

fn factorial(n: i32) -> f64 {
    let mut f = 1.0;
    for i in 1..=n {
        f *= i as f64;
    }
    f
}
