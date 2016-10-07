using System;
using System.IO;

namespace Channels.Http2
{
    public partial struct HuffmanCode
    {
        const int MinimumCodeLength = 5, MaximumCodeLength = 30;
        public static unsafe string ReadString(ReadableBuffer buffer)
        {
            int maxChars = (buffer.Length << 3) / MinimumCodeLength;
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
        int _bits, _bitCount;
        public HuffmanCode(ReadableBuffer buffer)
        {
            _buffer = buffer;
            _bits = _bitCount = 0;
        }
        public int ReadNext()
        {
            int index;
            switch(_bitCount)
            {
                case 0:
                    _bits = _buffer.Peek();
                    if (_bits < 0) return -1;
                    _buffer = _buffer.Slice(1);
                    _bitCount = 8;
                    goto case 8;
                case 1:
                case 2:
                case 3:
                case 4:
                    // the 5  bits we need spans 2 bytes
                    int bitsFromSecondByte = (5 - _bitCount);
                    // we're going to need more; we'll take the remains of the current,
                    // and shift it to populate the left part of the index
                    index = (_bits & ~(~0 << _bitCount)) << bitsFromSecondByte;

                    _bits = _buffer.Peek();
                    if(_bits < 0)
                    {
                        // EOF; only valid if all the padding is 1
                        // so: we'll populate the rest with 1, and check we get 31
                        if((index | (~(~0 << bitsFromSecondByte))) == 0x1F)
                        {
                            return -1;
                        }
                        throw new EndOfStreamException();
                    }
                    _buffer = _buffer.Slice(1);
                    index |= _bits >> (8 - bitsFromSecondByte);
                    // if we had 1 bit, we'll take 8 and use 4 => 4 left;
                    // if we had 3, we'll take 5 and use 2 => 6 left
                    _bitCount += 3;
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                    // we have enough in the byte
                    index = (_bits >> (_bitCount - 5)) & 0x1F;
                    _bitCount -= 5;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            // double the index because it is actually *pairs*
            index <<= 1;
            short left = _linearizedTree[index], right = _linearizedTree[index + 1];
            if (left == right)
            {   // note that this can only possibly apply to the 5-bit roots,
                // hence not replicated in the loop below
                return -left;
            }

            int bitsRead = 5;
            int bit = _bitCount == 0 ? 0 : 1 << (_bitCount - 1);
            do
            {
                // we need moar data!
                if(bit == 0)
                {
                    var lastBits = _bits;
                    _bits = _buffer.Peek();
                    if (_bits < 0)
                    {
                        // this is OK as long as the rest is padding and all 1
                        int mask = ~(~0 << bitsRead);
                        if (bitsRead <= 7 && (lastBits & mask) == mask)
                        {
                            return -1;
                        }
                        throw new EndOfStreamException();
                    }
                    _buffer = _buffer.Slice(1);
                    _bitCount = 8;
                    bit = 0x80;
                }

                index = (_bits & bit) == 0 ? left: right;
                _bitCount--;
                if(index <= 0)
                {
                    return -index; // leaf
                }
                bit >>= 1;
                bitsRead++;
                index <<= 1; // double it as before, and find the next left/right paths
                left = _linearizedTree[index];
                right = _linearizedTree[index + 1];
            } while (bitsRead <= MaximumCodeLength);
            throw new InvalidOperationException("Huffman code not recognized");
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
