// Verifica o que a UI GERA, sem browser: satélites de preview e HUD. Compilar
// não prova nada aqui — o risco é gerar rótulo errado, não quebrar sintaxe.
//   node test/ui.test.mjs
import {readFileSync} from 'fs';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';

const raiz = join(dirname(fileURLToPath(import.meta.url)),'..');
const html = readFileSync(join(raiz,'index.html'),'utf8');
const js   = html.split('<script>')[1].split('</script>')[0];
const corpo = js.split('// ---------------------------------------------------------------- boot')[0];

const nos = {};
// setAttribute GUARDA o valor: sem isso os testes de cor liam undefined e
// passavam por vacuidade em vez de por acerto
const doc = { getElementById: id => (nos[id] ||= {innerHTML:'',textContent:'',
  setAttribute(k,v){ this[k]=v; }, classList:{toggle(){}}, style:{}}) };
const store = {};
const M = new Function('performance','document','addEventListener','navigator',
  'requestAnimationFrame','localStorage','setTimeout','Blob','URL',
  corpo + `return {satellites, renderHud, stick, btn, setVar:v=>{variant=v}, held, flash,
                   commit, tele, tlogPad, current, cycleAccent, toggleSibilant, drawPad, paintPad, axesNow,
                   setWord:w=>{word=w}, getWord:()=>word,
                   setText:t=>{text=t}, getText:()=>text, faceButtons, face, atRest};`
)({now:()=>0}, doc, ()=>{}, {getGamepads:()=>[], userAgent:'teste'}, ()=>{},
  {getItem:k=>store[k]??null, setItem:(k,v)=>{store[k]=v}, removeItem:k=>{delete store[k]}},
  fn=>fn(), function(){}, {createObjectURL:()=>'', revokeObjectURL(){}});

const texto = id => [...(nos[id]?.innerHTML||'').matchAll(/>([^<>]+)</g)].map(m=>m[1].trim()).filter(Boolean).join(' | ');
let ok=0,bad=0;
const t=(nome,got,deve)=>{const p=deve.every(d=>got.includes(d)); p?ok++:bad++;
  console.log(`  ${nome.padEnd(30)} ${p?'ok':'X'}\n     ${got}`);
  if(!p) console.log(`     FALTA: ${deve.filter(d=>!got.includes(d)).join(', ')}`);};

console.log('— satélites: variante A (líquida é BOTÃO) —');
M.setVar('A'); Object.assign(M.btn,{LB:0,LT:0,RB:0,RT:0,L3:0,R3:0});
M.stick.L.gates=[0]; M.satellites('l');                       // ↑ = t
t('gate ↑ (t): tl É ataque (a·tle·ta)', texto('lsat'), ['L3','d','LB','tr','LT','tl']);
M.stick.L.gates=[3]; M.satellites('l');                       // ↘ = p, tem pr e pl
t('gate ↘ (p): as duas líquidas', texto('lsat'), ['L3','b','LB','pr','LT','pl']);
M.stick.L.gates=[2]; M.satellites('l');                       // → = s
t('gate → (s): oferece ↗', texto('lsat'), ['roll ↗','c / ç']);
t('gate → (s): NÃO oferece sr/sl',
  /\bsr\b|\bsl\b/.test(texto('lsat')) ? 'OFERECEU CLUSTER ILEGAL' : 'sr e sl ausentes', ['ausentes']);
M.stick.L.gates=[7]; M.satellites('l');                       // ↖ = x
t('gate ↖ (x): ch', texto('lsat'), ['roll ↗','ch']);
t('gate ↖ (x): NÃO oferece xr/xl',
  /\bxr\b|\bxl\b/.test(texto('lsat')) ? 'OFERECEU CLUSTER ILEGAL' : 'xr e xl ausentes', ['ausentes']);
Object.assign(M.btn,{L3:1}); M.satellites('l');               // x sonorizado = j
t('↖ + L3 (j): vira g', texto('lsat'), ['roll ↗','g']);

console.log('\n— satélites: variante B (líquida é ROLL) —');
M.setVar('B'); Object.assign(M.btn,{LB:0,LT:0,RB:0,RT:0,L3:0,R3:0});
M.stick.L.gates=[3]; M.satellites('l');
t('gate ↘ (p): gatilho muda', texto('lsat'), ['LB','b','roll →','pr','roll ←','pl']);

console.log('\n— satélites: núcleo —');
M.setVar('A'); M.stick.R.gates=[4]; M.satellites('r');        // ↓ = a
t('gate ↓ (a)', texto('rsat'), ['R3','ã','roll ↖','ai','roll ↗','au']);
M.stick.R.gates=[0]; M.satellites('r');                       // ↑ = sem vogal
t('gate ↑ (—): sem satélite', texto('rsat')||'(vazio)', ['(vazio)']);

console.log('\n— desenho do controle —');
M.drawPad();
const svg = nos.padsvg.innerHTML;
t('desenha todos os controles',
  ['LB','LT','RB','RT','A','B','X','Y','up','down','left','right','L3','R3']
    .filter(k=>svg.includes('id="p-'+k+'"')).join(' '),
  ['LB','LT','RB','RT','A','B','X','Y','up','down','left','right','L3','R3']);
t('8 gates por analógico',
  [0,1,2,3,4,5,6,7].every(i=>svg.includes(`id="p-Lg${i}"`)&&svg.includes(`id="p-Rg${i}"`))
    ? '16 pontos de gate' : 'FALTAM', ['16 pontos']);

M.stick.L.gates=[2,1]; M.stick.R.gates=[4];
Object.assign(M.btn,{LB:1,LT:0,RB:0,RT:1,L3:0,R3:0}); M.held.A=1;
M.axesNow.L=[0.8,-0.6]; M.axesNow.R=[0,1];
M.renderHud();
const cor = id => nos['p-'+id]?.fill;
const rot = id => nos['t-'+id]?.fill;
t('acende só o que está pressionado',
  ['LB','RT','A'].map(k=>cor(k)==='var(--hot)'?k:'!'+k).join(' '), ['LB','RT','A']);
t('e deixa o resto apagado',
  ['LT','RB','L3','B'].every(k=>cor(k)==='var(--line)') ? 'LT RB L3 B apagados' : 'ACESO INDEVIDO',
  ['apagados']);
t('rótulo inverte junto (legível nos dois estados)',
  `LB:${rot('LB')} LT:${rot('LT')}`, ['LB:var(--bg)','LT:var(--muted)']);
t('o analógico deflete de verdade',
  `${nos['p-L3'].cx>100?'direita':'?'} ${nos['p-L3'].cy<96?'cima':'?'}`, ['direita','cima']);
t('gate de origem ≠ gate de roll',
  `origem:${cor('Lg2')} roll:${cor('Lg1')} intocado:${cor('Lg5')}`,
  ['origem:var(--ink)','roll:var(--hot)','intocado:var(--line)']);
t('sequência do roll em texto', texto('hudseq'), ['→↗']);

console.log('\n— acento é um CICLO, e sempre volta —');
const ciclo = (palavra, n, dir=1) => { M.setWord(palavra);
  for(let i=0;i<n;i++) M.cycleAccent(dir); return M.getWord(); };
t('café: 1 toque',        ciclo('cafe',1),  ['café']);
t('   2 toques → ê',      ciclo('cafe',2),  ['cafê']);
t('   3 toques (e só tem 3) volta ao e', ciclo('cafe',3), ['cafe']);
t('a tem 5 estados, e fecha', ciclo('la',5), ['la']);
t('← desfaz o →',         (()=>{M.setWord('cafe');M.cycleAccent(1);M.cycleAccent(-1);return M.getWord();})(), ['cafe']);
t('NÃO pula pra vogal anterior', ciclo('cafe',2), ['cafê']);
console.log('  ↑ era o bug: 2 toques acentuavam o "a" em vez de reverter o "e"');
t('só: o tem 4 estados',  ciclo('so',1),    ['só']);
t('s↔z continua',         (()=>{M.setWord('faser');M.toggleSibilant();return M.getWord();})(), ['fazer']);

console.log('\n— backspace repete, e LB+LT+B limpa tudo —');
const padFake = (b={}) => ({buttons:[0,1,2,3,12,13,14,15].map((_,i)=>({pressed:0})).concat([]),
  axes:[0,0,0,0]});
// monta um pad com B apertado no índice 1
const pad = press => ({buttons:Array.from({length:16},(_,i)=>({pressed: press.includes(i)?1:0, value:0})),
                       axes:[0,0,0,0], mapping:'standard', id:'fake'});
M.stick.L.gates=[]; M.stick.R.gates=[]; M.stick.L.live=null; M.stick.R.live=null;
Object.assign(M.btn,{LB:0,LT:0,RB:0,RT:0,L3:0,R3:0});
M.setText('uma frase inteira aqui '); M.setWord('');
Object.assign(M.face,{A:0,B:0,X:0,Y:0,heldB:0,nextB:0,apagados:0});
M.faceButtons(pad([1]));            // aperta B: apaga 1
t('toque em B apaga uma letra', M.getText(), ['uma frase inteira aqui']);
M.faceButtons(pad([1]));            // segurando: ainda dentro da espera
M.faceButtons(pad([1]));
t('não dispara antes da espera', M.getText(), ['uma frase inteira aqui']);

// segurando de verdade: adianta o relógio interno
M.face.heldB -= 2000; M.face.nextB -= 2000;
M.faceButtons(pad([1]));
t('segurando passa a apagar por palavra',
  M.getText().length < 'uma frase inteira aqui'.length ? 'apagou palavra' : 'NÃO APAGOU', ['apagou palavra']);

// LB+LT+B limpa tudo
M.setText('nao deveria sobrar nada'); M.setWord('resto');
Object.assign(M.face,{A:0,B:0,X:0,Y:0,heldB:0,nextB:0,apagados:0});
Object.assign(M.btn,{LB:1,LT:1});
M.faceButtons(pad([1]));
t('LB+LT+B limpa tudo', JSON.stringify([M.getText(),M.getWord()]), ['["",""]']);
Object.assign(M.btn,{LB:0,LT:0});

console.log('\n— telemetria —');
const tipos = () => M.tele.eventos.map(e=>e.tipo).join(',');
M.tele.eventos.length = 0;
M.setVar('A'); M.stick.L.gates=[3]; M.stick.R.gates=[4];      // p + a
Object.assign(M.btn,{LB:0,LT:0,RB:0,RT:0,L3:0,R3:0});
M.commit();
t('sílaba boa vira 1 evento', tipos(), ['silaba']);
t('registra o que saiu', JSON.stringify(M.tele.eventos.at(-1).saiu), ['pa']);

// RB+LB é AMBÍGUO, não perda: é também o gesto normal de coda -r. O primeiro
// log real teve 2 "conflitos" e os dois eram digitação correta de infinitivo.
M.tele.eventos.length = 0;
M.setVar('A'); M.stick.L.gates=[3]; M.stick.R.gates=[4];
Object.assign(M.btn,{LB:1,LT:0,RB:1,RT:0,L3:0,R3:0});
M.commit();
t('coda -r não grita conflito', tipos(), ['silaba','ambiguo']);
t('não conta como CONFLITO',
  tipos().includes('CONFLITO') ? 'GRITOU LOBO' : 'silencioso', ['silencioso']);

// perda REAL e silenciosa: a líquida some porque o cluster não existe
M.tele.eventos.length = 0;
M.stick.L.gates=[2];                                           // → = s, e "sr" não existe
Object.assign(M.btn,{LB:1,LT:0,RB:0,RT:0,L3:0,R3:0});
M.commit();
t('cluster inexistente É conflito', tipos(), ['silaba','CONFLITO']);
t('e diz o motivo',
  M.tele.eventos.find(e=>e.tipo==='CONFLITO').motivo, ['cluster-inexistente:sr']);

M.tele.eventos.length = 0;
M.stick.L.gates=[]; M.stick.R.gates=[];
Object.assign(M.btn,{LB:0,LT:0,RB:0,RT:0,L3:0,R3:0});
M.commit();
t('commit sem nada vira "vazio"', tipos(), ['vazio']);

M.tele.pads.length = 0;
M.tlogPad({id:'Fake Pad', mapping:'', buttons:new Array(11), axes:[0,0,0,0,0,0,0,0,0,1.29]});
t('perfil do controle é gravado',
  JSON.stringify(M.tele.pads[0]), ['Fake Pad','"mapping":""','"botoes":11','1.29']);
console.log('  ↑ é isso que permite diagnosticar d-pad morto pelo log');

console.log(`\n${ok} ok, ${bad} falha(s)`);
process.exit(bad?1:0);
