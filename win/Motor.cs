// O motor silábico, portado do index.html.
//
// Isto é uma SEGUNDA IMPLEMENTAÇÃO, e duas implementações do mesmo motor
// divergem em silêncio — foi por isso que o port veio junto com um
// arquivo-gabarito gerado pelo JS (tools/gabarito.mjs) e um teste que afirma
// contra ele linha a linha. Regra: nada aqui muda sem a linha correspondente
// mudar lá, e quem manda é o JS.
//
// Por isso o código é uma tradução LITERAL, não uma reescrita idiomática: a
// ordem das regras do ortografador, a ordem das substituições do finish() e até
// o que cada função devolve quando não faz nada são o comportamento. Onde o C#
// e o JS discordam por baixo, está comentado — Math.Round e o `$` do regex são
// os dois lugares onde a tradução ingênua estaria errada.
//
// O RACIOCÍNIO por trás de cada regra mora no index.html e em docs/ACHADOS.md.
// Aqui ficam só as notas de PORTE.
using System.Text.RegularExpressions;

namespace Silabo;

public static class Motor {
  // ------------------------------------------------------------- tabelas
  public static readonly Dictionary<int,string> ONSET_BY_GATE =
    new() {{0,"t"},{2,"s"},{4,"m"},{6,"l"},{1,"c"},{3,"p"},{5,"f"},{7,"x"}};
  public static readonly Dictionary<string,string> VOICED =
    new() {{"t","d"},{"s","z"},{"m","n"},{"l","r"},{"c","g"},{"p","b"},{"f","v"},{"x","j"}};
  public static readonly Dictionary<int,string> NUCLEUS_BY_GATE =
    new() {{7,"i"},{1,"u"},{6,"e"},{2,"o"},{5,"é"},{3,"ó"},{4,"a"},{0,"—"}};
  public static readonly Dictionary<string,string> GLIDE =
    new() {{"i","i"},{"u","u"},{"e","i"},{"o","u"},{"é","i"},{"ó","u"},{"a","a"}};

  static readonly Dictionary<string,string> DIGRAPH =
    new() {{"nr","nh"},{"lr","lh"},{"rr","rr"},{"mr","nh"}};
  static readonly HashSet<string> SIBILANT = new() {"s","z","x","j"};
  static readonly HashSet<string> SONORANT = new() {"m","n","l","r"};
  static readonly HashSet<string> LIQUIDS  = new() {"r","l"};
  static bool ClusterOk(string c1, string c2) =>
    LIQUIDS.Contains(c2) && !SIBILANT.Contains(c1) && !SONORANT.Contains(c1);

  public const int RESPELL_GATE = 1;                 // ↗ — onde moram c/g
  static readonly Dictionary<string,Func<bool,string>> RESPELL = new() {
    {"s", front => front ? "c" : "ç"},               // cebola, cidade · ação, moço
    {"x", _ => "ch"},
    {"j", _ => "g"},
  };

  // ------------------------------------------------------------- estado
  public sealed class Stick { public int? Live; public readonly List<int> Gates = new(); }
  public static readonly Stick L = new(), R = new();
  public struct Botoes { public bool LB, LT, RB, RT, L3, R3; }
  public static Botoes Btn;

  public static string Word = "";     // palavra em curso, já ortografada
  public static string Text = "";     // texto fechado, até o CURSOR
  public static string Depois = "";   // texto depois do cursor — gap buffer

  public static void Limpa(){ Word = Text = Depois = ""; }

  public record struct Silaba(string Onset, string Vowel, bool Nasal, string Coda, bool Respell);
  public record struct Ataque(string C, bool Respell, string? Suprimido);

  // ----------------------------------------------------------- geometria
  public static int? GateOf(double x, double y, int? atual) {
    double r = Math.Sqrt(x*x + y*y);
    const double entra = 0.55, sai = 0.38;
    if (r < (atual == null ? entra : sai)) return null;
    double a = Math.Atan2(x, -y);                    // 0 = cima, horário
    if (a < 0) a += Math.PI * 2;
    // Math.Round do .NET arredonda para o PAR (banker's rounding): 2.5 vira 2,
    // e o JS vira 3. Numa fronteira de gate isso é o dedo cair na casa errada,
    // então o arredondamento é escrito à mão como o do JS.
    return (int)Math.Floor(a / (Math.PI/4) + 0.5) % 8;
  }

  public static void Track(Stick s, double x, double y) {
    var g = GateOf(x, y, s.Live);
    if (g == s.Live) return;
    if (g == null) { s.Live = null; s.Gates.Clear(); return; }   // centro reseta
    s.Live = g;
    if (s.Gates.Count == 0 || s.Gates[^1] != g.Value) s.Gates.Add(g.Value);
  }

  // Um roll pela borda cruza todos os gates do caminho, e esses são trânsito,
  // não intenção: ficam o primeiro, o último, e onde o sentido da rotação
  // INVERTE.
  public static List<int> Landmarks(IReadOnlyList<int> gs) {
    if (gs.Count < 2) return new List<int>(gs);
    int Passo(int i) { int d = gs[i+1] - gs[i]; if (d > 4) d -= 8; if (d < -4) d += 8; return Math.Sign(d); }
    var outp = new List<int> { gs[0] };
    for (int i = 1; i < gs.Count - 1; i++) if (Passo(i) != Passo(i-1)) outp.Add(gs[i]);
    outp.Add(gs[^1]);
    return outp;
  }

  // ------------------------------------------------------------ montagem
  static string RollLiquid() {
    var mk = Landmarks(L.Gates);
    if (mk.Count < 2) return "";
    var t = mk[^1];
    return t == 6 ? "l" : t == 2 ? "r" : "";
  }

  public static Ataque BuildOnset() {
    var gs = L.Gates;
    // ⟨h⟩ não soletra fonema nenhum: mora na única combinação que o desenho
    // jogava fora, o L3 com o analógico PARADO.
    if (gs.Count == 0) return new Ataque(Btn.L3 ? "h" : "", false, null);
    var mk = Landmarks(gs);
    var b = ONSET_BY_GATE[mk[0]];
    if (Btn.LT) b = VOICED[b];

    bool respell = mk.Count > 1 && mk[^1] == RESPELL_GATE
                   && mk[0] != RESPELL_GATE && RESPELL.ContainsKey(b);

    string liq = "";
    if (!respell) liq = RollLiquid();
    if (liq == "") return new Ataque(b, respell, null);
    var par = b + liq;
    if (DIGRAPH.TryGetValue(par, out var dg)) return new Ataque(dg, false, null);
    if (ClusterOk(b, liq)) return new Ataque(par, false, null);
    // cluster ilegal: a líquida não se aplica
    return new Ataque(b, respell, "cluster-inexistente:" + par);
  }

  public static (string V, bool Nasal) BuildNucleus() {
    var gs = Landmarks(R.Gates);
    if (gs.Count == 0) return ("", false);
    var v = NUCLEUS_BY_GATE[gs[0]];
    if (v == "—") return ("", false);
    for (int i = 1; i < gs.Count; i++) {
      var n = NUCLEUS_BY_GATE[gs[i]];
      if (n != "" && n != "—") v += GLIDE[n];
    }
    return (v, Btn.R3);
  }

  // As três codas orais (/S R l/) são exatamente dois bits, então ganham dois
  // bits — LB e RB, ordenados por frequência.
  public static string BuildCoda() {
    if (Btn.LB && Btn.RB) return "l";
    if (Btn.LB) return "r";
    if (Btn.RB) return "s";
    return "";
  }

  public static (Silaba Syl, string? Suprimido) Current() {
    var o = BuildOnset();
    var (v, nasal) = BuildNucleus();
    return (new Silaba(o.C, v, nasal, BuildCoda(), o.Respell), o.Suprimido);
  }

  // ---------------------------------------------------------- ortografia
  const string VOGAIS = "aeiouáéíóúâêôãõà";
  static readonly RegexOptions GI = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
  static readonly Regex ReVogal = new("[" + VOGAIS + "]", GI);
  static readonly Regex ReKw    = new("^u[aáoó]");            // sem IgnoreCase, como no JS

  // Age sobre a palavra INTEIRA, não sobre a sílaba — é isso que deixa a
  // segmentação livre (ca.mpo e cam.po têm que dar "campo").
  public static string Orthograph(string prevWord, Silaba syl, string contexto = "") {
    var (onset, vowel, nasal, coda, respell) = syl;
    if (onset == "" && vowel == "" && coda == "") return prevWord;

    var firstV = vowel.Length > 0 ? vowel[0].ToString() : "";
    bool front = firstV == "e" || firstV == "é" || firstV == "i";
    var fonte = prevWord != "" ? prevWord : contexto;
    var lastCh = fonte.Length > 0 ? fonte[^1].ToString() : "";
    bool afterVowel = ReVogal.IsMatch(lastCh);

    if (respell && RESPELL.ContainsKey(onset)) onset = RESPELL[onset](front);
    else {
      if (onset == "c" && front) onset = "qu";
      if (onset == "g" && front) onset = "gu";
      // o u vem do NÚCLEO, então o ataque cai pra um q pelado
      if (onset == "c" && ReKw.IsMatch(vowel)) onset = "q";
      if (onset == "s" && afterVowel && vowel != "") onset = "ss";
      if (onset == "z" && afterVowel && vowel != "") onset = "s";
    }

    // A nasalidade fica como arquifonema até o finish() saber que letra vem
    // depois. O sentinela é "~" e não uma letra porque o buffer tem maiúsculas.
    return prevWord + onset + vowel + (nasal ? "~" : "") + coda;
  }

  // Cada linha corresponde a uma linha do finish() no JS, na MESMA ORDEM — as
  // substituições consomem umas o resultado das outras.
  // `$` do .NET casa também antes de um \n final; o do JS (sem flag m) só no fim
  // absoluto. Por isso todo fim-de-cadeia aqui é \z.
  static readonly Regex R_au   = new("au~", GI),        R_ai = new("[aá]i~", GI);
  static readonly Regex R_oi   = new("[oó]i~", GI),     R_til = new("([ãõ])~", GI);
  static readonly Regex R_pb   = new("~(?=[pb])", GI),  R_as = new("a~(?=s\\z)", GI);
  static readonly Regex R_letra= new("~(?=[a-zà-ÿA-ZÀ-Ý])");
  static readonly Regex R_fim  = new("~\\z");

  public static string Finish(string w) {
    w = R_au.Replace(w, "ão");
    w = R_ai.Replace(w, "ãe");
    w = R_oi.Replace(w, "õe");
    w = R_til.Replace(w, "$1");
    w = R_pb.Replace(w, "m");
    w = R_as.Replace(w, "ã");
    w = R_letra.Replace(w, "n");
    w = R_fim.Replace(w, "m");
    return w.Replace("~", "n");
  }

  // Maiúscula de início de frase é REGRA, não endereço. Escrita DENTRO do
  // buffer, nunca só na tela, pra o backspace e o d-pad editarem a mesma
  // string que o usuário lê.
  static readonly Regex ReInicio = new("(^|[.!?][\"')”]?)\\s*\\z");
  public static bool InicioDeFrase() => ReInicio.IsMatch(Text);

  static readonly Regex ReMinusculaIni = new("^([a-zà-ÿ])");
  public static string Maiuscula(string w) =>
    ReMinusculaIni.Replace(w, m => m.Value.ToUpperInvariant(), 1);

  public static int PoeSilaba(Silaba syl) {
    var antes = Word;
    Word = Orthograph(Word, syl, Text);
    if (antes == "" && InicioDeFrase()) Word = Maiuscula(Word);
    return Word.Length - antes.Length;
  }

  // ------------------------------------------------------- fechar palavra
  static readonly Regex ReEspacosFim = new(" +\\z");
  static readonly Regex RePontuacao  = new("^[.,!?)\"”]");

  public static void EndWord(string suffix) {
    // Pontuação depois de uma palavra já fechada não pode herdar o espaço dela:
    // "palavra , " em vez de "palavra, ".
    if (Word == "" && RePontuacao.IsMatch(suffix)) Text = ReEspacosFim.Replace(Text, "");
    Text += Finish(Word) + suffix;
    Word = "";
  }

  // Um botão serve o par inteiro: qual dos dois é a vez é derivável do próprio
  // texto — parênteses pelo saldo, aspas pela paridade —, então não há modo pra
  // lembrar nem estado pra dessincronizar.
  public static string Delimita(char ab, char fe) {
    var t = Text + Finish(Word);
    int Conta(char c) { int n = 0; foreach (var x in t) if (x == c) n++; return n; }
    bool abrindo = ab == fe ? Conta(ab) % 2 == 0 : Conta(ab) <= Conta(fe);
    var ch = abrindo ? ab : fe;
    Text += Finish(Word); Word = "";
    Text = ReEspacosFim.Replace(Text, "");
    Text += abrindo ? (Text != "" ? " " : "") + ch : ch + " ";
    return ch.ToString();
  }

  // ------------------------------------------------------ pós-correções
  static readonly Dictionary<char,char[]> CICLO = new() {
    {'a', new[]{'a','á','â','ã','à'}}, {'e', new[]{'e','é','ê'}}, {'i', new[]{'i','í'}},
    {'o', new[]{'o','ó','ô','õ'}},     {'u', new[]{'u','ú'}},
  };
  static readonly Dictionary<char,char> BASE_DE = new();
  static Motor() { foreach (var (b, arr) in CICLO) foreach (var ch in arr) BASE_DE[ch] = b; }

  // Os ciclos escrevem de tabelas minúsculas; sem isto "ES" virava "Ez" e "Ex"
  // virava "êx" — a correção comia a maiúscula. Foi bug medido em 09/09.
  static readonly Regex ReMaiuscula = new("[A-ZÀ-Ý]");
  static string ComoEstava(string novo, string velho) =>
    ReMaiuscula.IsMatch(velho[0].ToString())
      ? novo[0].ToString().ToUpperInvariant() + novo.Substring(1) : novo;

  // Age no `word` quando há um; no `text` quando não há.
  static void EditBuffer(Func<string,string?> fn) {
    bool emWord = Word.Length > 0;
    var w = fn(emWord ? Word : Text);
    if (w == null) return;
    if (emWord) Word = w; else Text = w;
  }

  // dir = +1 próximo acento, -1 anterior. Age na última vogal do buffer. É um
  // CICLO e não três botões porque um ciclo é reversível por construção.
  public static string? CycleAccent(int dir) {
    string? virou = null;
    EditBuffer(w => {
      for (int i = w.Length - 1; i >= 0; i--) {
        var min = char.ToLowerInvariant(w[i]);
        if (!BASE_DE.TryGetValue(min, out var b)) continue;
        var arr = CICLO[b];
        int j = Array.IndexOf(arr, min);
        int k = (j + dir + arr.Length) % arr.Length;
        virou = ComoEstava(arr[k].ToString(), w[i].ToString());
        return w.Substring(0, i) + virou + w.Substring(i + 1);
      }
      return null;
    });
    return virou;
  }

  // Cicla a grafia da última sibilante. Entre vogais são três (ss=/s/, s=/z/,
  // z=/z/) mais o ⟨x⟩ alógrafo; em fim de palavra só duas, porque "ss" não
  // ocorre lá; antes de consoante é a casa do "explicar" e do "exceto".
  static readonly Regex ReSibilante =
    new("[" + VOGAIS + "](ss|s|z|x)(?=[" + VOGAIS + "~]|\\z|[^" + VOGAIS + "])", GI);
  static readonly Regex ReVogalIni = new("^[" + VOGAIS + "]", GI);

  public static string? ToggleSibilant() {
    string? virou = null;
    EditBuffer(w => {
      var hits = ReSibilante.Matches(w);
      if (hits.Count == 0) return null;
      var h = hits[^1];
      int i = h.Index + 1;
      var atual = h.Groups[1].Value.ToLowerInvariant();
      var depois = w.Substring(i + atual.Length);
      var ciclo = depois == ""                ? new[]{"s","z","x"}
                : ReVogalIni.IsMatch(depois)  ? new[]{"ss","s","z","x"}
                :                               new[]{"s","x"};
      int j = Array.IndexOf(ciclo, atual);
      virou = ComoEstava(j < 0 ? ciclo[0] : ciclo[(j + 1) % ciclo.Length],
                         w.Substring(i, atual.Length));
      return w.Substring(0, i) + virou + w.Substring(i + atual.Length);
    });
    return virou;
  }

  // minúscula → Inicial → TUDO. Resolve o buffer ANTES de reescrever, como o
  // backspace: "questau~" em caixa alta viraria "QUESTAU~" e as regras do
  // ditongo não reconheceriam mais.
  static readonly Regex ReUltimaPalavra = new("(^|\\s)(\\S+)\\s*\\z");
  static readonly Regex ReTemLetra = new("[a-zà-ÿ]", GI);
  static readonly Regex ReComecaMaiusculo = new("^[A-ZÀ-Ý]");

  public static string? CycleCaps() {
    string? virou = null;
    EditBuffer(w => {
      var t = Finish(w);
      var alvo = ReUltimaPalavra.Match(t);
      if (!alvo.Success || !ReTemLetra.IsMatch(alvo.Groups[2].Value)) return null;
      var p = alvo.Groups[2].Value;
      int i = t.LastIndexOf(p, StringComparison.Ordinal);
      bool tudo = p == p.ToUpperInvariant() && ReMaiuscula.IsMatch(p);
      bool inicial = !tudo && ReComecaMaiusculo.IsMatch(p);
      var novo = inicial ? p.ToUpperInvariant() : tudo ? p.ToLowerInvariant() : Maiuscula(p);
      virou = novo;
      return t.Substring(0, i) + novo + t.Substring(i + p.Length);
    });
    return virou;
  }

  // ------------------------------------------------------------- cursor
  // Apaga o que se VÊ, não o buffer cru: sem isto "questão" virava "questau",
  // porque o /N/ pendente é invisível e o backspace comia ele primeiro.
  // Devolve o aviso quando não há o que apagar — falha silenciosa em método de
  // entrada vira tentativa repetida.
  public static string? ApagaUm() {
    if (Word != "") { var f = Finish(Word); Word = f.Length > 0 ? f[..^1] : ""; return null; }
    if (Text != "") { Text = Text[..^1]; return null; }
    return "o cursor está no começo do texto";
  }

  static readonly Regex ReFimPalavra   = new("\\S*\\s*\\z");
  static readonly Regex ReIniPalavra   = new("^\\s*\\S*");

  // O cursor anda pelo texto VISÍVEL, então o buffer é resolvido antes de andar.
  public static string? MoveCursor(int dir, bool porPalavra) {
    if (Word != "") { Text += Finish(Word); Word = ""; }
    if (dir < 0) {
      if (Text == "") return null;
      int n = porPalavra ? Math.Max(1, ReFimPalavra.Match(Text).Value.Length) : 1;
      Depois = Text.Substring(Text.Length - n) + Depois;
      Text = Text.Substring(0, Text.Length - n);
    } else {
      if (Depois == "") return null;
      int n = porPalavra ? Math.Max(1, ReIniPalavra.Match(Depois).Value.Length) : 1;
      Text += Depois.Substring(0, n);
      Depois = Depois.Substring(n);
    }
    return dir < 0 ? "esq" : "dir";
  }
}
