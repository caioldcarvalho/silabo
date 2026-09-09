// A ponte entre o motor e um aplicativo que não é nosso.
//
// Na web o texto mora num buffer que a página desenha, então uma pós-correção é
// só reescrever a string. Aqui o texto já está DENTRO do Bloco de Notas, do
// navegador, do que for — e a única coisa que a overlay pode fazer é mandar
// teclas. Então ela guarda o que acredita ter mandado e, a cada mudança,
// calcula o menor conserto: apagar o sufixo que divergiu e redigitar o resto.
//
// Isso é o que faz "vose" virar "você" com dois backspaces em vez de apagar a
// frase toda. E é a única peça do caminho nativo capaz de destruir texto que
// não é nosso, e é por isso que ela mora num arquivo puro, sem Win32, testado
// junto com o motor em vez de solta dentro do laço de eventos.
namespace Silabo;

public sealed class Reconciliador {
  // O que a overlay acredita que está no aplicativo de baixo.
  public string Emitido = "";

  // Teto de reescrita. Uma pós-correção mexe no fim da palavra; se o conserto
  // pede muito mais que isso, é porque o `Emitido` dessincronizou — o usuário
  // clicou noutro lugar, outra janela roubou o foco, alguém digitou pelo
  // teclado. Aí o certo é RECUSAR e avisar: apagar 300 caracteres que a overlay
  // não escreveu é pior do que qualquer correção que ela deixe de fazer.
  public int Teto = 40;

  public readonly record struct Conserto(int Apagar, string Digitar, string? Recusa);

  public Conserto Passo(string alvo) {
    if (alvo == Emitido) return new Conserto(0, "", null);
    int i = 0;
    while (i < alvo.Length && i < Emitido.Length && alvo[i] == Emitido[i]) i++;
    int apagar = Emitido.Length - i;
    if (apagar > Teto) {
      // Reancora sem mexer no aplicativo: o texto de baixo fica como está e a
      // overlay volta a acreditar nele. Perde-se a correção, não o texto.
      var recusa = $"correção pedia apagar {apagar} — recusei e reancorei";
      Emitido = alvo;
      return new Conserto(0, "", recusa);
    }
    var digitar = alvo.Substring(i);
    Emitido = alvo;
    return new Conserto(apagar, digitar, null);
  }

  public void Reancora(string emitido = "") { Emitido = emitido; }

  // Aplica um conserto sobre um texto — é o que o aplicativo de baixo faz
  // quando recebe as teclas. Existe pra o teste poder simular o outro lado sem
  // Windows nenhum, e pra a definição de "certo" ser esta, e não uma segunda
  // interpretação escrita no teste.
  public static string Aplica(string texto, Conserto c) =>
    texto.Substring(0, Math.Max(0, texto.Length - c.Apagar)) + c.Digitar;
}
