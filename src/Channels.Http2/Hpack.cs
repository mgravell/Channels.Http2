using Channels.Text.Primitives;
using System;
using System.IO;
using System.Buffers;
using System.Text;
using System.Collections.Generic;

namespace Channels.Http2
{
    public struct Header
    {
        public string Name { get; }
        public string Value { get; }
        public int Length => (Name?.Length ?? 0) + (Value?.Length ?? 0) + 32;

        public bool IsDefault => Name == null && Value == null;

        internal Header(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => $"{Name}: {Value}";

        internal unsafe void WriteTo(Span<byte> span)
        {
            long lengths64 = 0;
            int* lengths32 = (int*)&lengths64;

            lengths32[0] = Name?.Length ?? 0;
            lengths32[1] = Value?.Length ?? 0;
            span.Write(lengths64);

            int offset = 32;
            if(!string.IsNullOrEmpty(Name))
            {
                var tmp = Encoding.ASCII.GetBytes(Name);
                span.Slice(offset, tmp.Length).Set(tmp);
                offset += tmp.Length;
            }
            if (!string.IsNullOrEmpty(Value))
            {
                var tmp = Encoding.ASCII.GetBytes(Value);
                span.Slice(offset, tmp.Length).Set(tmp);
            }
        }
    }


    internal static class Hpack
    {
        
        public static Header ReadHeader(ref ReadableBuffer buffer, ref HeaderTable headerTable, IBufferPool memoryPool)
        {
            int firstByte = buffer.Peek();
            if (firstByte < 0) ThrowEndOfStreamException();
            buffer = buffer.Slice(1);
            if ((firstByte & 0x80) != 0)
            {
                // 6.1.  Indexed Header Field Representation
                return headerTable.GetHeader(ReadUInt32(ref buffer, firstByte, 7));
            }
            else if ((firstByte & 0x40) != 0)
            {
                // 6.2.1.  Literal Header Field with Incremental Indexing
                var result = ReadHeader(ref buffer, ref headerTable, firstByte, 6);
                headerTable = headerTable.Add(result, memoryPool);
                return result;
            }
            else if ((firstByte & 0x20) != 0)
            {
                // 6.3. Dynamic Table Size Update
                var newSize = ReadInt32(ref buffer, firstByte, 5);
                headerTable = headerTable.SetMaxLength(newSize, memoryPool);
                return default(Header);
            }
            else
            {
                // 6.2.2.Literal Header Field without Indexing
                // 6.2.3.Literal Header Field Never Indexed
                return ReadHeader(ref buffer, ref headerTable, firstByte, 4);
            }
        }

        private static Header ReadHeader(ref ReadableBuffer buffer, ref HeaderTable headerTable, int header, int prefixBytes)
        {
            var index = ReadUInt32(ref buffer, header, prefixBytes);
            string name, value;
            if (index == 0)
            {
                name = ReadString(ref buffer);
            }
            else
            {
                name = headerTable.GetHeaderName(index);
            }
            value = ReadString(ref buffer);
            return new Header(name, value);
        }

        public static string ReadString(ref ReadableBuffer buffer)
        {
            int header = buffer.Peek();
            if (header < 0) ThrowEndOfStreamException();

            bool huffman = (header & 0x80) != 0;
            buffer = buffer.Slice(1);
            int len = checked((int)ReadUInt64(ref buffer, header, 7));
            string result;
            if (huffman)
            {
                result = HuffmanReader.ReadString(buffer.Slice(0, len));
            }
            else
            {
                result = buffer.Slice(0, len).GetAsciiString();
            }

            buffer = buffer.Slice(len);
            return result;
        }

        public static ulong ReadUInt64(ref ReadableBuffer buffer, int n)
        {
            int firstByte = buffer.Peek();
            if (firstByte < 0) throw new EndOfStreamException();
            buffer = buffer.Slice(1);
            return ReadUInt64(ref buffer, firstByte, n);

        }
        private static void ThrowEndOfStreamException()
        {
            throw new EndOfStreamException();
        }
        public static uint ReadUInt32(ref ReadableBuffer buffer, int firstByte, int n)
            => checked((uint)ReadUInt64(ref buffer, firstByte, n));
        public static int ReadInt32(ref ReadableBuffer buffer, int firstByte, int n)
            => checked((int)ReadUInt64(ref buffer, firstByte, n));

        public static ulong ReadUInt64(ref ReadableBuffer buffer, int firstByte, int n)
        {
            int mask = ~(~0 << n);
            int prefix = firstByte & mask;

            if (prefix != mask)
            {
                return (ulong)prefix; // short value encoded directly
            }
            ulong value = 0;
            int shift = 0, nextByte;
            for (int i = 0; i < 9; i++)
            {
                nextByte = buffer.Peek();
                if (nextByte < 0) ThrowEndOfStreamException();
                buffer = buffer.Slice(1);
                value |= ((ulong)nextByte & 0x7F) << shift;

                if ((nextByte & 0x80) == 0)
                {
                    // lack of continuation bit
                    return value + (ulong)mask;
                }
                shift += 7;
            }
            switch (nextByte = buffer.Peek())
            {
                case 0:
                case 1:
                    // note: lack of continuation bit (or anything else)
                    buffer = buffer.Slice(1);
                    value |= ((ulong)nextByte & 0x7F) << shift;
                    return value + (ulong)mask;
                default:
                    if (nextByte < 0) ThrowEndOfStreamException();
                    // 7*9=63, so max 9 groups of 7 bits plus either 0 or 1;
                    // after that: we've overflown
                    throw new OverflowException();
            }
        }

        internal static HttpHeader ParseHttpHeader(ref ReadableBuffer buffer, ref HeaderTable headerTable, IBufferPool memoryPool)
        {
            var headers = new List<Header>();
            while (!buffer.IsEmpty)
            {
                var header = Hpack.ReadHeader(ref buffer, ref headerTable, memoryPool);
                if (!header.IsDefault)
                {
                    headers.Add(header);
                }
            }
            return new HttpHeader(headers);
        }
    }
}
