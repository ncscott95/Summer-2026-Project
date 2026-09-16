using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ScoringSystem : Singleton<ScoringSystem>
{
    struct ScoreEntry
    {
        public string BindingId;
        public float ScoreValue;

        public ScoreEntry(string bindingId, float scoreValue)
        {
            BindingId = bindingId;
            ScoreValue = scoreValue;
        }
    }

    private const float MultiplierDecayRate = 0.5f; // Decay rate per spin
    private const float MultiplierGainRate = 0.5f; // Gain rate per successful binding
    private const float MaxMultiplier = 3f; // Maximum multiplier value

    public int CurrentScore { get; private set; } = 0;
    public float CurrentMultiplier { get; private set; } = 1f;

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _multiplierText;
    [SerializeField] private GameObject _queueContainer;
    [SerializeField] private GameObject _queueEntryPrefab;

    private List<ScoreEntry> _scoreQueue = new List<ScoreEntry>();

    void Start()
    {
        UpdateUI();
    }

    public void AddBinding(string bindingId, float scoreValue)
    {
        _scoreQueue.Add(new ScoreEntry(bindingId, scoreValue));
        CurrentMultiplier = Mathf.Min(MaxMultiplier, CurrentMultiplier + MultiplierGainRate);

        GameObject queueEntry = Instantiate(_queueEntryPrefab, _queueContainer.transform);
        TextMeshProUGUI queueEntryText = queueEntry.GetComponentInChildren<TextMeshProUGUI>();
        if (queueEntryText != null) queueEntryText.text = $"{bindingId}: +{scoreValue}";
        
        UpdateUI();
    }

    public void DoMultiplierDecay()
    {
        CurrentMultiplier = Mathf.Max(1f, CurrentMultiplier - MultiplierDecayRate);
        UpdateUI();
    }

    public void ClearQueue()
    {
        _scoreQueue.Clear();
        foreach (Transform child in _queueContainer.transform)
        {
            Destroy(child.gameObject);
        }
        UpdateUI();
    }

    public void ScoreHit(LevelTargetItem targetItem)
    {
        float totalScore = targetItem.PointValue;
        foreach (ScoreEntry score in _scoreQueue)
        {
            totalScore += score.ScoreValue;
        }

        CurrentScore += Mathf.RoundToInt(totalScore * CurrentMultiplier);
        UpdateUI();

        _scoreQueue.Clear();
        foreach (Transform child in _queueContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateUI()
    {
        if (_multiplierText != null) _multiplierText.text = $"x{CurrentMultiplier:F1}";
        if (_scoreText != null) _scoreText.text = CurrentScore.ToString();
    }
}
