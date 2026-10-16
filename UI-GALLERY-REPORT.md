# Bats Cards — UI e galeria

## Implementado

- Sete colunas preservadas. Nenhuma alteração nas regras, distribuição ou pontuação.
- Índice de mesa em faixa de UI com texto de 14 pixels, separado da textura reduzida. Apenas o índice superior esquerdo rasterizado é coberto; o original completo continua disponível no visualizador. Fotos, nomes, espécies e arquivos PNG não foram modificados.
- HUD responsivo, controles de texto em telas compactas, menu em duas colunas em paisagem baixa e tipografia compartilhada.
- Medidas de leitura/toque consideram a densidade do Android. Pilhas priorizam espaço para os índices abertos, comprimindo primeiro os versos quando há espaço suficiente.
- Ícones vetoriais nativos com grade e espessura uniformes. Sem bibliotecas adicionais. Estados de foco, hover, pressionado e desabilitado nos botões.
- Galeria com 52 cartas, rolagem vertical, filtros dos quatro naipes e destaque do filtro ativo. Usa o mesmo DeckManifest da mesa.
- Visualizador com imagem original integral, contador, anterior/próxima, swipe horizontal, setas do teclado, Escape, zoom e retorno à galeria. No zoom, arrastar movimenta a imagem; com a imagem ajustada à tela, swipe navega.
- Transições leves entre telas e cartas, desativadas pela preferência de redução de movimento.
- A galeria não referencia GameSession nem SaveService.

## Arquivos

Criados: `Assets/Scripts/Presentation/CardGallery.cs`, `CardSwipe.cs`, `UiIcon.cs` e seus metadados Unity.

Modificados na mesma pasta: `AppBootstrap.cs`, `UiFactory.cs`, `CardView.cs`, `CardDetails.cs`, `GameScreen.cs`, `DevelopmentQa.cs`.

Teste ampliado: `Assets/Tests/PlayMode/CardInteractionTests.cs`.

## Validação executada

- EditMode: 11 testes aprovados (`Builds/gallery-editmode.xml`).
- PlayMode: 6 testes aprovados (`Builds/gallery-playmode.xml`). Incluem seleção, duplo clique, arrasto de sequência, retorno de movimento inválido, coluna longa, imagem original no inspetor, filtros, percurso pelas 52 imagens, retorno circular, swipe simulado e preservação do estado e do arquivo salvo durante a galeria.
- Windows Development compilado e executado. QA em 320×720, 360×800, 390×844, 430×932, 800×360, 768×1024 e 1280×720. Capturas de menu, mesa, galeria e visualizador em `Builds/QA-Gallery`; verificação automatizada dos limites das cartas da distribuição inicial.
- Conferência SHA256 das 52 faces e do verso no build: 53 originais preservados.
- Log de execução sem exceções encontradas: `Builds/gallery-runtime.log`.

## Limites da validação

- Resoluções simuladas no player Windows, não em aparelhos Android físicos. Toque físico, desempenho de 60 FPS e comportamento em diferentes densidades/notches ainda precisam de teste em dispositivo.
- As teclas estão implementadas; o teste da navegação usa os métodos/eventos correspondentes, não injeção de teclado físico.
- Nomes e espécies são exibidos a partir da própria arte original; não foi feita revisão científica ou correção do conteúdo do baralho.
- Pilhas excepcionalmente longas continuam usando a compressão vertical existente; a legibilidade de todas as cartas dessas pilhas em paisagem baixa não está certificada.
- Os pacotes antigos de distribuição não representam automaticamente esta revisão. Use os arquivos indicados na entrega desta alteração.
