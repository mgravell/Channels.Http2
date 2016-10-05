using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Channels.Http2.Tests
{
    public class HpackFacts
    {
        [Theory]
        // C.1.1
        [InlineData("0A", 5, 10, 0x00)]
        [InlineData("2A", 5, 10, 0x20)]
        [InlineData("4A", 5, 10, 0x40)]
        [InlineData("6A", 5, 10, 0x60)]
        [InlineData("8A", 5, 10, 0x80)]
        [InlineData("AA", 5, 10, 0xA0)]
        [InlineData("CA", 5, 10, 0xC0)]
        [InlineData("EA", 5, 10, 0xE0)]
        // C.1.2
        [InlineData("1F9A0A", 5, 1337, 0x00)]
        [InlineData("3F9A0A", 5, 1337, 0x20)]
        [InlineData("5F9A0A", 5, 1337, 0x40)]
        [InlineData("7F9A0A", 5, 1337, 0x60)]
        [InlineData("9F9A0A", 5, 1337, 0x80)]
        [InlineData("BF9A0A", 5, 1337, 0xA0)]
        [InlineData("DF9A0A", 5, 1337, 0xC0)]
        [InlineData("FF9A0A", 5, 1337, 0xE0)]
        // C.1.3
        [InlineData("2A", 8, 42, 0x00)]
        public async Task C_1_123(string hex, int n, int expected, byte preamble)
        {
            var readable = HexToBuffer(ref hex);
            Assert.Equal((ulong)expected, Hpack.ReadUInt64(ref readable, n));

            using (var channelFactory = new ChannelFactory())
            {
                var channel = channelFactory.CreateChannel();
                var wb = channel.Alloc();
                Hpack.WriteUInt32(wb, (uint)expected, preamble, n);
                Assert.Equal(hex, ToHex(wb));
                await wb.FlushAsync();
            }
        }

        private string ToHex(WritableBuffer wb)
        {
            var readable = wb.AsReadableBuffer();
            if (readable.IsEmpty) return "";
            var sb = new StringBuilder(readable.Length * 2);
            foreach(var mem in readable)
            {
                var span = mem.Span;
                int len = span.Length;
                for(int i = 0; i < len; i++)
                {
                    sb.Append(span[i].ToString("x2"));
                }
            }
            return sb.ToString();
        }

        [Theory]
        [InlineData("0a 6375 7374 6f6d 2d6b 6579", "custom-key", false)]
        [InlineData("0d 6375 7374 6f6d 2d68 6561 6465 72", "custom-header", false)]
        [InlineData("8c f1e3 c2e5 f23a 6ba0 ab90 f4ff", "www.example.com", true)]
        public async Task BasicStringParse(string hex, string expected, bool huffman)
        {
            var readable = HexToBuffer(ref hex);
            Assert.Equal(expected, Hpack.ReadString(ref readable));

            using (var channelFactory = new ChannelFactory())
            {
                var channel = channelFactory.CreateChannel();
                var wb = channel.Alloc();
                Hpack.WriteString(wb, expected, huffman);
                Assert.Equal(hex, ToHex(wb));
                await wb.FlushAsync();
            }
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
                    var readable = HexToBuffer(ref hex);
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
                    var hex = "8286 8441 0f77 7777 2e65 7861 6d70 6c65 2e63 6f6d";
                    var readable = HexToBuffer(ref hex);
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

                    hex = "8286 84be 5808 6e6f 2d63 6163 6865";
                    readable = HexToBuffer(ref hex);
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

                    hex = "8287 85bf 400a 6375 7374 6f6d 2d6b 6579 0c63 7573 746f 6d2d 7661 6c75 65";
                    readable = HexToBuffer(ref hex);
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
                    var hex = "8286 8441 8cf1 e3c2 e5f2 3a6b a0ab 90f4 ff";
                    var readable = HexToBuffer(ref hex);
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

                    hex = "8286 84be 5886 a8eb 1064 9cbf";
                    readable = HexToBuffer(ref hex);
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

                    hex = "8287 85bf 4088 25a8 49e9 5ba9 7d7f 8925 a849 e95b b8e8 b4bf";
                    readable = HexToBuffer(ref hex);
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
        public void C51_C52_C53()
        {
            using (var memoryPool = new MemoryPool())
            {
                var headerTable = new HeaderTable(256);
                try
                {
                    var hex = @"
4803 3330 3258 0770 7269 7661 7465 611d
4d6f 6e2c 2032 3120 4f63 7420 3230 3133
2032 303a 3133 3a32 3120 474d 546e 1768
7474 7073 3a2f 2f77 7777 2e65 7861 6d70
6c65 2e63 6f6d";
                    var readable = HexToBuffer(ref hex);
                    var httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":status: 302
cache-control: private
date: Mon, 21 Oct 2013 20:13:21 GMT
location: https://www.example.com
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 63) location: https://www.example.com
[2] (s = 65) date: Mon, 21 Oct 2013 20:13:21 GMT
[3] (s = 52) cache-control: private
[4] (s = 42) :status: 302
Table size: 222
", headerTable.ToString());

                    hex = "4803 3330 37c1 c0bf";
                    readable = HexToBuffer(ref hex);
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":status: 307
cache-control: private
date: Mon, 21 Oct 2013 20:13:21 GMT
location: https://www.example.com
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 42) :status: 307
[2] (s = 63) location: https://www.example.com
[3] (s = 65) date: Mon, 21 Oct 2013 20:13:21 GMT
[4] (s = 52) cache-control: private
Table size: 222
", headerTable.ToString());

                    hex = @"
88c1 611d 4d6f 6e2c 2032 3120 4f63 7420
3230 3133 2032 303a 3133 3a32 3220 474d
54c0 5a04 677a 6970 7738 666f 6f3d 4153
444a 4b48 514b 425a 584f 5157 454f 5049
5541 5851 5745 4f49 553b 206d 6178 2d61
6765 3d33 3630 303b 2076 6572 7369 6f6e
3d31";
                    readable = HexToBuffer(ref hex);
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":status: 200
cache-control: private
date: Mon, 21 Oct 2013 20:13:22 GMT
location: https://www.example.com
content-encoding: gzip
set-cookie: foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 98) set-cookie: foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1
[2] (s = 52) content-encoding: gzip
[3] (s = 65) date: Mon, 21 Oct 2013 20:13:22 GMT
Table size: 215
", headerTable.ToString());
                }
                finally
                {
                    headerTable.Dispose();
                }
            }
        }

        [Fact]
        public void C61_C62_C63()
        {
            using (var memoryPool = new MemoryPool())
            {
                var headerTable = new HeaderTable(256);
                try
                {
                    var hex = @"
4882 6402 5885 aec3 771a 4b61 96d0 7abe
9410 54d4 44a8 2005 9504 0b81 66e0 82a6
2d1b ff6e 919d 29ad 1718 63c7 8f0b 97c8
e9ae 82ae 43d3";
                    var readable = HexToBuffer(ref hex);
                    var httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":status: 302
cache-control: private
date: Mon, 21 Oct 2013 20:13:21 GMT
location: https://www.example.com
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 63) location: https://www.example.com
[2] (s = 65) date: Mon, 21 Oct 2013 20:13:21 GMT
[3] (s = 52) cache-control: private
[4] (s = 42) :status: 302
Table size: 222
", headerTable.ToString());

                    hex = "4883 640e ffc1 c0bf";
                    readable = HexToBuffer(ref hex);
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":status: 307
cache-control: private
date: Mon, 21 Oct 2013 20:13:21 GMT
location: https://www.example.com
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 42) :status: 307
[2] (s = 63) location: https://www.example.com
[3] (s = 65) date: Mon, 21 Oct 2013 20:13:21 GMT
[4] (s = 52) cache-control: private
Table size: 222
", headerTable.ToString());

                    hex = @"
88c1 6196 d07a be94 1054 d444 a820 0595
040b 8166 e084 a62d 1bff c05a 839b d9ab
77ad 94e7 821d d7f2 e6c7 b335 dfdf cd5b
3960 d5af 2708 7f36 72c1 ab27 0fb5 291f
9587 3160 65c0 03ed 4ee5 b106 3d50 07";
                    readable = HexToBuffer(ref hex);
                    httpHeader = Hpack.ParseHttpHeader(ref readable, ref headerTable, memoryPool);
                    Assert.Equal(
@":status: 200
cache-control: private
date: Mon, 21 Oct 2013 20:13:22 GMT
location: https://www.example.com
content-encoding: gzip
set-cookie: foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1
", httpHeader.ToString());
                    Assert.Equal(
@"[1] (s = 98) set-cookie: foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1
[2] (s = 52) content-encoding: gzip
[3] (s = 65) date: Mon, 21 Oct 2013 20:13:22 GMT
Table size: 215
", headerTable.ToString());
                }
                finally
                {
                    headerTable.Dispose();
                }
            }
        }

        private static string NormalizeHex(string hex)
        {
            if (hex == null) return null;
            return hex.Replace("\r", "").Replace("\n", "").Replace(" ", "").Trim().ToLowerInvariant();
        }
        private static ReadableBuffer HexToBuffer(ref string hex)
        {
            hex = NormalizeHex(hex);
            if (hex == null) return default(ReadableBuffer);            
            byte[] data = new byte[hex.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return ReadableBuffer.Create(data, 0, data.Length);
        }
    }
}
