using UnityEngine;

public class MainMenuButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        SceneLoader.Instance.LoadScene(SceneIndex.LevelSelect);
    }
}
