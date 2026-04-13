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
        CanvasGroup cg = GetComponentInParent<Canvas>().GetComponent<CanvasGroup>();
        if (cg == null)
            cg = GetComponentInParent<Canvas>().gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float elapsed = 0f;
        float duration = 1.5f;   // секунд на появление

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