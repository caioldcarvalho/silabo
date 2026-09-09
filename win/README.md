# silabo-win — o método nativo no Windows

O sílabo funcionando por cima de qualquer aplicativo: o controle é lido sem
foco, a sílaba é montada pelo **mesmo motor** do protótipo web, e o texto é
injetado na janela de baixo.

| | como | estado |
|---|---|---|
| ler o controle com OUTRO app em foco | XInput é polling, não precisa de foco | ✅ roda |
| injetar texto no app de baixo | `SendInput` + `KEYEVENTF_UNICODE` | ✅ roda |
| overlay sem roubar o foco | `WS_EX_NOACTIVATE` + `WS_EX_TRANSPARENT` | ✅ roda |
| acorde de ativação | botão do acorde + LT+RT | ✅ roda, ver o Guide |
| motor completo (roll, coda, ortografia) | `Motor.cs`, provado contra gabarito | ✅ 65.058 asserções |
| cursor dentro do texto (página 2 do d-pad) | precisa mover o caret do app | ❌ não ligado |

Rode `silabo.exe --teste` pra provar a injeção **sem o controle**: ele chama o
`SendInput` de verdade e escreve o resultado em `%TEMP%\silabo-teste.txt`.

## Os três arquivos

| | o quê |
|---|---|
| `Motor.cs` | o motor silábico, portado do `index.html`. **Puro** — compila em `net8.0` sem Windows, e é ele que atravessa inteiro no dia do backend Linux. |
| `Reconcilia.cs` | a ponte pro aplicativo de baixo. Também puro, também testado. |
| `Native.cs` / `Program.cs` | tudo que é Win32: XInput, SendInput, a janela, o desenho. |

## Rodar

```fish
cd ~/workspace/silabo
dotnet.exe build win/silabo-win.csproj -c Release
chmod +x win/bin/Release/net8.0-windows/silabo.exe   # o WSL não herda o +x
./win/bin/Release/net8.0-windows/silabo.exe          # acorde no botão Back
./win/bin/Release/net8.0-windows/silabo.exe --guide  # acorde no botão Xbox
```

Duas armadilhas do WSL, as duas já pagas: o `.exe` sai **sem permissão de
execução** e não roda direto do shell do Linux; e o `timeout` do coreutils mata
o **wrapper**, não o processo do Windows — um `silabo.exe` esquecido segura o
build seguinte e só morre com `taskkill.exe /PID`.

A overlay aparece no canto inferior direito. **Ela nasce parada**: LT+RT+3× no
botão do acorde liga e desliga. Ligada, o RT confirma a sílaba e ela é digitada
na janela que estiver com o foco.

## Dois bugs que a primeira rodada pegou

**1. Detectava tudo e não digitava.** A struct `INPUT` é uma **união**, e o
tamanho dela é o do maior membro (`MOUSEINPUT`). Declarando só o `KEYBDINPUT`
ela media **32 bytes**; o `SendInput` exige **40**. Com o `cbSize` errado ele
**devolve 0 e não insere nada, sem erro e sem exceção** — a falha silenciosa
que este projeto inteiro persegue, agora do lado nativo. `Digita()` passou a
conferir o retorno e a overlay mostra o erro.

**2. O acorde colidia com o próprio método.** `LT+RT` era o prefixo da
ativação, mas **LT é vozeamento e RT é commit** — os dois juntos acontecem em
toda sílaba vozeada (25% delas, medido). Quem discrimina agora é o **botão**;
enquanto ele está segurado o commit fica suspenso, e fora disso a digitação é
normal.

## O problema do botão Xbox

O pedido era `LT + RT + 3×Xbox`. O botão Guide **não existe na API documentada
do XInput** — `XInputGetState` o mascara. Ele só sai pelo **ordinal 100**
(`XInputGetStateEx`), que não tem nome exportado nem documentação, e some se a
Microsoft mudar de ideia. Além disso o Game Bar também escuta esse botão.

No navegador ele existe: o pad reporta `mapping: standard` com **17 botões**, e
no layout W3C o índice 16 é o Guide. Ou seja, o protótipo web tem um endereço
que o nativo não tem — é uma assimetria a lembrar sempre que um gesto for
desenhado no web e portado.

Aqui o `#100` é tentado e, se falhar, cai no `XInputGetState` sem quebrar. O
**default é o botão Back**, que é documentado e não disputa com o sistema.

## O gabarito: como duas implementações não divergem

A partir do momento em que o motor existe duas vezes, elas divergem em silêncio.
Um teste em C# escrito à mão não resolve: ele prova que o C# faz o que **o autor
do teste achou** que o JS fazia, que é exatamente o erro que a divergência é.

Então o JS não é consultado, é a **fonte**:

```fish
node tools/gabarito.mjs            # varre o motor JS → test/gabarito.json
dotnet.exe run --project win/teste # o C# afirma contra o arquivo
```

`test/gabarito.json` guarda **65 mil saídas** do motor JS: varredura exaustiva
onde o espaço é finito (toda sequência de gates até 4 posições, o cross-product
inteiro do ortografador) e amostra determinística onde não é (4.000 palavras de
várias sílabas, semente fixa). Nenhum valor esperado é escrito à mão no
`Prova.cs` — ele só lê o que o JS produziu e compara.

Onde o gabarito guarda só as saídas (a ortografia), **a ordem dos laços é o
contrato**: está escrita nos dois lados e a contagem confere se bateu.

### O que a mutação revelou

Um teste verde que não sabe ficar vermelho não prova nada, então o port foi
quebrado de propósito, uma regra por vez. Seis das oito mutações morreram na
hora. Duas sobreviveram, e as duas ensinaram algo:

- **`Math.Round` do .NET no lugar do arredondamento do JS.** Passou batido: o
  gabarito varria letras e o gate não produz letra nenhuma. O .NET arredonda
  para o **par** e o JS para cima — numa fronteira de gate isso é o dedo cair na
  casa errada. Foi preciso **procurar os empates**: os pontos em que `a/(π/4)`
  cai exatamente em `k+0,5`, que existem e são alcançáveis com o analógico
  defletido. Estão no gabarito agora, achados por busca dentro do gerador.
- **O guarda de gate repetido no `track`.** Sobreviveu porque é **código morto
  nos dois lados**: o `if (g == s.Live) return` logo acima já garante a
  condição. Mutante equivalente, não buraco de cobertura.

## A reconciliação

Na web o texto mora num buffer que a página desenha, então uma pós-correção é só
reescrever a string. Aqui o texto já está **dentro do Bloco de Notas**, e a
única coisa que a overlay pode fazer é mandar teclas.

Então ela guarda o que acredita ter mandado e calcula o menor conserto: apagar o
sufixo que divergiu, redigitar o resto. É isso que faz `vose` virar `você` com
dois backspaces em vez de apagar a frase.

Medido nas duas frases-modelo: **367 caracteres digitados, 35 apagados, maior
conserto = 7**. O teto de segurança é 40 — acima disso a overlay **recusa e
reancora**, porque um conserto grande significa que o `Emitido` dessincronizou
(o usuário clicou noutro lugar, outra janela roubou o foco) e apagar 300
caracteres que a overlay não escreveu é pior do que deixar de corrigir.

Pelo mesmo motivo, **ligar o método é sempre um recomeço**: o buffer e o
`Emitido` zeram juntos.

## O que ainda não está aqui

- **A página 2 do d-pad (cursor).** Ela move um gap buffer nosso; no nativo
  precisaria mover o caret do aplicativo de baixo e reancorar a reconciliação a
  partir dali. Não estar ligada é melhor do que estar ligada errada.
- **Telemetria.** O log de sessão só existe no web.
- **Linux.** `Motor.cs` e `Reconcilia.cs` atravessam inteiros; `Native.cs` ganha
  um irmão (evdev + uinput). A overlay é a parte difícil — no Wayland precisa de
  layer-shell.
