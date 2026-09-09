// Prova que a FRASE-MODELO da UI é digitável de verdade. Duas provas, porque
// são perguntas diferentes:
//   1. o ortografador produz exatamente aquele texto a partir das sílabas;
//   2. cada ataque usado tem um GESTO real que o produz — sem isto, a prova 1
//      só diria que a string é construível, não que uma mão a alcança.
// A frase vive em index.html (FRASE_MODELO); aqui moram os gestos.
//   node test/frase.test.mjs
import {readFileSync} from 'fs';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';

const raiz = join(dirname(fileURLToPath(import.meta.url)),'..');
const html = readFileSync(join(raiz,'index.html'),'utf8');
const js   = html.split('<script>')[1].split('</script>')[0];
const corpo = js.split('// ---------------------------------------------------------------- boot')[0];
const nos = {};
const doc = {getElementById:id=>(nos[id]||={innerHTML:'',textContent:'',setAttribute(){},
  classList:{toggle(){}},style:{}})};
const M = new Function('performance','document','addEventListener','navigator',
  'requestAnimationFrame','localStorage','setTimeout','Blob','URL',
  corpo+`return {poeSilaba, endWord, delimita, cycleAccent, toggleSibilant, cycleCaps,
                 buildOnset, stick, btn, FRASE_MODELO, RESPELL, RESPELL_GATE,
                 setWord:w=>{word=w}, getWord:()=>word,
                 setText:t=>{text=t}, getText:()=>text, setDepois:d=>{depois=d}};`
)({now:()=>0},doc,()=>{},{getGamepads:()=>[],userAgent:'teste'},()=>{},
  {getItem:()=>null,setItem(){},removeItem(){}},f=>f(),function(){},
  {createObjectURL:()=>'',revokeObjectURL(){}});

let ok=0,bad=0;
const grupo = t => console.log('\n— '+t+' —');
const t = (nome,got,alvo)=>{const p=got===alvo; p?ok++:bad++;
  console.log(`  ${nome.padEnd(30)} ${p?'ok':'X'}`);
  if(!p){ console.log(`     saiu: ${got}`); console.log(`     alvo: ${alvo}`); }};

// notação compacta de sílaba: [ataque, núcleo, flags]
//   n = nasal · s/r/l = coda · R = re-grafia (roll até ↗)
const S=(o,v,f='')=>({onset:o, vowel:v, nasal:f.includes('n'),
  coda:f.includes('s')?'s':f.includes('r')?'r':f.includes('l')?'l':'', respell:f.includes('R')});
// coleta os ataques enquanto escreve, pra a varredura de gestos não poder
// dessincronizar da frase que ela diz cobrir
// A chave é o par FONEMA + re-grafia, não a letra: ⟨c⟩ de "casa" e ⟨c⟩ de
// "cidade" são o mesmo grafema e gestos diferentes (gate ↗ contra s roll ↗).
// Varrer por letra acharia o gesto errado e passaria verde.
const usados = new Set();
const P=(sils,{acc=0,sib=0,caps=0,fim=' '}={})=>{
  for(const s of sils){
    if(s.onset) usados.add(s.onset + (s.respell?'+↗':''));
    M.poeSilaba(s);
  }
  for(let i=0;i<acc;i++)  M.cycleAccent(+1);
  for(let i=0;i<sib;i++)  M.toggleSibilant();
  for(let i=0;i<caps;i++) M.cycleCaps();
  M.endWord(fim);
};
const asp=()=>M.delimita(['"','"']), par=()=>M.delimita(['(',')']);

// ---- as nove fases, na mesma ordem do FRASE_MODELO ------------------------
const FASES = [
  () => { P([S('','o')]); P([S('s','a'),S('p','o')]); P([S('p','u'),S('l','a')],{fim:'.'}); },
  () => { P([S('','o')]); P([S('g','a'),S('t','o')]); P([S('d','o')]);
          P([S('t','i'),S('','o')]); P([S('n','a'),S('d','a')],{fim:'.'}); },
  () => { P([S('b','i'),S('','a')]); P([S('v','iu')]); P([S('','o')]); P([S('p','ai')]);
          P([S('','e')]); P([S('','o')]); P([S('b','oi')],{fim:'.'}); },
  () => { P([S('','a')]); P([S('m','ai','n')]); P([S('c','a','n'),S('t','a')]);
          P([S('','u','n')]); P([S('s','o','n')]); P([S('b','o','n')],{fim:'.'}); },
  () => { P([S('','o','s')]); P([S('d','oi','s')]); P([S('v','au','n')]);
          P([S('c','a','n'),S('t','a','r')]); P([S('','a'),S('t','é')]); P([S('','o')]);
          P([S('s','o','l')]); P([S('s','u'),S('m','i','r')],{fim:'.'}); },
  () => { P([S('','o')]); P([S('pr','a'),S('t','o')],{fim:', '}); P([S('','o')]);
          P([S('l','i'),S('vr','o')]); P([S('','e')]); P([S('','a')]); P([S('fl','o','r')]);
          P([S('gr','a','n'),S('d','e')]); P([S('f','i'),S('c','a','n')]);
          P([S('n','a')]); P([S('s','a'),S('l','a')],{fim:'.'}); },
  () => { P([S('','a')]); P([S('x','u','R'),S('v','a')]); P([S('m','o'),S('lh','ou')]);
          P([S('','a')]); P([S('j','e','nR'),S('t','e')]); P([S('d','a')]);
          P([S('s','i','R'),S('d','a'),S('d','e')],{fim:'.'}); },
  () => { P([S('h','o'),S('j','e')]); P([S('c','ua'),S('tr','o')]);
          P([S('p','e'),S('s','o'),S('','a','s')]); P([S('c','ua'),S('z','e')]);
          P([S('c','a'),S('','e','n')],{fim:'.'}); },
  () => { P([S('v','o'),S('s','e','R')],{acc:2}); P([S('j','a')],{acc:1});
          P([S('','e','s'),S('pl','i'),S('c','ou')],{sib:1}); P([S('','a')]);
          P([S('pr','ó'),S('s','i'),S('m','a')],{sib:3});
          asp(); P([S('c','e','s'),S('t','au','n')]); asp();
          par(); P([S('','a')]); P([S('d','e')]); P([S('','o','n'),S('t','e','n')]); par();
          P([S('pr','a')]); P([S('b','i'),S('','a')],{caps:1,fim:'?'}); }
];

grupo('cada fase sai exatamente como está escrito na UI');
t('a UI tem 9 fases', String(M.FRASE_MODELO.length), '9');
t('e o teste cobre todas', String(FASES.length), String(M.FRASE_MODELO.length));
FASES.forEach((rodar,i)=>{
  M.setText(''); M.setWord(''); M.setDepois('');
  rodar();
  t(`${i+1}. ${M.FRASE_MODELO[i].mec}`, M.getText().trim(), M.FRASE_MODELO[i].texto);
});

grupo('e todo ataque da frase tem um gesto que o produz');
// varre o espaço de gestos do ataque: gate de origem × destino do roll × LT/L3
function gestoDe(chave){
  const [alvo, quer] = [chave.replace('+↗',''), chave.includes('+↗')];
  const destinos = [null, 2, 6, M.RESPELL_GATE];          // sem roll, →r, ←l, ↗grafia
  for(const voz of [0,1]) for(const g of [null,0,1,2,3,4,5,6,7]) for(const d of destinos){
    M.stick.L.gates = g===null ? [] : (d===null||d===g ? [g] : [g,d]);
    Object.assign(M.btn,{LB:0,LT:voz,RB:0,RT:0,L3:g===null?1:0,R3:0});
    const o = M.buildOnset();
    if(o.c===alvo && !!o.respell===quer && !o.suprimido){
      const nome = g===null ? 'L3 + analógico parado'
        : '↑↗→↘↓↙←↖'[g] + (d!==null&&d!==g ? ' roll '+'↑↗→↘↓↙←↖'[d] : '') + (voz?' + LT':'');
      const grafa = quer && M.RESPELL[alvo]
        ? '  → escreve ' + M.RESPELL[alvo](true) + ' / ' + M.RESPELL[alvo](false) : '';
      return {nome: nome + grafa};
    }
  }
  return null;
}
let semGesto = [];
for(const a of [...usados].sort()){
  const g = gestoDe(a);
  if(!g) semGesto.push(a); else console.log(`  ${a.padEnd(4)} ← ${g.nome}`);
}
t(`cobre os ${usados.size} ataques da frase`, String(usados.size>=20), 'true');
t('nenhum ataque sem gesto', semGesto.join(',')||'todos alcançáveis', 'todos alcançáveis');

console.log(`\n${ok} ok, ${bad} falha(s)`);
process.exit(bad?1:0);
