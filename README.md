# Bats Cards

Paciência Klondike Compra 1 para Windows e Android, desenvolvido em Unity 6.3 LTS.

## Abrir

```powershell
unity open .
```

Na primeira abertura, o configurador cria a cena `Assets/Scenes/Main.unity` e adiciona-a ao Build Settings.

## Testes

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Builds/editmode-results.xml -quit
```

## Builds

No Unity, use:

- `Bats Cards > Build > Windows Development` para teste local em Mono.
- `Bats Cards > Build > Windows` para release IL2CPP (requer Visual Studio C++ e Windows SDK).
- `Bats Cards > Build > Android AAB` para o pacote Android ARM64/IL2CPP.

As 52 faces e o verso exportados do Canva já estão em `Assets/Art/Cards/`, acompanhados do manifesto bilíngue de espécies.
