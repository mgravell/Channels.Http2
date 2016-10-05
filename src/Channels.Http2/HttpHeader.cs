using System;
using System.Collections.Generic;
using System.Text;

namespace Channels.Http2
{
    public struct HttpHeader
    {
        private readonly List<Header> _headers;
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
    }
}
