using System;
using System.Drawing;

namespace MatchingGame.Models
{
    /// <summary>
    /// Concrete icon card implementation.
    /// Demonstrates INHERITANCE - inherits from CardBase.
    /// Demonstrates POLYMORPHISM - overrides abstract methods.
    /// </summary>
    public class IconCard : CardBase
    {
        private static readonly Color[] CardColors = new Color[]
        {
            Color.FromArgb(255, 107, 107),   // Coral Red
            Color.FromArgb(255, 165, 2),     // Orange
            Color.FromArgb(255, 217, 61),    // Yellow
            Color.FromArgb(78, 205, 196),    // Teal
            Color.FromArgb(85, 183, 255),    // Sky Blue
            Color.FromArgb(162, 103, 255),   // Purple
            Color.FromArgb(255, 107, 188),   // Pink
            Color.FromArgb(100, 220, 120),   // Green
        };

        private static int _colorIndex = 0;

        public IconCard(string symbol, int row, int col) : base(symbol)
        {
            Row = row;
            Column = col;
            // Assign a color based on symbol uniqueness
            CardColor = GetColorForSymbol(symbol);
        }

        private static Color GetColorForSymbol(string symbol)
        {
            int hash = Math.Abs(symbol.GetHashCode()) % CardColors.Length;
            return CardColors[hash];
        }

        // POLYMORPHISM: method overriding
        public override void Reveal()
        {
            IsRevealed = true;
        }

        public override void Hide()
        {
            if (!IsMatched)
                IsRevealed = false;
        }

        public override void MarkMatched()
        {
            IsMatched = true;
            IsRevealed = true;
        }

        public override bool MatchesWith(CardBase other)
        {
            return other != null && other.Symbol == this.Symbol && !ReferenceEquals(this, other);
        }

        // POLYMORPHISM: method overriding with extension
        public override string GetDisplayText()
        {
            if (IsMatched) return Symbol;
            if (IsRevealed) return Symbol;
            return "?";
        }
    }
}
