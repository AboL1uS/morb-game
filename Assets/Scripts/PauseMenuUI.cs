using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Объекты экрана паузы")]
    [SerializeField] private GameObject pauseScreen;    // PauseScreen
    [SerializeField] private Button resumeButton;   // ResumeButton
    [SerializeField] private Button restartButton;  // RestartButton

    private CanvasGroup _cardGroup;
    private bool _isPaused = false;

    private void Start()
    {
        // Убеждаемся что экран скрыт
        pauseScreen.SetActive(false);

        Transform card = pauseScreen.transform.Find("Card");
        if (card != null)
        {
            _cardGroup = card.GetComponent<CanvasGroup>();
            if (_cardGroup == null)
                _cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        }

        // Подключаем кнопки
        resumeButton.onClick.AddListener(ResumeGame);
        restartButton.onClick.AddListener(RestartGame);
    }

    private void Update()
    {
        // Escape открывает и закрывает паузу
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void PauseGame()
    {
        _isPaused = true;
        pauseScreen.SetActive(true);
        Time.timeScale = 0f;   // время останавливается — все Update() замирают

        // Запускаем анимацию появления карточки
        StartCoroutine(FadeInCard());
    }

    private void ResumeGame()
    {
        _isPaused = false;
        pauseScreen.SetActive(false);
        Time.timeScale = 1f;   // время возобновляется
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;   // важно сбросить время перед загрузкой
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator FadeInCard()
    {
        if (_cardGroup == null) yield break;

        _cardGroup.alpha = 0f;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _cardGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        _cardGroup.alpha = 1f;
    }

    private void OnDestroy()
    {
        resumeButton?.onClick.RemoveAllListeners();
        restartButton?.onClick.RemoveAllListeners();
    }
}