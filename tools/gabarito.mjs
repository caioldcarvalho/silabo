// Gera o ARQUIVO-GABARITO: a saída do motor JS, congelada, pra o port em C#
// afirmar contra ela.
//
// A regra que isto existe pra cumprir: a partir do momento em que o motor tem
// duas implementações, elas divergem em silêncio. Um teste em C# escrito à mão
// prova que o C# faz o que o autor do teste achou que o JS fazia — que é
// exatamente o erro que a divergência é. Então o JS não é consultado, é a
// FONTE: ele varre o espaço de entradas e escreve o que sai; o C# só confere.
//
// A varredura é exaustiva onde o espaço é finito e pequeno (gestos: 8 gates,
// sequências até 4) e amostrada deterministicamente onde não é (palavras de
// várias sílabas). Amostra com semente fixa, nunca aleatória: um gabarito que
// muda sozinho entre duas rodadas não serve de gabarito.
//
//   node tools/gabarito.mjs        → test/gabarito.json
import {readFileSync, writeFileSync} from 'fs';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';
import {LONGA, CURTA, roda} from '../test/frase-script.mjs';

const raiz = join(dirname(fileURLToPath(import.meta.url)), '..');
const html = readFileSync(join(raiz, 'index.html'), 'utf8');
const js   = html.split('<script>')[1].split('</script>')[0];
const corpo = js.split('// ---------------------------------------------------------------- boot')[0];

const nos = {};
const doc = {getElementById: id => (nos[id] ||= {innerHTML:'', textContent:'',
  setAttribute(){}, classList:{toggle(){}}, style:{}})};
const M = new Function('performance','document','addEventListener','navigator',
  'requestAnimationFrame','localStorage','setTimeout','Blob','URL',
  corpo + `return {gateOf, track, landmarks, buildOnset, buildNucleus, buildCoda, orthograph, finish,
                   poeSilaba, endWord, delimita, cycleAccent, toggleSibilant, cycleCaps,
                   inicioDeFrase, maiuscula, stick, btn,
                   ONSET_BY_GATE, NUCLEUS_BY_GATE, VOICED, GLIDE, RESPELL_GATE,
                   TRILHAS, FRASE_MODELO,
                   setWord:w=>{word=w}, getWord:()=>word,
                   setText:t=>{text=t}, getText:()=>text, setDepois:d=>{depois=d}};`
)({now:()=>0}, doc, ()=>{}, {getGamepads:()=>[], userAgent:'gabarito'}, ()=>{},
  {getItem:()=>null, setItem(){}, removeItem(){}}, f=>f(), function(){},
  {createObjectURL:()=>'', revokeObjectURL(){}});

const zera = () => { M.setText(''); M.setWord(''); M.setDepois(''); };

// ---------------------------------------------------------------- gestos
// Toda sequência de gates alcançável até 4 posições. Gate repetido em seguida
// não existe: track() só empilha quando o gate MUDA, então "00" nunca chega no
// motor e varrer isso testaria uma entrada impossível.
function sequencias(max){
  const out = [[]];
  let nivel = [[]];
  for(let n = 0; n < max; n++){
    const prox = [];
    for(const s of nivel) for(let g = 0; g < 8; g++)
      if(s[s.length-1] !== g) prox.push([...s, g]);
    out.push(...prox); nivel = prox;
  }
  return out;
}
const SEQS = sequencias(4);
const chave = s => s.join('');

// ---------------------------------------------------------------- gate
// A geometria é a camada mais fácil de portar errado sem ninguém notar: ela não
// produz letra nenhuma, só decide em que casa o polegar está. O Math.Round do
// .NET arredonda para o PAR e o do JS para cima — numa fronteira de gate isso é
// o dedo cair na casa errada, e nenhuma varredura de letras pega.
// Por isso a varredura tem duas metades: uma grade cartesiana, que cobre o caso
// comum, e um anel polar de 7,5° em 7,5° com raios COLADOS na histerese
// (0,38 e 0,55), que é onde o arredondamento e o limiar decidem sozinhos.
const PONTOS = [];
for(let i = -10; i <= 10; i++) for(let j = -10; j <= 10; j++)
  PONTOS.push([i/10, j/10]);
for(let k = 0; k < 48; k++){
  const a = k * 7.5 * Math.PI / 180;                 // múltiplos de 22,5° inclusos
  for(const r of [0.3, 0.379, 0.38, 0.381, 0.54, 0.55, 0.56, 0.9, 1])
    PONTOS.push([+(Math.sin(a)*r).toFixed(12), +(-Math.cos(a)*r).toFixed(12)]);
}
// E os EMPATES: os pontos em que a/(π/4) cai exatamente no meio de dois gates.
// Aqui o JS arredonda pra cima e o Math.Round do .NET arredonda pro par, então
// é a única entrada em que a tradução ingênua da geometria produz outra letra.
// São achados por busca, não escritos à mão, porque dependem do arredondamento
// de sin/cos/atan2 e mudariam se alguém mexesse na fórmula.
for(let k = 0; k < 8; k++){
  const t = k + 0.5, A = t * Math.PI/4;
  let n = 0;
  for(let i = 0; i < 2e6 && n < 2; i++){
    const r = 0.56 + i/2e6 * 0.8;                    // já defletido: passa da histerese
    const x = Math.sin(A)*r, y = -Math.cos(A)*r;
    let a = Math.atan2(x,-y); if(a < 0) a += Math.PI*2;
    if(a/(Math.PI/4) === t){ PONTOS.push([x,y]); n++; }
  }
}

const ATUAIS = [null,0,1,2,3,4,5,6,7];
const gate = [];
for(const [x,y] of PONTOS) for(const atual of ATUAIS)
  gate.push([x, y, atual === null ? -1 : atual,
             (g => g === null ? -1 : g)(M.gateOf(x, y, atual))]);

// track() é a máquina de estado por cima do gate: o que empilha, o que ignora e
// o que zera. Caminhos determinísticos, porque o que interessa é a SEQUÊNCIA.
const trilhos = [];
{
  let sem = 424242;
  const rnd = n => { sem = (sem * 1103515245 + 12345) & 0x7fffffff; return sem % n; };
  for(let i = 0; i < 300; i++){
    const passos = [];
    for(let k = 0, n = 2 + rnd(6); k < n; k++) passos.push(PONTOS[rnd(PONTOS.length)]);
    const s = {live:null, gates:[]};
    const antes = M.stick.L.live, antesG = M.stick.L.gates;
    M.stick.L.live = null; M.stick.L.gates = [];
    for(const [x,y] of passos) M.track('L', x, y);
    trilhos.push([passos, M.stick.L.gates.join(''),
                  M.stick.L.live === null ? -1 : M.stick.L.live]);
    M.stick.L.live = antes; M.stick.L.gates = antesG;
  }
}

const onset = [];
for(const s of SEQS){
  // L3 só significa alguma coisa com o analógico parado, mas as duas metades
  // entram no gabarito de propósito: "é ignorado" é um comportamento, e um
  // port que esquecesse de ignorar passaria despercebido sem estas linhas.
  const l3s = s.length <= 2 ? [0,1] : [0];
  for(const lt of [0,1]) for(const l3 of l3s){
    M.stick.L.gates = s;
    Object.assign(M.btn, {LB:0, RB:0, LT:lt, L3:l3, R3:0, RT:0});
    const o = M.buildOnset();
    onset.push([chave(s), lt, l3, o.c, o.respell ? 1 : 0, o.suprimido || '']);
  }
}

const nucleo = [];
for(const s of SEQS){
  M.stick.R.gates = s;
  Object.assign(M.btn, {R3:0});
  nucleo.push([chave(s), M.buildNucleus().v]);
}

const coda = [];
for(const lb of [0,1]) for(const rb of [0,1]){
  Object.assign(M.btn, {LB:lb, RB:rb});
  coda.push([lb, rb, M.buildCoda()]);
}

const marcos = SEQS.map(s => [chave(s), chave(M.landmarks(s))]);

// ------------------------------------------------------------ ortografia
// Os ataques que o gesto de fato produz — colhidos da varredura acima, não
// listados à mão, pra o cross-product não testar um ataque que não existe nem
// deixar de testar um que existe.
const ATAQUES = [...new Set(onset.filter(r => !r[5]).map(r => r[3] + '\t' + r[4]))]
  .map(k => k.split('\t')).map(([c, r]) => [c, +r]).sort();

// Núcleos escolhidos por CLASSE, não por sorteio: os sete orais, o vazio, os
// ditongos com /u/ (que disparam a regra do ⟨qu⟩) e um de cada tipo de glide.
const NUCLEOS = ['', 'a','e','é','i','o','ó','u',
                 'ua','uá','uo','ui','ai','au','oi','ei','ou','iu'];
// (prevWord, contexto) — as duas coisas deixaram de ser a mesma string quando o
// cursor passou a poder cair no meio de uma palavra, e só as regras contextuais
// leem o contexto. Um ambiente termina em vogal, outro em consoante, um tem
// maiúscula, e dois testam o contexto valendo com o buffer vazio.
const AMBIENTES = [['',''], ['ca',''], ['atl',''], ['','ca'], ['','atlas'], ['Ma','']];
const CODAS = ['', 's', 'r', 'l'];

// O cross-product é DETERMINÍSTICO, então guardar as entradas é guardar o que
// os dois lados já sabem gerar. O gabarito carrega as listas uma vez e só as
// SAÍDAS, na ordem dos laços aninhados abaixo — ataque, núcleo, nasal, coda,
// ambiente. A ordem é o contrato; a contagem confere se ela bateu.
// Cada saída é "buffer" quando finish() não muda nada, e "buffer\tfinish"
// quando muda. Cortou o arquivo em três.
const saidas = [];
for(const [c, respell] of ATAQUES)
  for(const v of NUCLEOS)
    for(const nasal of [0,1])
      for(const cd of CODAS)
        for(const [prev, ctx] of AMBIENTES){
          const out = M.orthograph(prev, {onset:c, vowel:v, nasal:!!nasal,
                                          coda:cd, respell:!!respell}, ctx);
          const fim = M.finish(out);
          saidas.push(out === fim ? out : out + '\t' + fim);
        }
const orto = {ataques: ATAQUES, nucleos: NUCLEOS, codas: CODAS,
              ambientes: AMBIENTES, saidas};

// -------------------------------------------------------------- palavras
// Onde o espaço é infinito (encadear sílabas), amostra determinística. O que
// isto pega e o cross-product de uma sílaba não pega é a INTERAÇÃO: o "s" que
// vira "ss" porque a sílaba anterior terminou em vogal, o "~" que só resolve
// quando a próxima sílaba diz que letra vem depois.
let semente = 20260909;
const rnd = n => { semente = (semente * 1103515245 + 12345) & 0x7fffffff;
                   return semente % n; };
const escolhe = a => a[rnd(a.length)];

const palavras = [];
for(let i = 0; i < 4000; i++){
  const n = 1 + rnd(4);
  const sils = [];
  for(let k = 0; k < n; k++){
    const [c, respell] = escolhe(ATAQUES);
    sils.push({onset: k === 0 ? c : (c || escolhe(ATAQUES)[0]),
               vowel: escolhe(NUCLEOS.slice(1)),   // sílaba sem núcleo só na 1a
               nasal: rnd(4) === 0, coda: escolhe(['','','s','r','l']),
               respell: !!respell});
  }
  let w = '';
  for(const s of sils) w = M.orthograph(w, s);
  palavras.push([sils.map(s => [s.onset, s.respell?1:0, s.vowel, s.nasal?1:0, s.coda]),
                 w, M.finish(w)]);
}

// ---------------------------------------------------------- maiúscula automática
// poeSilaba é onde a regra de início de frase mora, e ela lê o `text` — então o
// gabarito tem que carregar o texto de antes, não só a sílaba.
const ANTES = ['', 'oi', 'oi.', 'oi. ', 'oi! ', 'oi? ', 'oi.) ', 'oi."  ', 'oi, ', 'oi” '];
const inicio = [];
for(const t of ANTES) for(const [c, respell] of ATAQUES.slice(0, 12))
  for(const v of ['a','e','o']) for(const w of ['', 'pr']){
    zera(); M.setText(t); M.setWord(w);
    const g = M.poeSilaba({onset:c, vowel:v, nasal:false, coda:'', respell:!!respell});
    inicio.push([t, w, c, respell, v, M.getWord(), g]);
  }

// ------------------------------------------------------------ pós-correções
// Os três ciclos do d-pad, aplicados TRÊS vezes seguidas em cada entrada: o que
// define um ciclo é fechar, e uma aplicação só não mostra isso.
const AMOSTRA = [...new Set([
  ...palavras.slice(0, 700).map(p => p[1]),
  // caixa entra explícita: os ciclos escreviam de tabelas minúsculas e comiam a
  // maiúscula ("ES" → "Ez"). Foi bug medido; fica no gabarito pra não voltar.
  'ES', 'Ex', 'Casa', 'CASA', 'Sao~', 'questau~', 'talves', 'esplico', 'proximo',
  'exceto', 'vose', 'ja', 'irmam', 'Nau~', 'A', 'a', '', 'MAIUSCULA', 'çé',
])];

const passo = (fn, w) => { zera(); M.setWord(w); const r = []; 
  for(let i = 0; i < 3; i++){ fn(); r.push(M.getWord()); } return r; };

const acento     = AMOSTRA.map(w => [w, +1, ...passo(() => M.cycleAccent(+1), w)]);
const acentoBack = AMOSTRA.map(w => [w, -1, ...passo(() => M.cycleAccent(-1), w)]);
const sibilante  = AMOSTRA.map(w => [w, ...passo(() => M.toggleSibilant(), w)]);
const caixa      = AMOSTRA.map(w => [w, ...passo(() => M.cycleCaps(), w)]);

// editBuffer age no `text` quando o `word` está vazio — o mesmo ciclo, no outro
// buffer. Sem esta metade um port poderia mexer sempre no word e passar.
const noTexto = AMOSTRA.slice(0, 200).map(w => {
  zera(); M.setText(w); M.cycleAccent(+1); const a = M.getText();
  zera(); M.setText(w); M.toggleSibilant(); const s = M.getText();
  zera(); M.setText(w); M.cycleCaps();     const c = M.getText();
  return [w, a, s, c];
});

// ------------------------------------------------------- fechar palavra
const SUFIXOS = [' ', '.', ', ', '! ', '? ', ': ', '; ', '...', ' — '];
const fechar = [];
for(const t of ['', 'oi ', 'oi', 'oi. ']) for(const w of ['', 'cas', 'sau~', 'Nau~'])
  for(const suf of SUFIXOS){
    zera(); M.setText(t); M.setWord(w); M.endWord(suf);
    fechar.push([t, w, suf, M.getText()]);
  }

const PARES = ['""', '()', '“”'];
const delimitadores = [];
for(const t of ['', 'oi ', 'ele disse "oi', 'ele disse "oi" e ', '(a', '(a) e '])
  for(const w of ['', 'sim']) for(const par of PARES){
    zera(); M.setText(t); M.setWord(w);
    const ch = M.delimita([par[0], par[1]]);
    delimitadores.push([t, w, par, ch, M.getText()]);
  }

// ------------------------------------------------------------- as frases
// A prova de ponta a ponta. O roteiro vai junto, em dado, pra o C# executar
// exatamente esta sequência — não uma tradução dela.
const replay = (fases, esperado) => fases.map((f, i) => {
  zera(); roda(M, f);
  return [M.getText().trim(), esperado[i].texto];
});

const frases = {
  roteiro: {longa: LONGA, curta: CURTA},
  longa: replay(LONGA, M.FRASE_MODELO),
  curta: replay(CURTA, M.TRILHAS.curta.fases),
};

// ------------------------------------------------------------------ saída
const G = {
  gerado: 'tools/gabarito.mjs a partir de index.html — não editar à mão',
  tabelas: {ONSET_BY_GATE: M.ONSET_BY_GATE, NUCLEUS_BY_GATE: M.NUCLEUS_BY_GATE,
            VOICED: M.VOICED, GLIDE: M.GLIDE, RESPELL_GATE: M.RESPELL_GATE},
  gate, trilhos, marcos, onset, nucleo, coda, orto, palavras, inicio,
  acento, acentoBack, sibilante, caixa, noTexto, fechar, delimitadores, frases,
};

const destino = join(raiz, 'test', 'gabarito.json');
writeFileSync(destino, JSON.stringify(G));
const linhas = Object.entries(G).filter(([,v]) => Array.isArray(v))
  .map(([k,v]) => `${k}=${v.length}`).join(' ') + ` orto=${orto.saidas.length}`;
console.log(`gabarito → test/gabarito.json`);
console.log(`  ${linhas} frases=${frases.longa.length}+${frases.curta.length}`);
console.log(`  ${(Buffer.byteLength(JSON.stringify(G))/1024).toFixed(0)} KB`);
