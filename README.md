<div align="center">

# Sílabo

**Um teclado que digita sílabas inteiras, com as duas mãos num controle de videogame.**

Os dois analógicos se movem ao mesmo tempo: o esquerdo escolhe a consoante,
o direito escolhe a vogal. Um gatilho fecha a sílaba.

[**▶ Abrir o protótipo**](https://caioldcarvalho.github.io/silabo/) · [Briefing](docs/BRIEFING.md) · [Achados](docs/ACHADOS.md)

</div>

---

<div align="center">

[![Sílabo em uso](docs/demo.gif)](docs/demo.mp4)

<sub>*Digitando ao vivo. [Vídeo completo com áudio](docs/demo.mp4) (1min).*</sub>

</div>

---

## A ideia

Um controle tem **dois polegares que se movem em paralelo**. Um teclado obriga
os dedos a irem um de cada vez. Se cada polegar carregar metade da sílaba, os
dois chegam juntos:

```
       analógico esquerdo            analógico direito
            ataque                        núcleo
              ↓                             ↓
              c            +                a          =    "ca"
                          ↑
                     um gesto só
```

```
programa   →  3 gestos, 8 caracteres
abacate    →  4 gestos, 7 caracteres
cantar     →  2 gestos, 6 caracteres
```

**Entre 1,8 e 2,5 caracteres por gesto — sem dicionário e sem predição.**
Medido em sessão real: **2,15**. O ganho é estrutural, não vem de heurística:
`B` + `a` é sempre "ba", por regra.

> Inspirado no [8vim](https://8vim.com/), onde a letra não é uma posição e sim
> um caminho. O que se quis preservar: gesto contínuo, uso *eyes-free* depois de
> treinado, e memória muscular de **movimento** em vez de posição.

---

## Rodar

Arquivo único, sem dependências, sem build. Abra
**[caioldcarvalho.github.io/silabo](https://caioldcarvalho.github.io/silabo/)**
ou o `index.html` local, plugue um controle e aperte um botão para o navegador
enxergá-lo.

Sem controle, dá para testar no teclado:

| teclas | função |
|---|---|
| `W A S D` + `Q E Z C` | analógico esquerdo — **ataque** |
| `I J K L` + `U O M .` | analógico direito — **núcleo** |
| `Espaço` | confirma a sílaba (RT) |
| `1` `2` `3` `4` `5` | LB, LT, RB, L3, R3 |
| `2` (LT) · `4` (L3) | sonoriza · **h** (com o analógico parado) |
| `Enter` · `Backspace` | fecha a palavra · apaga |
| `←` `→` · `↓` · `↑` | cicla acento · sibilante · caixa |
| `Alt`+setas (com `Ctrl`, por palavra) | move o cursor |
| `(` · `'` | parêntese · aspas (abrem e fecham sozinhos) |

---

## A frase-modelo

A página abre com um **arco de dificuldade** para treinar: nove fases, cada uma
entrando com um mecanismo e reusando todos os anteriores, então quem chega no fim
passou por tudo que o desenho sabe fazer.

| | mecanismo | |
|---|---|---|
| 1 | sílaba simples | *O sapo pula.* |
| 2 | vozeamento | *O gato do tio nada.* |
| 3 | ditongo | *Bia viu o pai e o boi.* |
| 4 | nasal | *A mãe canta um som bom.* |
| 5 | coda | *Os dois vão cantar até o sol sumir.* |
| 6 | cluster | *O prato, o livro e a flor grande ficam na sala.* |
| 7 | grafia | *A chuva molhou a gente da cidade.* |
| 8 | h e qu | *Hoje quatro pessoas quase caem.* |
| 9 | pós-correção | *Você já explicou a próxima "questão" (a de ontem) pra Bia?* |

A trilha compara **texto visível contra texto visível**, nunca gesto contra
gesto: `ca.mpo` e `cam.po` produzem "campo", então cobrar a divisão silábica
seria cobrar exatamente a coisa que o desenho declarou irrelevante. Divergir
**marca e não trava** — erro é anotação, não parede. E a fase é *função da
posição no texto*, não máquina de estados: não há "concluiu a fase", nada para
dessincronizar.

`test/frase.test.mjs` prova duas coisas diferentes: que o ortografador produz
exatamente esse texto a partir das sílabas, e que **cada um dos 25 ataques tem
um gesto real que o produz**. Sem a segunda, a primeira só diria que a string é
construível — não que uma mão a alcança.

---

## O layout

### Ataque — analógico esquerdo

As consoantes mais frequentes do português nas quatro cardinais; **LT**
sonoriza, dando o par:

| | ↑ | → | ↓ | ← | ↗ | ↘ | ↙ | ↖ |
|---|---|---|---|---|---|---|---|---|
| **base** | t | s | m | l | c | p | f | x |
| **+ LT** | d | z | n | r | g | b | v | j |

Analógico em repouso = **ataque zero** (sílaba que começa com vogal) — e
**repouso + `L3`** é o **h** mudo (*hoje*, *homem*, *história*), que não é som
nenhum e por isso mora justamente na combinação que o desenho descartava.

**Deslizar compõe.** Rolar o analógico até `→` acrescenta um **r**, até `←`
acrescenta um **l** — `p`→`→` dá "pr", `c`→`←` dá "cl". Onde o cluster é
proibido em português, o slot está vago e foi reaproveitado:

```
n → →  =  nh          l → →  =  lh          r → →  =  rr
```

### Núcleo — analógico direito

O arranjo segue o trapézio vocálico do IPA — frente à esquerda, fundo à direita,
altura em cima — mas a tela mostra só letras que qualquer alfabetizado
reconhece:

```
        —              ↑  sem vogal (consoante solta, sigla)
     i     u
     e     o           roll = ditongo:   a→i = "ai"   ·   o→u = "ou"
     é     ó
        a              R3 nasaliza:      a→ã   ·   o→õ
```

O roll lê **o gate de origem, o de destino e as inversões de sentido** — os
gates apenas atravessados no caminho são viagem, não intenção. É evento
discreto, sem cronômetro.

### Coda

Dois bits em `LB`/`RB`, ordenados por frequência de token — as codas orais do
português são exatamente três, e só a mais rara paga dois dedos:

| gesto | coda | tokens |
|---|---|---|
| `RB` | **-s** (plural) | 16,7% |
| `LB` | **-r** (todos os infinitivos) | 10,8% |
| `LB`+`RB` | **-l** | 3,1% |

A nasal não está aqui: ela é traço do **núcleo** e mora no `R3`, que é o que faz
*sons*, *alguns* e *monstro* saírem sem coda complexa nenhuma.

---

## A grafia

O português não é fonêmico o bastante para a escrita ser função do som: *sela* e
*cela*, *sinto* e *cinto* são homófonos. Onde nenhuma regra decide, a escolha
volta para quem digita — com **um fato motor só**:

> *O som do gate em que você começou, escrito com a letra que mora em ↗.*

O resto da grafia disputada é **ciclo no d-pad**, não endereço novo — porque
alógrafo não merece botão:

| d-pad | cicla |
|---|---|
| `↓` | a sibilante: `ss → s → z → x` — *isso, casa, fazer, próximo* |
| `→` `←` | o acento da última vogal, e sempre volta ao natural |
| `↑` | a caixa da última palavra: `caio → Caio → CAIO` |

### A segunda página do d-pad

Segure o **analógico direito em `↑`** e as quatro direções mudam de significado:

| d-pad | página 1 (analógico solto) | página 2 (direito em ↑) |
|---|---|---|
| `→` `←` | acento | cursor, uma letra |
| `↑` | caixa | cursor, uma palavra (esquerda) |
| `↓` | sibilante | cursor, uma palavra (direita) |

**Página 1 corrige o texto, página 2 navega nele.** O shift é um fato motor, não
uma tabela — e o gate `↑` é o certo por três motivos estruturais: é o único do
núcleo que **não escreve vogal**, então a tela não mostra nada pendente; o d-pad
e o analógico **esquerdo** são o mesmo polegar, então a combinação só é
alcançável com o ataque vazio e não colide com sílaba nenhuma **por anatomia**;
e estava morta — `dpad()` desistia a qualquer deflexão.

O cursor é uma **cauda** à direita: tudo que já existia continua colando no fim
do que está à esquerda dele, então passou a acontecer *na posição do cursor* de
graça. O ganho maior não é navegar — é que as três correções do `↓` `↑` `←→`
deixaram de alcançar só a última palavra e passaram a alcançar **o texto todo**.

O `↓` alcança as três posições em que o português deixa a escolha em aberto:
entre vogais, no fim da palavra e **na coda antes de consoante** — que é onde
moram *explicar*, *exceto* e *experiência*.

**A maiúscula de início de frase é regra**, não gesto: sai sozinha no começo do
texto e depois de `.`, `!` ou `?`. O `↑` serve o que nenhuma regra deriva —
nome próprio e sigla.

### Pontuação

`X` é a família inteira, e o qualificador diz qual:

| gesto | sai |
|---|---|
| `X` | `.` |
| `X`+`RB` | `,` |
| `X`+`LB` | `(` ou `)` — pelo saldo de parênteses abertos |
| `X`+`LT` | `"` — pela paridade |
| `Y` · `Y`+`RB` | `?` · `!` |

Um botão serve o **par inteiro**: qual dos dois é a vez sai do texto que já
está escrito, então não há modo pra lembrar nem estado pra dessincronizar. E o
espaço vai no lugar certo sozinho — o que abre cola na palavra seguinte, o que
fecha cola na anterior.

| gesto | sai | |
|---|---|---|
| `s` roll ↗ | **c** / **ç** | cebola, cidade · ação, moço |
| `x` roll ↗ | **ch** | chave, chão |
| `j` roll ↗ | **g** | gente, girafa |

Não é uma tabela de pares: os três terminam no mesmo gate. E `s`, `x`, `j` são
exatamente as consoantes que **não aceitam líquida** em português — então o slot
de roll delas estava livre, e a re-grafia nunca colide com um cluster.

O resto o motor deduz sozinho, sobre **o buffer da palavra inteira**: `c`→`qu`
antes de e/i, `s`→`ss` entre vogais, o `n` nasal virando `m` antes de p/b,
`ãu`→`ão`, `ãe`, `õe`.

**D-pad — o que é lexical e nenhuma regra resolve:**

| | |
|---|---|
| `→` / `←` | cicla o acento da última vogal · `a á â ã à` · `e é ê` · `o ó ô õ` |
| `↓` | cicla a sibilante · `ss → s → z` |

O ciclo é reversível por construção e sempre volta ao começo.

---

## Por que segmentar não tem como errar

`ca.mpo` e `cam.po` produzem **"campo"** — as duas. Não existe divisão errada,
porque o ortografador trabalha sobre a palavra, não sobre a sílaba. Consequência
prática: **não há estado de erro**. Ninguém trava tentando lembrar onde a
palavra se separa, e nomes próprios e estrangeirismos saem em pedaços
arbitrários sem problema nenhum.

## Por que não existe "modo iniciante"

O commit é por gatilho — **sem timeout, sem cronômetro**. O gesto do novato e o
do experiente são **o mesmo gesto**, só comprimido no tempo:

- **iniciante** — empurra, lê o overlay, empurra o outro, confirma *(~2s)*
- **experiente** — os dois polegares saem juntos, o overlay nem renderiza *(~150ms)*

Não há nada a desaprender depois. É onde quase todo método alternativo morre.

---

## O que a interface mostra

**O desenho do controle** acende em vermelho o que está pressionado, com os
analógicos defletindo de verdade e oito pontos ao redor de cada base marcando os
gates visitados — o de origem em branco, os do roll em vermelho.

**Os satélites** aparecem em volta da casa em que o analógico está, dizendo o
que cada modificador faria **e qual gatilho o produz**. Clusters que o português
não admite não são oferecidos.

**A telemetria** registra a sessão: cada sílaba com o gesto que a produziu, as
palavras, os apagamentos, e os conflitos — que o motor detecta sozinho, quando
um modificador é apertado e seu efeito descartado. Baixa em JSON pelo painel de
medição, sobrevive a recarga, e **nada sai da máquina**.

---

## Estado

Protótipo de navegador funcional. Um layout só, o que sobrou de
[quatro que foram testados](docs/ACHADOS.md) — os três anteriores tinham, cada
um, um jeito de tornar sílabas **impossíveis de digitar**, e todos pelo mesmo
motivo:

> Um modificador que significa uma coisa no ataque e outra na coda mais cedo ou
> mais tarde colide. A saída foi dar à líquida um endereço que só é dela — o
> roll — e deixar `LB`/`RB` livres para a coda. **Nada é compartilhado, então
> nada colide:** as 280 formas de sílaba que o desenho promete são alcançáveis.

Medido em duas sessões reais: **0 conflitos**, 2,15 e depois 2,24 caracteres por
gesto. A segunda sessão também mediu a *carga* de cada botão e reorganizou o
layout por ela: o vozeamento estava em 24–28% das sílabas morando no clique do
analógico, enquanto os dois bumpers ficavam em 4% e 0%. Trocar de lugar derruba
o clique de analógico de 41% das sílabas para 18%.

**Lacunas conhecidas**, deliberadamente explícitas em vez de meio-resolvidas:

- `x` vs `ch` ainda é decisão não tomada
- tritongo (`Uruguai`, `quais`) provavelmente pede labialização no ataque, não
  tritongo no núcleo
- `k`, `w` e `y` não têm endereço — o gate `—` do núcleo é redundante com o
  repouso e é o candidato natural (o `h` já saiu daqui: mora no `L3` com o
  analógico parado)
- coda complexa `/rs/` (*perspectiva*) não sai — medido em 0,03% dos tokens, e
  metade é nome estrangeiro
- dois-pontos, ponto-e-vírgula e travessão ainda não têm endereço
- plural de `-ão` é lexical (pães/mãos/ações), então vai sílaba a sílaba

## Testes e ferramentas

Sem dependências. Só `node`.

```fish
node test/motor.test.mjs      # 100 casos — montagem da sílaba e ortografia
node test/ui.test.mjs         # 49 casos — o que os satélites, o HUD e o log geram
node tools/corpus.mjs         # mede grafias contra corpus real de pt-BR
node tools/screenshot.mjs     # a página em PNG (--uso, --medir)
node tools/pad-preview.mjs    # o desenho do controle em PNG
```

O `tools/corpus.mjs` é o que decidiu várias escolhas por medição em vez de
intuição — inclusive derrubar duas decisões minhas que pareciam certas.

## Para onde vai

Separação inegociável: **um resolvedor de fonemas puro** e **um ortografador**.
Trocar de língua deve significar trocar só o segundo.

```
src/
  phonology.js       monta a sílaba a partir dos gates (puro, testável, sem UI)
  orthography-pt.js  a grafia do português sobre o buffer da palavra
  wheel.js  ·  input.js  ·  metrics.js
```

Depois, nativo: **Windows** (XInput/SDL2, overlay `WS_EX_LAYERED`, injeção via
`SendInput`) e **Linux** (evdev para ler, **uinput** para injetar — o overlay é
a parte dolorida, X11 é fácil e Wayland exige `layer-shell`). Num código só,
seria Rust com `gilrs` + `egui`.

> Este é um experimento, sem pretensão de substituir teclado. Mas decisões que
> fechem portas de internacionalização ou de acessibilidade são evitadas de
> propósito.

## Licença

MIT — ver [`LICENSE`](LICENSE).
