using Channels.Text.Primitives;
using System;
using System.IO;

namespace Channels.Http2
{
    public static class Hpack
    {
        
        public static string ReadString(ReadableBuffer buffer)
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
            for(int i = 0; i < 9; i++)
            {
                nextByte = buffer.Peek();
                if (nextByte < 0) ThrowEndOfStreamException();
                buffer = buffer.Slice(1);
                value |= ((ulong)nextByte & 0x7F) << shift;
                
                if((nextByte & 0x80) == 0)
                {
                    // lack of continuation bit
                    return value + (ulong)mask;
                }
                shift += 7;
            }
            switch(nextByte = buffer.Peek())
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
    }
}
