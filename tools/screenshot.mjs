// Tira screenshot da página, opcionalmente com uma sessão simulada, pra poder
// OLHAR o layout em vez de supor. Precisa de chromium.
//   node tools/screenshot.mjs            → vazio.png
//   node tools/screenshot.mjs --uso      → uso.png, com texto e sílaba no ar
//   node tools/screenshot.mjs --medir    → imprime as caixas e a altura da página
import {readFileSync, writeFileSync, unlinkSync} from 'fs';
import {execSync} from 'child_process';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';

const raiz = join(dirname(fileURLToPath(import.meta.url)), '..');
const tmp  = join(raiz, '.screenshot.html');
const arg  = process.argv.slice(2);
const VP   = '1440,900';

// Um estado plausível de meio de digitação. Reaplicado num intervalo porque o
// requestAnimationFrame do poll sobrescreve os botões a cada quadro.
const SIM = `<script>addEventListener('load',()=>{
  const fix = () => {
    text = ALVO.slice(0, 92);
    word = 'ca';
    stick.L.gates=[2,1]; stick.R.gates=[4];
    btn.LB=1; held.A=1; axesNow.L=[0.75,-0.62]; axesNow.R=[0,0.95];
    satellites('l'); satellites('r');
    paintWheel('l',axesNow.L); paintWheel('r',axesNow.R);
    renderHud(); render();
  };
  setInterval(fix,30); setTimeout(fix,400);
});</script>`;

const MEDIR = `<script>addEventListener('load',()=>{setTimeout(()=>{
  const g = s => { const e=document.querySelector(s); if(!e) return null;
    const b=e.getBoundingClientRect();
    return Math.round(b.width)+'x'+Math.round(b.height)+' @y'+Math.round(b.top+scrollY); };
  document.title = 'MEDIDAS ' + JSON.stringify({hud:g('#hud'), trilha:g('#trilhabox'),
    pad:g('#padsvg'), readout:g('.readout'), stage:g('.stage'), grid:g('.grid'),
    fimDasRodas: Math.round(document.querySelector('.stage').getBoundingClientRect().bottom+scrollY),
    pagina: document.documentElement.scrollHeight}, null, 1);
},500);});</script>`;

const html = readFileSync(join(raiz,'index.html'),'utf8');
const extra = arg.includes('--medir') ? MEDIR : arg.includes('--uso') ? SIM : '';
writeFileSync(tmp, html.replace('</body>', extra + '</body>'));

try {
  if (arg.includes('--medir')) {
    const dom = execSync(`chromium --headless --disable-gpu --no-sandbox --window-size=${VP} `
      + `--virtual-time-budget=4000 --dump-dom file://${tmp} 2>/dev/null`, {maxBuffer:1e8}).toString();
    const m = dom.match(/<title>MEDIDAS ([\s\S]*?)<\/title>/);
    console.log(m ? m[1] : 'não capturou');
    console.log('\nviewport útil a 900px de altura ≈ 830px');
  } else {
    const nome = arg.includes('--uso') ? 'uso.png' : 'vazio.png';
    execSync(`chromium --headless --disable-gpu --no-sandbox --hide-scrollbars `
      + `--window-size=${VP} --screenshot=${nome} --virtual-time-budget=4000 file://${tmp} 2>/dev/null`);
    console.log(nome);
  }
} finally { try{ unlinkSync(tmp); }catch(e){} }
