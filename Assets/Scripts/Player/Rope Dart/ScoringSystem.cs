using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ScoringSystem : MonoBehaviour
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

    public int CurrentScore { get; private set; } = 0;
    public float CurrentMultiplier { get; private set; } = 1f;

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private GameObject _queueContainer;
    [SerializeField] private GameObject _queueEntryPrefab;

    private List<ScoreEntry> _scoreQueue = new List<ScoreEntry>();

    void Start()
    {
        if (_scoreText != null) _scoreText.text = CurrentScore.ToString();
    }

    public void AddBinding(string bindingId, float scoreValue)
    {
        _scoreQueue.Add(new ScoreEntry(bindingId, scoreValue));
        GameObject queueEntry = Instantiate(_queueEntryPrefab, _queueContainer.transform);
        TextMeshProUGUI queueEntryText = queueEntry.GetComponentInChildren<TextMeshProUGUI>();
        if (queueEntryText != null) queueEntryText.text = $"{bindingId}: +{scoreValue}";
    }

    public void DoMultiplierDecay(float decay)
    {
        CurrentMultiplier = Mathf.Max(1f, CurrentMultiplier - decay);
    }

    public void ScoreHit(LevelTargetItem targetItem)
    {
        float totalScore = targetItem.PointValue;
        foreach (ScoreEntry score in _scoreQueue)
        {
            totalScore += score.ScoreValue;
        }

        CurrentScore += Mathf.RoundToInt(totalScore * CurrentMultiplier);
        if (_scoreText != null) _scoreText.text = CurrentScore.ToString();

        _scoreQueue.Clear();
        foreach (Transform child in _queueContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
