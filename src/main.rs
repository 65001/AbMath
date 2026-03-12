mod ast;
mod rules;
mod tokenizer;

pub use crate::tokenizer::{Assoc, DataStore, MathFunction, MathOperator, Token, tokenize};

pub struct Shunter<'a> {
    _ds: &'a DataStore,
}

impl<'a> Shunter<'a> {
    pub fn new(ds: &'a DataStore) -> Self {
        Self { _ds: ds }
    }

    pub fn shunt(&self, tokens: Vec<Token>) -> Vec<Token> {
        let mut output = Vec::new();
        let mut operator_stack = Vec::new();
        let mut arity_stack = Vec::new();

        for i in 0..tokens.len() {
            let token = &tokens[i];
            let prev = if i > 0 { Some(&tokens[i - 1]) } else { None };
            let next = if i + 1 < tokens.len() {
                Some(&tokens[i + 1])
            } else {
                None
            };

            let p_is_ending = prev.map_or(false, |p| {
                p.is_number()
                    || p.is_variable()
                    || p.is_rp()
                    || (if let Token::Function(f, _) = p {
                        f.is_constant()
                    } else {
                        false
                    })
            });
            let t_is_starting =
                token.is_number() || token.is_variable() || token.is_lp() || token.is_function();

            let implicit_mult = p_is_ending && t_is_starting;

            // Unary minus handling: -term -> -1 * term
            if let Token::Operator(MathOperator::Subtract, _) = token {
                let is_unary = prev.map_or(true, |p| p.is_operator() || p.is_lp() || p.is_comma());
                if is_unary {
                    if let Some(ahead) = next {
                        if ahead.is_variable()
                            || ahead.is_lp()
                            || ahead.is_function()
                            || ahead.is_number()
                        {
                            output.push(Token::Number("-1".to_string()));
                            self.push_operator(
                                &mut output,
                                &mut operator_stack,
                                &mut arity_stack,
                                Token::Operator(MathOperator::Multiply, 2),
                            );
                            continue;
                        }
                    }
                }
            }

            if implicit_mult {
                self.push_operator(
                    &mut output,
                    &mut operator_stack,
                    &mut arity_stack,
                    Token::Operator(MathOperator::Multiply, 2),
                );
            }

            // Normal token handling
            self.handle_token(
                &mut output,
                &mut operator_stack,
                &mut arity_stack,
                token.clone(),
            );
        }

        while let Some(op) = operator_stack.pop() {
            output.push(self.finalize_arity(op, &mut arity_stack));
        }

        output
    }

    fn handle_token(
        &self,
        output: &mut Vec<Token>,
        stack: &mut Vec<Token>,
        arity: &mut Vec<usize>,
        token: Token,
    ) {
        match token {
            Token::Number(_) | Token::Variable(_) => {
                output.push(token);
            }
            Token::Function(func, _) => {
                stack.push(Token::Function(func, 0));
                arity.push(if func.is_constant() { 0 } else { 1 });
            }
            Token::Operator(_, _) => {
                self.push_operator(output, stack, arity, token);
            }
            Token::LParen(ref v) => {
                if v == "{" {
                    stack.push(Token::Function(MathFunction::List, 0));
                    arity.push(1);
                    stack.push(Token::LParen("(".to_string()));
                } else {
                    stack.push(token);
                }
            }
            Token::RParen(_) => {
                while let Some(top) = stack.last() {
                    if top.is_lp() {
                        break;
                    }
                    let op = stack.pop().unwrap();
                    output.push(self.finalize_arity(op, arity));
                }
                stack.pop(); // Pop LParen
            }
            Token::Comma => {
                while let Some(top) = stack.last() {
                    if top.is_lp() {
                        break;
                    }
                    let op = stack.pop().unwrap();
                    output.push(self.finalize_arity(op, arity));
                }
                if let Some(a) = arity.last_mut() {
                    *a += 1;
                }
            }
        }
    }

    fn push_operator(
        &self,
        output: &mut Vec<Token>,
        stack: &mut Vec<Token>,
        arity: &mut Vec<usize>,
        token: Token,
    ) {
        let (op_type, _) = match token {
            Token::Operator(t, a) => (t, a),
            _ => panic!("Expected operator, got {:?}", token),
        };

        while let Some(top) = stack.last() {
            if top.is_lp() {
                break;
            }
            if let Token::Function(_, _) = top {
                let op = stack.pop().unwrap();
                output.push(self.finalize_arity(op, arity));
                continue;
            }

            if let Token::Operator(top_type, _) = top {
                if top_type.weight() > op_type.weight()
                    || (top_type.weight() == op_type.weight() && op_type.assoc() == Assoc::Left)
                {
                    let op = stack.pop().unwrap();
                    output.push(self.finalize_arity(op, arity));
                } else {
                    break;
                }
            } else {
                break;
            }
        }
        stack.push(token);
    }

    fn finalize_arity(&self, mut token: Token, arity: &mut Vec<usize>) -> Token {
        if token.is_function() {
            token.set_arity(arity.pop().unwrap_or(0));
        }
        token
    }
}

use std::io::{self, Write};
use std::time::Instant;

fn main() {
    let ds = DataStore::default();
    let mut engine = rules::RuleEngine::new();

    for rule in rules::standard_rules() {
        engine.add_rule(rule);
    }

    loop {
        print!(">");
        io::stdout().flush().unwrap();

        let mut input = String::new();
        io::stdin().read_line(&mut input).unwrap();
        let input = input.trim();

        if input.eq_ignore_ascii_case("q") || input.eq_ignore_ascii_case("quit") {
            break;
        }

        if input.is_empty() {
            continue;
        }

        // Tokenize
        let start_tokenize = Instant::now();
        let tokens = tokenize(input, &ds);
        let time_tokenize = start_tokenize.elapsed();

        // Shunt
        let shunter = Shunter::new(&ds);
        let start_shunt = Instant::now();
        let rpn = shunter.shunt(tokens);
        let time_shunt = start_shunt.elapsed();

        // AST
        let start_ast = Instant::now();
        let ast = match ast::build_ast(rpn) {
            Ok(node) => node,
            Err(e) => {
                println!("Failed to parse AST: {:?}", e);
                continue;
            }
        };
        let time_ast = start_ast.elapsed();

        // Rules Engine
        let mut local_engine = rules::RuleEngine::new();
        for rule in rules::standard_rules() {
            local_engine.add_rule(rule);
        }

        let start_rules = Instant::now();
        let simplified_ast = local_engine.simplify(ast);
        let time_rules = start_rules.elapsed();

        println!("\n--- Results ---");
        println!("Answer: {}", simplified_ast);
        println!("Phase Timings:");
        println!("  Tokenizer:   {:?}", time_tokenize);
        println!("  Shunting:    {:?}", time_shunt);
        println!("  AST Builder: {:?}", time_ast);
        println!("  Rule Engine: {:?}", time_rules);

        println!("Rules Applied ({} total):", local_engine.logs.len());
        for log in &local_engine.logs {
            println!("  [{}]", log.rule_name);
            println!("    Before: {}", log.before);
            println!("    After:  {}", log.after);
        }
    }
}
