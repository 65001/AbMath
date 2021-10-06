using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AbMath.Calculator;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net50)]
    [RPlotExporter]
    public class TokenizerBenchmark
    {
        [Params(10)]
        public int N;

        private string equation = "(2*(-2*4*cos(x)sin(x)+2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))+2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x)+2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))+2*-2cos(x)*sin(x)+2*-2*cos(x)sin(x)+0+0)+0+4*-2*cos(x)sin(x)+0+cos(x)sin(x)*1*2^2*1*-2+2*2*-2*cos(x)sin(x)+-2*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0))+8*-2*cos(x)sin(x)+2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x)+2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))+2*-2cos(x)*sin(x)+2*-2*cos(x)sin(x)+0+0)+-2*(2*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0)+0+2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+sin(x)*2^2*cos(x)*1*1+4cos(x)*sin(x))+-4*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0)+4*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))+sin(x)*4cos(x)*2*-2+-4*4*cos(x)sin(x)+0+2*4*-2*cos(x)sin(x)+0)-(12*2*cos(x)sin(x)+4*2*cos(x)sin(x)+2*(2*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0)+0+2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+sin(x)*2^2*cos(x)*1*1+4cos(x)*sin(x))+-2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x)+2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))+2*-2cos(x)*sin(x)+2*-2*cos(x)sin(x)+0+0)+-4*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))+4*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0)+16cos(x)*sin(x)+sin(x)*4^2*cos(x)*1*1+2*(2*4*cos(x)sin(x)+2*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0)+2*(2*(2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+0)+0+2*2cos(x)*sin(x)+0+4cos(x)*sin(x)+sin(x)*2^2*cos(x)*1*1+4cos(x)*sin(x))+0+4*2*cos(x)sin(x)+0+8cos(x)*sin(x)+2*sin(x)*2^2*cos(x)*1*1+-2*(2*-2*cos(x)sin(x)+-1*4*cos(x)sin(x))))";

        [Benchmark]
        public List<RPN.Token> traditionalTokenizer() => new RPN(equation).Compute().Tokens;
    }
}
