// A overlay: o método sílabo funcionando por cima de qualquer aplicativo do
// Windows. O motor é o Motor.cs — o mesmo do protótipo web, portado e provado
// contra um gabarito gerado pelo JS (win/teste). Aqui mora só o que é do
// Windows: ler o controle sem foco, desenhar por cima sem roubar o foco, e
// INJETAR o texto no aplicativo de baixo.
//
// A parte que não existe na web e é o coração daqui: sem um IME de verdade, o
// que já foi digitado está no aplicativo, não num campo nosso. Então a overlay
// guarda o que ACHA que mandou (`_emitido`), compara com o que o motor diz que
// deveria estar lá, e reconcilia com backspaces + redigitação. É isso que faz
// acento, caixa e sibilante funcionarem: "vose" vira "você" apagando dois e
// redigitando dois, no Bloco de Notas, no navegador, onde for.
using System.Runtime.InteropServices;
using static Silabo.Native;

namespace Silabo;

static class Program {
  const byte  GATILHO = 128;                 // 0,5 de 255, como o >0.5 da web
  const short EIXO = 32767;

  // Repetição do backspace: os mesmos números da web, pra a mão não reaprender.
  const double B_ESPERA = 400, B_LETRA = 90, B_PALAVRA = 220, B_VIRA_PALAVRA = 1200;

  const int LARG = 470, ALT = 214, RAIO = 44;
  const int LX = 96, RX = 236, WY = 88;      // centros das duas rodas

  static IntPtr _hwnd;
  static string _preview = "", _estado = "sem controle", _aviso = "";
  static double _avisoAte;
  static int _gL = -1, _gR = -1;
  static bool _vozeado, _ativo;
  static WndProc? _proc;                     // referência viva: o GC come o delegate

  static readonly Reconciliador _rec = new();
  static readonly System.Diagnostics.Stopwatch _rel = System.Diagnostics.Stopwatch.StartNew();
  static double Agora => _rel.Elapsed.TotalMilliseconds;
  static void Avisa(string t){ _aviso = t; _avisoAte = Agora + 1600; }

  // ------------------------------------------------------------ injeção
  // O texto visível até o cursor. `Depois` fica de fora de propósito: mover o
  // cursor do aplicativo de baixo é outro problema (ver win/README.md), então a
  // página 2 do d-pad não está ligada aqui — e não estar ligada é melhor do que
  // estar ligada errado.
  static string Alvo() => Motor.Text + Motor.Finish(Motor.Word);

  static void Sincroniza() {
    var c = _rec.Passo(Alvo());
    if (c.Recusa != null) { Avisa(c.Recusa); return; }
    if (c.Apagar > 0) Native.Apaga(c.Apagar);
    if (c.Digitar.Length > 0) Digita(c.Digitar);
  }

  // Ligar o método é sempre um recomeço: o buffer do motor e o `_emitido` zeram
  // juntos. Se não zerassem, a primeira correção depois de religar apagaria
  // texto escrito por outra pessoa — pelo teclado, por outro aplicativo.
  static void Alterna() {
    _ativo = !_ativo;
    Motor.Limpa(); _rec.Reancora();
    Motor.L.Gates.Clear(); Motor.L.Live = null;
    Motor.R.Gates.Clear(); Motor.R.Live = null;
    UltimoErro = null;
  }

  // ------------------------------------------------------------- sílaba
  static void Confirma() {
    var (syl, suprimido) = Motor.Current();
    if (syl.Onset == "" && syl.Vowel == "" && syl.Coda == "") { Avisa("gesto vazio"); return; }
    Motor.PoeSilaba(syl);
    if (suprimido != null) Avisa(suprimido);
    Sincroniza();
  }

  // ------------------------------------------------------------ desenho
  static void Centrado(IntPtr dc, int cx, int cy, string s) {
    GetTextExtentPoint32(dc, s, s.Length, out var sz);
    TextOut(dc, cx - sz.cx / 2, cy - sz.cy / 2, s, s.Length);
  }

  // As oito casas em volta e a casa viva em destaque. A roda existe pra você não
  // precisar decorar onde mora cada som, então é conteúdo, não enfeite — e as
  // letras vêm das tabelas do Motor, não de uma cópia que possa envelhecer.
  static void Roda(IntPtr dc, int cx, int cy, Func<int,string> letra, int vivo, uint cor, IntPtr fn, IntPtr fnG) {
    var caneta = CreatePen(0, 1, RGB(0x2C, 0x33, 0x3E));
    var velhaC = SelectObject(dc, caneta);
    var velhoB = SelectObject(dc, GetStockObject(5));   // NULL_BRUSH
    Ellipse(dc, cx - RAIO - 13, cy - RAIO - 13, cx + RAIO + 13, cy + RAIO + 13);
    SelectObject(dc, velhaC); SelectObject(dc, velhoB); DeleteObject(caneta);

    for (int g = 0; g < 8; g++) {
      double a = g * Math.PI / 4;
      int x = cx + (int)(Math.Sin(a) * RAIO);
      int y = cy - (int)(Math.Cos(a) * RAIO);
      bool on = g == vivo;
      SelectObject(dc, on ? fnG : fn);
      SetTextColor(dc, on ? cor : RGB(0x87, 0x91, 0x9F));
      Centrado(dc, x, y, letra(g));
    }
  }

  static string Ataque(int g) {
    var b = Motor.ONSET_BY_GATE[g];
    return _vozeado ? Motor.VOICED[b] : b;
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
        var fnP = CreateFont(38, 0, 0, 0, 500, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var fnS = CreateFont(14, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 5, 0, "Consolas");
        var velha = SelectObject(hdc, fn);

        Roda(hdc, LX, WY, Ataque, _gL, RGB(0x3D, 0xA3, 0xE8), fn, fnG);
        Roda(hdc, RX, WY, g => Motor.NUCLEUS_BY_GATE[g], _gR, RGB(0xBD, 0xC6, 0x57), fn, fnG);

        SelectObject(hdc, fnP);
        SetTextColor(hdc, _ativo ? RGB(0x3C, 0x60, 0xF2) : RGB(0x50, 0x58, 0x64));
        Centrado(hdc, 390, WY - 12, _preview.Length > 0 ? _preview : "—");

        // O rabo da palavra em curso: sem isto a overlay mostra a sílaba mas não
        // o que ela virou depois do ortografador (o "c" que virou "qu").
        SelectObject(hdc, fnS);
        SetTextColor(hdc, RGB(0x87, 0x91, 0x9F));
        var cauda = Motor.Finish(Motor.Word);
        if (cauda.Length > 14) cauda = "…" + cauda.Substring(cauda.Length - 14);
        Centrado(hdc, 390, WY + 26, cauda);

        var msg2 = UltimoErro ?? (Agora < _avisoAte ? _aviso : _estado);
        SetTextColor(hdc, UltimoErro != null || Agora < _avisoAte
                          ? RGB(0x3C, 0x60, 0xF2) : RGB(0x87, 0x91, 0x9F));
        TextOut(hdc, 16, ALT - 30, msg2, msg2.Length);

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
    var a = $"{_preview}|{_estado}|{_aviso}|{Agora < _avisoAte}|{_gL}|{_gR}|{_vozeado}|{_ativo}|{Motor.Word}|{UltimoErro}";
    if (a == _assinatura) return;
    _assinatura = a;
    InvalidateRect(_hwnd, IntPtr.Zero, true);
  }

  // ------------------------------------------------------------- entrada
  struct Borda { public bool A, B, X, Y, RT, Cima, Baixo, Esq, Dir, Acorde; }

  static void Main(string[] args) {
    // O acorde de ativação. O pedido era LT+RT+3×Xbox, e o Guide não existe na
    // API documentada — então default no Back e --guide pra quem quiser.
    //
    // E LT+RT NÃO pode ser o prefixo sozinho: LT é vozeamento e RT é commit,
    // então os dois juntos acontecem em toda sílaba vozeada (25% delas, medido
    // no log de 09/09). Quem discrimina o acorde é o BOTÃO; enquanto ele está
    // segurado o commit fica suspenso, e fora disso a digitação é normal.
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

    var ant = new Borda();
    int cliques = 0; double ultimo = -1e9;
    double seguraB = 0, proxB = 0;

    while (true) {
      while (PeekMessage(out var m, IntPtr.Zero, 0, 0, 1)) { TranslateMessage(ref m); DispatchMessage(ref m); }

      if (!LePad(0, out var g)) {
        _preview = ""; _gL = _gR = -1; _estado = "sem controle";
        Redesenha(); Thread.Sleep(60); continue;
      }

      bool botao = (g.wButtons & BOTAO) != 0;
      bool gatilhos = g.bLeftTrigger > GATILHO && g.bRightTrigger > GATILHO;
      bool acorde = botao && gatilhos;
      if (acorde && !ant.Acorde) {
        cliques = Agora - ultimo < 1200 ? cliques + 1 : 1;
        ultimo = Agora;
        if (cliques >= 3) { Alterna(); cliques = 0; }
      }
      ant.Acorde = acorde;

      // Os eixos entram na convenção da WEB (y positivo = pra baixo), porque é
      // a convenção do Motor. O XInput manda y positivo pra cima.
      Motor.Track(Motor.L,  g.sThumbLX / (double)EIXO, -g.sThumbLY / (double)EIXO);
      Motor.Track(Motor.R,  g.sThumbRX / (double)EIXO, -g.sThumbRY / (double)EIXO);
      _gL = Motor.L.Live ?? -1;
      _gR = Motor.R.Live ?? -1;

      Motor.Btn = new Motor.Botoes {
        LB = (g.wButtons & GAMEPAD_LB) != 0,      RB = (g.wButtons & GAMEPAD_RB) != 0,
        L3 = (g.wButtons & GAMEPAD_LTHUMB) != 0,  R3 = (g.wButtons & GAMEPAD_RTHUMB) != 0,
        LT = g.bLeftTrigger > GATILHO,            RT = g.bRightTrigger > GATILHO,
      };
      _vozeado = Motor.Btn.LT;

      // O que a sílaba seria se você confirmasse agora — passa pelo mesmo
      // ortografador, então "c"+"ua" já aparece como "qua".
      var (previa, _) = Motor.Current();
      _preview = Motor.Finish(Motor.Orthograph("", previa, Motor.Text + Motor.Word));

      bool rt = Motor.Btn.RT && !botao;         // o acorde suspende o commit
      if (rt && !ant.RT && _ativo) Confirma();
      ant.RT = rt;

      bool a  = (g.wButtons & GAMEPAD_A) != 0, b = (g.wButtons & GAMEPAD_B) != 0;
      bool xb = (g.wButtons & GAMEPAD_X) != 0, yb = (g.wButtons & GAMEPAD_Y) != 0;
      bool cima = (g.wButtons & GAMEPAD_UP) != 0,   baixo = (g.wButtons & GAMEPAD_DOWN) != 0;
      bool esq  = (g.wButtons & GAMEPAD_LEFT) != 0, dir  = (g.wButtons & GAMEPAD_RIGHT) != 0;
      bool parado = Motor.L.Live == null && Motor.R.Live == null;

      if (_ativo && parado && !botao) {
        // A = espaço · A+RB = quebra de linha
        if (a && !ant.A) { Motor.EndWord(Motor.Btn.RB ? "\n" : " "); Sincroniza(); }
        // X é a família da pontuação, e os qualificadores dizem qual.
        if (xb && !ant.X) {
          if (Motor.Btn.LB)      Motor.Delimita('(', ')');
          else if (Motor.Btn.LT) Motor.Delimita('"', '"');
          else                   Motor.EndWord(Motor.Btn.RB ? ", " : ". ");
          Sincroniza();
        }
        if (yb && !ant.Y) { Motor.EndWord(Motor.Btn.RB ? "! " : "? "); Sincroniza(); }

        // B = backspace, com repetição: letra a letra e, depois de 1,2 s,
        // palavra a palavra. LB+LT+B limpa tudo.
        if (b && !ant.B) {
          if (Motor.Btn.LB && Motor.Btn.LT) { Motor.Limpa(); seguraB = 0; }
          else { seguraB = Agora; proxB = seguraB + B_ESPERA; var w = Motor.ApagaUm(); if (w != null) Avisa(w); }
          Sincroniza();
        } else if (b && seguraB > 0 && Agora >= proxB) {
          bool porPalavra = Agora - seguraB > B_VIRA_PALAVRA;
          if (porPalavra) { if (Motor.Word != "") Motor.Word = ""; else Motor.Text = ApagaPalavra(Motor.Text); }
          else { var w = Motor.ApagaUm(); if (w != null) Avisa(w); }
          proxB = Agora + (porPalavra ? B_PALAVRA : B_LETRA);
          Sincroniza();
        }
        if (!b) seguraB = 0;

        // D-pad, página 1: as pós-correções. A página 2 (cursor) não está aqui
        // — ver o comentário em Alvo().
        if (cima  && !ant.Cima)  { if (Motor.CycleCaps() == null) Avisa("nenhuma palavra para trocar a caixa"); Sincroniza(); }
        if (dir   && !ant.Dir)   { if (Motor.CycleAccent(+1) == null) Avisa("nenhuma vogal para acentuar"); Sincroniza(); }
        if (esq   && !ant.Esq)   { if (Motor.CycleAccent(-1) == null) Avisa("nenhuma vogal para acentuar"); Sincroniza(); }
        if (baixo && !ant.Baixo) { if (Motor.ToggleSibilant() == null) Avisa("nenhuma sibilante para alternar"); Sincroniza(); }
      }
      ant.A = a; ant.B = b; ant.X = xb; ant.Y = yb;
      ant.Cima = cima; ant.Baixo = baixo; ant.Esq = esq; ant.Dir = dir;

      _estado = _ativo ? $"ATIVO · LT+RT+3x{nome} desliga"
                       : $"parado · LT+RT+3x{nome} liga"
                         + (usaGuide && !GuideDisponivel ? "  (Guide indisponível!)" : "");
      Redesenha();
      Thread.Sleep(4);
    }
  }

  // Equivalente ao text.replace(/\S*\s*$/,'') da web.
  static string ApagaPalavra(string t) {
    int i = t.Length;
    while (i > 0 && char.IsWhiteSpace(t[i-1])) i--;
    while (i > 0 && !char.IsWhiteSpace(t[i-1])) i--;
    return t.Substring(0, i);
  }
}
