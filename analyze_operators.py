#!/usr/bin/env python3
"""
Script to analyze and compare different neighborhood operator approaches:
- Static operators (swap, insert, invert)
- RL-based operator selection
"""

import pandas as pd
import numpy as np

# Load data files
print("Loading data files...")

# Parameter tuning results (static operators)
c101_param = pd.read_csv('article/results_raw_parameter_tuning_C101.csv', sep=';')
c201_param = pd.read_csv('article/results_raw_parameter_tuning_C201.csv', sep=';')

# RL results
c101_rl = pd.read_csv('RL_outputs/results_raw_RL_C101_C101.csv', sep=';')
c201_rl = pd.read_csv('RL_outputs/results_raw_RL_C201_C201.csv', sep=';')

def analyze_instance(param_df, rl_df, instance_name):
    """Analyze results for a single instance"""
    print(f"\n{'='*60}")
    print(f"Analysis for {instance_name}")
    print(f"{'='*60}")
    
    # Group parameter tuning by operator
    static_results = {}
    for operator in ['swap', 'insert', 'invert']:
        op_data = param_df[param_df['MutationType'] == operator]
        if len(op_data) > 0:
            static_results[operator] = {
                'mean': op_data['Objective'].mean(),
                'std': op_data['Objective'].std(),
                'min': op_data['Objective'].min(),
                'max': op_data['Objective'].max(),
                'count': len(op_data)
            }
    
    # RL results
    rl_results = {
        'mean': rl_df['Objective'].mean(),
        'std': rl_df['Objective'].std(),
        'min': rl_df['Objective'].min(),
        'max': rl_df['Objective'].max(),
        'count': len(rl_df)
    }
    
    # Print comparison table
    print(f"\nOperator Comparison for {instance_name}:")
    print(f"{'Operator':<15} {'Mean':>12} {'Std':>12} {'Min':>12} {'Max':>12} {'Count':>8}")
    print("-" * 80)
    
    for op, stats in static_results.items():
        print(f"{op:<15} {stats['mean']:>12.2f} {stats['std']:>12.2f} {stats['min']:>12.2f} {stats['max']:>12.2f} {stats['count']:>8}")
    
    print(f"{'RL':<15} {rl_results['mean']:>12.2f} {rl_results['std']:>12.2f} {rl_results['min']:>12.2f} {rl_results['max']:>12.2f} {rl_results['count']:>8}")
    
    # Calculate improvement percentages
    print(f"\n{instance_name} - Improvement vs RL:")
    for op, stats in static_results.items():
        improvement = ((stats['mean'] - rl_results['mean']) / stats['mean']) * 100
        print(f"  {op}: {improvement:+.2f}%")
    
    return static_results, rl_results

# Analyze both instances
c101_static, c101_rl_stats = analyze_instance(c101_param, c101_rl, "C101")
c201_static, c201_rl_stats = analyze_instance(c201_param, c201_rl, "C201")

# Generate LaTeX table
print(f"\n{'='*60}")
print("LaTeX Table for Article")
print(f"{'='*60}\n")

def generate_latex_table(instance, static_results, rl_results):
    """Generate LaTeX table for one instance"""
    print(f"% Table for {instance}")
    print(r"\begin{table}[ht]")
    print(r"\renewcommand{\arraystretch}{1.3}")
    print(r"\centering")
    print(f"\\caption{{Comparison of neighborhood operator selection strategies for {instance} instance.}}")
    print(f"\\label{{tab:operator_comparison_{instance.lower()}}}")
    print(r"\begin{tabular}{l|c|c|c|c}")
    print(r"\hline\hline")
    print(r"Operator & Mean & Std & Min & Max \\")
    print(r"\hline")
    
    for op in ['swap', 'insert', 'invert']:
        if op in static_results:
            stats = static_results[op]
            print(f"{op.capitalize():<10} & {stats['mean']:>7.2f} & {stats['std']:>7.2f} & {stats['min']:>7.2f} & {stats['max']:>7.2f} \\\\")
    
    print(f"{'RL':<10} & {rl_results['mean']:>7.2f} & {rl_results['std']:>7.2f} & {rl_results['min']:>7.2f} & {rl_results['max']:>7.2f} \\\\")
    print(r"\hline\hline")
    print(r"\end{tabular}")
    print(r"\end{table}")
    print()

generate_latex_table("C101", c101_static, c101_rl_stats)
generate_latex_table("C201", c201_static, c201_rl_stats)

print("\n" + "="*60)
print("Summary Statistics")
print("="*60)

# Overall comparison
all_operators = ['swap', 'insert', 'invert', 'RL']
c101_means = [c101_static['swap']['mean'], c101_static['insert']['mean'], 
              c101_static['invert']['mean'], c101_rl_stats['mean']]
c201_means = [c201_static['swap']['mean'], c201_static['insert']['mean'], 
              c201_static['invert']['mean'], c201_rl_stats['mean']]

print("\nMean Objective Values:")
for i, op in enumerate(all_operators):
    print(f"  {op:<10} C101: {c101_means[i]:>10.2f}   C201: {c201_means[i]:>10.2f}")

# Best operator
print(f"\nBest operator for C101: {all_operators[np.argmin(c101_means)]} ({min(c101_means):.2f})")
print(f"Best operator for C201: {all_operators[np.argmin(c201_means)]} ({min(c201_means):.2f})")
