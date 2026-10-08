using UnityEngine;
using UnityEngine.SceneManagement;

public class otherSceneButton : MonoBehaviour
{
    public void LoadResultScene()
    {
        SceneController.LoadResultScene();
    }
    public void LoadMainScene()
    {
        SceneController.LoadMainScene();
    }
    public void LoadKarteScene()
    {
        SceneController.LoadKarteScene();
    }
}