using System.Collections;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void StartGameplayLevel(LevelData levelData)
    {
        StartCoroutine(LoadGameplayLevelCoroutine(levelData));
    }

    private IEnumerator LoadGameplayLevelCoroutine(LevelData levelData)
    {
        yield return SceneLoader.Instance.LoadScene(SceneIndex.Gameplay);
        LevelManager.Instance.LoadLevel(levelData);
    }
}
