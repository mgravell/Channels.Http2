using System;

namespace Channels.Http2
{
    public class HttpConnection : IDisposable
    {
        private MemoryPool _memoryPool;
        private HeaderTable _decoderTable;

        public HttpConnection(MemoryPool memoryPool)
        {
            _memoryPool = memoryPool;
        }

        public HttpHeader ParseHeader(ref ReadableBuffer buffer)
            => Hpack.ParseHttpHeader(ref buffer, ref _decoderTable, _memoryPool);

        public void Dispose()
        {
            _decoderTable.Dispose();
            _decoderTable = default(HeaderTable);
        }

    }
}
