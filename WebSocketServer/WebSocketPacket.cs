using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WebSocketServer.RFC6455
{
    //
    // Websocket packet framing RFC6455
    //
    //ws-frame            = frame-fin           ; 1 bit in length
    //                      frame-rsv1          ; 1 bit in length
    //                      frame-rsv2          ; 1 bit in length
    //                      frame-rsv3          ; 1 bit in length
    //                      frame-opcode        ; 4 bits in length
    //                      frame-masked        ; 1 bit in length
    //                      frame-payload-length   ; either 7, 7+16,
    //                                             ; or 7+64 bits in
    //                                             ; length
    //                      [ frame-masking-key ]  ; 32 bits in length
    //                      frame-payload-data     ; n*8 bits in
    //                                             ; length, where
    //                                             ; n >= 0

    //Described in RFC 6455
    public enum OpCode : byte
    { 
        Continuation = 0x0,
        Text = 0x1,
        Binary = 0x2,
        Close = 0x8,
        Ping = 0x9,
        Pong = 0xA
    }

    /// <summary>
    /// Implements a WebSocket packet as defined in RFC6455.
    /// Does not handle extensions.
    /// </summary>
    public unsafe class WebSocketPacket
    {
        public static System.Threading.Tasks.Task HandlePacket(byte[] packet, Action<byte[], WebSocketPacket> onParsedMessage, Action<byte[], string/*, ErrorCode*/> onError)
        {
            return 
                new System.Threading.Tasks.Task(
                    () => 
                        { 
                            WebSocketPacket ph = new WebSocketPacket(packet);
                            ph.OnExtractedPayload += onParsedMessage;
                            ph.OnParsingError += onError;
                            try
                            {
                                ph.ExtractPayload();
                            }
                            catch { }
                        });
        }

        public static System.Threading.Tasks.Task FrameTextData(string data, Action<byte[], WebSocketPacket> onPacketFramed, Action<byte[], string/*, ErrorCode*/> onError)
        {
            return
                new System.Threading.Tasks.Task(
                    () =>
                    {
                        WebSocketPacket ph = new WebSocketPacket();
                        ph.IsLastPacket = true;
                        ph.OnPacketFramed += onPacketFramed;
                        ph.OnParsingError += onError;
                        ph.OpCode = OpCode.Text;
                        try
                        {
                            ph.SetPayload(Encoding.UTF8.GetBytes(data));
                        }
                        catch(Exception ex)
                        {
                            onError(ph.Payload, ex.Message);
                        }
                    }
                );
        }

        public static System.Threading.Tasks.Task FrameTextData(string data, bool isFirst, bool isFinal, Action<byte[], WebSocketPacket> onPacketFramed, Action<byte[], string/*, ErrorCode*/> onError)
        {
            if (isFirst && isFinal)
                return FrameTextData(data, onPacketFramed, onError);
            return
                new System.Threading.Tasks.Task(
                    () =>
                    {
                        WebSocketPacket ph = new WebSocketPacket();
                        ph.OnPacketFramed += onPacketFramed;
                        ph.OnParsingError += onError;
                        if (isFirst)
                            ph.OpCode = OpCode.Text;
                        else
                            ph.OpCode = OpCode.Continuation;
                        ph.isLastPacket = isFinal;
                        try
                        {
                            ph.SetPayload(Encoding.UTF8.GetBytes(data));
                        }
                        catch(Exception ex)
                        {
                            onError(ph.Payload, ex.Message);
                        }
                    }
                );
        }

        public static System.Threading.Tasks.Task FrameBinaryData(byte[] data, Action<byte[], WebSocketPacket> onPacketFramed, Action<byte[], string/*, ErrorCode*/> onError)
        {
            return
                new System.Threading.Tasks.Task(
                    () =>
                    {
                        WebSocketPacket ph = new WebSocketPacket();
                        ph.OpCode = OpCode.Binary;
                        ph.IsLastPacket = true;
                        ph.OnPacketFramed += onPacketFramed;
                        ph.OnParsingError += onError;
                        try
                        {
                            ph.SetPayload(data);
                        }
                        catch(Exception ex)
                        {
                            onError(data, ex.Message);
                        }
                    }
                );
        }

        public static System.Threading.Tasks.Task FrameBinaryData(byte[] data, bool isFirst, bool isFinal,Action<byte[], WebSocketPacket> onPacketFramed, Action<byte[], string/*, ErrorCode*/> onError)
        {
            if (isFirst && isFinal)
                return FrameBinaryData(data, onPacketFramed, onError);
            return
                new System.Threading.Tasks.Task(
                    () =>
                    {
                        WebSocketPacket ph = new WebSocketPacket();
                        if (isFirst)
                            ph.OpCode = OpCode.Binary;
                        else
                            ph.OpCode = OpCode.Continuation;
                        ph.isLastPacket = isFinal;
                        ph.OnPacketFramed += onPacketFramed;
                        ph.OnParsingError += onError;
                        try
                        {
                            ph.SetPayload(data);
                        }
                        catch(Exception ex)
                        {
                            onError(data, ex.Message);
                        }
                    }
                );
        }

        private byte[] _packet = null;

        private event Action<byte[], WebSocketPacket> OnExtractedPayload;
        private event Action<byte[], string/*, ErrorCode*/> OnParsingError;
        private event Action<byte[], WebSocketPacket> OnPacketFramed;

        private WebSocketPacket()
        {
        }

        private WebSocketPacket(byte[] packet)
        {
            if(packet == null)
                throw new ArgumentNullException("packet");
            _packet = packet;
            if ((int)(_packet[0] & (byte)0x70) != 0)
            {
                //TODO: These bits are "reserved" for extensions, in the future some attention will be devoted to them! ;)
                //Log warning...
            }
            //
            ExtractOpCode();
            ExtractIsLastPacket();
            ExtractIsMasked();
        }

        private void ExtractOpCode()
        {
            if (_packet == null)
            {
                throw new InvalidOperationException("packet not created");
            }
            opCode = (OpCode)(_packet[0] & (byte)0xF);
        }

        private void ExtractIsLastPacket()
        {
            if (_packet == null)
            {
                throw new InvalidOperationException("packet not created");
            }
            isLastPacket = (_packet[0] & (byte)0x80) != 0;
        }

        private void ExtractIsMasked()
        {
            if (_packet == null)
            {
                throw new InvalidOperationException("packet not created");
            }
            isMasked = (_packet[1] & (byte)0x80) != 0;
        }

        private void ExtractPayload()
        {
            if (_packet == null)
            {
                throw new InvalidOperationException("packet not created");
            }
            int _payloadLength = 0;
            //
            if (!isLastPacket && opCode != RFC6455.OpCode.Continuation)
            {
                //TODO: Enhance error reporting
                if (OnParsingError != null)
                    try
                    {
                        OnParsingError(_packet, "Invalid continuation packet!");
                    }
                    catch { }
                throw new ArgumentException("Invalid continuation packet!");
            }
            //
            //To skip first byte, uninteresting for payload extraction
            int _packetBaseIdx = 1;
            //
            //Determine payload length
            int tmpLen = 0;
            if (isMasked)
                tmpLen = (_packet[_packetBaseIdx] ^ (byte)0x80);
            else
                tmpLen = _packet[_packetBaseIdx];

            int addUp = 1;
            //
            if (tmpLen < 126)
                _payloadLength = tmpLen;
            else if (tmpLen == 126)
            {
                int _idx=_packetBaseIdx + addUp;
                byte[] len16Bits = new byte[] { _packet[_idx], _packet[_idx + 1] };
                //According to RFC6455 data comes in big endian format from the network
                _payloadLength = GetPayloadLength(BitConverter.ToUInt16(len16Bits, 0));
                addUp += 2;
            }
            else
            {
                int _idx = _packetBaseIdx + addUp;
                //RFC6455 most significant bit MUST be zero!
                if (((byte)_packet[2] & (byte)0x80) != 0)
                    throw new ArgumentException("Invalid packet: Most significant bit of 64 bits word for payload length, must be zero.");
                //
                byte[] len64bits = new byte[] { _packet[_idx], _packet[_idx + 1], _packet[_idx + 2], _packet[_idx + 3] };
                //According to RFC6455 data comes in big endian format from the network
                UInt64 longLen = GetPayloadLength(BitConverter.ToUInt64(len64bits, 0));
                //
                if (longLen > int.MaxValue)
                    throw new IndexOutOfRangeException(); //TODO: Invalid length exception...
                _payloadLength = (int)longLen;
                addUp += 4;
            }
            //
            //Get mask
            if (isMasked)
            {
                int _idx = _packetBaseIdx + addUp;
                byte[] mask = new byte[] { _packet[_idx], _packet[_idx + 1], _packet[_idx + 2], _packet[_idx + 3] };
                addUp += 4;
                byte[] toUnmask = _packet.Skip(_packetBaseIdx + addUp).ToArray();
                // Receiving a buffer with 4090 bytes with bytes as '0' after position payloadLen
                //if (toUnmask.Length != _payloadLength)
                //{
                //    //TODO: Enhance error reporting
                //    if(OnParsingError!= null)
                //        try
                //        {
                //            OnParsingError(_packet,
                //                string.Format("Invalid payload size, expected {0} present: {1}", _payloadLength, toUnmask.Length));
                //        }
                //        catch { }
                //    throw new ArgumentException(string.Format("Invalid payload size, expected {0} present: {1}", _payloadLength, toUnmask.Length));
                //}
                _maskedPayload = toUnmask;
                payload = UnEnthropyTheBytes(toUnmask, mask, _payloadLength);
                _maskKey = mask;
            }
            else
                payload = _packet.Skip(_packetBaseIdx + addUp).ToArray();

            if (OnExtractedPayload != null)
                OnExtractedPayload(payload, this);
        }

        //According to RFC6455 data comes in big endian format from the network
        private UInt16 GetPayloadLength(UInt16 val)
        {
            if (val == 0)
                return val;
            if (BitConverter.IsLittleEndian)
            {
                return (ushort)(val << 8 | val >> 8);
            }
            else
                return val;
        }

        //According to RFC6455 data comes in big endian format from the network
        private UInt64 GetPayloadLength(UInt64 val)
        {
            if (val == 0)
                return val;
            if (BitConverter.IsLittleEndian)
            {
                UInt64 reversed = 0;
                for (int _i = 1; _i <= 4; _i++)
                {
                    reversed |=
                        (
                            ((UInt64)(val & ((UInt64)0xFF << (8 * (_i - 1)))) << 8 * (9 - 2 * _i)) |
                            ((UInt64)(val & ((UInt64)0xFF << 8 * (8 - _i))) >> 8 * (9 - 2 * _i))
                        );
                }
                return reversed;
            }
            else
                return val;
        }

        private byte[] UnEnthropyTheBytes(byte[] data, byte[] mask, int payloadLen)
        {
            byte[] __payload = new byte[payloadLen];
            unsafe
            {
                fixed (byte* p = data)
                fixed (byte* m = mask)
                {
                    for (int i = 0; i < payloadLen; i++)
                        __payload[i] = (byte)(*(p + i) ^ *(m + (i % 4)));
                }
            }
            //
            return __payload;
        }

        private void SetPayload(byte[] data)
        {
            if (_packet != null)
            {
                throw new InvalidOperationException("packet already in use");
            }
            if (!isLastPacket)
                opCode = RFC6455.OpCode.Continuation;
            UInt64 _length = CalculatePacketSize(data.Length);
            byte[] message = new byte[_length];
            unsafe
            {
                fixed (byte* pMessage = message)
                {
                    using (System.IO.UnmanagedMemoryStream memory = new System.IO.UnmanagedMemoryStream(pMessage, (long)_length, (long)_length, System.IO.FileAccess.Write))
                    {
                        //TODO: Allow setting reserved bits... for now set them to zero
                        //Set Fin bit, Rsrvd bits (1,2 and 3), OpCode
                        memory.WriteByte((byte)(((isLastPacket ? 0x80 : 0) | ((byte)opCode & 0xF))));
                        memory.WriteByte((byte)((isMasked? 0x80 : 0) | (byte)(data.Length <= 126 ? data.Length : 127)));
                        UInt64 offset = 2;
                        if (_length == 126)
                        {
                            byte[] _len16 = BitConverter.GetBytes(GetPayloadLength((ushort)_length));
                            memory.Write(_len16, 0, _len16.Length);
                            offset += (UInt64)_len16.Length;
                        }
                        else if(_length > 126)
                        {
                            byte[] _len64 = BitConverter.GetBytes(GetPayloadLength(_length));
                            memory.Write(_len64, 0, _len64.Length);
                            offset += (UInt64)_len64.Length;
                        }
                        if (isMasked)
                        {
                            //TODO: Create a thread to handle the generation of entropic bytes and take some of the load off packet framing :)
                            System.Security.Cryptography.RNGCryptoServiceProvider rand = new System.Security.Cryptography.RNGCryptoServiceProvider();
                            byte[] entropy = new byte[4];
                            rand.GetBytes(entropy);
                            #region No need to implement endian restriction, Key is an array, not a "number"
                            //if (BitConverter.IsLittleEndian)
                            //{
                            //    uint _entropy_num = BitConverter.ToUInt32(entropy, 0);
                            //    _entropy_num = ((_entropy_num & 0xFF000000) >> 3 * 8) | ((_entropy_num & 0x00FF0000) >> 8) | ((_entropy_num & 0xFF) << 3 * 8) | ((_entropy_num & 0xFF00) << 8);
                            //    entropy = BitConverter.GetBytes(_entropy_num);
                            //}
                            #endregion
                            memory.Write(entropy, 0, entropy.Length);
                            offset += 4;
                            _maskedPayload = UnEnthropyTheBytes(data, entropy, data.Length);
                            _maskKey = entropy;
                        }
                        //
                        payload = data;
                        byte[] buffer = (isMasked ? _maskedPayload : payload);
                        memory.Write(buffer, 0, buffer.Length);
                        memory.Flush();
                    }
                }
            }
            _packet = message;
            //Signal that the packet is framed and ready to go!
            if (OnPacketFramed != null)
                try
                {
                    OnPacketFramed(_packet, this);
                }
                catch { }
        }

        //Determines the number of bytes of the packet being built
        private ulong CalculatePacketSize(int payload_len)
        {
            UInt64 len = 2;     //FrameFin + (Rsrv1 + Rsrv2 + Rsrv3) + OpCode + Masked + Len7bits
            if (payload_len == 126)
                len += 2;       //16 bits to hold payload length
            if (payload_len > 126)
                len += 8;       //64 bits to hold payload length
            if (isMasked)
                len += 4;       //32 bits for the masking key
            len += (uint)payload_len; //the rest of the packet length occupied by the payload...
            //
            return len;
        }

        private OpCode opCode = OpCode.Continuation;
        public OpCode OpCode
        {
            get 
            {
                if (_packet == null)
                    throw new InvalidOperationException("packet not created");
                return opCode; 
            }
            set
            {
                if (_packet != null)
                {
                    throw new InvalidOperationException("packet already in use");
                }
                opCode = value;
            }
        }

        private bool isMasked = false;
        public bool IsMasked
        {
            get
            {
                return isMasked;
            }
            //This is a server, RFC6455 states that only clients must mask payload
            internal set
            {
                if (_packet != null)
                    throw new InvalidOperationException("packet already un use");
                isMasked = value;
            }
        }

        private byte[] _maskedPayload = null;
        public byte[] MaskedPayload
        {
            get { return _maskedPayload; }
        }

        private byte[] _maskKey = null;
        internal byte[] MaskKey
        {
            get
            {
                return _maskKey;
            }
        }
        
        private bool isLastPacket = false;
        public bool IsLastPacket
        {
            get
            {
                return isLastPacket;
            }
            set
            {
                if (_packet != null)
                    throw new InvalidOperationException("packet already un use");
                isLastPacket = value;
            }
        }

        private byte[] payload = null;
        public byte[] Payload
        {
            get { return payload; }
        }
    }
}
