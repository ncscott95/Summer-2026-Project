using UnityEngine;

public class MainMenuButton : MonoBehaviour
{
    public void OnClick()
    {
        SceneLoader.Instance.LoadScene(SceneIndex.LevelSelect);
    }
}
