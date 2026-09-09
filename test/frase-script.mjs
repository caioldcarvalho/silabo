// O roteiro das duas frases-modelo, em DADOS.
//
// Isto era código: uma lista de closures dentro de frase.test.mjs que chamava
// poeSilaba/delimita/cycleAccent direto. Funcionava enquanto só o JS precisava
// digitar a frase — mas o motor agora tem uma segunda implementação em C#, e
// duas implementações que replayam roteiros diferentes provam nada. Como dado,
// o roteiro entra no arquivo-gabarito e os dois lados executam o MESMO script.
//
// Uma sílaba é [ataque, núcleo, flags]:
//   n = nasal · s/r/l = coda · R = re-grafia (roll até ↗)
// Uma operação é uma PALAVRA {s:[sílabas], acc, sib, caps, fim} — onde acc/sib/
// caps são quantas vezes a pós-correção do d-pad é acionada antes de fechar —
// ou um DELIMITADOR {d:'""'} / {d:'()'}, que se abre ou fecha sozinho.

const P = (s, extra = {}) => ({s, fim: ' ', ...extra});
const asp = {d: '""'}, par = {d: '()'};

// As nove fases, na mesma ordem do FRASE_MODELO em index.html. Cada fase
// introduz um mecanismo e reusa todos os anteriores.
export const LONGA = [
  [ P([['','o','']]), P([['s','a',''],['p','o','']]),
    P([['p','u',''],['l','a','']], {fim:'.'}) ],

  [ P([['','o','']]), P([['g','a',''],['t','o','']]), P([['d','o','']]),
    P([['t','i',''],['','o','']]), P([['n','a',''],['d','a','']], {fim:'.'}) ],

  [ P([['b','i',''],['','a','']]), P([['v','iu','']]), P([['','o','']]),
    P([['p','ai','']]), P([['','e','']]), P([['','o','']]),
    P([['b','oi','']], {fim:'.'}) ],

  [ P([['','a','']]), P([['m','ai','n']]), P([['c','a','n'],['t','a','']]),
    P([['','u','n']]), P([['s','o','n']]), P([['b','o','n']], {fim:'.'}) ],

  [ P([['','o','s']]), P([['d','oi','s']]), P([['v','au','n']]),
    P([['c','a','n'],['t','a','r']]), P([['','a',''],['t','é','']]),
    P([['','o','']]), P([['s','o','l']]),
    P([['s','u',''],['m','i','r']], {fim:'.'}) ],

  [ P([['','o','']]), P([['pr','a',''],['t','o','']], {fim:', '}),
    P([['','o','']]), P([['l','i',''],['vr','o','']]), P([['','e','']]),
    P([['','a','']]), P([['fl','o','r']]), P([['gr','a','n'],['d','e','']]),
    P([['f','i',''],['c','a','n']]), P([['n','a','']]),
    P([['s','a',''],['l','a','']], {fim:'.'}) ],

  [ P([['','a','']]), P([['x','u','R'],['v','a','']]),
    P([['m','o',''],['lh','ou','']]), P([['','a','']]),
    P([['j','e','nR'],['t','e','']]), P([['d','a','']]),
    P([['s','i','R'],['d','a',''],['d','e','']], {fim:'.'}) ],

  [ P([['h','o',''],['j','e','']]), P([['c','ua',''],['tr','o','']]),
    P([['p','e',''],['s','o',''],['','a','s']]),
    P([['c','ua',''],['z','e','']]),
    P([['c','a',''],['','e','n']], {fim:'.'}) ],

  [ P([['v','o',''],['s','e','R']], {acc:2}), P([['j','a','']], {acc:1}),
    P([['','e','s'],['pl','i',''],['c','ou','']], {sib:1}), P([['','a','']]),
    P([['pr','ó',''],['s','i',''],['m','a','']], {sib:3}),
    asp, P([['c','e','s'],['t','au','n']]), asp,
    par, P([['','a','']]), P([['d','e','']]), P([['','o','n'],['t','e','n']]), par,
    P([['pr','a','']]), P([['b','i',''],['','a','']], {caps:1, fim:'?'}) ],
];

// A volta de aquecimento: 54 caracteres em vez de 283, três fases em vez de nove.
export const CURTA = [
  [ P([['','o','s']]), P([['d','oi','s']]), P([['c','ai','ns']]),
    P([['b','e',''],['b','e',''],['r','a','n']], {fim:'.'}) ],

  [ P([['x','a','R']], {acc:1}), P([['c','e','n'],['t','e','']]),
    P([['h','o',''],['j','e','']], {fim:'.'}) ],

  [ P([['','o','']]), P([['l','i',''],['vr','o','']]),
    P([['gr','a','n'],['d','e','']], {fim:'.'}) ],
];

// Executa uma fase contra um motor `M` que exponha poeSilaba/endWord/delimita e
// os três ciclos. É a mesma sequência que o port em C# roda — a única diferença
// legítima entre os dois lados é a linguagem.
export function roda(M, fase, usados){
  for(const op of fase){
    if(op.d){ M.delimita([op.d[0], op.d[1]]); continue; }
    for(const [o, v, f] of op.s){
      const syl = {onset:o, vowel:v, nasal:f.includes('n'), respell:f.includes('R'),
                   coda: f.includes('s') ? 's' : f.includes('r') ? 'r'
                       : f.includes('l') ? 'l' : ''};
      // A chave é o par FONEMA + re-grafia, não a letra: ⟨c⟩ de "casa" e ⟨c⟩ de
      // "cidade" são o mesmo grafema e gestos diferentes.
      if(o && usados) usados.add(o + (syl.respell ? '+↗' : ''));
      M.poeSilaba(syl);
    }
    for(let i = 0; i < (op.acc  || 0); i++) M.cycleAccent(+1);
    for(let i = 0; i < (op.sib  || 0); i++) M.toggleSibilant();
    for(let i = 0; i < (op.caps || 0); i++) M.cycleCaps();
    M.endWord(op.fim);
  }
}
