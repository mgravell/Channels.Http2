using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Channels.Http2
{
    public struct HttpHeader : IEnumerable<Header>
    {
        private readonly List<Header> _headers;

        public List<Header>.Enumerator GetEnumerator() => _headers.GetEnumerator();
        public HttpHeader(List<Header> headers)
        {
            _headers = headers;
        }

        public override string ToString()
        {
            if ((_headers?.Count ?? 0) == 0) return "(nil)";
            var sb = new StringBuilder();
            foreach(var header in _headers)
            {
                sb.AppendLine(header.ToString());
            }
            return sb.ToString();
        }

        IEnumerator<Header> IEnumerable<Header>.GetEnumerator()
            => ((IEnumerable<Header>)_headers).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)_headers).GetEnumerator();
    }
}
