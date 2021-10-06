// Validating benchmarks:
// ***** BenchmarkRunner: Start   *****
// ***** Found 1 benchmark(s) in total *****
// ***** Building 1 exe(s) in Parallel: Start   *****
// start dotnet restore  /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\5a3765b3-5753-4997-8e0f-6676c6b65c38
// command took 1.85s and exited with 0
// start dotnet build -c Release  --no-restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\5a3765b3-5753-4997-8e0f-6676c6b65c38
// command took 3.58s and exited with 0
// ***** Done, took 00:00:05 (5.54 sec)   *****
// Found 1 benchmarks:
//   TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=10]

// **************************
// Benchmark: TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=10]
// *** Execute ***
// Launch: 1 / 1
// Execute: dotnet "5a3765b3-5753-4997-8e0f-6676c6b65c38.dll" --benchmarkName "Benchmark.TokenizerBenchmark.traditionalTokenizer(N: 10)" --job ".NET 5.0" --benchmarkId 0 in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\5a3765b3-5753-4997-8e0f-6676c6b65c38\bin\Release\net5.0
// BeforeAnythingElse

// Benchmark Process Environment Information:
// Runtime=.NET 5.0.9 (5.0.921.35908), X64 RyuJIT
// GC=Concurrent Workstation
// Job: .NET 5.0

OverheadJitting  1: 1 op, 385100.00 ns, 385.1000 us/op
WorkloadJitting  1: 1 op, 240156300.00 ns, 240.1563 ms/op

WorkloadPilot    1: 2 op, 232971300.00 ns, 116.4857 ms/op
WorkloadPilot    2: 3 op, 285307700.00 ns, 95.1026 ms/op
WorkloadPilot    3: 4 op, 377148300.00 ns, 94.2871 ms/op
WorkloadPilot    4: 5 op, 429236800.00 ns, 85.8474 ms/op
WorkloadPilot    5: 6 op, 507320500.00 ns, 84.5534 ms/op

WorkloadWarmup   1: 6 op, 521205700.00 ns, 86.8676 ms/op
WorkloadWarmup   2: 6 op, 650862800.00 ns, 108.4771 ms/op
WorkloadWarmup   3: 6 op, 527599400.00 ns, 87.9332 ms/op
WorkloadWarmup   4: 6 op, 595841600.00 ns, 99.3069 ms/op
WorkloadWarmup   5: 6 op, 583251500.00 ns, 97.2086 ms/op
WorkloadWarmup   6: 6 op, 514191200.00 ns, 85.6985 ms/op

// BeforeActualRun
WorkloadActual   1: 6 op, 559289400.00 ns, 93.2149 ms/op
WorkloadActual   2: 6 op, 531322400.00 ns, 88.5537 ms/op
WorkloadActual   3: 6 op, 515557400.00 ns, 85.9262 ms/op
WorkloadActual   4: 6 op, 534797800.00 ns, 89.1330 ms/op
WorkloadActual   5: 6 op, 587225400.00 ns, 97.8709 ms/op
WorkloadActual   6: 6 op, 532021600.00 ns, 88.6703 ms/op
WorkloadActual   7: 6 op, 552479500.00 ns, 92.0799 ms/op
WorkloadActual   8: 6 op, 512171000.00 ns, 85.3618 ms/op
WorkloadActual   9: 6 op, 539967100.00 ns, 89.9945 ms/op
WorkloadActual  10: 6 op, 577460600.00 ns, 96.2434 ms/op
WorkloadActual  11: 6 op, 532536500.00 ns, 88.7561 ms/op
WorkloadActual  12: 6 op, 509848500.00 ns, 84.9748 ms/op
WorkloadActual  13: 6 op, 510202800.00 ns, 85.0338 ms/op
WorkloadActual  14: 6 op, 512640500.00 ns, 85.4401 ms/op
WorkloadActual  15: 6 op, 502561300.00 ns, 83.7602 ms/op
WorkloadActual  16: 6 op, 535630500.00 ns, 89.2717 ms/op
WorkloadActual  17: 6 op, 516861700.00 ns, 86.1436 ms/op
WorkloadActual  18: 6 op, 522625200.00 ns, 87.1042 ms/op
WorkloadActual  19: 6 op, 536126800.00 ns, 89.3545 ms/op
WorkloadActual  20: 6 op, 523198800.00 ns, 87.1998 ms/op
WorkloadActual  21: 6 op, 537839800.00 ns, 89.6400 ms/op
WorkloadActual  22: 6 op, 538407800.00 ns, 89.7346 ms/op
WorkloadActual  23: 6 op, 541861500.00 ns, 90.3102 ms/op
WorkloadActual  24: 6 op, 520011600.00 ns, 86.6686 ms/op
WorkloadActual  25: 6 op, 536739600.00 ns, 89.4566 ms/op
WorkloadActual  26: 6 op, 521002500.00 ns, 86.8338 ms/op
WorkloadActual  27: 6 op, 522894500.00 ns, 87.1491 ms/op

// AfterActualRun
WorkloadResult   1: 6 op, 559289400.00 ns, 93.2149 ms/op
WorkloadResult   2: 6 op, 531322400.00 ns, 88.5537 ms/op
WorkloadResult   3: 6 op, 515557400.00 ns, 85.9262 ms/op
WorkloadResult   4: 6 op, 534797800.00 ns, 89.1330 ms/op
WorkloadResult   5: 6 op, 532021600.00 ns, 88.6703 ms/op
WorkloadResult   6: 6 op, 552479500.00 ns, 92.0799 ms/op
WorkloadResult   7: 6 op, 512171000.00 ns, 85.3618 ms/op
WorkloadResult   8: 6 op, 539967100.00 ns, 89.9945 ms/op
WorkloadResult   9: 6 op, 532536500.00 ns, 88.7561 ms/op
WorkloadResult  10: 6 op, 509848500.00 ns, 84.9748 ms/op
WorkloadResult  11: 6 op, 510202800.00 ns, 85.0338 ms/op
WorkloadResult  12: 6 op, 512640500.00 ns, 85.4401 ms/op
WorkloadResult  13: 6 op, 502561300.00 ns, 83.7602 ms/op
WorkloadResult  14: 6 op, 535630500.00 ns, 89.2717 ms/op
WorkloadResult  15: 6 op, 516861700.00 ns, 86.1436 ms/op
WorkloadResult  16: 6 op, 522625200.00 ns, 87.1042 ms/op
WorkloadResult  17: 6 op, 536126800.00 ns, 89.3545 ms/op
WorkloadResult  18: 6 op, 523198800.00 ns, 87.1998 ms/op
WorkloadResult  19: 6 op, 537839800.00 ns, 89.6400 ms/op
WorkloadResult  20: 6 op, 538407800.00 ns, 89.7346 ms/op
WorkloadResult  21: 6 op, 541861500.00 ns, 90.3102 ms/op
WorkloadResult  22: 6 op, 520011600.00 ns, 86.6686 ms/op
WorkloadResult  23: 6 op, 536739600.00 ns, 89.4566 ms/op
WorkloadResult  24: 6 op, 521002500.00 ns, 86.8338 ms/op
WorkloadResult  25: 6 op, 522894500.00 ns, 87.1491 ms/op
GC:  67 6 1 245976088 6
Threading:  2 0 6

// AfterAll
// Benchmark Process 16316 has exited with code 0.

Mean = 87.991 ms, StdErr = 0.465 ms (0.53%), N = 25, StdDev = 2.323 ms
Min = 83.760 ms, Q1 = 86.144 ms, Median = 88.554 ms, Q3 = 89.457 ms, Max = 93.215 ms
IQR = 3.313 ms, LowerFence = 81.174 ms, UpperFence = 94.426 ms
ConfidenceInterval = [86.250 ms; 89.731 ms] (CI 99.9%), Margin = 1.740 ms (1.98% of Mean)
Skewness = 0.21, Kurtosis = 2.34, MValue = 2

// ***** BenchmarkRunner: Finish  *****

// * Export *
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-report.csv
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-report-github.md
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-report.html
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-measurements.csv
  BenchmarkDotNet.Artifacts\results\BuildPlots.R
RPlotExporter couldn't find Rscript.exe in your PATH and no R_HOME environment variable is defined

// * Detailed results *
TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=10]
Runtime = .NET 5.0.9 (5.0.921.35908), X64 RyuJIT; GC = Concurrent Workstation
Mean = 87.991 ms, StdErr = 0.465 ms (0.53%), N = 25, StdDev = 2.323 ms
Min = 83.760 ms, Q1 = 86.144 ms, Median = 88.554 ms, Q3 = 89.457 ms, Max = 93.215 ms
IQR = 3.313 ms, LowerFence = 81.174 ms, UpperFence = 94.426 ms
ConfidenceInterval = [86.250 ms; 89.731 ms] (CI 99.9%), Margin = 1.740 ms (1.98% of Mean)
Skewness = 0.21, Kurtosis = 2.34, MValue = 2
-------------------- Histogram --------------------
[83.354 ms ; 85.238 ms) | @@@
[85.238 ms ; 87.324 ms) | @@@@@@@@@
[87.324 ms ; 90.475 ms) | @@@@@@@@@@@
[90.475 ms ; 93.690 ms) | @@
---------------------------------------------------

// * Summary *

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.400
  [Host]   : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  .NET 5.0 : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

|               Method |  N |     Mean |    Error |   StdDev |      Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|--------------------- |--- |---------:|---------:|---------:|-----------:|----------:|---------:|----------:|
| traditionalTokenizer | 10 | 87.99 ms | 1.740 ms | 2.323 ms | 11166.6667 | 1000.0000 | 166.6667 |     39 MB |

// * Hints *
Outliers
  TokenizerBenchmark.traditionalTokenizer: .NET 5.0 -> 2 outliers were removed (96.24 ms, 97.87 ms)

// * Legends *
  N         : Value of the 'N' parameter
  Mean      : Arithmetic mean of all measurements
  Error     : Half of 99.9% confidence interval
  StdDev    : Standard deviation of all measurements
  Gen 0     : GC Generation 0 collects per 1000 operations
  Gen 1     : GC Generation 1 collects per 1000 operations
  Gen 2     : GC Generation 2 collects per 1000 operations
  Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 ms      : 1 Millisecond (0.001 sec)

// * Diagnostic Output - MemoryDiagnoser *


// ***** BenchmarkRunner: End *****
// ** Remained 0 benchmark(s) to run **
Run time: 00:00:21 (21.93 sec), executed benchmarks: 1

Global total time: 00:00:27 (27.48 sec), executed benchmarks: 1
// * Artifacts cleanup *
