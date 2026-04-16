namespace MatchingGame.Models
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
        Monkey   // 🐒 Mini-game using monkey SVG images
    }

    /// <summary>
    /// Encapsulates difficulty settings.
    /// Demonstrates ENCAPSULATION - bundles data with behavior.
    /// </summary>
    public class DifficultySettings
    {
        public Difficulty Level { get; private set; }
        public int GridSize { get; private set; }       // e.g. 4 = 4x4
        public int TimeLimitSeconds { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsMonkeyMode { get; private set; }

        private DifficultySettings() { }

        // POLYMORPHISM: Factory method overloading
        public static DifficultySettings Create(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return new DifficultySettings
                    {
                        Level = Difficulty.Easy,
                        GridSize = 4,
                        TimeLimitSeconds = 0,   // No limit
                        DisplayName = "Easy (4×4)",
                        IsMonkeyMode = false
                    };
                case Difficulty.Medium:
                    return new DifficultySettings
                    {
                        Level = Difficulty.Medium,
                        GridSize = 6,
                        TimeLimitSeconds = 120,
                        DisplayName = "Medium (6×6)",
                        IsMonkeyMode = false
                    };
                case Difficulty.Hard:
                    return new DifficultySettings
                    {
                        Level = Difficulty.Hard,
                        GridSize = 6,
                        TimeLimitSeconds = 60,
                        DisplayName = "Hard (6×6, 60s)",
                        IsMonkeyMode = false
                    };
                case Difficulty.Monkey:
                    return new DifficultySettings
                    {
                        Level = Difficulty.Monkey,
                        GridSize = 4,           // 4×4 = 8 pairs, matches our 9 monkey images
                        TimeLimitSeconds = 0,
                        DisplayName = "🐒 Monkey (4×4)",
                        IsMonkeyMode = true
                    };
                default:
                    return Create(Difficulty.Easy);
            }
        }
    }
}

