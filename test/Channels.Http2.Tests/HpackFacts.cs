using System;
using Xunit;

namespace Channels.Http2.Tests
{
    public class HpackFacts
    {
        [Theory]
        // C.1.1
        [InlineData("0A", 5, 10)]
        [InlineData("2A", 5, 10)]
        [InlineData("4A", 5, 10)]
        [InlineData("6A", 5, 10)]
        [InlineData("8A", 5, 10)]
        [InlineData("AA", 5, 10)]
        [InlineData("CA", 5, 10)]
        [InlineData("EA", 5, 10)]
        // C.1.2
        [InlineData("1F9A0A", 5, 1337)]
        [InlineData("3F9A0A", 5, 1337)]
        [InlineData("5F9A0A", 5, 1337)]
        [InlineData("7F9A0A", 5, 1337)]
        [InlineData("9F9A0A", 5, 1337)]
        [InlineData("BF9A0A", 5, 1337)]
        [InlineData("DF9A0A", 5, 1337)]
        [InlineData("FF9A0A", 5, 1337)]
        // C.1.3
        [InlineData("2A", 8, 42)]
        public void C_1_123(string hex, int n, int expected)
        {
            var readable = HexToBuffer(hex);
            Assert.Equal((ulong)expected, Hpack.ReadUInt64(ref readable, n));

        }

        [Theory]
        [InlineData("0a 6375 7374 6f6d 2d6b 6579", "custom-key")]
        [InlineData("0d 6375 7374 6f6d 2d68 6561 6465 72", "custom-header")]
        [InlineData("8c f1e3 c2e5 f23a 6ba0 ab90 f4ff", "www.example.com")]
        public void BasicStringParse(string hex, string expected)
        {
            var readable = HexToBuffer(hex);
            Assert.Equal(expected, Hpack.ReadString(ref readable));
        }

        [Theory]
        // C.2.1.  Literal Header Field with Indexing
        [InlineData("400a 6375 7374 6f6d 2d6b 6579 0d63 7573 746f 6d2d 6865 6164 6572", "custom-key: custom-header",
@"[1] (s = 55) custom-key: custom-header
Table size: 55
")]
        // C.2.2.  Literal Header Field without Indexing
        [InlineData("040c 2f73 616d 706c 652f 7061 7468", ":path: /sample/path", "empty")]

        // C.2.3.  Literal Header Field Never Indexed
        [InlineData("1008 7061 7373 776f 7264 0673 6563 7265 74", "password: secret", "empty")]

        // C.2.4.  Indexed Header Field
        [InlineData("82", ":method: GET", "empty")]
        public void HeaderParse(string hex, string expectedHeader, string expectedTable)
        {
            using (var memoryPool = new MemoryPool())
            {
                var headerTable = default(HeaderTable);
                try
                {
                    var readable = HexToBuffer(hex);
                    var header = Hpack.ReadHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(expectedHeader, header.ToString());
                    Assert.Equal(expectedTable, headerTable.ToString());
                }
                finally
                {
                    headerTable.Dispose();
                }
            }
        }

        [Fact]
        public void C31_C32_C33()
        {
            using (var memoryPool = new MemoryPool())
            {
                var headerTable = default(HeaderTable);
                try
                {
                    var readable = HexToBuffer("8286 8441 0f77 7777 2e65 7861 6d70 6c65 2e63 6f6d");
                    var httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":method: GET
:scheme: http
:path: /
:authority: www.example.com
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 57) :authority: www.example.com
Table size: 57
", headerTable.ToString());

                    readable = HexToBuffer("8286 84be 5808 6e6f 2d63 6163 6865");
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":method: GET
:scheme: http
:path: /
:authority: www.example.com
cache-control: no-cache
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 53) cache-control: no-cache
[2] (s = 57) :authority: www.example.com
Table size: 110
", headerTable.ToString());

                    readable = HexToBuffer("8287 85bf 400a 6375 7374 6f6d 2d6b 6579 0c63 7573 746f 6d2d 7661 6c75 65");
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":method: GET
:scheme: https
:path: /index.html
:authority: www.example.com
custom-key: custom-value
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 54) custom-key: custom-value
[2] (s = 53) cache-control: no-cache
[3] (s = 57) :authority: www.example.com
Table size: 164
", headerTable.ToString());
                }
                finally
                {
                    headerTable.Dispose();
                }
            }

        }

        [Fact]
        public void C41_C42_C43()
        {
            using (var memoryPool = new MemoryPool())
            {
                var headerTable = default(HeaderTable);
                try
                {
                    var readable = HexToBuffer("8286 8441 8cf1 e3c2 e5f2 3a6b a0ab 90f4 ff");
                    var httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":method: GET
:scheme: http
:path: /
:authority: www.example.com
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 57) :authority: www.example.com
Table size: 57
", headerTable.ToString());

                    readable = HexToBuffer("8286 84be 5886 a8eb 1064 9cbf");
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":method: GET
:scheme: http
:path: /
:authority: www.example.com
cache-control: no-cache
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 53) cache-control: no-cache
[2] (s = 57) :authority: www.example.com
Table size: 110
", headerTable.ToString());

                    readable = HexToBuffer("8287 85bf 4088 25a8 49e9 5ba9 7d7f 8925 a849 e95b b8e8 b4bf");
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":method: GET
:scheme: https
:path: /index.html
:authority: www.example.com
custom-key: custom-value
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 54) custom-key: custom-value
[2] (s = 53) cache-control: no-cache
[3] (s = 57) :authority: www.example.com
Table size: 164
", headerTable.ToString());
                }
                finally
                {
                    headerTable.Dispose();
                }
            }

        }

        private static ReadableBuffer HexToBuffer(string hex)
        {
            if (hex == null) return default(ReadableBuffer);
            hex = hex.Replace(" ", "").Trim();
            byte[] data = new byte[hex.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return ReadableBuffer.Create(data, 0, data.Length);
        }
    }
}
