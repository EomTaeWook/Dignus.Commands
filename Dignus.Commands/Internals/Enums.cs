namespace Dignus.Commands.Internals
{
    internal enum ControlCharacter : byte
    {
        Null = 0x00,
        StartOfHeading = 0x01,
        StartOfText = 0x02,
        EndOfText = 0x03,      // Ctrl+C
        Backspace = 0x08,
        HorizontalTab = 0x09,
        LineFeed = 0x0A,
        Esc = 0x1B,
        CarriageReturn = 0x0D,
        Delete = 0x7F
    }

    internal enum TelnetInputDecodeState
    {
        None,
        TelnetCommand,
        EscapePrefix,
        EscapeBody
    }

    internal enum TerminalInputKey
    {
        ArrowUp,
        ArrowDown,
        ArrowLeft,
        ArrowRight
    }

}
