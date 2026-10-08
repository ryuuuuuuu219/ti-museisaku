using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // ステージID（シーンを跨いで保持）
    public static int stageID = 0;

    // インスペクターで設定
    [SerializeField] private string main = "Main";
    [SerializeField] private string karte = "Karte";
    [SerializeField] private string result = "Result";

    // シーン名をstaticで保持
    private static string mainScene;
    private static string karteScene;
    private static string resultScene;

    private void Awake()
    {
        mainScene = main;
        karteScene = karte;
        resultScene = result;

        DontDestroyOnLoad(gameObject);
    }

    public static void LoadResultScene()
    {
        SceneManager.LoadScene(resultScene);
    }

    public static void LoadMainScene()
    {
        SceneManager.LoadScene(mainScene);
    }

    public static void LoadKarteScene()
    {
        SceneManager.LoadScene(karteScene);
    }
}