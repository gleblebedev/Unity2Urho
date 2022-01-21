using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityToCustomEngineExporter.Editor
{
    internal struct SequenceReader : IEnumerable<string>
    {
        private readonly IReadOnlyList<string> _values;

        public SequenceReader(string sequence, char separator = ';')
        {
            _values = sequence.Split(separator);
        }

        //TODO: Make more efficient parser
        public IEnumerator<string> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}