using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using AbMath.Calculator.Simplifications;

namespace AbMath.Utilities
{
    public class Logger
    {

        private TextWriter stdout;
        private TextWriter stderr;

        public Logger()
        {
            stdout = Console.Out;
            stderr = Console.Error;
        }

        //TODO: Make this async?
        public void Log(Channels channel, string message)
        {
            if (channel == Channels.Debug)
            {
                stderr.WriteLine(message);
                stderr.Flush();
            }
            else if (channel == Channels.Output) {
                stdout.WriteLine(message);
                stdout.Flush();
            }
        }
    }

    public enum Channels
    {
        Debug, Output
    }
}
