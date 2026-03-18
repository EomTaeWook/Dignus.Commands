using Dignus.Commands.Internals;
using System.Text;

namespace Dignus.Commands.Network.Decoders
{
    internal class TelnetProtocolDecoder
    {
        public static readonly byte[] BackspaceEraseSequence =
        [
            (byte)ControlCharacter.Backspace,
            0x20,
            (byte)ControlCharacter.Backspace
        ];

        private const byte TelnetInterpretAsCommand = 0xFF;
        private const byte TelnetCommandMax = 0xEF;
        private const byte EscapeSequenceTerminatorMin = 0x40;
        private const byte EscapeSequenceTerminatorMax = 0x7E;

        private readonly StringBuilder _commandInputBuffer = new();

        private bool _isHandlingTelnetCommand;
        private bool _isHandlingEscapeSequence;
        public bool IsBufferEmpty => _commandInputBuffer.Length == 0;

        public void DecodeIncomingNetworkBytes(IReadOnlyList<byte> receivedBytes, Action<char> onValidCharacterProcessed)
        {
            for (int byteIndex = 0; byteIndex < receivedBytes.Count; byteIndex++)
            {
                byte currentByte = receivedBytes[byteIndex];

                if (ShouldSkipProtocolByte(currentByte))
                {
                    continue;
                }

                // 프로토콜이 아닌 실제 의미 있는 문자는 액터에게 전달
                onValidCharacterProcessed?.Invoke((char)currentByte);
            }
        }

        private bool ShouldSkipProtocolByte(byte currentByte)
        {
            if (IsTelnetCommandByte(currentByte))
            {
                return true;
            }

            if (IsEscapeSequenceByte(currentByte))
            {
                return true;
            }

            return false;
        }

        private bool IsTelnetCommandByte(byte currentByte)
        {
            if (currentByte == TelnetInterpretAsCommand || _isHandlingTelnetCommand)
            {
                _isHandlingTelnetCommand = true;

                if (currentByte != TelnetInterpretAsCommand && currentByte <= TelnetCommandMax)
                {
                    _isHandlingTelnetCommand = false;
                }

                return true;
            }

            return false;
        }

        private bool IsEscapeSequenceByte(byte currentByte)
        {
            if (_isHandlingEscapeSequence)
            {
                if (currentByte >= EscapeSequenceTerminatorMin &&
                    currentByte <= EscapeSequenceTerminatorMax)
                {
                    _isHandlingEscapeSequence = false;
                }

                return true;
            }

            if (currentByte == (byte)ControlCharacter.Esc)
            {
                _isHandlingEscapeSequence = true;
                return true;
            }
            return false;
        }
        public void RemoveLastCharacterFromBuffer()
        {
            if (_commandInputBuffer.Length > 0)
            {
                _commandInputBuffer.Length--;
            }
        }
        public void AppendCharacterToBuffer(char character) 
        {
            _commandInputBuffer.Append(character);
        }
        public string GetFinalCommandAndClearBuffer()
        {
            string finalCommand = _commandInputBuffer.ToString();
            _commandInputBuffer.Clear();
            return finalCommand;
        }
    }
}
