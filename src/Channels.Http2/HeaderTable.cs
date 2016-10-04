using System;
using System.Text;
using System.Text.Utf8;

namespace Channels.Http2
{
    internal partial struct HeaderTable : IDisposable
    {
        public readonly int MaxLength, Count;
        private readonly IBuffer _buffer;

        public override string ToString()
        {
            if (Count == 0) return "Table Size: 0";

            var sb = new StringBuilder();
            int total = 0;
            for (uint i = 0; i < Count; i++)
            {
                var header = GetDynamicHeader(i);
                sb.AppendLine($"[{i + 1}] (s = {header.Length}) {header.ToString()}");
                total += header.Length;
            }
            sb.AppendLine($"Table size: {total}");
            return sb.ToString();
        }

        public HeaderTable(int maxLength) : this(maxLength, 0, null) { }
        private HeaderTable(int maxLength, int count, IBuffer buffer)
        {
            MaxLength = maxLength;
            Count = count;
            _buffer = buffer;
        }
        internal unsafe HeaderTable Add(Header header, IBufferPool pool)
        {
            int newHeaderLength = header.Length;

            if (newHeaderLength > MaxLength)
            {
                throw new InvalidOperationException("Indexed header exceeds max length");
            }

            if (_buffer == null)
            {
                var buffer = pool.Lease(MaxLength);
                header.WriteTo(buffer.Data.Span.Slice(0, newHeaderLength));
                return new HeaderTable(MaxLength, 1, buffer);
            }
            else
            {
                var buffer = _buffer;
                // shuffle up
                int headersToKeep = 0, bytesToKeep = 0, totalBytes = newHeaderLength;
                var span = buffer.Data.Span;
                for (int i = 0; i < Count; i++)
                {
                    long lengths64 = span.Read<long>();
                    int* lengths32 = (int*)&lengths64;
                    int itemLen = lengths32[0] + lengths32[1] + 32;
                    totalBytes += itemLen;
                    if(totalBytes > MaxLength)
                    {
                        break;
                    }
                    bytesToKeep += itemLen;
                    headersToKeep++;
                }
                span.Slice(0, bytesToKeep).CopyTo(span.Slice(newHeaderLength, bytesToKeep));
                header.WriteTo(span.Slice(0, newHeaderLength));
                return new HeaderTable(MaxLength, headersToKeep + 1, buffer);
            }
        }

        internal string GetHeaderName(uint index) => GetHeader(index).Name;


        internal HeaderTable SetMaxLength(int maxLength, IBufferPool pool)
        {
            if (checked((int)maxLength) == MaxLength) return this;

            if (maxLength == 0 || Count == 0)
            {
                _buffer?.Dispose();
                return new HeaderTable(maxLength, 0, null);
            }

            throw new NotImplementedException();


        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }

        internal Header GetHeader(uint index)
        {
            return index <= _staticTableLength
                ? GetStaticHeader(index - 1)
                : GetDynamicHeader(index - _staticTableLength);
        }
        private unsafe Header GetDynamicHeader(uint index)
        {
            if (index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

            var span = _buffer.Data.Span;
            long lengths64 = 0;
            int* lengths32 = (int*)&lengths64;
            while (index-- != 0)
            {
                lengths64 = span.Read<long>();
                int itemLen = lengths32[0] + lengths32[1] + 32;
                span = span.Slice(itemLen);
            }

            lengths64 = span.Read<long>();
            int nameLen = lengths32[0], valueLen = lengths32[1];
            return new Header(
                name: nameLen == 0 ? "" : new Utf8String(span.Slice(32, nameLen)).ToString(),
                value: valueLen == 0 ? "" : new Utf8String(span.Slice(32 + nameLen, valueLen)).ToString()
            );

        }
    }
}
