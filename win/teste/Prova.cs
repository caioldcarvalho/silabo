// Confere o motor em C# contra o gabarito gerado pelo motor JS.
//
// Não há um único valor esperado escrito à mão neste arquivo. Isso é o ponto:
// um teste escrito à mão prova que o C# faz o que EU achei que o JS fazia, e o
// bug que o gabarito existe pra pegar é exatamente esse. Aqui só se lê o que o
// JS produziu e se compara.
//
// Onde o gabarito guarda só as saídas (a ortografia), a ORDEM DOS LAÇOS é o
// contrato — está documentada nos dois lados e a contagem confere se bateu.
//
//   dotnet.exe run --project win/teste
using System.Text;
using System.Text.Json;

namespace Silabo;

static class Prova {
  static int _ok, _mau;
  static readonly List<string> _falhas = new();

  static void T(string grupo, string nome, string? saiu, string? alvo) {
    if (saiu == alvo) { _ok++; return; }
    _mau++;
    if (_falhas.Count < 25) _falhas.Add($"  {grupo} · {nome}\n     saiu: {Mostra(saiu)}\n     alvo: {Mostra(alvo)}");
  }
  static string Mostra(string? s) => s == null ? "<null>" : "\"" + s + "\"";

  static int _antes;
  static void Abre() { _antes = _mau; }
  static void Fecha(string nome, int linhas) {
    var falhou = _mau - _antes;
    Console.WriteLine($"  {nome.PadRight(26)} {linhas,6} linhas  " + (falhou == 0 ? "ok" : $"X {falhou} falha(s)"));
  }

  static List<int> Gates(string s) { var l = new List<int>(); foreach (var c in s) l.Add(c - '0'); return l; }
  static void Estado(string gatesL, bool lt, bool l3, bool lb = false, bool rb = false, bool r3 = false) {
    Motor.L.Gates.Clear(); Motor.L.Gates.AddRange(Gates(gatesL));
    Motor.Btn = new Motor.Botoes { LT = lt, L3 = l3, LB = lb, RB = rb, R3 = r3 };
  }
  static string S(JsonElement e) => e.GetString()!;
  static int  I(JsonElement e) => e.GetInt32();
  static bool B(JsonElement e) => e.GetInt32() != 0;

  static string? Acha() {
    var d = new DirectoryInfo(AppContext.BaseDirectory);
    while (d != null) {
      var p = Path.Combine(d.FullName, "test", "gabarito.json");
      if (File.Exists(p)) return p;
      d = d.Parent;
    }
    return null;
  }

  static int Main(string[] args) {
    Console.OutputEncoding = Encoding.UTF8;
    var caminho = args.Length > 0 ? args[0] : Acha();
    if (caminho == null || !File.Exists(caminho)) {
      Console.WriteLine("gabarito não encontrado — rode `node tools/gabarito.mjs` primeiro");
      return 2;
    }
    using var doc = JsonDocument.Parse(File.ReadAllText(caminho));
    var g = doc.RootElement;
    Console.WriteLine($"gabarito: {caminho}\n");

    // ------------------------------------------------------------ tabelas
    Abre();
    var tab = g.GetProperty("tabelas");
    foreach (var p in tab.GetProperty("ONSET_BY_GATE").EnumerateObject())
      T("tabelas", "ONSET " + p.Name, Motor.ONSET_BY_GATE[int.Parse(p.Name)], S(p.Value));
    foreach (var p in tab.GetProperty("NUCLEUS_BY_GATE").EnumerateObject())
      T("tabelas", "NUCLEO " + p.Name, Motor.NUCLEUS_BY_GATE[int.Parse(p.Name)], S(p.Value));
    foreach (var p in tab.GetProperty("VOICED").EnumerateObject())
      T("tabelas", "VOZ " + p.Name, Motor.VOICED[p.Name], S(p.Value));
    foreach (var p in tab.GetProperty("GLIDE").EnumerateObject())
      T("tabelas", "GLIDE " + p.Name, Motor.GLIDE[p.Name], S(p.Value));
    T("tabelas", "RESPELL_GATE", Motor.RESPELL_GATE.ToString(), tab.GetProperty("RESPELL_GATE").GetInt32().ToString());
    Fecha("tabelas", 4);

    // --------------------------------------------------------------- gate
    // Onde o polegar está. Não produz letra nenhuma, então nenhuma varredura de
    // ortografia pega um erro aqui — e é a camada com a maior chance de porte
    // errado silencioso (arredondamento e histerese).
    Abre();
    var gate = g.GetProperty("gate");
    foreach (var r in gate.EnumerateArray()) {
      double x = r[0].GetDouble(), y = r[1].GetDouble();
      int atual = I(r[2]);
      var saiu = Motor.GateOf(x, y, atual < 0 ? null : atual);
      T("gate", $"({x:0.###},{y:0.###}) atual={atual}",
        (saiu ?? -1).ToString(), I(r[3]).ToString());
    }
    Fecha("gateOf", gate.GetArrayLength());

    Abre();
    var trilhos = g.GetProperty("trilhos");
    foreach (var r in trilhos.EnumerateArray()) {
      var s2 = new Motor.Stick();
      var passos = new StringBuilder();
      foreach (var p in r[0].EnumerateArray()) {
        double x = p[0].GetDouble(), y = p[1].GetDouble();
        Motor.Track(s2, x, y);
        passos.Append($"({x:0.##},{y:0.##})");
      }
      T("track", passos.ToString(),
        string.Concat(s2.Gates) + "|" + (s2.Live ?? -1),
        S(r[1]) + "|" + I(r[2]));
    }
    Fecha("track", trilhos.GetArrayLength());

    // ------------------------------------------------------------- marcos
    // O roll pela borda: primeiro gate, último, e as inversões de sentido.
    Abre();
    var marcos = g.GetProperty("marcos");
    foreach (var r in marcos.EnumerateArray()) {
      var e = S(r[0]);
      T("marcos", e.Length == 0 ? "·" : e, string.Concat(Motor.Landmarks(Gates(e))), S(r[1]));
    }
    Fecha("landmarks", marcos.GetArrayLength());

    // -------------------------------------------------------------- gesto
    Abre();
    var onset = g.GetProperty("onset");
    foreach (var r in onset.EnumerateArray()) {
      var gs = S(r[0]); bool lt = B(r[1]), l3 = B(r[2]);
      Estado(gs, lt, l3);
      var a = Motor.BuildOnset();
      var nome = $"{(gs.Length == 0 ? "·" : gs)} LT={(lt?1:0)} L3={(l3?1:0)}";
      T("ataque", nome, $"{a.C}|{(a.Respell?1:0)}|{a.Suprimido ?? ""}",
                        $"{S(r[3])}|{I(r[4])}|{S(r[5])}");
    }
    Fecha("buildOnset", onset.GetArrayLength());

    Abre();
    var nucleo = g.GetProperty("nucleo");
    foreach (var r in nucleo.EnumerateArray()) {
      var gs = S(r[0]);
      Motor.R.Gates.Clear(); Motor.R.Gates.AddRange(Gates(gs));
      Motor.Btn = new Motor.Botoes();
      T("núcleo", gs.Length == 0 ? "·" : gs, Motor.BuildNucleus().V, S(r[1]));
    }
    Fecha("buildNucleus", nucleo.GetArrayLength());

    Abre();
    var coda = g.GetProperty("coda");
    foreach (var r in coda.EnumerateArray()) {
      Motor.Btn = new Motor.Botoes { LB = B(r[0]), RB = B(r[1]) };
      T("coda", $"LB={I(r[0])} RB={I(r[1])}", Motor.BuildCoda(), S(r[2]));
    }
    Fecha("buildCoda", coda.GetArrayLength());

    // --------------------------------------------------------- ortografia
    // Cross-product determinístico: as listas vêm do gabarito, os laços têm que
    // estar na MESMA ordem em que o gerador os escreveu.
    Abre();
    var orto = g.GetProperty("orto");
    var ataques = orto.GetProperty("ataques").EnumerateArray()
                      .Select(a => (C: S(a[0]), R: B(a[1]))).ToList();
    var nucleos = orto.GetProperty("nucleos").EnumerateArray().Select(S).ToList();
    var codas   = orto.GetProperty("codas").EnumerateArray().Select(S).ToList();
    var ambs    = orto.GetProperty("ambientes").EnumerateArray()
                      .Select(a => (Prev: S(a[0]), Ctx: S(a[1]))).ToList();
    var saidas  = orto.GetProperty("saidas");
    int n = 0;
    foreach (var (c, respell) in ataques)
      foreach (var v in nucleos)
        foreach (var nasal in new[]{false, true})
          foreach (var cd in codas)
            foreach (var (prev, ctx) in ambs) {
              var esperado = S(saidas[n++]);
              var saiu = Motor.Orthograph(prev, new Motor.Silaba(c, v, nasal, cd, respell), ctx);
              var fim = Motor.Finish(saiu);
              T("ortografia", $"[{prev}|{ctx}] {c}{(respell?"↗":"")}+{v}{(nasal?"~":"")}+{cd}",
                saiu == fim ? saiu : saiu + "\t" + fim, esperado);
            }
    if (n != saidas.GetArrayLength()) {
      _mau++; _falhas.Add($"  ortografia · a ordem dos laços não bate: {n} contra {saidas.GetArrayLength()}");
    }
    Fecha("orthograph + finish", n);

    // ----------------------------------------------------------- palavras
    // O que uma sílaba sozinha não pega: a INTERAÇÃO entre elas.
    Abre();
    var palavras = g.GetProperty("palavras");
    foreach (var r in palavras.EnumerateArray()) {
      var w = "";
      var nome = new StringBuilder();
      foreach (var s in r[0].EnumerateArray()) {
        w = Motor.Orthograph(w, new Motor.Silaba(S(s[0]), S(s[2]), B(s[3]), S(s[4]), B(s[1])));
        nome.Append(S(s[0])).Append(S(s[2])).Append('·');
      }
      T("palavra", nome.ToString(), w + "\t" + Motor.Finish(w), S(r[1]) + "\t" + S(r[2]));
    }
    Fecha("palavras encadeadas", palavras.GetArrayLength());

    // ------------------------------------------------ maiúscula automática
    Abre();
    var inicio = g.GetProperty("inicio");
    foreach (var r in inicio.EnumerateArray()) {
      Motor.Limpa(); Motor.Text = S(r[0]); Motor.Word = S(r[1]);
      var ganho = Motor.PoeSilaba(new Motor.Silaba(S(r[2]), S(r[4]), false, "", B(r[3])));
      T("início", $"[{S(r[0])}]+[{S(r[1])}] {S(r[2])}{S(r[4])}",
        Motor.Word + "\t" + ganho, S(r[5]) + "\t" + I(r[6]));
    }
    Fecha("poeSilaba (maiúscula)", inicio.GetArrayLength());

    // ------------------------------------------------------ pós-correções
    // Três aplicações seguidas: o que define um ciclo é ele FECHAR.
    Abre();
    int Ciclo(string chave, Func<string?> passo, int col) {
      var linhas = g.GetProperty(chave);
      foreach (var r in linhas.EnumerateArray()) {
        var w = S(r[0]);
        Motor.Limpa(); Motor.Word = w;
        for (int i = 0; i < 3; i++) { passo(); T(chave, $"[{w}] {i+1}x", Motor.Word, S(r[col + i])); }
      }
      return linhas.GetArrayLength();
    }
    var na = Ciclo("acento", () => Motor.CycleAccent(+1), 2);
    var nb = Ciclo("acentoBack", () => Motor.CycleAccent(-1), 2);
    Fecha("cycleAccent ±1", na + nb);

    Abre(); var ns = Ciclo("sibilante", () => Motor.ToggleSibilant(), 1); Fecha("toggleSibilant", ns);
    Abre(); var nc = Ciclo("caixa", () => Motor.CycleCaps(), 1);         Fecha("cycleCaps", nc);

    // editBuffer age no `text` quando o `word` está vazio — a outra metade.
    Abre();
    var noTexto = g.GetProperty("noTexto");
    foreach (var r in noTexto.EnumerateArray()) {
      var w = S(r[0]);
      Motor.Limpa(); Motor.Text = w; Motor.CycleAccent(+1);
      T("noTexto", $"acento [{w}]", Motor.Text, S(r[1]));
      Motor.Limpa(); Motor.Text = w; Motor.ToggleSibilant();
      T("noTexto", $"sibilante [{w}]", Motor.Text, S(r[2]));
      Motor.Limpa(); Motor.Text = w; Motor.CycleCaps();
      T("noTexto", $"caixa [{w}]", Motor.Text, S(r[3]));
    }
    Fecha("ciclos no buffer text", noTexto.GetArrayLength());

    // ------------------------------------------------------ fechar palavra
    Abre();
    var fechar = g.GetProperty("fechar");
    foreach (var r in fechar.EnumerateArray()) {
      Motor.Limpa(); Motor.Text = S(r[0]); Motor.Word = S(r[1]);
      Motor.EndWord(S(r[2]));
      T("endWord", $"[{S(r[0])}]+[{S(r[1])}]+[{S(r[2])}]", Motor.Text, S(r[3]));
    }
    Fecha("endWord", fechar.GetArrayLength());

    Abre();
    var delim = g.GetProperty("delimitadores");
    foreach (var r in delim.EnumerateArray()) {
      Motor.Limpa(); Motor.Text = S(r[0]); Motor.Word = S(r[1]);
      var par = S(r[2]);
      var ch = Motor.Delimita(par[0], par[1]);
      T("delimita", $"[{S(r[0])}]+[{S(r[1])}] {par}", ch + "\t" + Motor.Text, S(r[3]) + "\t" + S(r[4]));
    }
    Fecha("delimita", delim.GetArrayLength());

    // ----------------------------------------------------------- as frases
    // A prova de ponta a ponta, com o roteiro que veio DENTRO do gabarito.
    var frases = g.GetProperty("frases");
    var roteiro = frases.GetProperty("roteiro");
    int maiorApaga = 0, totalApaga = 0, totalDigita = 0;

    int Frases(string chave) {
      Abre();
      var fases = roteiro.GetProperty(chave);
      var esperados = frases.GetProperty(chave);
      for (int i = 0; i < fases.GetArrayLength(); i++) {
        Motor.Limpa();
        // O outro lado da injeção: um aplicativo de mentira que recebe as
        // teclas. Se ele terminar com a frase certa, o caminho nativo inteiro
        // está provado no nível do texto — motor, ortografador e reconciliação.
        var rec = new Reconciliador();
        var app = "";
        void Sync() {
          var c = rec.Passo(Motor.Text + Motor.Finish(Motor.Word));
          if (c.Recusa != null) { T("reconcilia", $"{chave} {i+1} recusou", c.Recusa, null); return; }
          maiorApaga = Math.Max(maiorApaga, c.Apagar);
          totalApaga += c.Apagar; totalDigita += c.Digitar.Length;
          app = Reconciliador.Aplica(app, c);
        }

        foreach (var op in fases[i].EnumerateArray()) {
          if (op.TryGetProperty("d", out var d)) { var p = S(d); Motor.Delimita(p[0], p[1]); Sync(); continue; }
          foreach (var s in op.GetProperty("s").EnumerateArray()) {
            string o = S(s[0]), v = S(s[1]), f = S(s[2]);
            Motor.PoeSilaba(new Motor.Silaba(o, v, f.Contains('n'),
              f.Contains('s') ? "s" : f.Contains('r') ? "r" : f.Contains('l') ? "l" : "",
              f.Contains('R')));
            Sync();
          }
          int Q(string k) => op.TryGetProperty(k, out var x) ? x.GetInt32() : 0;
          for (int k = 0; k < Q("acc");  k++) { Motor.CycleAccent(+1);  Sync(); }
          for (int k = 0; k < Q("sib");  k++) { Motor.ToggleSibilant(); Sync(); }
          for (int k = 0; k < Q("caps"); k++) { Motor.CycleCaps();      Sync(); }
          Motor.EndWord(S(op.GetProperty("fim")));
          Sync();
        }
        // esperados[i] = [o que o JS produziu, o que a UI mostra] — os dois têm
        // que bater, senão o gabarito estaria congelando um bug do JS.
        T("frase", $"{chave} {i+1}", Motor.Text.Trim(), S(esperados[i][0]));
        T("frase", $"{chave} {i+1} = UI", S(esperados[i][0]), S(esperados[i][1]));
        // e o aplicativo de baixo tem que ter recebido a MESMA coisa
        T("injeção", $"{chave} {i+1}", app.Trim(), S(esperados[i][0]));
        Console.WriteLine($"     {i+1}. {app.Trim()}");
      }
      Fecha($"frase {chave} (motor + injeção)", fases.GetArrayLength());
      return fases.GetArrayLength();
    }
    Console.WriteLine();
    Frases("longa");
    Frases("curta");

    // Quanto a reconciliação custou. Interessa o MAIOR apagamento: ele é quem
    // decide se o teto de segurança do Reconciliador está apertado demais e
    // recusaria uma correção legítima.
    Console.WriteLine($"\n  reconciliação: {totalDigita} caracteres digitados, "
                    + $"{totalApaga} apagados, maior conserto = {maiorApaga}");
    T("reconcilia", "maior conserto cabe no teto padrão",
      (maiorApaga <= new Reconciliador().Teto).ToString(), "True");

    // ------------------------------------------------------------- veredito
    Console.WriteLine();
    foreach (var f in _falhas) Console.WriteLine(f);
    if (_mau > 25) Console.WriteLine($"  … e mais {_mau - 25} falha(s)");
    Console.WriteLine($"\n{_ok} ok, {_mau} falha(s)");
    return _mau == 0 ? 0 : 1;
  }
}
