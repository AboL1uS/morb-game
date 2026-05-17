using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("Кнопки главного меню")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Анимация заголовка (необязательно)")]
    [SerializeField] private TextMeshProUGUI titleText;  

    private void Start()
    {
        Time.timeScale = 1f;

        // Подключаем кнопки
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Запускаем анимацию плавного появления
        StartCoroutine(FadeIn());
        AudioManager.Instance?.PlayMenuMusic();
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene(1);
    }
    private void OnQuitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeIn()
    {
        // Ищем Canvas в сцене напрямую — надёжнее чем GetComponentInParent
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("MainMenuUI: Canvas не найден в сцене!");
            yield break;
        }

        // Получаем или добавляем CanvasGroup на Canvas
        CanvasGroup cg = canvas.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = canvas.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float elapsed = 0f;
        float duration = 1.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();
    }
}