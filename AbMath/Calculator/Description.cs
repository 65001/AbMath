using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    /// <summary>
    /// The description for functions and operators.
    /// </summary>
    public class Description
    {
        private List<string> _signature;
        private List<string> _blurbs;

        public IReadOnlyList<string> Signatures => _signature.AsReadOnly();
        public IReadOnlyList<string> Blurbs => _blurbs.AsReadOnly();

        public Description()
        {
            _signature = new List<string>();
            _blurbs = new List<string>();
        }

        public void Add(string signature, string blurb)
        {
            _signature.Add(signature);
            _blurbs.Add(blurb);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _signature.Count; i++)
            {
                sb.AppendLine(_signature[i]);
                sb.AppendLine(_blurbs[i]);
            }
            return sb.ToString();
        }
    }
}
