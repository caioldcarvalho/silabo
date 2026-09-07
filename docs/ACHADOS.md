# Achados

Registro do que as sessões de teste revelaram. Cada item traz como foi
verificado, para não virar impressão.

---

## 1. Variante A vence, e o motivo é estrutural

**Status: medido, decidido.**

Sessões de digitação com as duas variantes: A é mais rápida **apesar** do
incômodo de L3/R3. A causa não é hábito.

Em B, `LB` é sonorização do ataque **e** qualificador de coda `-r`; `LT` é
nasalização **e** coda `-l`. Como `buildCoda()` lê os mesmos botões, pedir coda
reescreve o ataque. Sílabas que se tornam impossíveis:

| sílaba | A | B |
|---|---|---|
| **tar** (can·**tar**) | ok | **X** — sai "dar" |
| das, bes, ges | ok | **X** |
| lar, mar, sol | ok | **X** |
| cans (nasal + plural) | ok | **X** |

`cantar` é um dos três exemplos do próprio briefing e é impossível em B: coda
`-r` exige RB+LB, mas LB sonoriza o `t` em `d`. **Todos os infinitivos quebram** —
justamente o que motivava a coda `-r` existir.

### O princípio geral

As duas variantes têm a mesma doença: **um modificador que muda de significado
conforme o RB está segurado torna sílabas impossíveis.** O que difere é *o que*
foi sobrecarregado, e o custo é proporcional à frequência disso:

- **A** sobrecarrega a **líquida** (`if(!btn.RB)` no `buildOnset`) → perde
  cluster + coda: "pres", "trans", "brins". Raro.
- **B** sobrecarrega **sonoridade e nasalidade** → perde 48 formas de ataque
  simples + coda, incluindo plural e infinitivo. Frequentíssimo.

### Por que L3/R3 aparecem

Não é preguiça de desenho, é aritmética. Durante a sílaba há LB, LT, RB, RT
(RT = commit), e o desenho precisa de **7 funções**: sonoro, +r, +l, nasal,
coda-s, coda-r, coda-l. Sete não cabe em três. As saídas reais são: tirar o
commit do RT para liberar um dedo bom, ou escolher conscientemente o que
sobrecarregar.

---

## 2. Bugs confirmados

### 2.1 O roll só funciona entre gates adjacentes

`track()` empilha **todo** gate cruzado e `buildNucleus()` transforma cada um em
glide. Ditongos que atravessam a roda saem com lixo, de um jeito **dependente de
velocidade** — exatamente o que o desenho jura não fazer:

```
ai (a→i)  gates crus [4,5,6,7]  → "aiii"
oi (o→i)  gates crus [2,1,0,7]  → "oui"
eu (e→u)  gates crus [6,7,0,1]  → "eiu"
ou (o→u)  gates crus [2,1]      → "ou"    (adjacente, único que funciona)
```

Inconsistência relacionada: o roll do **ataque** na variante B lê só o último
gate (`gs[gs.length-1]`), enquanto o **núcleo** lê todos.

**Correção proposta:** manter apenas o primeiro gate, o último, e os pontos onde
o **sentido de rotação inverte**. Inversão é evento discreto, sem timing — é a
mesma mecânica de direção do 8vim. Simulado:

```
ai  [4,5,6,7]   → "ai"   ok
oi  [2,1,0,7]   → "oi"   ok
eu  [6,7,0,1]   → "eu"   ok
ão  [4,3,2]     → "ãu" → "ão"  ok
```

**Limite conhecido:** não resolve tritongo monotônico. "uai" (u→a→i) gira todo
no mesmo sentido, então o `a` do meio é indistinguível de gate atravessado.
Mas os tritongos do português são quase todos `qu`/`gu` + ditongo (Uruguai,
quais, averiguei) — ou seja, o `u` pertence ao **ataque**, não ao núcleo. Isso
sugere que a saída é um modificador de labialização no ataque, não tritongo no
núcleo.

### 2.2 Nasal em final de sílaba está quebrada

`orthograph()` escreve `ã`/`õ` com til sempre que a vogal é a/o, e usa `n` solto
para as demais. Em português, `ã` só leva til em final de palavra ou antes de
vogal; antes de consoante escreve-se `an`/`am`. O `n → m` só acontece antes de
`p`/`b`, nunca em final de palavra.

```
cantar  → "cãtar"   (esperado "cantar")
campo   → "cãpo"    (esperado "campo")
sem     → "sen"     bem → "ben"   um → "un"
com     → "cõ"      bom → "bõ"
tempo   → "tempo"   ok — o único caso que a regra n→m cobre
```

Não é falta de coda nasal na interface: a nasalidade **entra** (R3 na variante A),
o que falha é a **grafia**. A regra correta depende do que vem depois, o que o
ortografador já pode saber porque opera sobre o buffer da palavra:
til só antes de vogal ou em fim de palavra; `m` antes de p/b **e em fim de
palavra**; `n` no resto.

### 2.3 Backspace não existe no controle

Só `pad.buttons[0]` (A) está ligado, e faz espaço (`index.html:378`). B, X, Y e
d-pad não são lidos. Toda a seção "Fora da sílaba" do briefing está por
implementar.

---

## 3. Gap ortográfico: `ce` e `ci` — RESOLVIDO

**Ideia do Caio, e é melhor que a alternativa que eu tinha proposto** (um
ciclador de grafia no d-pad). O ciclador é pós-correção: digita errado e
conserta, um gesto a mais. O dele é entrada direta, sai certo de primeira, sem
botão novo e sem modo — e **generaliza o princípio que o desenho já usava**
(`nr`→nh, `lr`→lh) em vez de inventar mecânica nova:

> aproveitar clusters que **não existem** em português para gerar as grafias
> que **faltam**.

### O orçamento de slots

Das 32 combinações ataque×líquida, só 13 são clusters reais em português
(`tr cr cl pr pl fr fl dr gr gl br bl vr`) e 4 já estavam reaproveitadas
(`nr`→nh, `lr`→lh, `rr`→rr, `mr`). **Sobram 15 buracos livres.**

| gesto | grafia | resolve |
|---|---|---|
| `s` + `r` | **c** | /s/ antes de e,i — cebola, cidade, certo |
| `s` + `l` | **ç** | /s/ antes de a,o,u — ação, caçar, moço |
| `j` + `r` | **g** | /ʒ/ antes de e,i — gente, girafa, gelo |
| `z` + `l` | **s** | /z/ grafado s — casa, mesa, coisa |

Os três primeiros são do Caio. O quarto fecha o par: sem ele `z` sempre grafa
"z" e "casa" saía "caza". Ainda sobram **11 slots** para `x`/`ch`, `h`, `k`,
`w`, `y`, acentos.

### Por que duas tabelas, e não uma

`nh`/`lh`/`rr` são **fonemas** distintos (/ɲ/, /ʎ/, /ʀ/): pertencem ao
resolvedor e sobrevivem a uma troca de língua. `c`/`ç`/`g`/`s` são **a mesma
consoante com outra grafia**: pertencem ao ortografador, e outra língua troca a
tabela inteira. Estão separadas no código como `DIGRAPH` e `SPELLING` por isso —
misturar as duas faria `orthography-pt.js` nascer torto.

Grafia pedida explicitamente **não passa pelas regras automáticas**: é o ponto
inteiro de pedir. Por isso `sr`→"c" não vira "qu" antes de e/i, enquanto `c`
sozinho continua virando.

### O problema de fundo que isso contorna

Em português o fonema /s/ tem quatro grafias e a escolha **não é decidível por
regra fonotática — é lexical**: "sela"/"cela", "sinto"/"cinto" são homófonos.
Este era o ponto onde "fonologia arranja, ortografia rotula" batia no limite de
o português não ser fonêmico o bastante. A saída não foi um dicionário: foi
**devolver a escolha ao usuário, de graça**, num slot que já estava vago.

---

## 3b. Bugs achados ao implementar o item 3

### `r` intervocálico não podia ser simples
A regra "`/R/` intervocálico dobra" impedia escrever "girafa", "caro", "hora" —
saíam "girrafa", "carro". Era **redundante**, porque o /ʀ/ forte já tem cluster
explícito (`r`+`r` → rr). Regra removida; `caro` e `carro` agora são gestos
diferentes, como devem ser.

### `é` e `ó` saíam acentuados
Os gates `é`/`ó` marcam uma distinção **fonológica** real (sela /ɛ/ vs selo /e/)
que a ortografia do português **não escreve**: ambos são `e`/`o`. O motor
grafava o acento, então "bom" saía "bóm". Normalizado; o acento agudo continua
sendo pós-correção no d-pad (pé, avó, só).

Efeito colateral que vale registrar: isso deixa **dois dos oito gates do núcleo
sem distinção ortográfica**. Pelo próprio princípio deste item, são dois slots
potencialmente reaproveitáveis.

---

## 3c. (histórico) a proposta descartada

**Ciclador de sibilante no d-pad**, percorrendo `s → ss → c → ç` na consoante
mais recente. Descartada em favor da ideia acima: pós-correção custa um gesto
extra e quebra o fluxo. Pelo ataque `c`, a regra `/k/ + vogal frontal → qu`
produz "que"/"qui" (correto e frequente, vale manter). Pelo ataque `s`, sai
"se"/"sse". **Não há caminho para "ce"/"ci"**, que são ultra comuns.

O motivo é mais fundo que uma regra faltando: em português o fonema /s/ tem
quatro grafias (`s`, `ss`, `c`, `ç`) e a escolha **não é decidível por regra
fonotática — é lexical**. "sela"/"cela", "sinto"/"cinto" são homófonos. Este é o
ponto exato onde "fonologia arranja, ortografia rotula" encontra seu limite: o
português não é fonêmico o bastante para a grafia ser função pura do fonema.

**Saída proposta, sem dicionário:** um **ciclador de sibilante** no d-pad, que
percorre `s → ss → c → ç` na consoante mais recente. Uma tecla resolve os quatro
casos de uma vez, incluindo o `ç` que o briefing já lista como lacuna, e mantém
o motor composicional. Encaixa no espaço que o briefing já reservava para
pós-correção no d-pad.

---

## 4. `ão` — regra, não dicionário

`ã` + glide `u` produz "ãu". Como **"ãu" não existe em português**, a conversão
`ãu → ão` é determinística: uma linha no ortografador, da mesma natureza do
`n → m` antes de p/b que já está lá. Não precisa de dicionário.

Depende da correção 2.1 para o roll `a→o` não sair "ãou".

---

## 5. Botões fora da sílaba — desenho revisto

O briefing propunha X = maiúscula, Y = modo letra-a-letra, pontuação em
LB/LT + d-pad. Proposta nova, por frequência de uso real:

| botão | função |
|---|---|
| A | espaço |
| **A + RB** | **enter** (RB está livre fora da sílaba) |
| B | backspace (segurar = apaga palavra) |
| **X** | **ponto final** |
| **Y** | **interrogação** |

Ponto final é muito mais frequente que "modo letra-a-letra"; enterrá-lo dois
níveis fundo em LB/LT + d-pad custava caro. Maiúscula e modo letra-a-letra
precisam de novo endereço.

---

## Agenda

**Feito** (35 testes em `test/motor.test.mjs`, `node test/motor.test.mjs`):
- roll corrigido — primeiro, último e inversões de sentido
- grafia da nasal corrigida — cantar, campo, sem, com, irmãs, sons
- ditongos nasais — ão, ãe, õe
- grafia por cluster ilegal — c, ç, g, s
- `r` intervocálico simples; `é`/`ó` normalizados
- botões de face: A espaço, A+RB enter, B backspace, X ponto, Y interrogação

**Em aberto:**
1. Fechar A no motor e decidir o que fazer com B (hoje as duas seguem lá).
2. D-pad: acentos e o resto da pontuação.
3. Onde vão maiúscula e o modo letra-a-letra, que eram X e Y.
4. Labialização no ataque (`qu`/`gu` + ditongo → Uruguai, quais).
5. Como pagar menos caro pelo L3/R3 — 7 funções em 3 botões.
6. Reaproveitar os slots livres restantes: `x`/`ch`, `h`, `k`, `w`, `y`, e
   possivelmente os dois gates de núcleo sem distinção ortográfica.
