using MATSING.Models;
using MATSING.Utils;

namespace MATSING.Game;

public class GameEngine
{
    // ── Private state ─────────────────────────────────────────────────────
    private List<CardBase>   _deck          = new();
    private CardBase?        _firstFlipped;
    private CardBase?        _secondFlipped;
    private int              _matchCount;
    private int              _moveCount;
    private bool             _boardLocked;
    private Difficulty       _difficulty;
    private DifficultyConfig _config        = DifficultyConfig.For(Difficulty.Easy);
    private GameModifier     _modifiers     = GameModifier.None;

    // Hearts (HardcoreMode)
    private int _hearts;
    private int _maxHearts;

    // GhostCard: set of cards currently showing blank face
    private readonly HashSet<CardBase> _ghostedCards = new(ReferenceEqualityComparer.Instance);
    private readonly Random _rng = new();
    private const float GHOST_CHANCE = 0.40f;

    // MemoryLeakMode: decoy cards (have CardId = -1, no match)
    private readonly HashSet<CardBase> _decoyCards = new(ReferenceEqualityComparer.Instance);

    // EndlessMode: how many boards completed
    private int _boardsCompleted;

    private readonly ScoreTracker _score = new();

    // ── Public Events ─────────────────────────────────────────────────────
    public event EventHandler<CardPairEventArgs>?  MatchFound;
    public event EventHandler<CardPairEventArgs>?  MismatchFound;
    public event EventHandler?                     GameWon;
    public event EventHandler?                     GameOverHardcore;
    public event EventHandler<DriftEventArgs>?     CardsDrifted;
    public event EventHandler?                     NewBoardDealt;       // EndlessMode
    public event EventHandler<CardBase>?           GhostRevealReady;   // GhostCard: real face ready
    public event EventHandler<HeartsChangedArgs>?  HeartsChanged;      // HardcoreMode hearts

    // ── Public Read-Only ──────────────────────────────────────────────────
    public int             MatchCount      => _matchCount;
    public int             MoveCount       => _moveCount;
    public int             Score           => _score.Score;
    public int             Streak          => _score.Streak;
    public bool            IsComplete      => _matchCount == TotalPairs;
    public DifficultyConfig Config         => _config;
    public GameModifier    Modifiers       => _modifiers;
    public bool            IsZenMode       => _modifiers.HasFlag(GameModifier.ZenMode);
    public int             TotalPairs      => _deck.Count(c => !_decoyCards.Contains(c)) / 2;
    public int             Hearts          => _hearts;
    public int             MaxHearts       => _maxHearts;
    public int             BoardsCompleted => _boardsCompleted;

    /// <summary>True if card is a decoy (MemoryLeakMode).</summary>
    public bool IsDecoy(CardBase card) => _decoyCards.Contains(card);

    /// <summary>True if card is currently showing its ghost (blank) face.</summary>
    public bool IsGhosted(CardBase card) => _ghostedCards.Contains(card);

    // ── StartNewGame ──────────────────────────────────────────────────────
    public void StartNewGame(Difficulty difficulty, GameModifier modifiers = GameModifier.None)
    {
        _difficulty       = difficulty;
        _modifiers        = modifiers;
        _config           = DifficultyConfig.For(difficulty);
        _matchCount       = 0;
        _moveCount        = 0;
        _boardsCompleted  = 0;
        _boardLocked      = false;
        _firstFlipped     = null;
        _secondFlipped    = null;
        _score.Reset(_modifiers.HasFlag(GameModifier.ComboMultiplier));
        _ghostedCards.Clear();
        _decoyCards.Clear();

        // Hearts for HardcoreMode
        if (_modifiers.HasFlag(GameModifier.HardcoreMode))
        {
            _maxHearts = difficulty switch
            {
                Difficulty.Easy   => 3,
                Difficulty.Medium => 4,
                Difficulty.Hard   => 5,
                _                 => 3,
            };
            _hearts = _maxHearts;
        }

        DealBoard();
    }

    /// <summary>
    /// Deals (or re-deals) a fresh board. Called on start and on each
    /// EndlessMode board completion.
    /// </summary>
    private void DealBoard()
    {
        _firstFlipped  = null;
        _secondFlipped = null;
        _boardLocked   = false;
        _ghostedCards.Clear();
        _decoyCards.Clear();

        var master   = BuildMasterDeck();
        var selected = ShuffleHelper.Shuffle(master).Take(_config.PairCount).ToList();
        var pairs    = selected.SelectMany(c => new[] { c, CloneCard(c) }).ToList();

        // MemoryLeakMode: inject N decoy cards (unmatched, disappear on flip)
        if (_modifiers.HasFlag(GameModifier.MemoryLeakMode))
        {
            int decoyCount = Math.Max(1, _config.PairCount / 3);
            for (int i = 0; i < decoyCount; i++)
            {
                // Reuse a random existing card's image but assign CardId = -(i+1) so no match
                var template = master[_rng.Next(master.Count)];
                var decoy    = new MonkeyCard(-(i + 1), "???",
                    template.ImagePath,
                    "Ghost", "Mysterious");
                pairs.Add(decoy);
                _decoyCards.Add(decoy);
            }
        }

        _deck = ShuffleHelper.Shuffle(pairs);
    }

    public IReadOnlyList<CardBase> GetDeck() => _deck.AsReadOnly();

    // ── OnCardSelected ────────────────────────────────────────────────────
    public bool OnCardSelected(CardBase card, int elapsedSeconds)
    {
        if (_boardLocked)   return false;
        if (card.IsMatched) return false;
        if (card.IsFaceUp)  return false;
        if (card == _firstFlipped || card == _secondFlipped) return false;

        // GhostCard: 40% chance this flip shows blank first for 600ms
        if (_modifiers.HasFlag(GameModifier.GhostCard) && !_decoyCards.Contains(card))
        {
            if (_rng.NextSingle() < GHOST_CHANCE)
            {
                _ghostedCards.Add(card);
                // After 600ms, un-ghost and fire the ready event so UI shows real face
                var captured = card;
                Task.Delay(600).ContinueWith(_ =>
                {
                    _ghostedCards.Remove(captured);
                    GhostRevealReady?.Invoke(this, captured);
                });
            }
        }

        card.FlipUp();
        card.Reveal();

        if (_firstFlipped == null)
        {
            _firstFlipped = card;
            return true;
        }

        _secondFlipped = card;
        _moveCount++;
        _boardLocked = true;
        EvaluatePair(elapsedSeconds);
        return true;
    }

    private void EvaluatePair(int elapsedSeconds)
    {
        CardBase first  = _firstFlipped!;
        CardBase second = _secondFlipped!;

        // Decoy card: always a mismatch — card vanishes after a short pause
        if (_decoyCards.Contains(first) || _decoyCards.Contains(second))
        {
            _score.RegisterMismatch();
            MismatchFound?.Invoke(this, new CardPairEventArgs(first, second));
            // Decoys are removed from the deck after being shown (disappear)
            // The UI handles the visual; engine marks them "matched" so they
            // are no longer interactable, but doesn't count toward the match total.
            if (_decoyCards.Contains(first))  { first.MarkAsMatched();  _decoyCards.Remove(first); }
            if (_decoyCards.Contains(second)) { second.MarkAsMatched(); _decoyCards.Remove(second); }
            ResetFlipGroup();
            _boardLocked = false;
            return;
        }

        bool match = first.CardId == second.CardId;

        if (match)
        {
            foreach (var c in new[] { first, second }) c.MarkAsMatched();
            _matchCount++;
            _score.RegisterMatch(elapsedSeconds, first.PointValue);
            MatchFound?.Invoke(this, new CardPairEventArgs(first, second));
            ResetFlipGroup();
            _boardLocked = false;

            if (IsComplete)
            {
                if (_modifiers.HasFlag(GameModifier.EndlessMode))
                {
                    _boardsCompleted++;
                    _matchCount = 0;
                    DealBoard();
                    NewBoardDealt?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    GameWon?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        else
        {
            _score.RegisterMismatch();

            if (_modifiers.HasFlag(GameModifier.HardcoreMode))
            {
                _hearts--;
                HeartsChanged?.Invoke(this, new HeartsChangedArgs(_hearts, _maxHearts));

                if (_hearts <= 0)
                {
                    MismatchFound?.Invoke(this, new CardPairEventArgs(first, second));
                    // Short delay so UI shows the bad flip before game over fires
                    Task.Delay(700).ContinueWith(_ =>
                        GameOverHardcore?.Invoke(this, EventArgs.Empty));
                    return;
                }
            }

            MismatchFound?.Invoke(this, new CardPairEventArgs(first, second));
        }
    }

    public void FlipDownMismatch()
    {
        _firstFlipped?.FlipDown();
        _secondFlipped?.FlipDown();
        ResetFlipGroup();
        _boardLocked = false;
    }

    public void TriggerCardDrift()
    {
        var unmatchedIdx = _deck
            .Select((c, i) => (c, i))
            .Where(x => !x.c.IsMatched)
            .Select(x => x.i)
            .ToList();

        var shuffled = ShuffleHelper.Shuffle(unmatchedIdx);
        var newDeck  = _deck.ToList();
        for (int i = 0; i < unmatchedIdx.Count; i++)
            newDeck[unmatchedIdx[i]] = _deck[shuffled[i]];

        _deck = newDeck;
        CardsDrifted?.Invoke(this, new DriftEventArgs(_deck.AsReadOnly()));
    }

    private void ResetFlipGroup()
    {
        _firstFlipped  = null;
        _secondFlipped = null;
    }

    private static List<CardBase> BuildMasterDeck() => new()
    {
        new MonkeyCard(1, "Serenade", @"Assets\Cards\download__7_.jpg",          "Troubadour",    "Romantic"),
        new MonkeyCard(2, "CEO Mode", @"Assets\Cards\download__2_.jpg",           "Executive",     "Serious"),
        new MonkeyCard(3, "Scholar",  @"Assets\Cards\meme_macaco.jpg",            "Scribbles",     "Intellectual"),
        new MonkeyCard(4, "Shocked",  @"Assets\Cards\download__4_.jpg",           "Gaspito",       "Dramatic"),
        new MonkeyCard(5, "Ottoman",  @"Assets\Cards\turkish_monkey__thumb_up__reaction_pic.jpg", "Mehmet Bey", "Authoritative"),
        new MonkeyCard(6, "Romeo",    @"Assets\Cards\Instagram_fahimvine.jpg",    "Señor Flower",  "Charming"),
        new MonkeyCard(7, "Cutie",    @"Assets\Cards\download__5_.jpg",           "Rosie",         "Sweet"),
        new MonkeyCard(8, "Thinker",  @"Assets\Cards\download__6_.jpg",           "Philosopher",   "Contemplative"),
        new MonkeyCard(9, "Astro",    @"Assets\Cards\monyet_astronot.jpg",        "Commander Bak", "Adventurous"),
    };

    private static CardBase CloneCard(CardBase source) => source switch
    {
        MonkeyCard  m => new MonkeyCard(m.CardId, m.Label, m.ImagePath, m.CharacterName, m.Personality),
        SpecialCard s => new SpecialCard(s.CardId, s.Label, s.ImagePath, s.SpecialAbility),
        _             => throw new InvalidOperationException("Unknown card type")
    };
}

// ── Event Args ────────────────────────────────────────────────────────────────

public class CardPairEventArgs : EventArgs
{
    public CardBase  First  { get; }
    public CardBase  Second { get; }
    public CardBase? Third  { get; }
    public CardPairEventArgs(CardBase first, CardBase second, CardBase? third = null)
    { First = first; Second = second; Third = third; }
}

public class DriftEventArgs : EventArgs
{
    public IReadOnlyList<CardBase> NewOrder { get; }
    public DriftEventArgs(IReadOnlyList<CardBase> order) => NewOrder = order;
}

public class HeartsChangedArgs : EventArgs
{
    public int Hearts    { get; }
    public int MaxHearts { get; }
    public HeartsChangedArgs(int hearts, int max) { Hearts = hearts; MaxHearts = max; }
}