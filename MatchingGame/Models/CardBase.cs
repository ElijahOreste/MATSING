using System;
using System.Drawing;
using System.Windows.Forms;

namespace MatchingGame.Models
{
    /// <summary>
    /// Abstract base class for all card types.
    /// Demonstrates ABSTRACTION - hides complex implementation details.
    /// Demonstrates ENCAPSULATION - private fields with public properties.
    /// </summary>
    public abstract class CardBase
    {
        // ENCAPSULATION: private backing fields
        private bool _isRevealed;
        private bool _isMatched;
        private string _symbol;
        private Color _cardColor;

        // Properties with controlled access
        public bool IsRevealed
        {
            get => _isRevealed;
            protected set => _isRevealed = value;
        }

        public bool IsMatched
        {
            get => _isMatched;
            protected set => _isMatched = value;
        }

        public string Symbol
        {
            get => _symbol;
            protected set => _symbol = value;
        }

        public Color CardColor
        {
            get => _cardColor;
            protected set => _cardColor = value;
        }

        public int Row { get; set; }
        public int Column { get; set; }

        // ABSTRACTION: abstract methods that subclasses must implement
        public abstract void Reveal();
        public abstract void Hide();
        public abstract void MarkMatched();
        public abstract bool MatchesWith(CardBase other);

        // Virtual method for POLYMORPHISM - can be overridden
        public virtual string GetDisplayText()
        {
            return IsRevealed || IsMatched ? Symbol : string.Empty;
        }

        protected CardBase(string symbol)
        {
            _symbol = symbol;
            _isRevealed = false;
            _isMatched = false;
        }
    }
}
