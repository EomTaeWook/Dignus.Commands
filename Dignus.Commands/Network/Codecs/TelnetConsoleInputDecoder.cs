using Dignus.Commands.Internals;
using System.Text;

namespace Dignus.Commands.Network.Codecs
{
    internal class TelnetConsoleInputDecoder
    {
        private readonly StringBuilder _commandInputBuffer = new();
        private TelnetInputDecodeState _decodeState;

        public int CurrentBufferLength => _commandInputBuffer.Length;
        public bool IsBufferEmpty => _commandInputBuffer.Length == 0;

        public void DecodeIncomingNetworkBytes(IReadOnlyList<byte> receivedBytes,
            Action<char> onValidCharacterProcessed,
            Action<TerminalInputKey> onTerminalInputKeyProcessed = null)
        {
            ArgumentNullException.ThrowIfNull(receivedBytes);
            ArgumentNullException.ThrowIfNull(onValidCharacterProcessed);

            for (int i = 0; i < receivedBytes.Count; ++i)
            {
                byte currentByte = receivedBytes[i];

                if (TryHandleProtocolByte(currentByte, onTerminalInputKeyProcessed))
                {
                    continue;
                }
                onValidCharacterProcessed?.Invoke((char)currentByte);
            }
        }
        
        private bool TryHandleProtocolByte(
            byte currentByte,
            Action<TerminalInputKey> onTerminalInputKeyProcessed)
        {
            switch (_decodeState)
            {
                case TelnetInputDecodeState.TelnetCommand:
                    _decodeState = TelnetInputDecodeState.None;
                    return true;

                case TelnetInputDecodeState.EscapePrefix:
                    if (currentByte == (byte)'[' || currentByte == (byte)'O')
                    {
                        _decodeState = TelnetInputDecodeState.EscapeBody;
                        return true;
                    }

                    _decodeState = TelnetInputDecodeState.None;
                    return true;

                case TelnetInputDecodeState.EscapeBody:
                    _decodeState = TelnetInputDecodeState.None;

                    if (onTerminalInputKeyProcessed != null)
                    {
                        if (currentByte == (byte)'A')
                        {
                            onTerminalInputKeyProcessed(TerminalInputKey.ArrowUp);
                            return true;
                        }

                        if (currentByte == (byte)'B')
                        {
                            onTerminalInputKeyProcessed(TerminalInputKey.ArrowDown);
                            return true;
                        }

                        if (currentByte == (byte)'C')
                        {
                            onTerminalInputKeyProcessed(TerminalInputKey.ArrowRight);
                            return true;
                        }

                        if (currentByte == (byte)'D')
                        {
                            onTerminalInputKeyProcessed(TerminalInputKey.ArrowLeft);
                            return true;
                        }
                    }

                    return true;
            }

            if (currentByte == TelnetControlSequence.InterpretAsCommand)
            {
                _decodeState = TelnetInputDecodeState.TelnetCommand;
                return true;
            }

            if (currentByte == (byte)ControlCharacter.Esc)
            {
                _decodeState = TelnetInputDecodeState.EscapePrefix;
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

        public void ReplaceBuffer(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            _commandInputBuffer.Clear();
            _commandInputBuffer.Append(text);
        }

        public string GetFinalCommandAndClearBuffer()
        {
            string finalCommand = _commandInputBuffer.ToString();
            _commandInputBuffer.Clear();
            return finalCommand;
        }
    }
}
