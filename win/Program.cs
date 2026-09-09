// SPIKE do método nativo no Windows. O objetivo NÃO é o motor — é provar as
// quatro coisas que só o Windows responde, e que não dá pra saber sem rodar:
//   1. dá pra ler o controle com OUTRO app em foco?        (XInput: sim, é polling)
//   2. dá pra injetar "ção" no app de baixo?               (SendInput UNICODE)
//   3. dá pra mostrar a overlay SEM roubar o foco?         (WS_EX_NOACTIVATE)
//   4. o acorde de ativação é alcançável?                  (o Guide é o problema)
//
// O motor silábico aqui é um TOCO deliberado: só ataque e núcleo das 8
// cardinais, sem roll, sem coda, sem ortografador. Portar o motor de verdade é
// o passo seguinte, e tem que ser feito contra um arquivo-gabarito gerado pelo
// motor JS — senão passam a existir duas implementações que divergem em
// silêncio.
using System.Runtime.InteropServices;
using static Silabo.Native;

namespace Silabo;

static class Program {
  // as mesmas 8 cardinais do protótipo web, pra o toque ser reconhecível
  static readonly string[] ATAQUE  = { "t", "c", "s", "p", "m", "f", "l", "x" };  // ↑ ↗ → ↘ ↓ ↙ ← ↖
  static readonly string[] VOZEADO = { "d", "g", "z", "b", "n", "v", "r", "j" };
  static readonly string[] NUCLEO  = { "—", "u", "o", "ó", "a", "é", "e", "i" };

  const short ZONA = 18000;        // ~0.55 do curso, a mesma histerese da web
  const short SAIDA = 12500;
  const byte  GATILHO = 100;

  const int LARG = 470, ALT = 214, RAIO = 44;
  const int LX = 96, RX = 236, WY = 88;      // centros das duas rodas

  static IntPtr _hwnd;
  static string _preview = "", _estado = "sem controle";
  static int _gL = -1, _gR = -1;
  static bool _vozeado, _ativo;
  static WndProc? _proc;           // referência viva: sem isso o GC come o delegate

  static int Gate(short x, short y, int atual) {
    double r = Math.Sqrt((double)x * x + (double)y * y);
    if (r < (atual < 0 ? ZONA : SAIDA)) return -1;
    double a = Math.Atan2(x, y);                       // 0 = cima, horário
    if (a < 0) a += Math.PI * 2;
    return (int)Math.Round(a / (Math.PI / 4)) % 8;
  }

  static void Centrado(IntPtr dc, int cx, int cy, string s) {
    GetTextExtentPoint32(dc, s, s.Length, out var sz);
    TextOut(dc, cx - sz.cx / 2, cy - sz.cy / 2, s, s.Length);
  }

  // Uma roda: as 8 casas em volta, e a casa viva em destaque. É o mesmo desenho
  // do protótipo web reduzido ao que cabe numa overlay — a roda existe pra você
  // não precisar decorar onde mora cada som, então ela é conteúdo, não enfeite.
  static void Roda(IntPtr dc, int cx, int cy, string[] letras, int vivo, uint cor, IntPtr fn, IntPtr fnG) {
    var caneta = CreatePen(0, 1, RGB(0x2C, 0x33, 0x3E));
    var velhaC = SelectObject(dc, caneta);
    var vazio = GetStockObject(5);                     // NULL_BRUSH
    var velhoB = SelectObject(dc, vazio);
    Ellipse(dc, cx - RAIO - 13, cy - RAIO - 13, cx + RAIO + 13, cy + RAIO + 13);
    SelectObject(dc, velhaC); SelectObject(dc, velhoB); DeleteObject(caneta);

    for (int g = 0; g < 8; g++) {
      double a = g * Math.PI / 4;
      int x = cx + (int)(Math.Sin(a) * RAIO);
      int y = cy - (int)(Math.Cos(a) * RAIO);
      bool on = g == vivo;
      SelectObject(dc, on ? fnG : fn);
      SetTextColor(dc, on ? cor : RGB(0x87, 0x91, 0x9F));
      Centrado(dc, x, y, letras[g]);
    }
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

        var fn  = CreateFont(17, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var fnG = CreateFont(22, 0, 0, 0, 600, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var fnP = CreateFont(40, 0, 0, 0, 500, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var fnS = CreateFont(14, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var velha = SelectObject(hdc, fn);

        Roda(hdc, LX, WY, _vozeado ? VOZEADO : ATAQUE, _gL, RGB(0x3D, 0xA3, 0xE8), fn, fnG);
        Roda(hdc, RX, WY, NUCLEO, _gR, RGB(0xBD, 0xC6, 0x57), fn, fnG);

        SelectObject(hdc, fnP);
        SetTextColor(hdc, _ativo ? RGB(0x3C, 0x60, 0xF2) : RGB(0x50, 0x58, 0x64));
        Centrado(hdc, 390, WY, _preview.Length > 0 ? _preview : "—");

        SelectObject(hdc, fnS);
        SetTextColor(hdc, UltimoErro != null ? RGB(0x3C, 0x60, 0xF2) : RGB(0x87, 0x91, 0x9F));
        var st = UltimoErro ?? _estado;
        TextOut(hdc, 16, ALT - 30, st, st.Length);

        SelectObject(hdc, velha);
        DeleteObject(fn); DeleteObject(fnG); DeleteObject(fnP); DeleteObject(fnS);
        EndPaint(h, ref ps);
        return IntPtr.Zero;
      }
    }
    return DefWindowProc(h, msg, w, l);
  }

  static string _assinatura = "";
  static void Redesenha() {
    var a = $"{_preview}|{_estado}|{_gL}|{_gR}|{_vozeado}|{_ativo}|{UltimoErro}";
    if (a == _assinatura) return;
    _assinatura = a;
    InvalidateRect(_hwnd, IntPtr.Zero, true);
  }

  static void Main(string[] args) {
    // O acorde de ativação. O pedido era LT+RT+3×Xbox, e o Guide não existe na
    // API documentada — então default no Back e --guide pra quem quiser.
    //
    // E LT+RT NÃO pode ser o prefixo: LT é vozeamento e RT é commit, então os
    // dois juntos acontecem em toda sílaba vozeada (25% delas, medido no log de
    // 09/09). Quem discrimina o acorde é o BOTÃO; enquanto ele está segurado o
    // commit fica suspenso, e fora disso a digitação é normal.
    bool usaGuide = args.Contains("--guide");
    ushort BOTAO = usaGuide ? GAMEPAD_GUIDE : GAMEPAD_BACK;
    string nome = usaGuide ? "Xbox" : "Back";

    _proc = Proc;
    var cls = new WNDCLASSEX {
      cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(), lpfnWndProc = _proc,
      lpszClassName = "SilaboOverlay", hbrBackground = IntPtr.Zero,
    };
    RegisterClassEx(ref cls);

    int x = GetSystemMetrics(SM_CXSCREEN) - LARG - 40;
    int y = GetSystemMetrics(SM_CYSCREEN) - ALT - 80;
    _hwnd = CreateWindowEx(
      WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE,
      "SilaboOverlay", null, WS_POPUP, x, y, LARG, ALT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    SetLayeredWindowAttributes(_hwnd, 0, 232, LWA_ALPHA);
    ShowWindow(_hwnd, SW_SHOWNOACTIVATE);

    // --teste: prova o caminho de injeção SEM precisar do controle. Chama o
    // SendInput de verdade e confere quantos eventos o Windows aceitou — que é
    // exatamente o que falhava em silêncio quando o cbSize estava errado.
    if (args.Contains("--teste")) {
      Thread.Sleep(1500);
      const string alvo = "silabo ok ";
      Digita(alvo);
      var r = UltimoErro ?? $"OK — {alvo.Length * 2} eventos aceitos pelo SendInput";
      File.WriteAllText(Path.Combine(Path.GetTempPath(), "silabo-teste.txt"), r);
      _estado = r; _preview = "teste"; Redesenha();
      Thread.Sleep(6000);
      return;
    }

    bool rtAntes = false, botaoAntes = false;
    int cliques = 0; var ultimo = DateTime.MinValue;

    while (true) {
      while (PeekMessage(out var m, IntPtr.Zero, 0, 0, 1)) { TranslateMessage(ref m); DispatchMessage(ref m); }

      if (!LePad(0, out var g)) {
        _preview = ""; _gL = _gR = -1; _estado = "sem controle";
        Redesenha(); Thread.Sleep(60); continue;
      }

      bool botao = (g.wButtons & BOTAO) != 0;
      bool gatilhos = g.bLeftTrigger > GATILHO && g.bRightTrigger > GATILHO;
      if (botao && !botaoAntes && gatilhos) {
        var agora = DateTime.UtcNow;
        cliques = (agora - ultimo).TotalMilliseconds < 1200 ? cliques + 1 : 1;
        ultimo = agora;
        if (cliques >= 3) { _ativo = !_ativo; cliques = 0; UltimoErro = null; }
      }
      botaoAntes = botao;

      _gL = Gate(g.sThumbLX, g.sThumbLY, _gL);
      _gR = Gate(g.sThumbRX, g.sThumbRY, _gR);
      _vozeado = g.bLeftTrigger > GATILHO;

      var nucleo = _gR < 0 ? "" : (NUCLEO[_gR] == "—" ? "" : NUCLEO[_gR]);
      _preview = (_gL < 0 ? "" : (_vozeado ? VOZEADO : ATAQUE)[_gL]) + nucleo;

      // RT confirma. Só o botão do acorde suspende — os gatilhos não, senão
      // nenhuma sílaba vozeada poderia ser confirmada.
      bool rt = g.bRightTrigger > GATILHO && !botao;
      if (rt && !rtAntes && _ativo && _preview.Length > 0) Digita(_preview);
      rtAntes = rt;

      _estado = _ativo ? $"ATIVO · LT+RT+3x{nome} desliga"
                       : $"parado · LT+RT+3x{nome} liga"
                         + (usaGuide && !GuideDisponivel ? "  (Guide indisponível!)" : "");
      Redesenha();
      Thread.Sleep(4);
    }
  }
}
