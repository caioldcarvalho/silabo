// Suíte de regressão do motor. Extrai o JS do index.html (sem cópia paralela
// que possa divergir) e exercita só as partes puras: montagem da sílaba e
// ortografia. Roda com:  node test/motor.test.mjs
import {readFileSync} from 'fs';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';

const raiz = join(dirname(fileURLToPath(import.meta.url)), '..');
const html = readFileSync(join(raiz,'index.html'),'utf8');
const js   = html.split('<script>')[1].split('</script>')[0];
const puro = js.split('// ---------------------------------------------------------------- wheels')[0];
const M = new Function('performance','document', puro +
  `return {orthograph, finish, landmarks, NUCLEUS_BY_GATE, GLIDE,
           buildOnset, stick, btn, setVar: v => { variant = v; }};`
)({now:()=>0},{getElementById:()=>({})});

const S = (o,v,n=false,c='',respell=false) => ({onset:o, vowel:v, nasal:n, coda:c, respell});
// R(...) = a syllable whose onset came from an actual left-stick roll
const R = (gates,v,{voiced=false,n=false,c=''}={}) => {
  M.setVar('A'); M.stick.L.gates = gates;
  Object.assign(M.btn,{LB:0,LT:0,RB:0,L3:voiced?1:0,R3:0});
  const o = M.buildOnset();
  return {onset:o.c, respell:o.respell, vowel:v, nasal:n, coda:c};
};
const P = (...sils) => { let w=''; for(const s of sils) w = M.orthograph(w,s); return M.finish(w); };
const N = gs => { const mk=M.landmarks(gs); let v=M.NUCLEUS_BY_GATE[mk[0]];
  for(let i=1;i<mk.length;i++){const n=M.NUCLEUS_BY_GATE[mk[i]]; if(n&&n!=='—') v+=M.GLIDE[n];} return v; };

let ok=0, bad=0;
const grupo = t => console.log('\n— '+t+' —');
const t = (nome, got, alvo) => { const p = got===alvo; p?ok++:bad++;
  console.log(`  ${nome.padEnd(34)} → "${got}"`.padEnd(58) + (p?'ok':`X  esperado "${alvo}"`)); };

grupo('nasal em final de sílaba');
t('cantar',  P(S('c','a',true),S('t','a',false,'r')), 'cantar');
t('campo',   P(S('c','a',true),S('p','o')),           'campo');
t('sem',     P(S('s','e',true)),                      'sem');
t('bem',     P(S('b','e',true)),                      'bem');
t('um',      P(S('','u',true)),                       'um');
t('com',     P(S('c','o',true)),                      'com');
t('bom',     P(S('b','ó',true)),                      'bom');
t('tempo',   P(S('t','e',true),S('p','o')),           'tempo');
t('irmã',    P(S('','i',false,'r'),S('m','a',true)),  'irmã');
t('irmãs',   P(S('','i',false,'r'),S('m','a',true,'s')), 'irmãs');
t('sons',    P(S('s','o',true,'s')),                  'sons');
t('bens',    P(S('b','e',true,'s')),                  'bens');

grupo('ditongos nasais');
t('não',     P(S('n','au',true)),                     'não');
t('pão',     P(S('p','au',true)),                     'pão');
t('mãe',     P(S('m','ai',true)),                     'mãe');
t('põe',     P(S('p','oi',true)),                     'põe');
t('ações',   P(S('','a'),R([2,1],'oi',{n:true,c:'s'})), 'ações');

grupo('re-grafia: roll do analógico esquerdo até ↗');
// s vive em → (gate 2), x em ↖ (7), c/g em ↗ (1)
t('cebola  [s→↗]+e',   P(R([2,1],'e'),S('b','o'),S('l','a')),        'cebola');
t('cidade  [s→↗]+i',   P(R([2,1],'i'),S('d','a'),S('d','e')),        'cidade');
t('começar co·me·çar', P(S('c','o'),S('m','e'),R([2,1],'a',{c:'r'})), 'começar');
t('ação    [s→↗]+ão',  P(S('','a'),R([2,1],'au',{n:true})),          'ação');
t('moço    [s→↗]+o',   P(S('m','o'),R([2,1],'o')),                   'moço');
t('caçar   [s→↗]+a-r', P(S('c','a'),R([2,1],'a',{c:'r'})),           'caçar');
t('chave   [x→↗]+a',   P(R([7,0,1],'a'),S('v','e')),                 'chave');
t('chão    [x→↗]+ão',  P(R([7,0,1],'au',{n:true})),                  'chão');
t('gente   [j→↗]+ẽ',   P(R([7,0,1],'e',{voiced:true,n:true}),S('t','e')), 'gente');
t('girafa  [j→↗]+i',   P(R([7,0,1],'i',{voiced:true}),S('r','a'),S('f','a')), 'girafa');
console.log('  c vs ç é REGRA (vogal seguinte), não um segundo endereço');

grupo('/z/ intervocálico: ⟨s⟩ é o default, ⟨z⟩ é o caso marcado');
t('casa',    P(S('c','a'),S('z','a')),                'casa');
t('mesa',    P(S('m','e'),S('z','a')),                'mesa');
t('coisa',   P(S('c','oi'),S('z','a')),               'coisa');
t('zero  (início: não é intervocálico)', P(S('z','e'),S('r','o')), 'zero');
t('faser → d-pad ↓ → fazer', (()=>{ let w=''; for(const x of [S('f','a'),S('z','e'),S('r','')]) w=M.orthograph(w,x);
   const V='aeiouáéíóúâêôãõà'; const hits=[...w.matchAll(new RegExp(`[${V}]([sz])(?=[${V}])`,'gi'))];
   const i=hits[hits.length-1].index+1; return M.finish(w.slice(0,i)+'z'+w.slice(i+1)); })(), 'fazer');

grupo('cluster fonotaticamente ilegal não vira lixo');
const LIQ = (gates,liq,v,{c=''}={}) => { M.setVar('A'); M.stick.L.gates=gates;
  Object.assign(M.btn,{LB:liq==='r'?1:0, LT:liq==='l'?1:0, RB:0, L3:0, R3:0});
  const o=M.buildOnset(); return {onset:o.c,respell:o.respell,vowel:v,nasal:false,coda:c}; };
t('s + LB  (sr não existe)', P(LIQ([2],'r','a')),  'sa');
t('s + LT  (sl não existe)', P(LIQ([2],'l','a')),  'sa');
t('x + LB  (xr não existe)', P(LIQ([7],'r','a')),  'xa');
t('t + LB  (tr existe)',     P(LIQ([0],'r','a')),  'tra');
t('p + LT  (pl existe)',     P(LIQ([3],'l','a')),  'pla');
// /t/ não pode ser coda, então "atleta" só pode ser a·tle·ta: tl É ataque
t('atleta [ +a][t+LT e][t+a]', P(S('','a'),LIQ([0],'l','e'),S('t','a')), 'atleta');
t('atlas  [ +a][t+LT a coda-s]', P(S('','a'),LIQ([0],'l','a',{c:'s'})), 'atlas');
t('atlas  [ +a][t+LT a][s sem vogal]', P(S('','a'),LIQ([0],'l','a'),S('s','')), 'atlas');

grupo('roll acidental degrada para o gate simples');
t('só ↗ não re-grafa (c)',  P(R([1],'a')),        'ca');
t('roll s→← é ignorado',    P(R([2,6],'a')),      'sa');
t('roll s→↗ re-grafa',      P(R([2,1],'a')),      'ça');

grupo('regras automáticas não podem regredir');
t('que',      P(S('c','e')),                          'que');
t('guitarra', P(S('g','i'),S('t','a'),S('rr','a')),   'guitarra');
t('passo',    P(S('p','a'),S('s','o')),               'passo');
t('caro',     P(S('c','a'),S('r','o')),               'caro');
t('carro',    P(S('c','a'),S('rr','o')),              'carro');
t('trabalho', P(S('tr','a'),S('b','a'),S('lh','o')),  'trabalho');

grupo('roll: só primeiro, último e inversões de sentido');
t('ai  [4,5,6,7]', N([4,5,6,7]), 'ai');
t('oi  [2,1,0,7]', N([2,1,0,7]), 'oi');
t('eu  [6,7,0,1]', N([6,7,0,1]), 'eu');
t('ou  [2,1]',     N([2,1]),     'ou');

console.log(`\n${ok} ok, ${bad} falha(s)`);
process.exit(bad ? 1 : 0);
