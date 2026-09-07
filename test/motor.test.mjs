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
  'return {orthograph, finish, landmarks, NUCLEUS_BY_GATE, GLIDE};'
)({now:()=>0},{getElementById:()=>({})});

const S = (o,v,n=false,c='') => ({onset:o, vowel:v, nasal:n, coda:c});
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
t('ações',   P(S('','a'),S('sl','oi',true,'s')),      'ações');

grupo('grafia por cluster fonotaticamente ilegal');
t('cebola',  P(S('sr','e'),S('b','o'),S('l','a')),    'cebola');
t('cidade',  P(S('sr','i'),S('d','a'),S('d','e')),    'cidade');
t('caçar',   P(S('c','a'),S('sl','a',false,'r')),     'caçar');
t('moço',    P(S('m','o'),S('sl','o')),               'moço');
t('gente',   P(S('jr','e',true),S('t','e')),          'gente');
t('girafa',  P(S('jr','i'),S('r','a'),S('f','a')),    'girafa');
t('casa',    P(S('c','a'),S('zl','a')),               'casa');
t('mesa',    P(S('m','e'),S('zl','a')),               'mesa');

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
