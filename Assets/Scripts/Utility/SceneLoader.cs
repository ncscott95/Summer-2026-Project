using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    // TODO: add transition animation
    // [SerializeField] private Animator transitionAnimator;
    // private const float transitionDuration = 1f;
    // private const float loadBufferDuration = 0.1f;

    // TODO: temporarily values while animation is not implemented
    private const float transitionDuration = 0.1f;
    private const float loadBufferDuration = 0.1f;
    private static int totalScenes;

    public override void Awake()
    {
        base.Awake();

        totalScenes = SceneManager.sceneCountInBuildSettings;
    }

    public Coroutine LoadScene(SceneIndex sceneIndex)
    {
        if ((int)sceneIndex < 0 || (int)sceneIndex >= totalScenes)
        {
            Debug.LogWarning($"Scene index {sceneIndex} is out of bounds. Total scenes: {totalScenes}");
            return null;
        }

        return StartCoroutine(LoadSceneAnimation(sceneIndex));
    }

    private IEnumerator LoadSceneAnimation(SceneIndex sceneIndex)
    {
        // transitionAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(transitionDuration);

        yield return new WaitForSeconds(loadBufferDuration);
        SceneManager.LoadSceneAsync((int)sceneIndex);
        yield return new WaitForSeconds(loadBufferDuration);

        // transitionAnimator.SetTrigger("End");
        yield return new WaitForSeconds(transitionDuration);
    }

    public SceneIndex GetCurrentSceneIndex()
    {
        return (SceneIndex)SceneManager.GetActiveScene().buildIndex;
    }
}

// every scene in the build must be added here
public enum SceneIndex
{
    // MainMenu = 0,
    LevelSelect = 0,
    Gameplay = 1,
}
