# Bats Cards — preservação e testes mobile

## P0 — identidade do deck

- 52 faces e 1 verso comparados byte a byte com `work/canva-cards`, exportação original do Canva. Todos os hashes coincidiram e são únicos.
- `Assets/Art/Cards/original-sha256.json` registra chave, página do Canva e SHA256. Cada build valida essa baseline e bloqueia alterações ou arquivos ausentes.
- A baseline foi criada dos PNGs exportados, não da imagem conceitual de interface.
- Nenhum PNG de carta foi editado. Proporção preservada; seleção usa contorno. A visualização de detalhes usa o mesmo sprite original, inteiro no modo ajustado, com zoom e arraste opcionais.
- Os metadados anteriormente digitados em `deck-metadata.csv` não foram certificados como transcrição literal; não são usados para escrever textos sobre as cartas ou na inspeção. A fonte visível de nome, espécie e família é o PNG original. Revisão textual individual continua pendente.

## P1 — interação e mobile

- Retrato: cabeçalho em duas linhas; menus ampliados; área segura respeitada. Rotação recalcula a apresentação.
- Estoque/descarte ficam na faixa superior, separada das colunas, para evitar colisões com sequências longas.
- Sobreposição das colunas comprimida conforme espaço disponível; inspeção da arte permite ler detalhes.
- Toque seleciona; `Ver carta original` abre inspeção; duplo toque envia para fundação quando válido.
- Arraste move visualmente a sequência completa; retorno inválido animado; deslocamentos animados após jogadas e distribuição inicial. Redução de movimento elimina essas transições.
- Destinos de arraste usam áreas retangulares próprias, sem escolher arbitrariamente a primeira coluna próxima.
- Dicas destacam a origem. Menu de pausa bloqueia a mesa; inspeção e pausa suspendem o cronômetro.

## P2 — robustez e som

- Save com substituição atômica, backup e recuperação. Sair no menu com sessão vazia não apaga a partida salva.
- Áudio procedural: deslizamento de carta, envelopes para evitar descontinuidades nos efeitos e loop ambiente com ciclos inteiros. Nenhum áudio externo sem licença foi incorporado.
- Testes cobrem conservação de 52 cartas, regras, determinismo, desfazer, save/backup, seleção sem alteração de cor e arraste de sequência.

## P3 — evidências e limites de aceitação

- Resultados atuais: `Builds/mobile-editmode.xml` e `Builds/mobile-playmode.xml`.
- Ensaio de apresentação desktop com janelas 360×800, 800×360, 768×1024 e 1280×720: `Builds/QA-Mobile`. Executável Development com argumento `-bats-qa`; usa save de teste separado.
- APK para instalação local: `Builds/Android/BatsCards-Test.apk`, ARM64/IL2CPP, assinatura de desenvolvimento. Nenhuma publicação em loja.
- Ainda exigem dispositivo Android físico identificado: desempenho de 60 FPS, consumo de memória, toque real, recortes de câmera e interrupção pelo sistema operacional. Não considerar esses critérios aprovados pelo teste no Windows.
- Acabamento visual final, experiência de áudio e transcrição dos metadados requerem revisão. Esta entrega não certifica todos os critérios da versão oficial 1.0.
