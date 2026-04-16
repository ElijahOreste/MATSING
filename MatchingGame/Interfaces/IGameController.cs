using System.Collections.Generic;
using MatchingGame.Models;

namespace MatchingGame.Interfaces
{
    /// <summary>
    /// Interface for game controller.
    /// Demonstrates ABSTRACTION - defines contract without implementation.
    /// </summary>
    public interface IGameController
    {
        GameState State { get; }
        DifficultySettings Settings { get; }
        List<List<CardBase>> Board { get; }

        void StartNewGame(Difficulty difficulty);
        bool RevealCard(int row, int col);
        void ResetGame();
        event System.EventHandler<MatchEventArgs> MatchFound;
        event System.EventHandler<MatchEventArgs> MatchMissed;
        event System.EventHandler GameCompleted;
    }

    public class MatchEventArgs : System.EventArgs
    {
        public CardBase Card1 { get; set; }
        public CardBase Card2 { get; set; }
        public bool IsMatch { get; set; }
    }
}
