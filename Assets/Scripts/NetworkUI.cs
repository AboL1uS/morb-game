using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;


public class NetworkUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playersText;

    private const string DEFAULT_IP = "127.0.0.1";
    private const ushort DEFAULT_PORT = 7777;

    private void Start()
    {
        hostButton?.onClick.AddListener(StartHost);
        clientButton?.onClick.AddListener(StartClient);
        stopButton?.onClick.AddListener(StopConnection);

        stopButton?.gameObject.SetActive(false);
        UpdateStatus("Не подключено");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    // ── ХОСТ ─────────────────────────────────────────────────
    private void StartHost()
    {
        SetupTransport(DEFAULT_IP, DEFAULT_PORT);

        if (NetworkManager.Singleton.StartHost())
        {
            UpdateStatus($"Хост запущен\nIP: {DEFAULT_IP}:{DEFAULT_PORT}\nОжидание игроков...");
            SetButtonsActive(false);
            stopButton?.gameObject.SetActive(true);
            Debug.Log("Хост запущен на порту " + DEFAULT_PORT);
        }
        else
        {
            UpdateStatus("Ошибка запуска хоста!");
        }
    }

    // ── КЛИЕНТ ───────────────────────────────────────────────
    private void StartClient()
    {
        string ip = (ipInput != null && !string.IsNullOrEmpty(ipInput.text))
            ? ipInput.text
            : DEFAULT_IP;

        SetupTransport(ip, DEFAULT_PORT);
        UpdateStatus($"Подключение к {ip}:{DEFAULT_PORT}...");

        if (NetworkManager.Singleton.StartClient())
        {
            SetButtonsActive(false);
            stopButton?.gameObject.SetActive(true);
            Debug.Log($"Подключаемся к хосту: {ip}:{DEFAULT_PORT}");
        }
        else
        {
            UpdateStatus("Ошибка подключения!");
        }
    }

    // ── ОТКЛЮЧЕНИЕ ───────────────────────────────────────────
    private void StopConnection()
    {
        NetworkManager.Singleton.Shutdown();
        UpdateStatus("Отключено");
        SetButtonsActive(true);
        stopButton?.gameObject.SetActive(false);
        if (playersText != null) playersText.text = "";
    }

    // ── ТРАНСПОРТ ────────────────────────────────────────────
    private void SetupTransport(string ip, ushort port)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(ip, port);
        else
            Debug.LogError("UnityTransport не найден на NetworkManager!");
    }

    // ── СОБЫТИЯ ──────────────────────────────────────────────
    private void OnServerStarted()
    {
        Debug.Log("Сервер запущен!");
        UpdatePlayerCount();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Клиент подключился: ID {clientId}");
        UpdateStatus($"Подключено!\nID клиента: {clientId}");
        UpdatePlayerCount();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Клиент отключился: ID {clientId}");
        UpdatePlayerCount();
    }

    // ── УТИЛИТЫ ──────────────────────────────────────────────
    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void UpdatePlayerCount()
    {
        if (playersText == null || NetworkManager.Singleton == null) return;
        int count = NetworkManager.Singleton.ConnectedClients.Count;
        playersText.text = $"Игроков онлайн: {count}";
    }

    private void SetButtonsActive(bool active)
    {
        hostButton?.gameObject.SetActive(active);
        clientButton?.gameObject.SetActive(active);
        if (ipInput != null) ipInput.gameObject.SetActive(active);
    }
}