// SPIKE do método nativo no Windows. O objetivo NÃO é o motor — é provar as
// quatro coisas que só o Windows pode responder, e que não dá pra saber sem
// rodar:
//   1. dá pra ler o controle com OUTRO app em foco?        (XInput: sim, é polling)
//   2. dá pra injetar "ção" no app de baixo?               (SendInput UNICODE)
//   3. dá pra mostrar a overlay SEM roubar o foco?         (WS_EX_NOACTIVATE)
//   4. o acorde de ativação é alcançável?                  (o Guide é o problema)
//
// O motor silábico aqui é um TOCO deliberado: só ataque e núcleo das 8
// cardinais, sem roll, sem ortografador. Portar o motor de verdade é o passo
// seguinte, e tem que ser feito contra um arquivo-gabarito gerado pelo motor JS
// — senão passam a existir duas implementações que divergem em silêncio.
using System.Runtime.InteropServices;
using static Silabo.Native;

namespace Silabo;

static class Program {
  // as mesmas 8 cardinais do protótipo web, pra o toque ser reconhecível
  static readonly string[] ATAQUE  = { "t", "c", "s", "p", "m", "f", "l", "x" };  // ↑ ↗ → ↘ ↓ ↙ ← ↖
  static readonly string[] VOZEADO = { "d", "g", "z", "b", "n", "v", "r", "j" };
  static readonly string[] NUCLEO  = { "",  "u", "o", "ó", "a", "é", "e", "i" };

  const short ZONA = 18000;        // ~0.55 do curso, a mesma histerese da web
  const short SAIDA = 12500;
  const byte  GATILHO = 100;

  static IntPtr _hwnd;
  static string _preview = "", _estado = "sem controle";
  static bool _ativo = false;
  static WndProc? _proc;           // referência viva: sem isso o GC come o delegate

  static int Gate(short x, short y, int atual) {
    double r = Math.Sqrt((double)x * x + (double)y * y);
    if (r < (atual < 0 ? ZONA : SAIDA)) return -1;
    double a = Math.Atan2(x, y);                       // 0 = cima, horário
    if (a < 0) a += Math.PI * 2;
    return (int)Math.Round(a / (Math.PI / 4)) % 8;
  }

  static IntPtr Proc(IntPtr h, uint msg, IntPtr w, IntPtr l) {
    switch (msg) {
      case WM_MOUSEACTIVATE: return MA_NOACTIVATE;     // nunca aceitar foco
      case WM_PAINT: {
        var hdc = BeginPaint(h, out var ps);
        GetClientRect(h, out var rc);
        var fundo = CreateSolidBrush(RGB(0x1D, 0x23, 0x2C));
        FillRect(hdc, ref rc, fundo); DeleteObject(fundo);
        SetBkMode(hdc, 1);                             // TRANSPARENT
        var fonte = CreateFont(34, 0, 0, 0, 500, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var velha = SelectObject(hdc, fonte);
        SetTextColor(hdc, _ativo ? RGB(0x3C, 0x60, 0xF2) : RGB(0x9F, 0x91, 0x87));
        var txt = _preview.Length > 0 ? _preview : "—";
        TextOut(hdc, 18, 12, txt, txt.Length);
        var f2 = CreateFont(15, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        SelectObject(hdc, f2);
        SetTextColor(hdc, RGB(0x9F, 0x91, 0x87));
        TextOut(hdc, 18, 56, _estado, _estado.Length);
        SelectObject(hdc, velha); DeleteObject(fonte); DeleteObject(f2);
        EndPaint(h, ref ps);
        return IntPtr.Zero;
      }
    }
    return DefWindowProc(h, msg, w, l);
  }

  static void Redesenha(string prev, string est) {
    if (prev == _preview && est == _estado) return;
    _preview = prev; _estado = est;
    InvalidateRect(_hwnd, IntPtr.Zero, true);
  }

  static void Main(string[] args) {
    // O acorde de ativação. O pedido era LT+RT+3×Xbox, mas o Guide não existe na
    // API documentada e, quando existe, o Game Bar também o escuta. Então:
    // default no Back (documentado, sem disputa) e --guide pra quem quiser.
    bool usaGuide = args.Contains("--guide");
    ushort BOTAO_ACORDE = usaGuide ? GAMEPAD_GUIDE : GAMEPAD_BACK;
    string nomeAcorde = usaGuide ? "Xbox" : "Back";

    _proc = Proc;
    var cls = new WNDCLASSEX {
      cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(), lpfnWndProc = _proc,
      lpszClassName = "SilaboOverlay", hbrBackground = IntPtr.Zero,
    };
    RegisterClassEx(ref cls);

    int larg = 360, alt = 88;
    int x = GetSystemMetrics(SM_CXSCREEN) - larg - 40;
    int y = GetSystemMetrics(SM_CYSCREEN) - alt - 80;
    _hwnd = CreateWindowEx(
      WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE,
      "SilaboOverlay", null, WS_POPUP, x, y, larg, alt, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    SetLayeredWindowAttributes(_hwnd, 0, 225, LWA_ALPHA);
    ShowWindow(_hwnd, SW_SHOWNOACTIVATE);

    int gL = -1, gR = -1;
    bool rtAntes = false, acordeAntes = false;
    int cliques = 0; var ultimoClique = DateTime.MinValue;

    while (true) {
      while (PeekMessage(out var m, IntPtr.Zero, 0, 0, 1)) { TranslateMessage(ref m); DispatchMessage(ref m); }

      if (!LePad(0, out var g)) {
        Redesenha("", "sem controle"); Thread.Sleep(60); continue;
      }

      // acorde: LT+RT segurados e o botão batido 3x em 1,2s
      bool gatilhos = g.bLeftTrigger > GATILHO && g.bRightTrigger > GATILHO;
      bool acorde = (g.wButtons & BOTAO_ACORDE) != 0;
      if (gatilhos && acorde && !acordeAntes) {
        var agora = DateTime.UtcNow;
        cliques = (agora - ultimoClique).TotalMilliseconds < 1200 ? cliques + 1 : 1;
        ultimoClique = agora;
        if (cliques >= 3) { _ativo = !_ativo; cliques = 0; InvalidateRect(_hwnd, IntPtr.Zero, true); }
      }
      acordeAntes = acorde;

      gL = Gate(g.sThumbLX, g.sThumbLY, gL);
      gR = Gate(g.sThumbRX, g.sThumbRY, gR);

      bool vozeado = g.bLeftTrigger > GATILHO && !gatilhos;
      string silaba = (gL < 0 ? "" : (vozeado ? VOZEADO : ATAQUE)[gL]) + (gR < 0 ? "" : NUCLEO[gR]);

      // RT confirma — e é aqui que o app de baixo recebe o texto
      bool rt = g.bRightTrigger > GATILHO && !gatilhos;
      if (rt && !rtAntes && _ativo && silaba.Length > 0) Digita(silaba);
      rtAntes = rt;

      Redesenha(silaba, _ativo
        ? $"ATIVO · LT+RT+3x{nomeAcorde} desliga"
        : $"parado · LT+RT+3x{nomeAcorde} liga"
          + (usaGuide && !GuideDisponivel ? "  (Guide indisponível!)" : ""));
      Thread.Sleep(4);
    }
  }
}
