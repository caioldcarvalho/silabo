// Renderiza o desenho do controle em PNG, headless, pra poder OLHAR o resultado
// em vez de confiar que compilou. Precisa de rsvg-convert (librsvg).
//   node tools/pad-preview.mjs        → pad_repouso.png e pad_silaba.png
// Nota: librsvg não resolve var(--x), então as cores são substituídas aqui.
import {readFileSync, writeFileSync} from 'fs';
import {execSync} from 'child_process';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';
const raiz = join(dirname(fileURLToPath(import.meta.url)),'..');
const js = readFileSync(join(raiz,'index.html'),'utf8').split('<script>')[1].split('</script>')[0];
const corpo = js.split('// ------------------------------------------------------------- boot')[0]
                .split('// ---------------------------------------------------------------- boot')[0];
export function shot(nome, estado){
  const nos={};
  const doc={getElementById:id=>(nos[id]||={innerHTML:'',textContent:'',setAttribute(k,v){this[k]=v},classList:{toggle(){}},style:{}})};
  const M=new Function('performance','document','addEventListener','navigator','requestAnimationFrame','localStorage','setTimeout','Blob','URL',
    corpo+'return {drawPad,paintPad,btn,stick,held,axesNow};')
    ({now:()=>0},doc,()=>{},{getGamepads:()=>[],userAgent:'t'},()=>{},
     {getItem:()=>null,setItem(){},removeItem(){}},fn=>fn(),function(){},{createObjectURL:()=>'',revokeObjectURL(){}});
  M.drawPad(); estado(M); M.paintPad();
  let svg = nos.padsvg.innerHTML;
  for(const [id,n] of Object.entries(nos)){
    if(!/^[pt]-/.test(id)) continue;
    for(const attr of ['fill','cx','cy','r']){
      if(n[attr]===undefined) continue;
      const esc = id.replace(/-/g,'\\-');
      const re = new RegExp('(id="'+esc+'"[^>]*?)'+attr+'="[^"]*"');
      svg = re.test(svg) ? svg.replace(re,'$1'+attr+'="'+n[attr]+'"')
                         : svg.replace(new RegExp('(id="'+esc+'")'),'$1 '+attr+'="'+n[attr]+'"');
    }
  }
  const cores={'var(--bg)':'#151920','var(--panel)':'#1D232C','var(--line)':'#2C333E',
               'var(--ink)':'#E6E9ED','var(--muted)':'#87919F','var(--hot)':'#F2603C'};
  for(const [k,v] of Object.entries(cores)) svg = svg.split(k).join(v);
  writeFileSync(nome+'.svg',`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 440 250" width="620" height="352"><rect width="440" height="250" fill="#1D232C"/>${svg}</svg>`);
  execSync(`rsvg-convert -o ${nome}.png ${nome}.svg`);
}
import {readFileSync, writeFileSync} from 'fs';
import {execSync} from 'child_process';
import {fileURLToPath} from 'url';
import {dirname, join} from 'path';
const raiz = join(dirname(fileURLToPath(import.meta.url)),'..');
const js = readFileSync(join(raiz,'index.html'),'utf8').split('<script>')[1].split('</script>')[0];
const corpo = js.split('// ------------------------------------------------------------- boot')[0]
                .split('// ---------------------------------------------------------------- boot')[0];
export function shot(nome, estado){
  const nos={};
  const doc={getElementById:id=>(nos[id]||={innerHTML:'',textContent:'',setAttribute(k,v){this[k]=v},classList:{toggle(){}},style:{}})};
  const M=new Function('performance','document','addEventListener','navigator','requestAnimationFrame','localStorage','setTimeout','Blob','URL',
    corpo+'return {drawPad,paintPad,btn,stick,held,axesNow};')
    ({now:()=>0},doc,()=>{},{getGamepads:()=>[],userAgent:'t'},()=>{},
     {getItem:()=>null,setItem(){},removeItem(){}},fn=>fn(),function(){},{createObjectURL:()=>'',revokeObjectURL(){}});
  M.drawPad(); estado(M); M.paintPad();
  let svg = nos.padsvg.innerHTML;
  for(const [id,n] of Object.entries(nos)){
    if(!/^[pt]-/.test(id)) continue;
    for(const attr of ['fill','cx','cy','r']){
      if(n[attr]===undefined) continue;
      const esc = id.replace(/-/g,'\\-');
      const re = new RegExp('(id="'+esc+'"[^>]*?)'+attr+'="[^"]*"');
      svg = re.test(svg) ? svg.replace(re,'$1'+attr+'="'+n[attr]+'"')
                         : svg.replace(new RegExp('(id="'+esc+'")'),'$1 '+attr+'="'+n[attr]+'"');
    }
  }
  const cores={'var(--bg)':'#151920','var(--panel)':'#1D232C','var(--line)':'#2C333E',
               'var(--ink)':'#E6E9ED','var(--muted)':'#87919F','var(--hot)':'#F2603C'};
  for(const [k,v] of Object.entries(cores)) svg = svg.split(k).join(v);
  writeFileSync(nome+'.svg',`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 440 250" width="620" height="352"><rect width="440" height="250" fill="#1D232C"/>${svg}</svg>`);
  execSync(`rsvg-convert -o ${nome}.png ${nome}.svg`);
}

shot('pad_repouso', () => {});
shot('pad_silaba', M => {
  Object.assign(M.btn,{LB:1,LT:0,RB:1,RT:1,L3:1,R3:0});
  M.stick.L.gates=[2,1]; M.stick.R.gates=[4,7];
  M.axesNow.L=[0.7,-0.7]; M.axesNow.R=[-0.7,-0.7];
});
console.log('pad_repouso.png e pad_silaba.png');
