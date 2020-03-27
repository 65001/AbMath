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
        private List<string> _examples;

        public IReadOnlyList<string> Signatures => _signature.AsReadOnly();
        public IReadOnlyList<string> Blurbs => _blurbs.AsReadOnly();

        public Description()
        {
            _signature = new List<string>();
            _blurbs = new List<string>();
            _examples = new List<string>();
        }

        public Description(string signature, string blurb)
        {
            _signature = new List<string>();
            _blurbs = new List<string>();
            _examples = new List<string>();
            this.Add(signature, blurb);
        }

        public void Add(string signature, string blurb)
        {
            if (signature == null || blurb == null)
            {
                throw new ArgumentNullException(signature,"The parameter cannot be null for the description.");
            }
            _signature.Add(signature);
            _blurbs.Add(blurb);
        }

        public void Add(string example)
        {
            if (example == null)
            {
                throw new ArgumentNullException(example, "The example parameter cannot be null.");
            }
            _examples.Add(example);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _signature.Count; i++)
            {
                sb.AppendLine(_signature[i]);
                sb.AppendLine(_blurbs[i]);
            }

            if (_examples.Count > 0)
            {
                sb.AppendLine("\nExamples:");
            }

            foreach (var ex in _examples)
            {
                sb.AppendLine(ex);
            }

            return sb.ToString();
        }
    }
}
