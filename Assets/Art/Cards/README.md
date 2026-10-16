# Exportação das cartas

As cartas foram exportadas do Canva como PNG em alta resolução sem alterar o design original.

Use estes nomes:

- `Clubs_Ace.png` até `Clubs_King.png`
- `Diamonds_Ace.png` até `Diamonds_King.png`
- `Hearts_Ace.png` até `Hearts_King.png`
- `Spades_Ace.png` até `Spades_King.png`
- `Back.png`

No Unity, execute `Bats Cards > Refresh Deck Manifest`. O importador valida as 52 faces, o verso e carrega `deck-metadata.csv`.

Auditoria do arquivo original (108 páginas): página 1 é o verso; páginas pares 2–104 são as 52 faces na ordem Copas, Paus, Espadas e Ouros; as demais páginas são versos repetidos ou extras e ficaram fora do build.
