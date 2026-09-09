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

## 3. Gap ortográfico: `ce` e `ci` — RESOLVIDO (em duas rodadas)

> **Rodada 2 (07/09, implementada).** O autor objetou contra a própria solução:
> *"começa a entrar um pouco no problema da estenografia. É uma decisão
> arbitrária."* Um parecer externo reenquadrou: a analogia com estenografia não
> procede — a arbitrariedade é **do português**, não do desenho; o usuário já
> sabe que "cebola" tem c, e o desenho só dá endereço a uma decisão que a língua
> já tomou. Mas o incômodo apontava um defeito real e **outro**: um operador
> composicional (`ataque+líquida`) estava produzindo resultado não-composicional.
>
> **O que ficou no lugar: roll do analógico esquerdo até ↗.** *O som do gate em
> que você começou, escrito com a letra que mora em ↗.* Um fato motor, não
> quatro pares: `s`→↗ = c/ç, `x`→↗ = ch, `j`→↗ = g (e ↗ sonorizado **é** g, o
> que torna o terceiro duplamente motivado). Os ataques que precisam de
> re-grafia são exatamente os que não admitem líquida, então nunca colide com
> cluster; roll para outro destino é ignorado e degrada para o gate simples.
>
> Isso matou dois dos quatro endereços originais: `ç` virou regra (a vogal
> decide) e `z` virou default invertido + pós-correção no d-pad. O texto abaixo
> fica como o raciocínio que levou até aqui.

### O caminho até ela

**Ideia do Caio, e é melhor que a alternativa que eu tinha proposto** (um
ciclador de grafia no d-pad). O ciclador é pós-correção: digita errado e
conserta, um gesto a mais. O dele é entrada direta, sai certo de primeira, sem
botão novo e sem modo — e **generaliza o princípio que o desenho já usava**
(`nr`→nh, `lr`→lh) em vez de inventar mecânica nova:

> aproveitar clusters que **não existem** em português para gerar as grafias
> que **faltam**.

### O orçamento de slots

Das 32 combinações ataque×líquida, **16 são ataques complexos bem-formados** e 4
estavam reaproveitadas como dígrafos (`nr`→nh, `lr`→lh, `rr`→rr, `mr`).

**Corrigido em 07/09, e a correção é do Caio.** Eu tinha listado `tl` como
inexistente, alegando que "atleta" silabifica at·le·ta. Ele derrubou em um passo:
*"at.le.ta implicaria que t existe em coda e eu sei que NÃO existe"*. As codas do
português são só /S R l N/ — nenhuma oclusiva. Logo `a·tle·ta`, e `tl` **é**
ataque complexo.

E isso não é um item de lista, é a regra inteira:

> Uma sequência CC é **ataque complexo** apenas quando C1 **não pode ser coda** —
> senão a língua lê como coda + ataque.

| palavra | leitura | por quê |
|---|---|---|
| atleta | a·**tle**·ta | /t/ não pode ser coda → só resta ataque complexo |
| desligar | de**s**·**l**i·gar | /S/ é coda lícita → não é ataque complexo |

Foi por isso que a primeira medição errou: um regex `VccV` conta as duas
leituras juntas. `sl` aparece em 114 formas do corpus e **nenhuma** é ataque —
são "desligar", "desligue", "island". `tl` aparece em 33 e as portuguesas
(atleta, atlântico, atlas, atlético) são ataque de verdade.

**O ganho colateral:** as consoantes excluídas são exatamente as **sibilantes**
(s, z, x, j) — que são exatamente as que `RESPELL` serve. É o mesmo fato dito
duas vezes: *sibilante não toma líquida, então o slot de roll dela está livre
para significar grafia*. O conjunto passou a ser derivado no código
(`clusterOk`), não listado.

`dl` e `vl` passam pela regra e o léxico nativo não os usa (só bradley, vladimir,
kevlar) — são gap **acidental**, não sistemático. Ficam habilitados de propósito:
custa nada e serve para estrangeirismo, que o desenho já assume.

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

### Medido contra corpus (07/09)

`node tools/corpus.mjs` — OpenSubtitles pt_BR, 50k formas / 419M tokens,
ponderado por token, que é o que um input method realmente paga:

| endereço | custo (tokens que dependem dele) | veredito |
|---|---|---|
| `s`+`r` → **c** | 2,84% | endereço se justifica |
| `z`+`l` → **s** | 2,13% | **polaridade invertida** — ver abaixo |
| `s`+`l` → **ç** | 1,35% | **redundante** — é regra, não endereço |
| `j`+`r` → **g** | 0,74% | endereço se justifica |

**`ç` é derivável, o slot é desperdício.** ⟨ç⟩ nunca ocorre antes de e/i nem em
início de palavra, então dado /s/ a grafia é função da vogal seguinte: frontal →
`c`, posterior → `ç`. Mesma família do `c→qu` que o motor já faz. As 7
contra-evidências do corpus (`voçê`, `começe`, `conheçe`…) são **erros de grafia
de legenda amadora** — o teste automático dizia "FALSO" e a inspeção das formas
mostrou o contrário. Vale de lição: contar não basta, é preciso olhar o que
foi contado.

**`z`+`l` está resolvendo o problema pelo lado errado.** ⟨s⟩ para /z/
intervocálico é 1,86× mais frequente que ⟨z⟩ (2,13% vs 1,14%) — maioria, não
esmagadora, mas suficiente para inverter: o default deveria ser ⟨s⟩ (casa, mesa,
coisa, precisa) e ⟨z⟩ é que deveria custar o endereço (fazer, dizer, azul).

---

## 3b. Bugs achados ao implementar o item 3

### `r` intervocálico não podia ser simples
A regra "`/R/` intervocálico dobra" impedia escrever "girafa", "caro", "hora" —
saíam "girrafa", "carro". Era **redundante**, porque o /ʀ/ forte já tem cluster
explícito (`r`+`r` → rr). Regra removida; `caro` e `carro` agora são gestos
diferentes, como devem ser.

### `é` e `ó` — decisão revertida em 07/09

Eu tinha colapsado os dois em `e`/`o` (a distinção /e/ vs /ɛ/ não é marcada na
escrita) e mandado todo acento para o d-pad. **Errado por custo:** medido,
**10,73% dos tokens** carregam vogal acentuada — `é` sozinho é 3,02%, `ê` 2,55%.
Uma pós-correção em 1 de cada 10 tokens é caríssima, e os gates `é`/`ó` estavam
**ortograficamente vazios**: endereço já pago, gasto em nada.

Agora saem literais, como o rótulo promete. O d-pad cobre `ê ô â` (2,66%) e
`á í ú`, que não têm gate próprio.

### (histórico) `é` e `ó` saíam acentuados
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

**Feito na rodada 2:**
- re-grafia por roll até ↗ (substitui os 4 clusters arbitrários)
- `c`/`ç` por regra da vogal seguinte
- ⟨s⟩ como default de /z/ intervocálico, medido contra corpus
- d-pad ligado: ↑ ê ô â · → é ó á · ← à · ↓ alterna s↔z

**Instrumentação (07/09):** a sessão agora é registrada — sílaba, gesto,
palavra, apagamento — e o **conflito é detectado pelo próprio motor**: quando um
modificador é apertado e seu efeito descartado, sai um evento `CONFLITO` com o
motivo (`liquida-comida-pela-coda`, `cluster-inexistente:sr`). O perfil do
controle (`id`, `mapping`, nº de botões e eixos) vai junto, o que permite
diagnosticar um d-pad morto pelo log em vez de por adivinhação. Baixa em JSON
pelo painel de medição.

**Acento vira ciclo (07/09).** Relato: *"os acentos eu digitei a mais com d-pad
e dps n consegui voltar"*. A causa: `accentLast` procurava a última vogal **que
estava no mapa do acento** — uma vogal já acentuada não está, então o segundo
toque pulava para a vogal **anterior** e acentuava aquela. Não havia volta.
Agora `→` avança e `←` retrocede num ciclo fechado por vogal
(`a á â ã à` · `e é ê` · `o ó ô õ`), reversível por construção. Sobrou o `↑`.

---

## 6. A primeira sessão medida (07/09) — e a variante C que ela pediu

Log cru em [`sessao-2026-09-07.json`](sessao-2026-09-07.json): 42 sílabas, 18
palavras, 5 apagamentos, **3 trocas de variante no meio da digitação**.

### As trocas têm um padrão, e ele é o diagnóstico

| momento | trocou para | o que tinha acabado de acontecer |
|---|---|---|
| 19:08:59 | **B** | usou `L3` para sonorizar `p→b` em "boiola" |
| 19:10:17 | **A** | ia digitar "des·mer·e·cer" — três codas `-r`/`-s` |
| 19:11:11 | **B** | ia digitar "pro·je·ti·nho", "ul·tra" — clusters |

Ou seja: **foge para B para não usar L3; volta para A porque em B a coda
quebra.** Ele estava fazendo na mão o que o desenho deveria fazer — nenhuma das
duas variantes cobre uma frase inteira.

### Variante C

Junta o que cada uma acerta, e a peça que faltava era onde qualificar a coda:

- `LB` **sonoriza**, `LT` **nasaliza** — dedos dedicados, que é o que B jogou fora
- roll do esquerdo `←`/`→` = **líquida**; `↗` = re-grafia (como já era)
- `RB` = coda; **com RB, o roll manda a líquida para a coda em vez do ataque**

Uma regra, dois lugares: *o roll compõe a líquida, e RB diz onde ela cai.* Não
gasta dedo novo, e **L3 e R3 somem do desenho inteiro** — que era o objetivo
declarado da variante B, agora sem o custo que a matou.

Cobertura enumerada, sobre as 280 formas de sílaba que o desenho promete:

| | alcança | usa L3/R3 |
|---|---|---|
| A | 166 | **sim** |
| B | 102 | não |
| **C** | **166** | **não** |

C empata com A em cobertura e não paga o L3/R3. O que continua faltando nas
duas é ataque-com-cluster **mais** coda ("pres", "trans", "nhos") — 96 das 114
ausências.

### Outros achados do mesmo log

**O zero do analógico não é zero.** O controle reportou repouso em
`x=-0.164, y=0.066`. Isso nunca troca de gate, mas come até **44% da margem
angular** (10° de 22,5° no ↑) e, com deflexão parcial de 0.7 numa diagonal, o
raio efetivo cai para **0.54 — abaixo do limiar de entrada 0.55**, e o gate
simplesmente não dispara. Agora o repouso é aprendido continuamente (só de
amostras com raio < 0.25, devagar, para um empurrão real não arrastar o zero) e
subtraído antes de calcular gate e raio.

**Meu detector de conflito gritava lobo.** Ele marcava `RB`+`LB` como conflito —
mas essa é também a digitação **normal** da coda `-r`. Os 2 "conflitos" do log
eram os infinitivos de "desmerecer", digitados corretamente. Agora `ambiguo` é
registrado sem alarme e `CONFLITO` fica só para **perda silenciosa** de verdade
(cluster inexistente). E a troca de variante passou a gravar o que aconteceu
logo antes dela, que foi a evidência que valeu neste log.

**O `m` provisório engana.** Em "insta", o commit de `i`+nasal mostrou "im" —
correto para uma palavra que terminasse ali — e ele apagou achando que estava
errado. Se tivesse seguido, `iN`+`s`+`ta` resolveria para "insta". O arquifonema
nasal aparece como `m` no fim do buffer e assusta antes de a palavra fechar.
Sem correção decidida.

**O que funcionou:** `é` saiu acentuado direto do gate; o ciclo de acento fez
"porque"→"porquê" com um toque de `←` (ele foi pelo caminho curto sozinho) e
"to"→"tó"→"tô"; o `s↔z` consertou "fasem"→"fazem"; e "fazendo" saiu certo com o
`N` resolvendo contra o `d` da sílaba seguinte.

---

## 7. Segunda sessão medida (07/09, 20h) — 110 sílabas

Log em [`sessao-2026-09-07b.json`](sessao-2026-09-07b.json). Frases reais
("agora é a ora da verdade. todas as opçois tiveram seus problemas").

**Taxa de erro: 14%** — 15 de 110 sílabas foram apagadas em menos de 3s. É a
primeira linha de base honesta do projeto.

### 112 apagamentos, e só 1 era de palavra

O número parecia frustração e não era: eram **rajadas limpando a tela letra por
letra** — uma delas com **50 apagamentos em 10,3s** para apagar uma frase.

A causa é desenho meu: segurar B apagava **uma** palavra e depois travava até
soltar. Limpar oito palavras exigia oito ciclos de segurar-e-soltar de 450ms;
martelar o B era mais rápido, e foi o que aconteceu. Backspace tem que
**repetir**, como em qualquer campo de texto:

- toque → uma letra
- segurando >400ms → repete letra a cada 90ms
- passando de 1,2s → passa a apagar **palavra** a cada 220ms
- `LB`+`LT`+`B` → **limpa tudo** (três dedos, não sai por acidente), e há um
  botão "Limpar texto" na tela

A rajada agora também vira **um evento** no log em vez de cinquenta.

### A variante C não chegou a ser testada

Ele selecionou C às 20:27:34 e voltou para A **23 segundos depois, sem uma única
sílaba** — nenhum evento de commit, nem sequer um `vazio`. O motivo não está no
log: ou o painel de atalhos não deixou claro o que mudou, ou não era hora.
Continua sem evidência a favor ou contra.

### Bugs de instrumentação achados pelo próprio log

- **`de` e `para` sempre iguais** nas trocas de variante (`C → C`): eu gravava
  `de: variant` **depois** de já ter atribuído o novo valor.
- **O log é cumulativo** e junta sessões sem marca, porque persiste em
  localStorage. Agora cada carga da página grava um evento `sessao`.

---

## 8. Terceira sessão (07/09, 20h50) — C testada e reprovada, e nasce a D

Log em [`sessao-2026-09-07c.json`](sessao-2026-09-07c.json). O backspace que
repete funcionou: **17 apagamentos contra 112** na sessão anterior.

### C foi usada por 13 sílabas e quebrou numa palavra

Escreveu "o lucas é guei e ele vai" sem tropeçar. Aí veio "comprovar":

```
20:51:46 [C] L:↙↓ R:↓↘ [LB+RB+RT] → 'vaus'   ← queria "var"
20:51:51   ⇄ C → A
20:51:55 [A] L:← R:· [RT+L3]      → 'r'      ← emendou o r sozinho
```

Em C a coda `-r` sai de rolar até `→`, **depois** de já ter marcado o ataque.
Partindo de `↙`(f) isso é meia volta de polegar, e ele parou no meio. Pior:
**o roll não pode terminar no gate onde começou**, então para o ataque `s`/`z`
a coda `-r` é literalmente **impossível** — "ser", "fazer", "dizer", "será":
**1,54% dos tokens**. C está reprovada.

| ataque | setores até `→` (coda -r) |
|---|---|
| s/z | **impossível** |
| c/g, p/b | 1 |
| t/d, m/n | 2 |
| f/v, x/j | 3 |
| l/r | 4 — meia volta |

### Variante D — proposta do Caio, e cobre tudo

> *"o real problema dos outros é que usar o LB como modificador de CODA E de
> ataque necessariamente vai virar conflito"*

É literalmente o teorema. A saída é dar à líquida um endereço que **só** é dela:

- **roll do esquerdo** = líquida, e nada mais
- **L3** sonoriza · **R3** nasaliza
- **RB** = coda, **LB/LT** qualificam o tipo — e só fazem isso

Nenhum canal compartilhado, então nenhuma colisão possível:

| variante | alcança (de 280) | usa L3/R3 |
|---|---|---|
| A | 166 | sim |
| B | 102 | não |
| C | 158 | não |
| **D** | **280** | **sim** |

D é a única que resolve o que **nenhuma** outra resolvia: `pres`, `nhos`,
`grande` — cluster no ataque **e** coda na mesma sílaba.

### O trade-off ficou aritmético, não estético

Durante a sílaba existem: L-gate, L-roll, R-gate, R-roll, LB, LT, RB, RT. O RT
é o commit; os gates e o R-roll são ataque, núcleo e ditongo. Sobram **L-roll,
LB, LT, RB** para líquida, sonoro, nasal e coda-com-tipo — quatro canais para
cinco funções. **Sem L3/R3 a conta não fecha**, e o que B e C fizeram foi
escolher onde pagar a diferença.

Ou seja: *ou* L3/R3, *ou* sílabas impossíveis. Não há terceira via com este
conjunto de dedos — a menos que apareça um quinto, e aí a saída é de hardware
(paddles traseiros, que o Xbox Elite tem quatro).

---

## 10. Quarta sessão (07/09, 21h35) — D aprovada, 99 sílabas sem trocar

Log em [`sessao-2026-09-07d.json`](sessao-2026-09-07d.json). Frase escrita
dentro do próprio protótipo, e é o melhor resumo do resultado:

> *"oi claude esse é o teste de digitação com o modo d acabei de perceber que
> falta vírgula e exclamação…"*

| | |
|---|---|
| sílabas | **99, todas em D** |
| trocas de variante | **0** |
| conflitos / ambiguidades | **0 / 0** |
| sílabas corrigidas em <3s | 12% (era 14%) |
| caracteres por sílaba | **2,15** — dentro da faixa prometida |
| velocidade | 34 caracteres/min |

### O bug que o log pegou, e que custou tempo real

Onze toques seguidos no `s↔z` sobre "talves", **sem nenhum efeito**:

```
acento← [ê] → 'talvês'
s↔z     → 'talvês'    ← nada
s↔z     → 'talvês'    ← nada
s↔z     → 'talvês'    ← nada
```

O `toggleSibilant` só enxergava sibilante **entre vogais**; em "talves" o `s` é
final, o regex não casava e a tecla estava morta — **sem nenhum retorno**.
Corrigido em duas frentes:

- o alvo passa a incluir sibilante em fim de palavra, e o ciclo virou
  `ss → s → z` entre vogais (`inclussi` → `inclusi` → `incluzi`), `s → z` no
  fim (`talves` → `talvez`)
- **toda tecla que não acha alvo agora avisa na tela.** Falha silenciosa foi o
  que transformou um bug pequeno em onze tentativas.

### Também dessa sessão

Vírgula e exclamação ganharam endereço, no padrão que já existia com `A + RB`:
**`X + RB`** = vírgula, **`Y + RB`** = exclamação.

E ficou registrado o que o gate `—` do núcleo é: **redundante**. Analógico em
repouso já produz "sem vogal", então `↑` no direito não faz nada de novo — é um
slot pago e vazio, candidato natural para `k`, `w`, `y`, `h` (medidos no corpus:
0,48%, 0,23%, 0,59% e 1,57%, mas quase tudo nome próprio estrangeiro, exceto o
`h`, que é português de verdade — há, homem, hoje).

---

## 11. Sessão de 10 segundos, três achados

Log em [`sessao-2026-09-07e.json`](sessao-2026-09-07e.json). Um gesto só, e vale
por vários.

**1. O motor fez o que nenhuma variante anterior fazia:**

```
L:↑↗→ R:↓ [RB+RT+R3] → 'trãs'
partes = {ataque:"tr", nucleo:"a", nasal:true, coda:"s"}
```

Cluster **mais** nasal **mais** coda, num gesto. Em A isso era impossível (a
líquida sumia com RB), em B e C também. Aqui saiu de primeira — a fonologia
está certa; só a **grafia** escolheu "ãs".

**2. Vírgula e interrogação funcionaram, mas o log quase não contou.**
`endWord` só registrava quando havia palavra no buffer, então pontuação isolada
aparecia só de raspão, no `restou` de um apagamento posterior. Agora sai como
evento `pontuacao`.

E o teste dessa correção pegou um bug de verdade: pontuação depois de uma
palavra **já fechada** herdava o espaço dela — `"palavra , "` em vez de
`"palavra, "`.

**3. Log limpo pelo usuário ficava sem marcador de sessão.** `limparTele`
zerava tudo e não gravava o `sessao` de volta, então a sessão nascia anônima.

### O caso `trans` continua sem saída, e é barato resolver

| | tokens | formas |
|---|---|---|
| **-ãs** | 0,0086% | 19 — irmãs, fãs, maçãs, manhãs, alemãs |
| -ans | 0,0050% | 23 — orleans, jeans, trans, slogans |

O default está certo (o `-ãs` é quase o dobro e é português nativo), mas o caso
marcado **não tem endereço**: o ciclo de acento opera no buffer cru, onde a
vogal ainda é `a`, então ciclar produz "tráns", nunca "trans".

**Proposta: o `↑` do d-pad, que está livre, vira o ciclador da nasal escrita** —
domínio "última vogal nasal", alfabeto `til → n → m`. É o mesmo formato dos
outros dois cicladores (escopo e alfabeto definidos, reversível), e resolveria
também qualquer discordância em `-am`/`-ã` sem depender do ciclo de acento.
Não implementado: 0,005% é pouco para gastar um slot sem o dono decidir.

---

## 9. O til é pós-correção, não default — e isso corrigiu um bug grande

Ideia do Caio, a partir do caso `ãs`/`ans`:

> *"o ~ vira um acento possível e a grafia padrão sempre cai pra ans. quem quiser
> ãs acentua como se fosse um acento qualquer. faz menos sentido e sai um pouco
> da lógica que construímos, mas resolve."*

**Não sai da lógica — é ela aplicada.** A doutrina do projeto é *frequente e
derivável → regra; frequente e lexical → endereço no gesto; **raro e lexical →
pós-correção***. E a medição mostra de que lado cada um cai:

| fim de palavra | tokens | formas |
|---|---|---|
| **-am** | **0,671%** | 1508 — foram, estavam, eram, precisam, tinham |
| -ã | 0,119% | 64 — amanhã, manhã, irmã, fã, maçã |
| -ãs | 0,009% | 19 — irmãs, maçãs, manhãs |
| -ans | 0,005% | 23 — quase tudo estrangeiro (jeans, trans) |

O caso não era "trans": era que **toda terceira pessoa do plural estava
quebrada**. `falam` saía "falã", `foram` saía "forã", `eram` saía "erã",
`tinham` saía "tinhã" — 5,6× mais frequente que o caso que a regra protegia.

**A correção não precisou de mecanismo novo.** O til já estava no ciclo de
acento (`a á â ã à`), e o `finish` já absorvia o arquifonema quando o til caía
em cima dele. Bastou inverter o default:

- `/aN/` em fim de palavra → **"am"**
- antes do `-s` do plural continua **"ãs"**, porque "ams" não existe
- `irmam` + 3 toques do ciclo → **irmã**, e mais 2 voltam para `irmam`

### Sobre o ciclador universal

A outra ideia — *"algum dos d-buttons poderia ciclar todos os casos
ambíguos"* — eu não faria. Um ciclador só funciona quando tem **domínio e
alfabeto definidos**: o de acento é "última vogal" × "os acentos que ela
aceita"; o de sibilante é "última sibilante intervocálica" × "s/z". Um
universal teria que **adivinhar o alvo**, e vira imprevisível — exatamente o
oposto do que faz os outros dois funcionarem. O d-pad tem quatro direções, três
em uso; se aparecer um terceiro caso lexical frequente, ele ganha o `↑`.

---

**FECHADO 07/09:** o motor tem **um layout só, a D**. A, B e C viram registro
histórico — este documento — e saíram do código. Cada uma tinha um jeito de
tornar sílaba impossível, e todas pelo mesmo motivo, que é o achado central do
projeto:

> Um modificador que significa uma coisa no ataque e outra na coda mais cedo ou
> mais tarde colide. O custo é proporcional à frequência do que foi
> sobrecarregado — e por isso A (que sobrecarregava a líquida) doía menos que B
> (que sobrecarregava sonoridade e nasalidade).

---

## 12. Ponto de retomada (07/09, fim da noite)

Levantado pelo Caio, para resolver depois. Ele mesmo já desconfiou da própria
ideia (*"acho q vai até estragar kk"*), e a medição abaixo é para decidir com
número em vez de intuição.

### O que ele observou

1. **`LT` + `LB` está livre** durante a sílaba.
2. **Fazer `-s` e fazer `-r` são mutuamente excludentes** — mas existe coda
   complexa: *superstição* tem `pers`, com **r + s** fechando a sílaba. Se `-r`
   e `-s` fossem gestos diferentes e combináveis, a sílaba sairia.
3. Ideia de rearranjo: **a letra `l` iria para o `R3`** e o **vozeamento para o
   `LT`** — porque *"haja L3 pra tanta digitação"*.

### A medição muda a prioridade dos dois pontos

**O item 2 é menor do que parece, porque o caso frequente já funciona.**
Codas complexas, medidas por token:

| | tokens | formas | |
|---|---|---|---|
| **/ns/** | **0,113%** | 344 | monstro, construir, instante, transporte |
| /bs/ | 0,0126% | 58 | substituir, substância, obstáculo |
| /rs/ | 0,0066% | 45 | perspectiva, superstição, perspicaz — e metade é nome estrangeiro (porsche, kirsten, first) |
| /ls/ | 0,0008% | 8 | quase tudo estrangeiro |

**/ns/ é 17× mais frequente que /rs/ — e já sai hoje**, porque a nasalidade é
traço do **núcleo** (R3), não da coda: `i` + nasal + coda `-s` produz "ins"
direto. "sons", "bens", "instante" já funcionam. Sobra o /rs/, com 0,0066% e
poucas palavras portuguesas.

**O item 3 tem base forte, e é o mais promissor.** Contagem de botões nas 99
sílabas da sessão em D:

| botão | sílabas | |
|---|---|---|
| RT | 100% | o commit |
| **L3** | **28%** | **vozeamento — a cada 3 ou 4 sílabas** |
| R3 | 10% | nasalização |
| RB | 9% | coda |
| LB | 4% | tipo da coda |
| **LT** | **2%** | tipo da coda |

Ou seja: **o modificador mais usado mora no clique do analógico, que é o que
mais incomoda, enquanto os dois bumpers estão praticamente ociosos.** Trocar
vozeamento (28%) por tipo-de-coda (4% e 2%) de lugar é rearranjo por frequência,
e a queixa é legítima.

O que essa troca custa é o que precisa ser pensado com calma: `LT`/`LB` durante
a sílaba já qualificam a coda, e o desenho inteiro da D existe porque **nada é
compartilhado**. Mover o vozeamento para o `LT` recria exatamente o conflito que
matou a B, a menos que o tipo da coda vá para outro lugar antes.

---

## 13. Quinta sessão (09/09, 01h50) — o rearranjo por frequência, decidido

**A sessão é uma mensagem.** 82 sílabas, 35 palavras, 6 minutos, e o texto
digitado é um pedido de features escrito com a própria ferramenta:

> *olá, claude! precisamos de agá para palavras como oje. precisamos de
> maiúsculas também. pontuação como parênteses e aspas podem começar a entrar na
> conversa também. e a questão do modificador tbm. te esplico em chat.*

Os erros do texto **são** a lista de pendências: `oje` sem h, tudo minúsculo,
`esplico` porque ⟨x⟩ com valor /s/ não tem endereço.

### Medição, e o que ela confirma

**2,24 char/gesto** (contra 2,15 da sessão anterior), 13,4 sílabas/min, **zero
conflito** de novo. Contagem de botões nas 82 sílabas:

| botão | sílabas | |
|---|---|---|
| RT | 100% | commit |
| **L3** | **24,4%** | vozeamento |
| R3 | 17,1% | nasalização |
| RB | 14,6% | coda |
| LB | 4,9% | tipo da coda |
| **LT** | **0%** | tipo da coda — não foi tocado uma vez |

Duas sessões independentes, mesma conclusão: **o modificador mais frequente
morava no clique de analógico e os bumpers estavam ociosos.**

### A proposta do Caio, e por que ela fecha

> *"e se RB for -s, como já é, LB for -r e LB + RB for -l? Aí LT sobra pra ficar
> como vozeador do ataque."*

Isto satisfaz exatamente a condição que a §12 tinha deixado em aberto — mover o
vozeamento para o LT só é seguro **se o tipo da coda sair de lá antes**. Sai.
Depois da troca, `LB`/`RB` são só coda e `LT` é só vozeamento: nada
compartilhado, nenhuma sílaba impossível, a variante B não volta.

E o custo passa a ser **monotônico com a frequência pela primeira vez**:

| coda | tokens | antes | agora |
|---|---|---|---|
| /S/ | 16,72% | RB | RB |
| /R/ | 10,81% | RB+LB | **LB** |
| /l/ | 3,06% | RB+LT | LB+RB |

Antes, /R/ e /l/ custavam o mesmo sendo um 3,5× o outro. **Um botão a menos em
10,8% dos tokens**, e a carga de clique de analógico cai de 41,5% para ~18%
(só o R3) — previsão falsificável para a próxima sessão.

### O que isso custa, dito por extenso

1. **O /rs/ morre.** `LB+RB` era o último endereço livre do espaço de coda.
   Remedido com o critério certo — só conta se o `s` também estiver na coda,
   porque *conversar* é con·ver·sar, coda + ataque, não coda complexa — dá
   **0,0279%**, e as formas do topo são `marshall, rogers, rangers, porsche,
   sanders, brothers`. "perspectiva" (5.600 tokens) é quase a única palavra
   portuguesa da lista. /ns/ (0,318%) continua de graça porque a nasalidade é
   traço do núcleo. Vale gastar.
2. **`LB+RB` = `l` é um código, não uma composição.** Lido composicionalmente,
   LB(r) + RB(s) "deveria" dar /rs/. Não dá. É honesto chamar de código de 2
   bits — as codas orais do português são exatamente três, então dois bits é o
   tamanho certo do endereço — mas é a mesma arbitrariedade que incomodou na
   rodada dos sibilantes, e fingir que é regra seria pior.
3. **`LB+LT` vira acorde do mesmo dedo.** Ataque vozeado + coda -r pede os dois
   juntos, e isso cai em **2,74% dos tokens** (`fazer, dizer, ver, saber, dar,
   ajudar, pegar`) — todo infinitivo de raiz vozeada. Mais 0,59% com RB junto
   (`voltar, legal, possível`). O trade é −1 entrada e +1 acorde de mesmo dedo;
   o desenho já assumia LB+LT viável (o limpa-tudo é LB+LT+B). **É a única coisa
   aqui que aritmética não decide — só a mão.**

### O ⟨h⟩ mora no combo que estava morto

`buildOnset` retornava cedo com o analógico esquerdo em repouso, então **L3 com
o stick parado era ignorado**: um combo morto. E ⟨h⟩ é precisamente um ataque
vazio ("hoje" é ∅+o; nh/lh/ch são digrafos que o roll já resolve). Então o `h`
não gasta o L3 — preenche buraco, que é o princípio do §5 aplicado ao espaço dos
**botões** em vez do de clusters. Custo medido: **1,02% dos tokens** (`há`
794k, homem, hoje, hora, história).

De brinde, a ergonomia fecha: clique de analógico só briga com o gate quando o
stick está defletido. Aqui ele está centrado. O `h` foi parar no único lugar
onde clicar o L3 é de graça — e **L3 defletido continua livre**, então o layout
termina com um canal a mais, não a menos.

### O `qu` antes de A é regra, não endereço

Ele levantou "estamos sem QU antes de A". Medido:

| | tokens | formas | |
|---|---|---|---|
| ⟨qua/quo⟩ | **0,6665%** | 126 | quando, qual, quanto, qualquer, enquanto, quase, quarto, quatro |
| ⟨cua/cuo⟩ | 0,0036% | 17 | recuar, evacuação, vácuo — hiato de fronteira de morfema, não /kw/ |

185× de diferença: é uma linha do ortografador, irmã do `c+e→que` que já
existia. O `u` vem do **núcleo**, então o ataque cai para um `q` pelado.

**O escopo tem que parar em a/o.** ⟨cui/cue⟩ dá 0,0705% e é frequentíssimo em
forma (`cuidado` 110k, `cuidar` 60k): uma regra mais larga escreveria
"quidado". Antes de e/i a grafia é genuinamente disputada — ali sim precisaria
de endereço. E o ⟨g⟩ não precisa de nada: `g+u+a` já soletra "gua" (água,
língua, guarda).

Aplicada a peneira do §12 item 6, portanto: **derivável por regra → não gasta
slot.**

### O bug que o log pegou: o backspace comia um caractere invisível

O buffer carrega o arquifonema `/N/`, que não tem glifo: "questão" é `questauN`
cru contra `questão` na tela. Um backspace apagava o `N` — e a palavra virava
**"questau"**. Uma tecla que corrompe em vez de apagar.

No log, 01:54:54: quatro apagamentos até voltar a `ques`, redigitou o **mesmo
gesto** e saiu idêntico. ~20 segundos num no-op. Em nasal simples não aparecia
(`poteN` → `potem` é 1:1), por isso passou batido nas quatro sessões anteriores.

Corrigido resolvendo o buffer **antes** de fatiar: apaga-se o que se vê. É a
mesma regra do §10 vista do outro lado — *falha silenciosa em input method vira
tentativa repetida*, e ali era tecla sem alvo, aqui é tecla com o alvo errado.

---

## 14. O recado do log, atendido (09/09, madrugada)

As quatro pendências que ele digitou dentro da própria ferramenta. Nenhuma
gastou endereço novo além de um: a peneira do §12 item 6 barrou o resto.

### Maiúscula — metade é regra, metade precisava de endereço

**Início de frase é derivável** (texto vazio, ou depois de `.` `!` `?`, mesmo
com aspas ou parêntese fechando no meio), então sai sozinha. Nome próprio e
sigla não são deriváveis de nada — esses ficaram no **`↑` do d-pad**, ciclando
`caio → Caio → CAIO`.

O `↑` era o último slot livre do d-pad e tinha um concorrente anotado no §11: o
ciclador de nasal escrita (`til → n → m`), que daria saída pro "trans". Perdeu
por frequência, e não de pouco: todo nome próprio e toda sigla contra um
prefixo marcado.

**A maiúscula é escrita no buffer, não na tela.** Fosse só na exibição, seria
exatamente o bug do §13 outra vez, com o backspace e o d-pad editando uma
string diferente da que se lê.

### O arquifonema deixou de ser uma letra

Consequência que só apareceu ao pôr maiúscula: com `/N/` como sentinela, a
regra que resolve a nasal antes de vogal (`/N(?=[a-zà-ú])/gi`) **comia o N
maiúsculo de "Não"** — a palavra voltava pra "não" sozinha. Trocado por `~`.

> **Sentinela não pode ser um caractere que o conteúdo também usa.** Enquanto o
> buffer era todo minúsculo o conflito ficou latente por cinco sessões.

### ⟨x⟩ entrou no ciclo, não ganhou botão

Em *próximo*, *exato* e *explicar* o ⟨x⟩ escreve uma sibilante que o desenho já
alcança: é **alógrafo**, e alógrafo é ciclo — a mesma peneira que matou o `ç` e
o `z` como endereços. O ciclo do `↓` virou `ss → s → z → x`.

O que faltava de verdade era a **posição**: o regex exigia vogal ou fim de
palavra depois da sibilante, então a **coda antes de consoante era
inalcançável**. É onde moram `explicar` (44k), `experiência` (32k), `exceto`,
`excelente`, `extra` — 0,1554% dos tokens — e é por isso que "esplico" saiu sem
conserto no log. Entre vogais, ⟨x⟩ vale outros 0,617%.

Custo do membro novo: fechar o ciclo passou a custar um toque a mais. Ele entra
por último porque é a mais rara das grafias.

### Parênteses e aspas: um botão serve o par

`X` virou a família inteira da pontuação, com o qualificador dizendo qual —
`RB` vírgula, **`LB` parêntese, `LT` aspas**. Fora da sílaba os bumpers não
disputam com coda nenhuma, então custou zero.

Qual dos dois membros do par é a vez **é derivável do texto**: parêntese pelo
saldo de abertos, aspas pela paridade. Um botão, sem modo pra lembrar e sem
estado pra dessincronizar. E a posição do espaço é o que torna o par legível: o
que abre cola na palavra seguinte, o que fecha na anterior.

**Estado:** 100 casos no motor, 79 na UI.

---

**Em aberto:**
2. O conflito líquida×coda de A continua de pé — **186 formas** contra 122 de B,
   porque em B a líquida vinha do roll e não colidia com RB. O roll agora está
   provado como canal viável; a pergunta é se a líquida deve migrar para ele, e
   com o que qualificar a coda depois disso.
3. O modo letra-a-letra (a maiúscula saiu do ar em 09/09: regra + ciclo no ↑).
4. Labialização no ataque: `qu`+a resolvido por regra em 09/09; falta o
   tritongo (Uruguai, quais) e o /kw/ antes de e/i (frequente, tranquilo).
5. Resto da pontuação (dois-pontos, ponto-e-vírgula, travessão).
7. `L3` com o analógico **defletido** segue livre — o único canal sobrando do
   desenho inteiro. Não gastar sem passar pela peneira do item 6.
8. Coda complexa /rs/, agora sem endereço nenhum (0,0279%, metade estrangeira).
   Só volta se o item 7 for gasto nela, e provavelmente não vale.
6. Antes de gastar qualquer slot novo, a peneira: **é derivável por regra? → é
   alógrafo de algo que já tem endereço? → tem endereço natural na roda?** Slot
   livre é passivo, não ativo: endereço barato faz gastar tabela onde uma regra
   sairia de graça. Foi assim que nasceram o `ç` e o `z` que acabaram de morrer.
