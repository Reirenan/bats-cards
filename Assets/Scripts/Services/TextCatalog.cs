using System.Collections.Generic;

namespace BatsSolitaire.Services
{
    public sealed class TextCatalog
    {
        private readonly Dictionary<string, string> pt = new()
        {
            ["title"] = "BATS CARDS",
            ["tagline"] = "Descubra os morcegos do Cerrado Maranhense",
            ["random"] = "Partida aleatória",
            ["winnable"] = "Partida vencível",
            ["daily"] = "Desafio diário",
            ["resume"] = "Continuar",
            ["undo"] = "Desfazer",
            ["hint"] = "Dica",
            ["restart"] = "Reiniciar",
            ["menu"] = "Menu",
            ["score"] = "Pontos",
            ["moves"] = "Jogadas",
            ["time"] = "Tempo",
            ["win"] = "Você venceu!",
            ["no_hint"] = "Nenhuma jogada encontrada",
            ["settings"] = "Configurações",
            ["back"] = "Voltar",
            ["reduce_motion"] = "Reduzir animações",
            ["high_contrast"] = "Alto contraste",
            ["left_handed"] = "Modo canhoto",
            ["haptics"] = "Vibração",
            ["music"] = "Música",
            ["effects"] = "Efeitos",
            ["ui_scale"] = "Escala da interface"
        };

        private readonly Dictionary<string, string> en = new()
        {
            ["title"] = "BATS CARDS",
            ["tagline"] = "Discover the bats of the Maranhão Cerrado",
            ["random"] = "Random game",
            ["winnable"] = "Winnable game",
            ["daily"] = "Daily challenge",
            ["resume"] = "Continue",
            ["undo"] = "Undo",
            ["hint"] = "Hint",
            ["restart"] = "Restart",
            ["menu"] = "Menu",
            ["score"] = "Score",
            ["moves"] = "Moves",
            ["time"] = "Time",
            ["win"] = "You won!",
            ["no_hint"] = "No move found",
            ["settings"] = "Settings",
            ["back"] = "Back",
            ["reduce_motion"] = "Reduce motion",
            ["high_contrast"] = "High contrast",
            ["left_handed"] = "Left-handed mode",
            ["haptics"] = "Haptics",
            ["music"] = "Music",
            ["effects"] = "Effects",
            ["ui_scale"] = "UI scale"
        };

        public string Language { get; set; } = "pt-BR";
        public string Get(string key)
        {
            var source = Language.StartsWith("en") ? en : pt;
            return source.TryGetValue(key, out var value) ? value : key;
        }
    }
}
