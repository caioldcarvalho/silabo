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
const doc = { getElementById: id => (nos[id] ||= {innerHTML:'',textContent:'',
  setAttribute(){}, classList:{toggle(){}}, style:{}}) };
const M = new Function('performance','document','addEventListener','navigator','requestAnimationFrame',
  corpo + `return {satellites, renderHud, stick, btn, setVar:v=>{variant=v}, held, flash};`
)({now:()=>0}, doc, ()=>{}, {getGamepads:()=>[]}, ()=>{});

const texto = id => [...(nos[id]?.innerHTML||'').matchAll(/>([^<>]+)</g)].map(m=>m[1].trim()).filter(Boolean).join(' | ');
let ok=0,bad=0;
const t=(nome,got,deve)=>{const p=deve.every(d=>got.includes(d)); p?ok++:bad++;
  console.log(`  ${nome.padEnd(30)} ${p?'ok':'X'}\n     ${got}`);
  if(!p) console.log(`     FALTA: ${deve.filter(d=>!got.includes(d)).join(', ')}`);};

console.log('— satélites: variante A (líquida é BOTÃO) —');
M.setVar('A'); Object.assign(M.btn,{LB:0,LT:0,RB:0,RT:0,L3:0,R3:0});
M.stick.L.gates=[0]; M.satellites('l');                       // ↑ = t
t('gate ↑ (t): só tr, pois tl não é ataque em PT',
  texto('lsat'), ['L3','d','LB','tr']);
t('gate ↑ (t): NÃO oferece tl',
  /\btl\b/.test(texto('lsat')) ? 'OFERECEU tl' : 'tl ausente', ['ausente']);
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

console.log('\n— HUD —');
M.stick.L.gates=[2,1]; M.stick.R.gates=[4];
Object.assign(M.btn,{LB:1,LT:0,RB:0,RT:1,L3:0,R3:0}); M.held.A=1;
M.renderHud();
const h = nos.hud.innerHTML;
t('mostra roll do esquerdo', texto('hud'), ['→↗']);
t('acende só o que está ativo',
  ['LB','RT','A'].map(k=>new RegExp(`on[^>]*>${k}`).test(h)?k:'').join(' '), ['LB','RT','A']);
t('deixa apagado o que não está',
  ['LT','RB','L3','B'].every(k=>!new RegExp(`on[^>]*>${k}<`).test(h)) ? 'LT RB L3 B apagados':'ACESO INDEVIDO',
  ['apagados']);

console.log(`\n${ok} ok, ${bad} falha(s)`);
process.exit(bad?1:0);
