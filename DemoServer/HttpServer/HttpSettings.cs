using System;

namespace DemoServer.HttpServer
{
    public enum SettingsParameter : ushort
    {
        HeaderTableSize = 1, // 4,096
        EnablePush = 2, // = 1, 0 or 1
        MaxConcurrentStreams = 3, // uint.MaxValue
        InitialWindowSize = 4, // 65,535
        MaxFrameSize = 5, // 16,384, max: 16,777,215
        MaxHeaderListSize = 6 // uint.MaxValue
    }
    public struct HttpSettings
    {
        private uint
            _xor_headerTableSize,
            _xor_enablePush,
            _xor_maxConcurrentStreams,
            _xor_initialWindowSize,
            _xor_maxFrameSize,
            _xor_maxHeaderListSize;

        public uint HeaderTableSize => _xor_headerTableSize ^ 4096;
        public uint EnablePush => _xor_enablePush ^ 1;
        public uint MaxConcurrentStreams => _xor_maxConcurrentStreams ^ uint.MaxValue;
        public uint InitialWindowSize => _xor_initialWindowSize ^ 65535;
        public uint MaxFrameSize => _xor_maxHeaderListSize ^ 16384;
        public uint MaxHeaderListSize => _xor_maxHeaderListSize ^ uint.MaxValue;

        public override string ToString()
            => $"{nameof(HeaderTableSize)}={HeaderTableSize}, {nameof(EnablePush)}={EnablePush}, {nameof(MaxConcurrentStreams)}={MaxConcurrentStreams}, {nameof(InitialWindowSize)}={InitialWindowSize}, {nameof(MaxFrameSize)}={MaxFrameSize}, {nameof(MaxHeaderListSize)}={MaxHeaderListSize}";
        public uint this[SettingsParameter key]
        {
            get
            {
                switch(key)
                {
                    case SettingsParameter.HeaderTableSize:
                        return HeaderTableSize;
                    case SettingsParameter.EnablePush:
                        return EnablePush;
                    case SettingsParameter.InitialWindowSize:
                        return InitialWindowSize;
                    case SettingsParameter.MaxConcurrentStreams:
                        return MaxConcurrentStreams;
                    case SettingsParameter.MaxFrameSize:
                        return MaxFrameSize;
                    case SettingsParameter.MaxHeaderListSize:
                        return MaxHeaderListSize;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(key), key.ToString());
                }
            }
            set
            {
                switch(key)
                {
                    case SettingsParameter.EnablePush:
                        if (value != 0 && value != 1) throw new ArgumentOutOfRangeException(nameof(value));
                        _xor_enablePush = value ^ 1;
                        break;
                    case SettingsParameter.HeaderTableSize:
                        _xor_headerTableSize = value ^ 4096;
                        break;
                    case SettingsParameter.InitialWindowSize:
                        _xor_initialWindowSize = value ^ 65535;
                        break;
                    case SettingsParameter.MaxConcurrentStreams:
                        _xor_maxConcurrentStreams = value ^ uint.MaxValue;
                        break;
                    case SettingsParameter.MaxFrameSize:
                        if(value < 16384 || value > 16777215) throw new ArgumentOutOfRangeException(nameof(value));
                        _xor_maxFrameSize = value ^ 16384;
                        break;
                    case SettingsParameter.MaxHeaderListSize:
                        _xor_maxConcurrentStreams = value ^ uint.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(key), key.ToString());
                }
            }
        }
    }
}
