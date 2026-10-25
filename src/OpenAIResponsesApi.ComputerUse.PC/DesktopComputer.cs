using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ComputerUse;

internal sealed class DesktopComputer
{
    private readonly Rectangle desktopBounds;

    public DesktopComputer()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        desktopBounds = SystemInformation.VirtualScreen;
    }

    public Task<byte[]> ScreenshotAsync()
    {
        using Bitmap bitmap = new(desktopBounds.Width, desktopBounds.Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
            graphics.CopyFromScreen(desktopBounds.Location, Point.Empty, desktopBounds.Size);
        using MemoryStream stream = new();
        bitmap.Save(stream, ImageFormat.Png);
        return Task.FromResult(stream.ToArray());
    }

    public async Task ExecuteAsync(JsonElement action)
    {
        string type = action.GetProperty("type").GetString()!;
        ushort[] modifiers = type != "keypress" && action.TryGetProperty("keys", out JsonElement keys) && keys.ValueKind != JsonValueKind.Null
            ? ResolveKeys(keys.EnumerateArray().Select(key => key.GetString()!))
            : [];
        try
        {
            foreach (ushort modifier in modifiers) Key(modifier, false);
            switch (type)
            {
                case "click":
                case "double_click":
                    string? button = action.TryGetProperty("button", out JsonElement value) ? value.GetString() : null;
                    await Click(action.GetProperty("x").GetInt32(), action.GetProperty("y").GetInt32(), NormalizeButton(button), type == "double_click");
                    break;
                case "type":
                    await TypeText(action.GetProperty("text").GetString()!);
                    break;
                case "keypress":
                    await PressKeys(action.GetProperty("keys").EnumerateArray().Select(key => key.GetString()!).ToArray());
                    break;
                case "scroll":
                    await Scroll(action.GetProperty("x").GetInt32(), action.GetProperty("y").GetInt32(), action.GetProperty("scroll_x").GetInt32(), action.GetProperty("scroll_y").GetInt32());
                    break;
                case "move":
                    Move(action.GetProperty("x").GetInt32(), action.GetProperty("y").GetInt32());
                    break;
                case "drag":
                    JsonElement[] path = action.GetProperty("path").EnumerateArray().ToArray();
                    if (path.Length < 2) throw new ArgumentException("A drag requires at least two points.");
                    Move(path[0].GetProperty("x").GetInt32(), path[0].GetProperty("y").GetInt32());
                    Mouse(0x0002);
                    try
                    {
                        foreach (JsonElement point in path.Skip(1))
                        {
                            Move(point.GetProperty("x").GetInt32(), point.GetProperty("y").GetInt32());
                            await Task.Delay(20);
                        }
                    }
                    finally { Mouse(0x0004); }
                    await Task.Delay(250);
                    break;
                case "wait":
                    await Task.Delay(1000);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported computer action: {type}");
            }
        }
        finally
        {
            foreach (ushort modifier in modifiers.Reverse()) Key(modifier, true);
        }
    }

    private static string NormalizeButton(string? button) => button is null ? "left" : button == "wheel" ? "middle" : button;

    private async Task Click(
        int x,
        int y,
        string button = "left",
        bool doubleClick = false)
    {
        (uint down, uint up) = button.ToLowerInvariant() switch
        {
            "left" => (0x0002u, 0x0004u), "right" => (0x0008u, 0x0010u), "middle" => (0x0020u, 0x0040u),
            _ => throw new ArgumentException($"Unsupported mouse button: {button}")
        };
        Move(x, y);
        for (int count = 0; count < (doubleClick ? 2 : 1); count++)
        {
            Mouse(down); Mouse(up);
        }
        await Task.Delay(250);
    }

    private async Task TypeText(
        string text)
    {
        foreach (char character in text)
        {
            if (character == '\n') { Key(0x0D, false); Key(0x0D, true); }
            else if (character != '\r') { Unicode(character, false); Unicode(character, true); }
        }
        await Task.Delay(250);
    }

    private async Task PressKeys(
        string[] keys)
    {
        ushort[] codes = ResolveKeys(keys);
        try { foreach (ushort code in codes) Key(code, false); }
        finally { foreach (ushort code in codes.Reverse()) Key(code, true); }
        await Task.Delay(250);
    }

    private async Task Scroll(int x, int y, int horizontal, int vertical)
    {
        Move(x, y);
        Mouse(0x0800, -vertical);
        Mouse(0x1000, horizontal);
        await Task.Delay(250);
    }

    private void Move(int x, int y)
    {
        if (!SetCursorPos(desktopBounds.Left + x, desktopBounds.Top + y))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private static ushort[] ResolveKeys(IEnumerable<string> keys) => keys
        .SelectMany(ResolveKey)
        .Distinct()
        .OrderBy(code => code is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C ? 0 : 1)
        .ToArray();

    private static IEnumerable<ushort> ResolveKey(string key)
    {
        if (key.Length != 1 || char.IsAsciiLetterOrDigit(key[0]))
            return [VirtualKey(key)];

        uint threadId = GetWindowThreadProcessId(GetForegroundWindow(), nint.Zero);
        short mapping = VkKeyScanExW(key[0], GetKeyboardLayout(threadId));
        if (mapping == -1 || (mapping & 0xF800) != 0)
            throw new NotSupportedException($"The active keyboard layout cannot produce key '{key}'. Use a type action for text input.");

        List<ushort> codes = [];
        if ((mapping & 0x0100) != 0) codes.Add(0x10);
        if ((mapping & 0x0200) != 0) codes.Add(0x11);
        if ((mapping & 0x0400) != 0) codes.Add(0x12);
        codes.Add((ushort)(mapping & 0xFF));
        return codes;
    }

    private static ushort VirtualKey(string key) => key.ToUpperInvariant() switch
    {
        "CTRL" or "CONTROL" => 0x11, "ALT" => 0x12, "SHIFT" => 0x10,
        "WIN" or "WINDOWS" or "META" or "SUPER" => 0x5B, "ESC" or "ESCAPE" => 0x1B,
        "ENTER" or "RETURN" => 0x0D, "TAB" => 0x09, "SPACE" => 0x20,
        "BACKSPACE" => 0x08, "DELETE" => 0x2E, "HOME" => 0x24, "END" => 0x23,
        "UP" or "ARROWUP" => 0x26, "DOWN" or "ARROWDOWN" => 0x28,
        "LEFT" or "ARROWLEFT" => 0x25, "RIGHT" or "ARROWRIGHT" => 0x27,
        "PAGEUP" => 0x21, "PAGEDOWN" => 0x22,
        "INSERT" => 0x2D, "MULTIPLY" => 0x6A, "ADD" => 0x6B, "SUBTRACT" => 0x6D, "DECIMAL" => 0x6E, "DIVIDE" => 0x6F,
        _ when key.Length > 1 && key[0] is 'F' or 'f' && int.TryParse(key[1..], out int number) && number is >= 1 and <= 24 => (ushort)(0x70 + number - 1),
        _ when key.Length == 1 && char.IsAsciiLetterOrDigit(key[0]) => char.ToUpperInvariant(key[0]),
        _ => throw new NotSupportedException($"Unsupported key: {key}")
    };

    private static void Key(ushort key, bool up)
    {
        uint flags = up ? 2u : 0u;
        if (key is >= 0x21 and <= 0x2E or 0x5B or 0x5C or 0x6F)
            flags |= 0x0001;
        Send(new Input { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { VirtualKey = key, Flags = flags } } });
    }
    private static void Unicode(char character, bool up) => Send(new Input { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { ScanCode = character, Flags = 4u | (up ? 2u : 0u) } } });
    private static void Mouse(uint flags, int data = 0) => Send(new Input { Data = new InputData { Mouse = new MouseInput { Flags = flags, MouseData = unchecked((uint)data) } } });
    private static void Send(Input input)
    {
        if (SendInput(1, [input], Marshal.SizeOf<Input>()) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input { public uint Type; public InputData Data; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputData { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { public int X, Y; public uint MouseData, Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort VirtualKey, ScanCode; public uint Flags, Time; public nuint ExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern short VkKeyScanExW(char character, nint keyboardLayout);
    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, nint processId);
}
