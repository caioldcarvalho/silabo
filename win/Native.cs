// P/Invoke, num arquivo só. Tudo que é Win32 mora aqui pra o resto do programa
// ficar legível — e pra o dia em que houver um backend Linux, é este arquivo
// que ganha um irmão, não o motor.
using System.Runtime.InteropServices;

namespace Silabo;

internal static class Native {
  // ---------------------------------------------------------------- XInput
  [StructLayout(LayoutKind.Sequential)]
  public struct XInputGamepad {
    public ushort wButtons;
    public byte bLeftTrigger, bRightTrigger;
    public short sThumbLX, sThumbLY, sThumbRX, sThumbRY;
  }
  [StructLayout(LayoutKind.Sequential)]
  public struct XInputState { public uint dwPacketNumber; public XInputGamepad Gamepad; }

  [DllImport("xinput1_4.dll")]
  static extern uint XInputGetState(uint i, out XInputState s);

  // O botão Guide (Xbox) NÃO existe na API documentada: XInputGetState o
  // mascara. Ele só sai pelo ordinal 100, que não tem nome exportado nem
  // documentação — e some se a Microsoft mudar de ideia. Por isso é opcional:
  // se o P/Invoke falhar, caímos no XInputGetState e o Guide simplesmente não
  // existe, em vez de o programa morrer.
  [DllImport("xinput1_4.dll", EntryPoint = "#100")]
  static extern uint XInputGetStateEx(uint i, out XInputState s);

  static bool _semGuide;
  public static bool LePad(uint idx, out XInputGamepad g) {
    XInputState s; uint r;
    if (!_semGuide) {
      try { r = XInputGetStateEx(idx, out s); g = s.Gamepad; return r == 0; }
      catch (EntryPointNotFoundException) { _semGuide = true; }
      catch (DllNotFoundException) { _semGuide = true; }
    }
    r = XInputGetState(idx, out s); g = s.Gamepad; return r == 0;
  }
  public static bool GuideDisponivel => !_semGuide;

  public const ushort GAMEPAD_A = 0x1000, GAMEPAD_B = 0x2000, GAMEPAD_X = 0x4000,
                      GAMEPAD_Y = 0x8000, GAMEPAD_LB = 0x0100, GAMEPAD_RB = 0x0200,
                      GAMEPAD_BACK = 0x0020, GAMEPAD_START = 0x0010,
                      GAMEPAD_LTHUMB = 0x0040, GAMEPAD_RTHUMB = 0x0080,
                      GAMEPAD_UP = 0x0001, GAMEPAD_DOWN = 0x0002,
                      GAMEPAD_LEFT = 0x0004, GAMEPAD_RIGHT = 0x0008,
                      GAMEPAD_GUIDE = 0x0400;   // só com XInputGetStateEx

  // ------------------------------------------------------------- SendInput
  [StructLayout(LayoutKind.Sequential)]
  struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
  // MOUSEINPUT precisa estar aqui mesmo sem ser usado: INPUT é uma UNIÃO, e o
  // tamanho dela é o do maior membro. Sem isto a struct mede 32 e o SendInput
  // exige 40 — e quando o cbSize está errado ele devolve 0 e NÃO INSERE NADA,
  // sem erro e sem exceção. Foi o que fez a overlay detectar tudo e não digitar.
  [StructLayout(LayoutKind.Sequential)]
  struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
  [StructLayout(LayoutKind.Explicit)]
  struct INPUT {
    [FieldOffset(0)] public uint type;
    [FieldOffset(8)] public MOUSEINPUT mi;
    [FieldOffset(8)] public KEYBDINPUT ki;
  }

  [DllImport("user32.dll", SetLastError = true)]
  static extern uint SendInput(uint n, [In] INPUT[] p, int cb);

  const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 2, KEYEVENTF_UNICODE = 4;

  // Digita texto arbitrário na janela que tem o foco. KEYEVENTF_UNICODE manda o
  // caractere direto, sem passar por layout de teclado — que é exatamente o que
  // um método silábico precisa: ele emite "ção", não uma sequência de teclas.
  public static string? UltimoErro;
  public static void Digita(string txt) {
    var ins = new List<INPUT>(txt.Length * 2);
    foreach (var ch in txt) {
      // fora do BMP viraria par surrogate; o português não precisa, mas se
      // precisar um dia é aqui que entra
      ins.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE } });
      ins.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } });
    }
    if (ins.Count == 0) return;
    // Tecla que não acha alvo tem que avisar — a regra do projeto vale aqui
    // também. SendInput falha devolvendo 0, sem exceção.
    var enviados = SendInput((uint)ins.Count, ins.ToArray(), Marshal.SizeOf<INPUT>());
    UltimoErro = enviados == ins.Count ? null
      : $"SendInput inseriu {enviados}/{ins.Count} (erro {Marshal.GetLastWin32Error()})";
  }

  // O backspace não pode ir por KEYEVENTF_UNICODE: aquele caminho manda um
  // CARACTERE, e U+0008 não é a tecla de apagar — é um caractere de controle que
  // a maioria dos aplicativos ignora. Tecla de verdade vai por virtual-key.
  // A overlay precisa disto porque toda pós-correção (acento, caixa, sibilante)
  // reescreve texto que já foi injetado: apaga o que divergiu e redigita.
  const ushort VK_BACK = 0x08;
  public static void Apaga(int quantos) {
    if (quantos <= 0) return;
    var ins = new List<INPUT>(quantos * 2);
    for (int i = 0; i < quantos; i++) {
      ins.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_BACK } });
      ins.Add(new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_BACK, dwFlags = KEYEVENTF_KEYUP } });
    }
    var enviados = SendInput((uint)ins.Count, ins.ToArray(), Marshal.SizeOf<INPUT>());
    UltimoErro = enviados == ins.Count ? null
      : $"SendInput (backspace) inseriu {enviados}/{ins.Count} (erro {Marshal.GetLastWin32Error()})";
  }

  // --------------------------------------------------------------- janela
  public delegate IntPtr WndProc(IntPtr h, uint msg, IntPtr w, IntPtr l);

  [StructLayout(LayoutKind.Sequential)]
  public struct WNDCLASSEX {
    public uint cbSize, style; public WndProc lpfnWndProc; public int cbClsExtra, cbWndExtra;
    public IntPtr hInstance, hIcon, hCursor, hbrBackground;
    [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
    [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    public IntPtr hIconSm;
  }
  [StructLayout(LayoutKind.Sequential)] public struct MSG {
    public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y;
  }
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int left, top, right, bottom; }
  [StructLayout(LayoutKind.Sequential)] public struct PAINTSTRUCT {
    public IntPtr hdc; public bool fErase; public RECT rcPaint; public bool fRestore, fIncUpdate;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
  }

  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern ushort RegisterClassEx(ref WNDCLASSEX c);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr CreateWindowEx(
    uint exStyle, string cls, string? name, uint style, int x, int y, int w, int h,
    IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
  [DllImport("user32.dll")] public static extern IntPtr DefWindowProc(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool PeekMessage(out MSG m, IntPtr h, uint a, uint b, uint remove);
  [DllImport("user32.dll")] public static extern bool TranslateMessage(ref MSG m);
  [DllImport("user32.dll")] public static extern IntPtr DispatchMessage(ref MSG m);
  [DllImport("user32.dll")] public static extern bool InvalidateRect(IntPtr h, IntPtr r, bool erase);
  [DllImport("user32.dll")] public static extern IntPtr BeginPaint(IntPtr h, out PAINTSTRUCT ps);
  [DllImport("user32.dll")] public static extern bool EndPaint(IntPtr h, ref PAINTSTRUCT ps);
  [DllImport("user32.dll")] public static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern int FillRect(IntPtr hdc, ref RECT r, IntPtr br);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
  [DllImport("gdi32.dll")] public static extern IntPtr CreateSolidBrush(uint color);
  [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr CreateFont(
    int h, int w, int esc, int ori, int weight, uint italic, uint under, uint strike,
    uint charset, uint outPrec, uint clipPrec, uint quality, uint pitch, string face);
  [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
  [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
  [DllImport("gdi32.dll")] public static extern uint SetTextColor(IntPtr dc, uint c);
  [DllImport("gdi32.dll")] public static extern int SetBkMode(IntPtr dc, int mode);
  [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern bool TextOut(IntPtr dc, int x, int y, string s, int len);
  [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int cx, cy; }
  [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern bool GetTextExtentPoint32(IntPtr dc, string s, int len, out SIZE sz);
  [DllImport("gdi32.dll")] public static extern IntPtr CreatePen(int style, int width, uint color);
  [DllImport("gdi32.dll")] public static extern bool Ellipse(IntPtr dc, int l, int t, int r, int b);
  [DllImport("gdi32.dll")] public static extern IntPtr GetStockObject(int i);

  // WS_EX_NOACTIVATE é o que faz tudo funcionar: sem ele a overlay rouba o
  // foco e o SendInput passa a digitar nela mesma em vez de no app de baixo.
  public const uint WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20,
                    WS_EX_TOOLWINDOW = 0x80, WS_EX_TOPMOST = 0x8, WS_EX_NOACTIVATE = 0x8000000;
  public const uint WS_POPUP = 0x80000000;
  public const uint LWA_ALPHA = 2;
  public const uint WM_PAINT = 0x000F, WM_DESTROY = 0x0002, WM_MOUSEACTIVATE = 0x0021;
  public const int MA_NOACTIVATE = 3, SW_SHOWNOACTIVATE = 4, SM_CXSCREEN = 0, SM_CYSCREEN = 1;
  public static uint RGB(int r, int g, int b) => (uint)(r | (g << 8) | (b << 16));
}
