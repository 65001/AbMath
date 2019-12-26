using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AbMath.Calculator.Simplifications;

namespace AbMath.Utilities
{
    public class Logger
    {
        private object lockingObject = new object();

        private Dictionary<Channels, Queue<string>> queues;
        private Dictionary<Channels, EventHandler<string>> handlers;

        public Logger()
        {
            queues = new Dictionary<Channels, Queue<string>>();
            queues.Add(Channels.Debug, new Queue<string>());
            queues.Add(Channels.Output, new Queue<string>());

            handlers = new Dictionary<Channels, EventHandler<string>>();
        }

        //TODO: Make this async?
        public void Log(Channels channel, string message)
        {
            queues[channel].Enqueue(message);

            //If there is no handler return
            if (!handlers.ContainsKey(channel) || handlers[channel] == null)
            {
                return;
            }

            while (queues[channel].Count > 0)
            {
                EventHandler<string> handler = handlers[channel];
                lock (lockingObject)
                {
                    handler.Invoke(this, queues[channel].Dequeue());
                }
            }
        }

        public void Bind(Channels channel, EventHandler<string> handler)
        {
            handlers[channel] = handler;
        }
    }

    public enum Channels
    {
        Debug, Output
    }
}
