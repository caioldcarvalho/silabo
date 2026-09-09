# silabo-win — spike do método nativo

Prova as quatro coisas que só o Windows responde, e que não dá pra saber sem rodar:

| | como | estado |
|---|---|---|
| ler o controle com OUTRO app em foco | XInput é polling, não precisa de foco | ✅ compila |
| injetar texto no app de baixo | `SendInput` + `KEYEVENTF_UNICODE` | ✅ compila |
| overlay sem roubar o foco | `WS_EX_NOACTIVATE` + `WS_EX_TRANSPARENT` | ✅ compila |
| acorde de ativação | botão do acorde + LT+RT | ⚠️ ver o Guide |

Rode `silabo.exe --teste` pra provar a injeção **sem o controle**: ele chama o
`SendInput` de verdade e escreve o resultado em `%TEMP%\silabo-teste.txt`.

## Rodar

```fish
cd ~/workspace/silabo/win
"/mnt/c/Program Files/dotnet/dotnet.exe" build -c Release
./bin/Release/net8.0-windows/silabo.exe          # acorde no botão Back
./bin/Release/net8.0-windows/silabo.exe --guide  # acorde no botão Xbox
```

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

## O que este spike NÃO é

O motor silábico aqui é um **toco**: só as 8 cardinais de ataque e núcleo, sem
roll, sem coda, sem ortografador. Portar o motor de verdade é o passo seguinte,
e a regra é: **gerar um arquivo-gabarito a partir do motor JS** (todas as formas
alcançáveis + as frases-modelo) e fazer o teste do C# afirmar contra ele. Sem
isso passam a existir duas implementações que divergem em silêncio — que é
exatamente o que `test/motor.test.mjs` evita hoje extraindo o JS do
`index.html` em vez de manter uma cópia.
