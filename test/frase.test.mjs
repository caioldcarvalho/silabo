// Prova que a FRASE-MODELO da UI é digitável de verdade. Duas provas, porque
// são perguntas diferentes:
//   1. o ortografador produz exatamente aquele texto a partir das sílabas;
//   2. cada ataque usado tem um GESTO real que o produz — sem isto, a prova 1
//      só diria que a string é construível, não que uma mão a alcança.
// O TEXTO da frase vive em index.html (FRASE_MODELO); o ROTEIRO que a digita
// vive em frase-script.mjs, como dado, porque o port em C# replaya o mesmo — e
// dois roteiros separados provariam que cada motor sabe digitar a sua frase.
// Aqui moram os gestos.
//   node test/frase.test.mjs
import {readFileSync} from 'fs';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';
import {LONGA, CURTA, roda} from './frase-script.mjs';

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
                 buildOnset, stick, btn, FRASE_MODELO, TRILHAS, RESPELL, RESPELL_GATE,
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

// coleta os ataques enquanto escreve, pra a varredura de gestos não poder
// dessincronizar da frase que ela diz cobrir
const usados = new Set();
const fase = f => { M.setText(''); M.setWord(''); M.setDepois('');
                    roda(M, f, usados); return M.getText().trim(); };

grupo('cada fase sai exatamente como está escrito na UI');
t('a UI tem 9 fases', String(M.FRASE_MODELO.length), '9');
t('e o roteiro cobre todas', String(LONGA.length), String(M.FRASE_MODELO.length));
LONGA.forEach((f,i)=>
  t(`${i+1}. ${M.FRASE_MODELO[i].mec}`, fase(f), M.FRASE_MODELO[i].texto));

grupo('a trilha curta também sai exatamente como está escrita');
// mesma prova, na volta de aquecimento — 54 caracteres em vez de 283
t('a curta tem 3 fases', String(M.TRILHAS.curta.fases.length), String(CURTA.length));
CURTA.forEach((f,i)=>
  t(`curta ${i+1}. ${M.TRILHAS.curta.fases[i].mec}`, fase(f),
    M.TRILHAS.curta.fases[i].texto));

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
