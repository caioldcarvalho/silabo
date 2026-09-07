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
| setas `→` / `←` | cicla o acento da última vogal |
| seta `↓` | alterna s ↔ z |

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

**Re-grafia: role o ataque até ↗.** O português não é fonêmico o bastante para
a grafia ser função do fonema — "sela"/"cela" e "sinto"/"cinto" são homófonos,
nenhuma regra decide. Então a escolha volta para o usuário, com **um fato motor
só**: *o som do gate em que você começou, escrito com a letra que mora em ↗.*

| gesto | sai | |
|---|---|---|
| `s`(→) roll ↗ | **c** / **ç** | cebola, cidade · ação, moço |
| `x`(↖) roll ↗ | **ch** | chave, chão — ch começa com o c de ↗ |
| `j` roll ↗ | **g** | gente, girafa — ↗ sonorizado *é* g |

Não é uma tabela de pares: os três terminam no mesmo gate. E `s`, `x`, `j` são
exatamente os ataques que **não admitem líquida** em português, então a re-grafia
nunca colide com cluster. Roll para qualquer outro destino é ignorado — desleixo
degrada para o gate simples, não vira erro.

`c` vs `ç` **não é um segundo endereço**: ⟨ç⟩ nunca ocorre antes de e/i, então a
vogal decide. Ver [`docs/ACHADOS.md`](docs/ACHADOS.md).

**D-pad — pós-correção** sobre o que já está escrito. O acento **cicla** na
última vogal em vez de ser aplicado: `→` avança, `←` volta, e cada vogal só
oferece os acentos que ela aceita.

```
a → á → â → ã → à → a        e → é → ê → e        o → ó → ô → õ → o
```

Sempre reversível e sempre volta à vogal nua — com três botões de disparo único
não havia como desfazer: apertar de novo caminhava para a vogal *anterior* e
acentuava aquela. `↓` alterna `s`↔`z` na última sibilante intervocálica
(faser → fazer). `↑` está livre.

`é` e `ó` têm gate próprio e saem acentuados como o rótulo promete — o d-pad
cobre o resto. Medido: **10,7% dos tokens** carregam vogal acentuada, caro
demais para ser tudo pós-correção.

**Telemetria.** A sessão é registrada: cada sílaba com o gesto que a produziu, as
palavras fechadas, os apagamentos (o sinal honesto de que algo não saiu certo) e
os **conflitos, que o motor detecta sozinho** — quando um modificador é
apertado mas seu efeito é descartado, como a líquida que some porque RB está
segurado. O contador fica no painel de medição e o log baixa em JSON. Sobrevive
a recarga; nada sai da máquina.

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

A interface mostra, ao redor da casa em que o analógico está, **o que cada
modificador faria e qual gatilho o produz** — e o gatilho muda entre as
variantes (na A a líquida é botão, na B é movimento), que é justamente a parte
confusa de aprender. Clusters que o português não admite não são oferecidos.

No topo há um **desenho do controle** que acende em vermelho o que está sendo
pressionado, com os analógicos defletindo de verdade e oito pontos ao redor de
cada base marcando os gates **visitados** — o de origem em branco, os do roll em
vermelho, porque um roll é uma sequência e uma posição só não a mostra. Serve à
gravação de tela: quem assiste vê o polegar e os botões no mesmo quadro que o
texto que saiu.

Para conferir o desenho sem abrir o navegador (precisa de `rsvg-convert`):

```fish
node tools/pad-preview.mjs
```

Duas suítes de regressão, sem dependências:

```fish
node test/motor.test.mjs   # 50 casos — sílaba e ortografia
node test/ui.test.mjs      # 14 casos — o que os satélites e o HUD geram
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
