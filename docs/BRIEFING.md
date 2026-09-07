# Sílabo — briefing do projeto

Documento de handoff. Contém todo o contexto necessário; não depende de nenhuma
conversa anterior.

---

## 1. O que é

Um **input method dirigido por controle de videogame** (Xbox / genérico) que
digita **sílabas inteiras por gesto**, não caracteres.

Overlay na tela mostra duas rodas. O analógico esquerdo escolhe o **ataque**
(consoante inicial), o direito escolhe o **núcleo** (vogal), **simultaneamente**.
Um gatilho confirma a sílaba.

Alvo: Windows primeiro, Linux se possível. Protótipo atual roda no navegador
via Gamepad API.

### Natureza do projeto

Experimento e curiosidade — não há pretensão de substituir teclado nem de
adoção em massa. **Mas** há interesse real em que o resultado possa ser útil
para uma comunidade mais ampla no futuro, então decisões que fechem portas de
internacionalização ou de acessibilidade devem ser evitadas.

Inspiração: **8vim** (teclado de celular herdeiro do 8pen), onde a letra não é
uma posição e sim um caminho — setor de origem, direção da rotação, quantos
limites de setor foram cruzados. As propriedades do 8vim que queremos preservar:
gesto contínuo sem confirmação discreta, uso eyes-free depois de treinado,
memória muscular de movimento em vez de posição.

---

## 2. Alternativas avaliadas e descartadas

Registradas para não serem re-propostas.

| Ideia | Status | Motivo |
|---|---|---|
| Menu radial hierárquico (quadrante dentro de quadrante) | baseline, não destino | Fácil de aprender, mas é o oposto filosófico do 8vim |
| Reconhecimento de traçado ($1 recognizer, DTW) | **fora** | Decisão explícita: não queremos classificador de desenho |
| Estenografia (acordes → palavra via dicionário) | **fora** | Depende de dicionário; internacionalização inviável |
| Braille / acorde por letra | **fora** | Pega o pior dos dois mundos: custo cognitivo do acorde sem o ganho de compressão. QWERTY sobrevive porque 10 dedos dividem o alcance; acorde só se paga se comprimir sílaba ou palavra |
| Roda de cofre (catraca com detentes hápticos) | **fora** | O(n) em vez de O(1); é um 8vim degenerado |
| Física / bolinha com momentum | **fora** | Momentum é exatamente o que não se quer num input method |
| Morse / ritmo em gatilho analógico | **fora** | "abacate" = 16 pulsos vs. 4 gestos no silábico |
| Anéis de magnitude (deflexão parcial) | adiado | Exige precisão analógica fina; é parâmetro, não conceito |
| Layout adaptativo por n-grama | v2, com ressalva | Só pode mudar **destaque visual e hitbox**, nunca a posição — mover letra destrói a memória muscular |
| Acordes dedicados para palavras frequentes | v2, camada opcional | Ok como camada por cima do motor composicional, vazia por padrão. Nunca como dependência estrutural |

---

## 3. Decisões de projeto já fechadas

**Silábico composicional, não lookup.** `B + a` = "ba" por regra, sempre. Sem
dicionário, sem ambiguidade, sem conflito. Modelo mental é o Hangul, não a
estenografia.

**Segmentação livre é feature, não bug.** `quis.er` e `qui.ser` produzem a mesma
string. Não existe segmentação errada. Consequências:
- Não há estado de erro; o usuário nunca trava tentando lembrar como a palavra
  se divide.
- Não é preciso silabificador nem validação fonotática.
- Estrangeirismos e nomes próprios saem em pedaços arbitrários.
- **O ortografador precisa operar sobre o buffer da palavra, não sobre a
  sílaba.** `ca.mpo` e `cam.po` precisam produzir "campo".

**Ditongo é movimento de vogal.** Roll entre gates fixos no analógico direito:
`a→i` = "ai". Isso **não** é reconhecimento de traçado — são dois estados
discretos e uma ordem entre eles, sem tolerância nem classificador. É a mesma
lógica de cruzamento de setor do 8vim. Escala para tritongo ("Uruguai").

**Zero precisão analógica.** Tudo é deflexão total até o gate; o octógono do
Xbox já entrega isso. Nada de segurar meia deflexão.

**Fonologia arranja, ortografia rotula.** O layout das vogais segue o trapézio
vocálico do IPA porque isso dá coerência interna ao mapa (frente à esquerda,
fundo à direita, altura em cima) — mas **nada disso aparece na interface**.
O usuário lê `i u e o é ó a`, que são letras que qualquer alfabetizado
reconhece. Terminologia fonológica nunca aparece na tela. Onde houver dúvida,
usar palavra-exemplo (`é (pé)`, `o (bolo)`), nunca o nome da distinção.

**Sem timeout, sem timing.** Commit é por gatilho. Isso faz o gesto do iniciante
e o do expert serem **o mesmo gesto**, só comprimido no tempo:
- iniciante: empurra, lê o overlay, empurra o outro, confirma (~2s)
- expert: os dois polegares saem juntos, o overlay nem renderiza (~150ms)

Não existe "modo iniciante" a ser abandonado — é onde quase todo método
alternativo morre.

---

## 4. Restrição física do controle

O polegar direito não pode estar no analógico direito **e** em A/B/X/Y ao mesmo
tempo. Mesma coisa à esquerda com analógico e d-pad.

Orçamento real de simultaneidade durante uma sílaba:
- analógico esquerdo (direção)
- analógico direito (direção)
- **LB, LT, RB, RT** — quatro dedos independentes

Botões de face e d-pad só servem com os analógicos em repouso: modo, pontuação,
correção.

**O usuário não gosta de L3/R3 (clique dos analógicos) e prefere substituí-los.**

---

## 5. Layout

### Analógico esquerdo — ataque

| Direção | Base | Sonoro |
|---|---|---|
| ↑ | t | d |
| → | s | z |
| ↓ | m | n |
| ← | l | r |
| ↗ | c | g |
| ↘ | p | b |
| ↙ | f | v |
| ↖ | x | j |

Cardinais carregam as consoantes mais frequentes do português.
**Analógico em repouso = ataque zero** (sílaba iniciada por vogal).

Líquidas compõem clusters: `p+r` = "pr", `c+l` = "cl".
Onde o cluster é **ilegal em português**, o slot está vago e é reaproveitado:
- `n+r` → **nh**
- `l+r` → **lh**
- `r+r` → **rr**

Nenhuma regra nova, só aproveitamento de buraco fonotático.

### Analógico direito — núcleo

Arranjo = trapézio vocálico (frente/altura), rótulo = ortografia comum.

```
      —
   i     u
   e     o
   é     ó
      a
```

- ↖ i · ↗ u · ← e · → o · ↙ é · ↘ ó · ↓ a
- ↑ `—` = sem vogal (consoante solta, sigla, código)
- Nasalidade: `a→ã`, `o→õ`; nas demais o motor decide `n` ou `m` pelo contexto

Roll = ditongo. `a→i` = "ai", `o→u` = "ou".

### Coda

- `RB` → **-s** (plural, o mais frequente de longe)
- `RB + LB` → **-r** (todos os infinitivos)
- `RB + LT` → **-l**

`LB` significa "r" e `LT` significa "l" em qualquer posição — uma regra, dois
lugares.

### Fora da sílaba (analógicos em repouso)

- A = espaço · B = backspace (segurar = apaga palavra)
- X = maiúscula na próxima · Y = modo letra-a-letra (senha, código)
- D-pad = acento sobre a última vogal: ↑ circunflexo · → agudo · ← crase · ↓ cedilha
- LB/LT + d-pad = pontuação
- Entrar/sair do modo: L3+R3 juntos com os analógicos em repouso (não colide com
  nada, porque durante a digitação os analógicos estão deflexionados)

---

## 6. A pergunta em aberto — o objeto do teste atual

**Como o ataque compõe com a líquida?** Duas variantes implementadas, a decidir
empiricamente.

### Variante A — modificador dedicado
- L3 = sonoriza (t→d, s→z, c→g)
- LB = +r · LT = +l
- R3 = nasaliza
- RB = coda (com LB/LT qualificando o tipo)

Vantagem hipotética: mais rápido no caso mais frequente.
Custo: usa L3 e R3, que o usuário detesta. `LB` tem função dupla (ataque +r
quando RB solto, coda -r quando RB segurado) — pode ser ambíguo na mão.

### Variante B — roll no ataque (mesma regra do núcleo)
- roll do analógico esquerdo: segundo gate lido no eixo das líquidas
  (`←` = +l, `→` = +r)
- **LB = sonoriza · LT = nasaliza**
- RB = coda

Vantagem: os dois analógicos passam a seguir **uma única regra** ("deslizar
compõe"), o que é mais simples de ensinar. E **B libera LB/LT o suficiente para
não usar L3 nem R3 em lugar nenhum** — se vencer, mata o incômodo de vez.

Custo: `←` significa "l" na primeira posição e "+l" na segunda. É o único ponto
conceitualmente sujo do desenho. Palavras como "programa" exigem roll dos dois
lados simultaneamente.

### O que medir

- caracteres por gesto
- ms por caractere após N sessões
- **taxa de erro por posição de slot** (revela diagonal mal alocada)
- tempo específico de palavras com cluster nos dois lados ("programa", "brincar")

---

## 7. Exemplos rodando

```
abacate  → 4 gestos, 7 caracteres
  1. L repouso        R ↓(a)          →  a
  2. L ↘ sonoro(b)    R ↓(a)          →  ba
  3. L ↗(c)           R ↓(a)          →  ca
  4. L ↑(t)           R ←(e)          →  te

cantar   → 2 gestos, 6 caracteres
  1. L ↗(c)           R ↓ nasal(ã)    →  can
  2. L ↑(t) +coda -r  R ↓(a)          →  tar

programa → 3 gestos, 8 caracteres
  1. L ↘(p) +r        R →(o)          →  pro
  2. L ↗ sonoro(g) +r R ↓(a)          →  gra
  3. L ↓(m)           R ↓(a)          →  ma
```

Média entre 1.8 e 2.5 caracteres por gesto, sem dicionário e sem predição.
O ganho é estrutural, não vem de heurística.

---

## 8. Estado do código

`index.html` (ou `silabo.html`) — protótipo funcional, arquivo único,
Gamepad API, sem dependências. Contém:
- as duas variantes com botão de troca
- detecção de gate com histerese (entra em 0.55, sai em 0.38)
- roll com reset ao voltar ao centro
- duas rodas em SVG com destaque de gate ativo e de sequência de roll
- ortografador operando sobre o buffer da palavra
- painel de medição (gestos, caracteres, caracteres/gesto, ms/caractere)
- fallback de teclado: `WASD`+`QEZC` (esq.), `IJKL`+`UOM.` (dir.),
  `Espaço` confirma, `1`=LB `2`=LT `3`=RB `4`=L3 `5`=R3

### Regras de ortografia já implementadas
- `c` e `g` antes de vogal anterior → `qu`, `gu`
- `/s/` e `/R/` intervocálicos dobram → `ss`, `rr`
- nasal: `ã`, `õ` com til; demais viram `n`, com `n → m` antes de `p`/`b`

### Lacunas conhecidas (deliberadamente explícitas, não meio-resolvidas)
- `ç` não existe — "ação" sai "assão"
- `j`/`g` antes de e/i sempre sai `j`
- `ê`/`ô`/`â` sem entrada; iriam no d-pad como pós-correção
- `x` vs `ch` é decisão não tomada

Nenhuma dessas impede medir o que importa agora.

---

## 9. Arquitetura alvo

Separação inegociável: **um resolvedor de fonemas puro** e **um ortografador**.
Trocar de língua deve significar trocar só o segundo.

```
src/
  phonology.js      montagem da sílaba a partir dos gates (puro, testável, sem UI)
  orthography-pt.js grafia do português sobre o buffer da palavra
  wheel.js          overlay SVG
  input.js          leitura do gamepad, gates, histerese, roll
  metrics.js        instrumentação
```

Nativo, depois:
- **Windows**: XInput ou SDL2 para ler; overlay como janela layered
  (`WS_EX_LAYERED | WS_EX_TRANSPARENT`); injeção via `SendInput`
- **Linux**: evdev para ler, **uinput** para injetar (parte limpa). Overlay é a
  dor: X11 é fácil, Wayland exige `layer-shell` e varia por compositor
- **Cross-platform num código só**: Rust com `gilrs` + `egui`/`winit`

Overlay: revelar a roda após ~120ms de deflexão, com fade rápido. Quem já sabe
nunca chega a ver.

---

## 10. Contexto do autor

Bacharel em Letras/Linguística com ênfase forte em fonética e fonologia —
terminologia técnica (ataque, coda, núcleo, ditongo, vogal alta, fonotática)
pode ser usada livremente na discussão. Engenheiro de software, mão na massa.
Comunicação em português brasileiro.
