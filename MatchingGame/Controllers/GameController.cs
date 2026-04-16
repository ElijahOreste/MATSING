using System;
using System.Collections.Generic;
using MatchingGame.Interfaces;
using MatchingGame.Models;
using MatchingGame.Utils;

namespace MatchingGame.Controllers
{
    /// <summary>
    /// Core game controller. Implements IGameController.
    /// Demonstrates ABSTRACTION (via interface), ENCAPSULATION (private fields).
    /// </summary>
    public class GameController : IGameController
    {
        // All emoji symbols available
        private static readonly string[] AllSymbols = new string[]
        {
            "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼",
            "🐨", "🐯", "🦁", "🐮", "🐷", "🐸", "🐙", "🦋",
            "🌸", "🌻", "🍎", "🍕", "⚽", "🎸", "🚀", "🏆",
            "💎", "⭐", "🎯", "🎲", "🎪", "🌈", "🍦", "🎃"
        };

        private readonly Random _rng = new Random();
        private CardBase _firstCard = null;
        private CardBase _secondCard = null;
        private bool _isProcessing = false;

        // ENCAPSULATION
        private GameState _state;
        private DifficultySettings _settings;
        private List<List<CardBase>> _board;

        public GameState State => _state;
        public DifficultySettings Settings => _settings;
        public List<List<CardBase>> Board => _board;

        // Events for UI notification
        public event EventHandler<MatchEventArgs> MatchFound;
        public event EventHandler<MatchEventArgs> MatchMissed;
        public event EventHandler GameCompleted;

        public GameController()
        {
            _settings = DifficultySettings.Create(Difficulty.Easy);
        }

        public void StartNewGame(Difficulty difficulty)
        {
            _settings = DifficultySettings.Create(difficulty);
            _board = GenerateBoard(_settings.GridSize);
            int totalPairs = (_settings.GridSize * _settings.GridSize) / 2;
            _state = new GameState(totalPairs);
            _firstCard = null;
            _secondCard = null;
            _isProcessing = false;
        }

        private List<List<CardBase>> GenerateBoard(int size)
        {
            // --- Monkey mini-game uses image cards ---
            if (_settings.IsMonkeyMode)
                return GenerateMonkeyBoard(size);

            int totalCards = size * size;
            int pairs = totalCards / 2;

            // Pick random symbols
            var shuffled = new List<string>(AllSymbols);
            Shuffle(shuffled);
            var selectedSymbols = shuffled.GetRange(0, pairs);

            // Duplicate for pairs
            var cardSymbols = new List<string>(selectedSymbols);
            cardSymbols.AddRange(selectedSymbols);
            Shuffle(cardSymbols);

            // Build 2D board
            var board = new List<List<CardBase>>();
            int idx = 0;
            for (int r = 0; r < size; r++)
            {
                var row = new List<CardBase>();
                for (int c = 0; c < size; c++)
                {
                    row.Add(new IconCard(cardSymbols[idx++], r, c));
                }
                board.Add(row);
            }
            return board;
        }

        private List<List<CardBase>> GenerateMonkeyBoard(int size)
        {
            int totalCards = size * size;   // 4x4 = 16
            int pairs = totalCards / 2;     // 8 pairs

            // Pick 'pairs' unique monkey indices (we have 9, pick 8)
            int available = MonkeyImageLoader.Count; // 9
            var indices = new List<int>();
            for (int i = 1; i <= available; i++) indices.Add(i);
            Shuffle(indices);
            var selected = indices.GetRange(0, Math.Min(pairs, available));

            // Duplicate for pairs
            var cardIndices = new List<int>(selected);
            cardIndices.AddRange(selected);
            Shuffle(cardIndices);

            var board = new List<List<CardBase>>();
            int idx = 0;
            for (int r = 0; r < size; r++)
            {
                var row = new List<CardBase>();
                for (int c = 0; c < size; c++)
                    row.Add(new MonkeyCard(cardIndices[idx++], r, c));
                board.Add(row);
            }
            return board;
        }

        public bool RevealCard(int row, int col)
        {
            if (_isProcessing) return false;
            var card = _board[row][col];
            if (card.IsRevealed || card.IsMatched) return false;

            card.Reveal();

            if (_firstCard == null)
            {
                _firstCard = card;
                return true;
            }
            else
            {
                _secondCard = card;
                _state.RecordMove();
                _isProcessing = true;

                if (_firstCard.MatchesWith(_secondCard))
                {
                    _firstCard.MarkMatched();
                    _secondCard.MarkMatched();
                    _state.RecordMatch();
                    MatchFound?.Invoke(this, new MatchEventArgs
                    {
                        Card1 = _firstCard,
                        Card2 = _secondCard,
                        IsMatch = true
                    });
                    _firstCard = null;
                    _secondCard = null;
                    _isProcessing = false;

                    if (_state.IsComplete)
                        GameCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _state.RecordMismatch();
                    MatchMissed?.Invoke(this, new MatchEventArgs
                    {
                        Card1 = _firstCard,
                        Card2 = _secondCard,
                        IsMatch = false
                    });
                    // Caller is responsible for calling HideMismatchedCards() after delay
                }
                return true;
            }
        }

        public void HideMismatchedCards()
        {
            _firstCard?.Hide();
            _secondCard?.Hide();
            _firstCard = null;
            _secondCard = null;
            _isProcessing = false;
        }

        public void ResetGame()
        {
            if (_settings != null)
                StartNewGame(_settings.Level);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }
    }
}
