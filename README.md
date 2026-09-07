# Sílabo

Um **input method dirigido por controle de videogame** que digita **sílabas
inteiras por gesto**, não caracteres.

Duas rodas na tela. O analógico esquerdo escolhe o **ataque** (consoante
inicial), o direito escolhe o **núcleo** (vogal), ao mesmo tempo. Um gatilho
confirma a sílaba.

```
abacate  → 4 gestos, 7 caracteres
programa → 3 gestos, 8 caracteres
cantar   → 2 gestos, 6 caracteres
```

Média entre 1.8 e 2.5 caracteres por gesto — **sem dicionário e sem predição**.
O ganho é estrutural, não vem de heurística.

Inspiração declarada: [8vim](https://8vim.com/) (herdeiro do 8pen), onde a letra
não é uma posição e sim um caminho. As propriedades que queremos preservar:
gesto contínuo, uso eyes-free depois de treinado, memória muscular de movimento
em vez de posição.

> Experimento e curiosidade — sem pretensão de substituir teclado. Mas decisões
> que fechem portas de internacionalização ou acessibilidade são evitadas de
> propósito.

## Rodar o protótipo

Arquivo único, sem dependências, sem build:

```fish
xdg-open index.html          # ou abra no navegador na mão
```

Plugue um controle (Xbox / genérico) e aperte um botão para a Gamepad API
enxergá-lo. Sem controle, dá para testar pelo teclado:

| Teclas | Função |
|---|---|
| `W A S D` + `Q E Z C` | analógico esquerdo (ataque) |
| `I J K L` + `U O M .` | analógico direito (núcleo) |
| `Espaço` | confirma a sílaba (RT) |
| `1` `2` `3` | LB, LT, RB |
| `4` `5` | L3, R3 |
| `Enter` | fecha a palavra |
| `Backspace` | apaga |
| `Shift+.` / `Shift+/` | ponto final / interrogação |

No controle, fora da sílaba (analógicos em repouso): **A** = espaço, **A+RB** =
enter, **B** = backspace (segurar apaga a palavra), **X** = ponto final,
**Y** = interrogação.

## O layout

**Ataque** (analógico esquerdo) — cardinais carregam as consoantes mais
frequentes do português; o modificador de sonoridade dá o par sonoro:

| | ↑ | → | ↓ | ← | ↗ | ↘ | ↙ | ↖ |
|---|---|---|---|---|---|---|---|---|
| base | t | s | m | l | c | p | f | x |
| sonoro | d | z | n | r | g | b | v | j |

Analógico em repouso = ataque zero (sílaba iniciada por vogal). Líquidas
compõem clusters (`p+r` = "pr"); onde o cluster é ilegal em português o slot é
reaproveitado: `n+r` → **nh**, `l+r` → **lh**, `r+r` → **rr**.

**Núcleo** (analógico direito) — arranjo segue o trapézio vocálico do IPA
(frente à esquerda, fundo à direita, altura em cima), mas o rótulo na tela é só
ortografia comum:

```
      —          ↑  sem vogal (consoante solta, sigla)
   i     u
   e     o       roll entre gates = ditongo:  a→i = "ai",  o→u = "ou"
   é     ó
      a
```

**Grafia por cluster ilegal** — combinações que o português proíbe ficam vagas,
e as vagas viram as grafias que faltavam. Sem botão novo, sem modo, sem
dicionário:

| gesto | grafia | exemplo |
|---|---|---|
| `s` + `r` | **c** | cebola, cidade |
| `s` + `l` | **ç** | ação, moço |
| `j` + `r` | **g** | gente, girafa |
| `z` + `l` | **s** | casa, mesa |

Das 32 combinações ataque×líquida só 13 são clusters reais; ainda sobram 11
buracos livres. Ver [`docs/ACHADOS.md`](docs/ACHADOS.md).

**Coda** — `RB` = -s (plural) · `RB+LB` = -r (infinitivos) · `RB+LT` = -l.
`LB` significa "r" e `LT` significa "l" em qualquer posição: uma regra, dois
lugares.

## A pergunta em aberto

**Como o ataque compõe com a líquida?** As duas variantes estão implementadas
lado a lado, com botão de troca, justamente para decidir isso por medição:

- **A — modificador dedicado.** L3 sonoriza, LB = +r, LT = +l, R3 nasaliza.
  Mais rápido no caso frequente, mas usa L3/R3 (que o autor detesta) e dá
  função dupla ao LB.
- **B — roll no ataque.** O segundo gate do analógico esquerdo lê a líquida
  (`←` = +l, `→` = +r), LB sonoriza, LT nasaliza. Os dois analógicos passam a
  seguir **uma única regra** ("deslizar compõe") e L3/R3 somem do desenho —
  ao custo de `←` significar "l" na primeira posição e "+l" na segunda.

O painel de medição no protótipo conta gestos, caracteres, caracteres/gesto e
ms/caractere. O que interessa medir: taxa de erro **por posição de slot** (revela
diagonal mal alocada) e o tempo de palavras com cluster dos dois lados
("programa", "brincar").

## Estado

Protótipo funcional de navegador (`index.html`): as duas variantes, detecção de
gate com histerese (entra em 0.55, sai em 0.38), roll com reset ao centro, rodas
em SVG, ortografador operando **sobre o buffer da palavra** (é o que permite
`ca.mpo` e `cam.po` produzirem "campo") e painel de medição.

Suíte de regressão com 35 casos, sem dependências:

```fish
node test/motor.test.mjs
```

Lacunas conhecidas, deliberadamente explícitas em vez de meio-resolvidas:

- `ê`/`ô`/`â` e os acentos agudos sem entrada (vão no d-pad, como pós-correção)
- `x` vs `ch` é decisão não tomada
- tritongo (`Uruguai`, `quais`) — provavelmente pede labialização no ataque,
  não tritongo no núcleo
- plural de `-ão` é lexical (pães/mãos/ações), então é digitado sílaba a sílaba

Nenhuma delas impede medir o que importa agora.

## Para onde vai

Separação inegociável: **um resolvedor de fonemas puro** e **um ortografador**.
Trocar de língua deve significar trocar só o segundo.

```
src/
  phonology.js       montagem da sílaba a partir dos gates (puro, testável, sem UI)
  orthography-pt.js  grafia do português sobre o buffer da palavra
  wheel.js           overlay SVG
  input.js           gamepad, gates, histerese, roll
  metrics.js         instrumentação
```

Depois, nativo: Windows (XInput/SDL2, overlay `WS_EX_LAYERED | WS_EX_TRANSPARENT`,
injeção via `SendInput`) e Linux (evdev para ler, **uinput** para injetar; o
overlay é a parte dolorida — X11 é fácil, Wayland exige `layer-shell`).
Cross-platform num código só seria Rust com `gilrs` + `egui`/`winit`.

O contexto completo — inclusive as alternativas **avaliadas e descartadas**, com
o motivo de cada uma, para não serem re-propostas — está em
[`docs/BRIEFING.md`](docs/BRIEFING.md).

## Licença

MIT — ver [`LICENSE`](LICENSE).
