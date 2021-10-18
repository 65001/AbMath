// Validating benchmarks:
// ***** BenchmarkRunner: Start   *****
// ***** Found 2 benchmark(s) in total *****
// ***** Building 1 exe(s) in Parallel: Start   *****
// start dotnet restore  /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\8be8e7bd-3772-4cf0-9300-9e54a4cb5930
// command took 2.49s and exited with 0
// start dotnet build -c Release  --no-restore /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\8be8e7bd-3772-4cf0-9300-9e54a4cb5930
// command took 3.73s and exited with 0
// ***** Done, took 00:00:06 (6.36 sec)   *****
// Found 2 benchmarks:
//   TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=100]
//   TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=500]

// **************************
// Benchmark: TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=100]
// *** Execute ***
// Launch: 1 / 1
// Execute: dotnet "8be8e7bd-3772-4cf0-9300-9e54a4cb5930.dll" --benchmarkName "Benchmark.TokenizerBenchmark.traditionalTokenizer(N: 100)" --job ".NET 5.0" --benchmarkId 0 in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\8be8e7bd-3772-4cf0-9300-9e54a4cb5930\bin\Release\net5.0
// BeforeAnythingElse

// Benchmark Process Environment Information:
// Runtime=.NET 5.0.9 (5.0.921.35908), X64 RyuJIT
// GC=Concurrent Workstation
// Job: .NET 5.0

OverheadJitting  1: 1 op, 369900.00 ns, 369.9000 us/op
WorkloadJitting  1: 1 op, 217980100.00 ns, 217.9801 ms/op

WorkloadPilot    1: 2 op, 191683700.00 ns, 95.8418 ms/op
WorkloadPilot    2: 3 op, 243847100.00 ns, 81.2824 ms/op
WorkloadPilot    3: 4 op, 324458800.00 ns, 81.1147 ms/op
WorkloadPilot    4: 5 op, 384755900.00 ns, 76.9512 ms/op
WorkloadPilot    5: 6 op, 459415500.00 ns, 76.5692 ms/op
WorkloadPilot    6: 7 op, 529404900.00 ns, 75.6293 ms/op

WorkloadWarmup   1: 7 op, 546274400.00 ns, 78.0392 ms/op
WorkloadWarmup   2: 7 op, 560314900.00 ns, 80.0450 ms/op
WorkloadWarmup   3: 7 op, 553956600.00 ns, 79.1367 ms/op
WorkloadWarmup   4: 7 op, 562624800.00 ns, 80.3750 ms/op
WorkloadWarmup   5: 7 op, 551758900.00 ns, 78.8227 ms/op
WorkloadWarmup   6: 7 op, 556902500.00 ns, 79.5575 ms/op

// BeforeActualRun
WorkloadActual   1: 7 op, 526180700.00 ns, 75.1687 ms/op
WorkloadActual   2: 7 op, 526502900.00 ns, 75.2147 ms/op
WorkloadActual   3: 7 op, 527317700.00 ns, 75.3311 ms/op
WorkloadActual   4: 7 op, 534721300.00 ns, 76.3888 ms/op
WorkloadActual   5: 7 op, 550521000.00 ns, 78.6459 ms/op
WorkloadActual   6: 7 op, 557437500.00 ns, 79.6339 ms/op
WorkloadActual   7: 7 op, 525625500.00 ns, 75.0894 ms/op
WorkloadActual   8: 7 op, 528939300.00 ns, 75.5628 ms/op
WorkloadActual   9: 7 op, 589193500.00 ns, 84.1705 ms/op
WorkloadActual  10: 7 op, 525217600.00 ns, 75.0311 ms/op
WorkloadActual  11: 7 op, 528567800.00 ns, 75.5097 ms/op
WorkloadActual  12: 7 op, 528226300.00 ns, 75.4609 ms/op
WorkloadActual  13: 7 op, 529469900.00 ns, 75.6386 ms/op
WorkloadActual  14: 7 op, 527380900.00 ns, 75.3401 ms/op
WorkloadActual  15: 7 op, 534735600.00 ns, 76.3908 ms/op

// AfterActualRun
WorkloadResult   1: 7 op, 526180700.00 ns, 75.1687 ms/op
WorkloadResult   2: 7 op, 526502900.00 ns, 75.2147 ms/op
WorkloadResult   3: 7 op, 527317700.00 ns, 75.3311 ms/op
WorkloadResult   4: 7 op, 534721300.00 ns, 76.3888 ms/op
WorkloadResult   5: 7 op, 525625500.00 ns, 75.0894 ms/op
WorkloadResult   6: 7 op, 528939300.00 ns, 75.5628 ms/op
WorkloadResult   7: 7 op, 525217600.00 ns, 75.0311 ms/op
WorkloadResult   8: 7 op, 528567800.00 ns, 75.5097 ms/op
WorkloadResult   9: 7 op, 528226300.00 ns, 75.4609 ms/op
WorkloadResult  10: 7 op, 529469900.00 ns, 75.6386 ms/op
WorkloadResult  11: 7 op, 527380900.00 ns, 75.3401 ms/op
WorkloadResult  12: 7 op, 534735600.00 ns, 76.3908 ms/op
GC:  77 6 2 286673360 7
Threading:  2 0 7

// AfterAll
// Benchmark Process 10572 has exited with code 0.

Mean = 75.511 ms, StdErr = 0.130 ms (0.17%), N = 12, StdDev = 0.451 ms
Min = 75.031 ms, Q1 = 75.203 ms, Median = 75.401 ms, Q3 = 75.582 ms, Max = 76.391 ms
IQR = 0.379 ms, LowerFence = 74.635 ms, UpperFence = 76.149 ms
ConfidenceInterval = [74.932 ms; 76.089 ms] (CI 99.9%), Margin = 0.578 ms (0.77% of Mean)
Skewness = 1, Kurtosis = 2.62, MValue = 2

// **************************
// Benchmark: TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=500]
// *** Execute ***
// Launch: 1 / 1
// Execute: dotnet "8be8e7bd-3772-4cf0-9300-9e54a4cb5930.dll" --benchmarkName "Benchmark.TokenizerBenchmark.traditionalTokenizer(N: 500)" --job ".NET 5.0" --benchmarkId 1 in C:\Users\65001\Documents\GitHub\AbMath\Benchmark\bin\Release\net5.0\8be8e7bd-3772-4cf0-9300-9e54a4cb5930\bin\Release\net5.0
// BeforeAnythingElse

// Benchmark Process Environment Information:
// Runtime=.NET 5.0.9 (5.0.921.35908), X64 RyuJIT
// GC=Concurrent Workstation
// Job: .NET 5.0

OverheadJitting  1: 1 op, 321200.00 ns, 321.2000 us/op
WorkloadJitting  1: 1 op, 204666400.00 ns, 204.6664 ms/op

WorkloadPilot    1: 2 op, 183193600.00 ns, 91.5968 ms/op
WorkloadPilot    2: 3 op, 239694100.00 ns, 79.8980 ms/op
WorkloadPilot    3: 4 op, 306569400.00 ns, 76.6423 ms/op
WorkloadPilot    4: 5 op, 381729600.00 ns, 76.3459 ms/op
WorkloadPilot    5: 6 op, 448132000.00 ns, 74.6887 ms/op
WorkloadPilot    6: 7 op, 527626200.00 ns, 75.3752 ms/op

WorkloadWarmup   1: 7 op, 521095100.00 ns, 74.4422 ms/op
WorkloadWarmup   2: 7 op, 517345200.00 ns, 73.9065 ms/op
WorkloadWarmup   3: 7 op, 515853700.00 ns, 73.6934 ms/op
WorkloadWarmup   4: 7 op, 519272400.00 ns, 74.1818 ms/op
WorkloadWarmup   5: 7 op, 521851800.00 ns, 74.5503 ms/op
WorkloadWarmup   6: 7 op, 522604500.00 ns, 74.6578 ms/op
WorkloadWarmup   7: 7 op, 534725100.00 ns, 76.3893 ms/op
WorkloadWarmup   8: 7 op, 521167800.00 ns, 74.4525 ms/op
WorkloadWarmup   9: 7 op, 533925200.00 ns, 76.2750 ms/op
WorkloadWarmup  10: 7 op, 518706300.00 ns, 74.1009 ms/op

// BeforeActualRun
WorkloadActual   1: 7 op, 527830500.00 ns, 75.4044 ms/op
WorkloadActual   2: 7 op, 529338100.00 ns, 75.6197 ms/op
WorkloadActual   3: 7 op, 520442800.00 ns, 74.3490 ms/op
WorkloadActual   4: 7 op, 522440200.00 ns, 74.6343 ms/op
WorkloadActual   5: 7 op, 523879900.00 ns, 74.8400 ms/op
WorkloadActual   6: 7 op, 525457800.00 ns, 75.0654 ms/op
WorkloadActual   7: 7 op, 522206600.00 ns, 74.6009 ms/op
WorkloadActual   8: 7 op, 527588900.00 ns, 75.3698 ms/op
WorkloadActual   9: 7 op, 518981000.00 ns, 74.1401 ms/op
WorkloadActual  10: 7 op, 523237500.00 ns, 74.7482 ms/op
WorkloadActual  11: 7 op, 524904500.00 ns, 74.9864 ms/op
WorkloadActual  12: 7 op, 516207100.00 ns, 73.7439 ms/op
WorkloadActual  13: 7 op, 526179700.00 ns, 75.1685 ms/op
WorkloadActual  14: 7 op, 527638300.00 ns, 75.3769 ms/op
WorkloadActual  15: 7 op, 563224000.00 ns, 80.4606 ms/op

// AfterActualRun
WorkloadResult   1: 7 op, 527830500.00 ns, 75.4044 ms/op
WorkloadResult   2: 7 op, 529338100.00 ns, 75.6197 ms/op
WorkloadResult   3: 7 op, 520442800.00 ns, 74.3490 ms/op
WorkloadResult   4: 7 op, 522440200.00 ns, 74.6343 ms/op
WorkloadResult   5: 7 op, 523879900.00 ns, 74.8400 ms/op
WorkloadResult   6: 7 op, 525457800.00 ns, 75.0654 ms/op
WorkloadResult   7: 7 op, 522206600.00 ns, 74.6009 ms/op
WorkloadResult   8: 7 op, 527588900.00 ns, 75.3698 ms/op
WorkloadResult   9: 7 op, 518981000.00 ns, 74.1401 ms/op
WorkloadResult  10: 7 op, 523237500.00 ns, 74.7482 ms/op
WorkloadResult  11: 7 op, 524904500.00 ns, 74.9864 ms/op
WorkloadResult  12: 7 op, 516207100.00 ns, 73.7439 ms/op
WorkloadResult  13: 7 op, 526179700.00 ns, 75.1685 ms/op
WorkloadResult  14: 7 op, 527638300.00 ns, 75.3769 ms/op
GC:  77 5 1 286673160 7
Threading:  2 0 7

// AfterAll
// Benchmark Process 7408 has exited with code 0.

Mean = 74.861 ms, StdErr = 0.143 ms (0.19%), N = 14, StdDev = 0.534 ms
Min = 73.744 ms, Q1 = 74.609 ms, Median = 74.913 ms, Q3 = 75.320 ms, Max = 75.620 ms
IQR = 0.710 ms, LowerFence = 73.544 ms, UpperFence = 76.385 ms
ConfidenceInterval = [74.258 ms; 75.463 ms] (CI 99.9%), Margin = 0.603 ms (0.81% of Mean)
Skewness = -0.48, Kurtosis = 2.17, MValue = 2

// ***** BenchmarkRunner: Finish  *****

// * Export *
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-report.csv
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-report-github.md
  BenchmarkDotNet.Artifacts\results\Benchmark.TokenizerBenchmark-report.html

// * Detailed results *
TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=100]
Runtime = .NET 5.0.9 (5.0.921.35908), X64 RyuJIT; GC = Concurrent Workstation
Mean = 75.511 ms, StdErr = 0.130 ms (0.17%), N = 12, StdDev = 0.451 ms
Min = 75.031 ms, Q1 = 75.203 ms, Median = 75.401 ms, Q3 = 75.582 ms, Max = 76.391 ms
IQR = 0.379 ms, LowerFence = 74.635 ms, UpperFence = 76.149 ms
ConfidenceInterval = [74.932 ms; 76.089 ms] (CI 99.9%), Margin = 0.578 ms (0.77% of Mean)
Skewness = 1, Kurtosis = 2.62, MValue = 2
-------------------- Histogram --------------------
[74.772 ms ; 76.650 ms) | @@@@@@@@@@@@
---------------------------------------------------

TokenizerBenchmark.traditionalTokenizer: .NET 5.0(Runtime=.NET 5.0) [N=500]
Runtime = .NET 5.0.9 (5.0.921.35908), X64 RyuJIT; GC = Concurrent Workstation
Mean = 74.861 ms, StdErr = 0.143 ms (0.19%), N = 14, StdDev = 0.534 ms
Min = 73.744 ms, Q1 = 74.609 ms, Median = 74.913 ms, Q3 = 75.320 ms, Max = 75.620 ms
IQR = 0.710 ms, LowerFence = 73.544 ms, UpperFence = 76.385 ms
ConfidenceInterval = [74.258 ms; 75.463 ms] (CI 99.9%), Margin = 0.603 ms (0.81% of Mean)
Skewness = -0.48, Kurtosis = 2.17, MValue = 2
-------------------- Histogram --------------------
[73.453 ms ; 75.911 ms) | @@@@@@@@@@@@@@
---------------------------------------------------

// * Summary *

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1288 (21H1/May2021Update)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.400
  [Host]   : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT
  .NET 5.0 : .NET 5.0.9 (5.0.921.35908), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

|               Method |   N |     Mean |    Error |   StdDev |      Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------- |---- |---------:|---------:|---------:|-----------:|---------:|---------:|----------:|
| traditionalTokenizer | 100 | 75.51 ms | 0.578 ms | 0.451 ms | 11000.0000 | 857.1429 | 285.7143 |     39 MB |
| traditionalTokenizer | 500 | 74.86 ms | 0.603 ms | 0.534 ms | 11000.0000 | 714.2857 | 142.8571 |     39 MB |

// * Hints *
Outliers
  TokenizerBenchmark.traditionalTokenizer: .NET 5.0 -> 3 outliers were removed (78.65 ms..84.17 ms)
  TokenizerBenchmark.traditionalTokenizer: .NET 5.0 -> 1 outlier  was  removed (80.46 ms)

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
Run time: 00:00:32 (32.18 sec), executed benchmarks: 2

Global total time: 00:00:38 (38.55 sec), executed benchmarks: 2
// * Artifacts cleanup *
