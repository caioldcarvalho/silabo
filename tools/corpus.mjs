// Mede grafias contra corpus real de português brasileiro, ponderado por
// frequência de TOKEN (que é o que um input method paga), não de forma.
// Usa OpenSubtitles pt_BR 50k (hermitdave/FrequencyWords), baixado sob demanda.
//   node tools/corpus.mjs
import {readFileSync, existsSync, writeFileSync} from 'fs';
import {join, dirname} from 'path';
import {fileURLToPath} from 'url';

const cache = join(dirname(fileURLToPath(import.meta.url)), '.pt_br_50k.txt');
if(!existsSync(cache)){
  const url='https://raw.githubusercontent.com/hermitdave/FrequencyWords/master/content/2018/pt_br/pt_br_50k.txt';
  process.stderr.write('baixando corpus...\n');
  writeFileSync(cache, await (await fetch(url)).text());
}
const V='aeiouáéíóúâêôàãõ';
const linhas = readFileSync(cache,'utf8').trim().split('\n')
  .map(l=>{const [w,n]=l.split(' '); return [w,+n];})
  .filter(([w,n])=>w&&n&&/^[a-zà-ÿç]+$/i.test(w));
const total = linhas.reduce((a,[,n])=>a+n,0);
const pct = n => (100*n/total).toFixed(3)+'%';
const conta = re => { let tok=0, tipos=0, ex=[];
  for(const [w,n] of linhas){ const m=w.match(re); if(m){ tok+=n*m.length; tipos++; if(ex.length<6) ex.push(w); } }
  return {tok,tipos,ex}; };

console.log(`corpus: ${linhas.length.toLocaleString()} formas, ${total.toLocaleString()} tokens\n`);

console.log('/z/ intervocálico — qual grafia é o default certo?');
const sz=conta(new RegExp(`[${V}]s[${V}]`,'gi')), zz=conta(new RegExp(`[${V}]z[${V}]`,'gi'));
console.log(`  ⟨s⟩  ${pct(sz.tok).padStart(8)}  ${sz.ex.join(', ')}`);
console.log(`  ⟨z⟩  ${pct(zz.tok).padStart(8)}  ${zz.ex.join(', ')}`);
console.log(`  → ⟨s⟩ é ${(sz.tok/zz.tok).toFixed(2)}× mais frequente\n`);

console.log('ç antes de e,i — existe? (se não, c/ç é regra, não endereço)');
const cf=conta(/ç[eiéíê]/gi);
console.log(`  ${cf.tipos} formas: ${cf.ex.join(', ')||'nenhuma'}`);
console.log('  INSPECIONE as formas antes de concluir: o corpus é de legenda');
console.log('  amadora e contém erros de grafia ("voçê", "começe").\n');

console.log('custo de cada endereço (tokens que dependem dele):');
for(const [nome,re] of [['s+r → c   /s/ antes de e,i', /c[ei]/gi],
                        ['s+l → ç', new RegExp(`ç[${V}]`,'gi')],
                        ['j+r → g   /ʒ/ antes de e,i', /g[ei]/gi],
                        ['z+l → s   /z/ intervocálico', new RegExp(`[${V}]s[${V}]`,'gi')]])
  console.log(`  ${nome.padEnd(30)} ${pct(conta(re).tok).padStart(8)}`);
