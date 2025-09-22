using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject introPanel;
    private bool isGamePaused = true;
    void Start()
    {
        PauseGame();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isGamePaused) PauseGame();
            else QuitGame();
        }
    }
    public void StartGame()
    {
        Time.timeScale = 1;
        introPanel.SetActive(false);
        isGamePaused = false;
    }
    public void PauseGame()
    {
        Time.timeScale = 0;
        isGamePaused = true;
        introPanel.SetActive(true);
    }
    public void QuitGame()
    {
        Time.timeScale = 1;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
