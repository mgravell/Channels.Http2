using System;
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ItemLength(long lengths)
        {
            return (int)((lengths & Mask32) + ((lengths >> 32) & Mask32) + 32);
        }
        private HeaderTable(int xoredMaxLength, int count, IBuffer buffer)
        {
            _maxLength = xoredMaxLength;
            Count = count;
            _buffer = buffer;
        }

        internal const long Mask32 = 0xFFFFFFFF;
        internal HeaderTable Add(Header header, IBufferPool pool)
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
                    long lengths = span.Slice(bytesToKeep).Read<long>();

                    int itemLen = ItemLength(lengths);
                    totalBytes += itemLen;
                    if (totalBytes > MaxLength)
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


        internal HeaderTable SetMaxLength(int maxLength, IBufferPool pool)
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
                long lengths = span.Slice(bytesToKeep).Read<long>();
                int itemLen = ItemLength(lengths);
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

        internal Header GetHeader(uint key)
        {
            return key-- <= _staticTableLength
                ? GetStaticHeader(key)
                : GetDynamicHeader(key - _staticTableLength);
        }
        private Header GetDynamicHeader(uint index)
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var span = _buffer.Data.Span;
            long lengths;
            while (index-- != 0)
            {
                lengths = span.Read<long>();
                int itemLen = ItemLength(lengths);
                span = span.Slice(itemLen);
            }

            lengths = span.Read<long>();
            int nameLen = (int)(lengths & Mask32), valueLen = (int)((lengths >> 32) & Mask32);
            return new Header(
                name: nameLen == 0 ? "" : new Utf8String(span.Slice(32, nameLen)).ToString(),
                value: valueLen == 0 ? "" : new Utf8String(span.Slice(32 + nameLen, valueLen)).ToString(),
                options: HeaderOptions.IndexExistingValue
            );

        }
        internal uint GetKey(string name)
        {
            int staticIndex = Array.IndexOf(_staticHeaderNames, name);
            if (staticIndex >= 0)
            {
                return (uint)(staticIndex + 1);
            }
            if (Count != 0)
            {
                int nameLength = name.Length, nameHashCode = name.GetHashCode();
                var span = _buffer.Data.Span;
                long lengths, hashcodes;
                for (uint i = 0; i < Count; i++)
                {
                    lengths = span.Read<long>();

                    int nameLen = (int)(lengths & Mask32);

                    if (nameLen == nameLength)
                    {
                        hashcodes = span.Slice(8).Read<long>();
                        if ((int)(hashcodes & Mask32) == nameHashCode
                            && Equals(span.Slice(32, nameLen), name))
                        {
                            return _staticTableLength + i + 1;
                        }
                    }

                    // next!
                    span = span.Slice(ItemLength(lengths));
                }
            }


            return 0;
        }

        internal uint GetKey(string name, string value)
        {
            int staticIndex = value.Length == 0 ? -1 : Array.IndexOf(_staticHeaderValues, value);
            if (staticIndex >= 0 && _staticHeaderNames[staticIndex] == name)
            {
                return (uint)(staticIndex + 1);
            }
            if (Count != 0)
            {
                int nameLength = name.Length, nameHashCode = name.GetHashCode(),
                    valueLength = value.Length, valueHashCode = value.GetHashCode();
                var span = _buffer.Data.Span;
                long lengths, hashcodes;
                for (uint i = 0; i < Count; i++)
                {
                    lengths = span.Read<long>();
                    int nameLen = (int)(lengths & Mask32), valueLen = (int)((lengths >> 32) & Mask32);

                    if (nameLen == nameLength && valueLen == valueLength)
                    {
                        hashcodes = span.Slice(8).Read<long>();
                        if ((int)(hashcodes & Mask32) == nameHashCode
                         && (int)((hashcodes >> 32) & Mask32) == valueHashCode
                         && Equals(span.Slice(32, nameLen), name)
                         && Equals(span.Slice(32 + nameLen, valueLen), value))
                        {
                            return _staticTableLength + i + 1;
                        }
                    }
                    // next!
                    span = span.Slice(ItemLength(lengths));
                }
            }


            return 0;
        }

        private static bool Equals(Span<byte> slice, string value)
        {
            int len = value.Length;
            for (int i = 0; i < len; i++)
            {
                if (slice[i] != (byte)value[i])
                {
                    return false;
                }
            }
            return true;

        }
    }
}
