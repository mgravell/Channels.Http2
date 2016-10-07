using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Formatting;
using System.Threading.Tasks;
using Channels.Text.Primitives;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Primitives;
using System.Binary;
using DemoServer.HttpServer;

namespace Channels.Samples.Http
{
    public partial class HttpConnection<TContext>
    {
        private static readonly byte[] _http11Bytes = Encoding.UTF8.GetBytes("HTTP/1.1 ");
        private static readonly byte[] _chunkedEndBytes = Encoding.UTF8.GetBytes("0\r\n\r\n");
        private static readonly byte[] _endChunkBytes = Encoding.ASCII.GetBytes("\r\n");
        private static readonly byte[] _http2SwitchBytes = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: h2c\r\n\r\n");

        private readonly IReadableChannel _input;
        private readonly IWritableChannel _output;
        private readonly IHttpApplication<TContext> _application;
        private readonly WritableChannelFormatter _outputFormatter;
        private bool _isHttp2;
        private HttpSettings _settings;

        public RequestHeaderDictionary RequestHeaders => _parser.RequestHeaders;
        public ResponseHeaderDictionary ResponseHeaders { get; } = new ResponseHeaderDictionary();

        public PreservedBuffer HttpVersion => _parser.HttpVersion;
        public PreservedBuffer Path => _parser.Path;
        public PreservedBuffer Method => _parser.Method;

        // TODO: Check the http version
        public bool KeepAlive => true; //RequestHeaders.ContainsKey("Connection") && string.Equals(RequestHeaders["Connection"], "keep-alive");

        private bool HasContentLength => ResponseHeaders.ContainsKey("Content-Length");
        private bool HasTransferEncoding => ResponseHeaders.ContainsKey("Transfer-Encoding");

        private HttpRequestStream<TContext> _requestBody;
        private HttpResponseStream<TContext> _responseBody;

        private bool _autoChunk;

        private HttpRequestParser _parser = new HttpRequestParser();

        public HttpConnection(IHttpApplication<TContext> application, IReadableChannel input, IWritableChannel output)
        {
            _application = application;
            _input = input;
            _output = output;
            _requestBody = new HttpRequestStream<TContext>(this);
            _responseBody = new HttpResponseStream<TContext>(this);
            _outputFormatter = _output.GetFormatter(EncodingData.InvariantUtf8);
        }

        public IReadableChannel Input => _input;

        public IWritableChannel Output => _output;

        public HttpRequestStream<TContext> RequestBody { get; set; }

        public HttpResponseStream<TContext> ResponseBody { get; set; }


        public async Task ProcessAllRequests()
        {
            Reset();

            while (true)
            {
                var buffer = await _input.ReadAsync();

                try
                {
                    if (buffer.IsEmpty && _input.Reading.IsCompleted)
                    {
                        // We're done with this connection
                        return;
                    }
                    Console.WriteLine($"Read: {buffer.Length} bytes");
                    Console.WriteLine(BitConverter.ToString(buffer.ToArray()));
                    Console.WriteLine(buffer.GetAsciiString());
                    var result = _parser.ParseRequest(ref buffer);
                    Console.WriteLine($"Result: {result}");
                    switch (result)
                    {
                        case HttpRequestParser.ParseResult.Incomplete:
                            if (_input.Reading.IsCompleted)
                            {
                                // Didn't get the whole request and the connection ended
                                throw new EndOfStreamException();
                            }
                            // Need more data
                            continue;
                        case HttpRequestParser.ParseResult.Complete:
                            // Done
                            break;
                        case HttpRequestParser.ParseResult.BadRequest:
                            // TODO: Don't throw here;
                            throw new Exception();
                        default:
                            break;
                    }

                }
                catch (Exception)
                {
                    StatusCode = 400;

                    await EndResponse();

                    return;
                }
                finally
                {
                    _input.Advance(buffer.Start, buffer.End);
                }

                var context = _application.CreateContext(this);

                try
                {
                    await _application.ProcessRequestAsync(context);
                }
                catch (Exception ex)
                {
                    StatusCode = 500;

                    _application.DisposeContext(context, ex);
                }
                finally
                {
                    await EndResponse();
                }

                if (!KeepAlive)
                {
                    break;
                }

                Reset();
            }
        }

        private Task EndResponse()
        {
            if (!HasStarted)
            {
                WriteBeginResponseHeaders();
            }

            if (_autoChunk)
            {
                WriteEndResponse();

                return _outputFormatter.FlushAsync();
            }

            return Task.CompletedTask;
        }

        private void Reset()
        {
            RequestBody = _requestBody;
            ResponseBody = _responseBody;
            _parser.Reset();
            ResponseHeaders.Reset();
            HasStarted = false;
            StatusCode = 200;
            _autoChunk = false;
            _method = null;
            _path = null;
        }

        public Task WriteAsync(Span<byte> data)
        {
            if (!HasStarted)
            {
                WriteBeginResponseHeaders();
            }

            if (_autoChunk)
            {
                _outputFormatter.Append(data.Length, Format.Parsed.HexLowercase);
                _outputFormatter.Write(data);
                _outputFormatter.Write(_endChunkBytes);
            }
            else
            {
                _outputFormatter.Write(data);
            }

            return _outputFormatter.FlushAsync();
        }

        private void WriteBeginResponseHeaders()
        {
            if (HasStarted)
            {
                return;
            }

            HasStarted = true;
            Console.WriteLine("Upgrade: " + RequestHeaders["Upgrade"]);
            if (!_isHttp2 && RequestHeaders.ContainsKey("Upgrade") && TryUpgradeToHttp2())
            {
                _outputFormatter.Write(_http2SwitchBytes);
                _isHttp2 = true;

                /*
                 The first HTTP/2 frame sent by the server MUST be a server connection
                 preface (Section 3.5) consisting of a SETTINGS frame (Section 6.5).
                */
                throw new NotImplementedException();
            }

            if (_isHttp2)
            {
                throw new NotImplementedException();
            }
            else
            {
                _outputFormatter.Write(_http11Bytes);
                var status = ReasonPhrases.ToStatusBytes(StatusCode);
                _outputFormatter.Write(status);

                _autoChunk = !HasContentLength && !HasTransferEncoding && KeepAlive;

                ResponseHeaders.CopyTo(_autoChunk, _outputFormatter);
            }
        }

        private bool TryUpgradeToHttp2()
        {
            try
            {
                if (RequestHeaders.ContainsKey("HTTP2-Settings") && RequestHeaders.ContainsKey("Connection"))
                {
                    var connection = RequestHeaders["Connection"];
                    if (ContainsToken(connection, "Upgrade") && ContainsToken(connection, "HTTP2-Settings")
                        && ContainsToken(RequestHeaders["Upgrade"], "htc"))
                    {
                        var settings = RequestHeaders["HTTP2-Settings"];
                        if (settings.Count == 1)
                        {
                            var base64 = settings[0];
                            switch (base64.Length % 4)
                            {
                                case 0: break; // do nothing
                                case 2: base64 += "=="; break;
                                case 3: base64 += "="; break;
                                default: return false;
                            }
                            var payload = Convert.FromBase64String(base64);
                            ParseSettings(payload);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void ParseSettings(Span<byte> settings)
        {
            if ((settings.Length % 6) != 0) throw new ArgumentException(nameof(settings));
            while(settings.Length != 0)
            {
                var id = settings.ReadBigEndian<SettingsParameter>();
                settings = settings.Slice(2);
                var value = settings.ReadBigEndian<uint>();
                settings = settings.Slice(4);
                _settings[id] = value;
            }
            Console.WriteLine(_settings.ToString());
        }

        private bool ContainsToken(StringValues headerValue, string token)
        {
            foreach (var value in headerValue)
            {
                if (string.Equals(token, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void WriteEndResponse()
        {
            _outputFormatter.Write(_chunkedEndBytes);
        }
    }
}
