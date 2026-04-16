using System;

namespace MatchingGame.Models
{
    /// <summary>
    /// Encapsulates game state and score tracking.
    /// Demonstrates ENCAPSULATION.
    /// </summary>
    public class GameState
    {
        private int _moves;
        private int _matchesFound;
        private int _totalPairs;
        private DateTime _startTime;
        private int _score;

        public int Moves => _moves;
        public int MatchesFound => _matchesFound;
        public int TotalPairs => _totalPairs;
        public bool IsComplete => _matchesFound >= _totalPairs;
        public int Score => _score;

        public TimeSpan ElapsedTime => DateTime.Now - _startTime;

        public GameState(int totalPairs)
        {
            _totalPairs = totalPairs;
            Reset();
        }

        public void Reset()
        {
            _moves = 0;
            _matchesFound = 0;
            _score = 0;
            _startTime = DateTime.Now;
        }

        public void RecordMove()
        {
            _moves++;
        }

        public void RecordMatch()
        {
            _matchesFound++;
            // Score: more points for fewer moves and less time
            int timeBonus = Math.Max(0, 100 - (int)ElapsedTime.TotalSeconds);
            _score += 100 + timeBonus;
        }

        public void RecordMismatch()
        {
            // Deduct from score on mismatch
            _score = Math.Max(0, _score - 10);
        }

        public int CalculateFinalScore()
        {
            if (_totalPairs == 0) return 0;
            double efficiency = (double)_totalPairs / Math.Max(_moves, _totalPairs);
            int timeSeconds = (int)ElapsedTime.TotalSeconds;
            int bonus = Math.Max(0, 500 - timeSeconds * 2);
            return (int)(_score * efficiency) + bonus;
        }
    }
}
