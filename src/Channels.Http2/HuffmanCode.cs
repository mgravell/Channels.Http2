using System;
using System.Collections.Generic;
using System.IO;

namespace Channels.Http2
{
    public partial struct HuffmanCode
    {
        public static unsafe string ReadString(ReadableBuffer buffer)
        {
            int maxChars = (buffer.Length << 3) / _minCodeLength;
            if (maxChars <= 1024)
            {
                char* c = stackalloc char[maxChars];
                return ReadString(buffer, c, maxChars);
            }
            else
            {
                char[] arr = new char[maxChars];
                fixed (char* c = arr)
                {
                    return ReadString(buffer, c, maxChars);
                }
            }
        }

        private static unsafe string ReadString(ReadableBuffer buffer, char* c, int maxLen)
        {
            var reader = new HuffmanCode(buffer);

            for (int i = 0; i < maxLen; i++)
            {
                int next = reader.ReadNext();
                if (next < 0) return i == 0 ? "" : new string(c, 0, i);

                c[i] = (char)next;
            }
            if (reader.ReadNext() < 0) return new string(c, 0, maxLen);

            throw new EndOfStreamException();
        }

        ReadableBuffer _buffer;
        int _bit, _bits;
        HuffmanNode _node;
        public HuffmanCode(ReadableBuffer buffer)
        {
            _buffer = buffer;
            _bits = _bit = 0;
            _node = _root;
        }
        public int ReadNext()
        {
            int bitsRead = 0;
            while (true)
            {
                if (_bit == 0)
                {
                    _bits = _buffer.Peek();
                    if (_bits < 0)
                    {
                        if (bitsRead < 8) return -1;
                        throw new EndOfStreamException(); // not allowed full byte of padding
                    }
                    _buffer = _buffer.Slice(1);
                    _bit = 0x80;
                }
                _node = ((_bits & _bit) == 0) ? _node.False : _node.True;
                _bit >>= 1;
                if (_node.IsLeaf)
                {
                    int val = _node.Value;
                    _node = _root;
                    return val;
                }
                bitsRead++;
            }


        }

        class HuffmanNode
        {
            public HuffmanNode True, False;
            public int Value = -1;

            public bool IsLeaf => True == null;
        }

        internal static unsafe void Write(WritableBuffer buffer, string value)
        {
            int charLen = value.Length;
            if (charLen == 0) return; // nothing to do

            // we know we're going to need at least one byte, so init as though
            // we are about to write that first byte with 8 bits available
            const int StackSize = 512;
            byte* scratch = stackalloc byte[StackSize], writeHead = scratch;
            int unusedBytes = StackSize - 1, dirtyBytes = 1, bitsLeftInCurrentByte = 8;

            for (int charIndex = 0; charIndex < charLen; charIndex++)
            {
                // going to need at least *something*
                if (bitsLeftInCurrentByte == 0)
                {
                    if (unusedBytes-- != 0)
                    {
                        dirtyBytes++;
                        *(++writeHead) = 0;
                    }
                    else
                    {
                        // flush
                        buffer.Write(new Span<byte>(scratch, dirtyBytes));
                        writeHead = scratch;
                        dirtyBytes = 1;
                        unusedBytes = StackSize - 1;
                    }
                    bitsLeftInCurrentByte = 8;
                }

                byte c = (byte)value[charIndex];
                int bitsForCode = _codeLengths[c];

                if (bitsLeftInCurrentByte >= bitsForCode)
                {
                    // fits inside the existing byte; this means we also know it is <= 1 byte
                    int shift = bitsLeftInCurrentByte - (int)bitsForCode;
                    var shiftedCode = (byte)(_codes[c] << shift);

                    *writeHead |= shiftedCode;
                    bitsLeftInCurrentByte -= bitsForCode;
                }
                else
                {

                    uint code = _codes[c];
                    int shift = bitsForCode - bitsLeftInCurrentByte;

                    // write the first byte (note we know the shift will nuke all but the needed bits)
                    *writeHead |= (byte)(code >> shift);
                    shift -= 8;

                    while (shift >= 8)
                    {
                        if (unusedBytes-- != 0)
                        {
                            dirtyBytes++;
                            writeHead++;
                        }
                        else
                        {
                            // flush
                            buffer.Write(new Span<byte>(scratch, dirtyBytes));
                            writeHead = scratch;
                            dirtyBytes = 1;
                            unusedBytes = StackSize - 1;
                        }
                        *writeHead = (byte)((code >> shift) & 0xFF);
                        shift -= 8;
                    }

                    // write a final incomplete byte
                    if (shift != 0)
                    {
                        shift &= 0x07; // can be negative
                        if (unusedBytes-- != 0)
                        {
                            dirtyBytes++;
                            writeHead++;
                        }
                        else
                        {
                            // flush
                            buffer.Write(new Span<byte>(scratch, dirtyBytes));
                            writeHead = scratch;
                            dirtyBytes = 1;
                            unusedBytes = StackSize - 1;
                        }
                        bitsLeftInCurrentByte = 8 - shift;
                        var mask = ~(~0 << shift);
                        *writeHead = (byte)((code & mask) << bitsLeftInCurrentByte);
                    }
                    else
                    {
                        bitsLeftInCurrentByte = 0;
                    }
                }
            }
            if (bitsLeftInCurrentByte != 0)
            {
                // pad with 1
                *writeHead |= (byte)(~(~0 << bitsLeftInCurrentByte));
            }
            if (dirtyBytes != 0)
            {
                buffer.Write(new Span<byte>(scratch, dirtyBytes));
            }
        }

        internal static int GetByteCount(string value)
        {
            int len = 0;
            foreach (char c in value)
            {
                len += _codeLengths[(byte)c];
            }
            return (len + 7) / 8;
        }
    }

}
