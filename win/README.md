# silabo-win — spike do método nativo

Prova as quatro coisas que só o Windows responde, e que não dá pra saber sem rodar:

| | como | estado |
|---|---|---|
| ler o controle com OUTRO app em foco | XInput é polling, não precisa de foco | ✅ compila |
| injetar texto no app de baixo | `SendInput` + `KEYEVENTF_UNICODE` | ✅ compila |
| overlay sem roubar o foco | `WS_EX_NOACTIVATE` + `WS_EX_TRANSPARENT` | ✅ compila |
| acorde de ativação | LT+RT+3× — ver abaixo | ⚠️ ver o Guide |

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
