using System.Drawing;
using MatchingGame.Utils;

namespace MatchingGame.Models
{
    /// <summary>
    /// A card that displays a monkey image instead of an emoji.
    /// Demonstrates INHERITANCE from CardBase and POLYMORPHISM via overrides.
    /// </summary>
    public class MonkeyCard : CardBase
    {
        /// <summary>1-based index of the monkey SVG (1–9).</summary>
        public int ImageIndex { get; private set; }

        public MonkeyCard(int imageIndex, int row, int col)
            : base($"MONKEY_{imageIndex}")
        {
            ImageIndex = imageIndex;
            Row = row;
            Column = col;
            CardColor = Color.FromArgb(205, 133, 63); // warm wooden/monkey tone
        }

        public override void Reveal()    => IsRevealed = true;
        public override void Hide()      { if (!IsMatched) IsRevealed = false; }
        public override void MarkMatched() { IsMatched = true; IsRevealed = true; }

        public override bool MatchesWith(CardBase other)
            => other != null && other.Symbol == Symbol && !ReferenceEquals(this, other);

        public override string GetDisplayText() => "🐒"; // fallback text (image takes priority)

        /// <summary>Returns the pre-loaded monkey Image (may be null if load failed).</summary>
        public Image GetImage() => MonkeyImageLoader.GetMonkeyImage(ImageIndex);
    }
}
