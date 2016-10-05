using System;
using System.Text;
using System.Text.Utf8;

namespace Channels.Http2
{
    internal partial struct HeaderTable : IDisposable
    {
        public readonly int Count;

        public const int DefaultMaxLength = 4096;
        private readonly int _maxLength;

        // use XOR here so that a default(HeaderTable) or new HeaderTable()
        // picks up the right default length
        public int MaxLength => _maxLength ^ DefaultMaxLength;

        private readonly IBuffer _buffer;

        public override string ToString()
        {
            if (Count == 0) return "empty";

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

        public HeaderTable(int maxLength) : this(maxLength ^ DefaultMaxLength, 0, null) { }
        private HeaderTable(int xoredMaxLength, int count, IBuffer buffer)
        {
            _maxLength = xoredMaxLength;
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
                return new HeaderTable(_maxLength, 1, buffer);
            }
            else
            {
                // shuffle up
                int headersToKeep = 0, bytesToKeep = 0, totalBytes = newHeaderLength;
                var span = _buffer.Data.Span;
                for (int i = 0; i < Count; i++)
                {
                    long lengths64 = span.Slice(bytesToKeep).Read<long>();
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
                return new HeaderTable(_maxLength, headersToKeep + 1, _buffer);
            }
        }

        internal string GetHeaderName(uint index) => GetHeader(index).Name;


        internal unsafe HeaderTable SetMaxLength(int maxLength, IBufferPool pool)
        {
            if (checked((int)maxLength) == MaxLength) return this;

            if (maxLength == 0 || Count == 0)
            {
                _buffer?.Dispose();
                return new HeaderTable(maxLength ^ DefaultMaxLength, 0, null);
            }

            int bytesToKeep = 0, headersToKeep = 0, totalBytes = 0;
            var span = _buffer.Data.Span;
            for (int i = 0; i < Count; i++)
            {
                long lengths64 = span.Slice(bytesToKeep).Read<long>();
                int* lengths32 = (int*)&lengths64;
                int itemLen = lengths32[0] + lengths32[1] + 32;
                totalBytes += itemLen;
                if (totalBytes > MaxLength)
                {
                    break;
                }
                bytesToKeep += itemLen;
                headersToKeep++;
            }

            var newBuffer = pool.Lease(maxLength);
            _buffer.Data.Span.Slice(0, bytesToKeep).CopyTo(newBuffer.Data.Span.Slice(bytesToKeep));
            _buffer.Dispose();
            return new HeaderTable(maxLength ^ DefaultMaxLength, headersToKeep, newBuffer);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
        }

        internal Header GetHeader(uint index)
        {
            return index-- <= _staticTableLength
                ? GetStaticHeader(index)
                : GetDynamicHeader(index - _staticTableLength);
        }
        private unsafe Header GetDynamicHeader(uint index)
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if(index == 2)
            {
                Console.WriteLine("Hi");
            }

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
