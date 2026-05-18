using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;


public class NetworkPacketHandler : NetworkBehaviour
{
    // ============================================================
    // СТРУКТУРА ПАКЕТА — INetworkSerializable для передачи по сети
    // ============================================================
    public struct GamePacket : INetworkSerializable
    {
        public ulong SenderId;      // ID отправителя
        public float Timestamp;     // время отправки
        public int PacketType;    // 0=чат, 1=действие, 2=данные
        public float DataValue;     // числовое значение
        public int SequenceNum;   // порядковый номер

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref SenderId);
            serializer.SerializeValue(ref Timestamp);
            serializer.SerializeValue(ref PacketType);
            serializer.SerializeValue(ref DataValue);
            serializer.SerializeValue(ref SequenceNum);
        }
    }

    // ── UI ───────────────────────────────────────────────────
    [Header("UI пакетов")]
    [SerializeField] private Button sendPacketBtn;
    [SerializeField] private Button sendActionBtn;
    [SerializeField] private TextMeshProUGUI packetLogText;
    [SerializeField] private TextMeshProUGUI packetStatsText;

    // ── Статистика ────────────────────────────────────────────
    private int _sentCount = 0;
    private int _receivedCount = 0;
    private int _sequenceNum = 0;
    private List<string> _log = new List<string>();
    private const int MAX_LOG = 8;

    private void Start()
    {
        sendPacketBtn?.onClick.AddListener(SendDataPacket);
        sendActionBtn?.onClick.AddListener(SendActionPacket);
    }

    // ── ОТПРАВКА ПАКЕТА ДАННЫХ ────────────────────────────────
    private void SendDataPacket()
    {
        if (!IsConnected()) return;

        GamePacket packet = new GamePacket
        {
            SenderId = NetworkManager.Singleton.LocalClientId,
            Timestamp = Time.time,
            PacketType = 2,
            DataValue = Random.Range(0f, 100f),
            SequenceNum = _sequenceNum++
        };

        SendPacketServerRpc(packet);
        _sentCount++;
        UpdateStats();
        AddLog($"→ Пакет #{packet.SequenceNum} | Значение: {packet.DataValue:F1}");
    }

    // ── ОТПРАВКА ПАКЕТА ДЕЙСТВИЯ ──────────────────────────────
    private void SendActionPacket()
    {
        if (!IsConnected()) return;

        GamePacket packet = new GamePacket
        {
            SenderId = NetworkManager.Singleton.LocalClientId,
            Timestamp = Time.time,
            PacketType = 1,
            DataValue = 1f,
            SequenceNum = _sequenceNum++
        };

        SendPacketServerRpc(packet);
        _sentCount++;
        UpdateStats();
        AddLog($"→ Действие #{packet.SequenceNum} отправлено");
    }

    // ── SERVER RPC — клиент → сервер ─────────────────────────
    [ServerRpc(RequireOwnership = false)]
    private void SendPacketServerRpc(GamePacket packet, ServerRpcParams rpcParams = default)
    {
        _receivedCount++;

        string typeName = packet.PacketType switch
        {
            0 => "Чат",
            1 => "Действие",
            2 => "Данные",
            _ => "Неизвестный"
        };

        Debug.Log($"[Сервер] Пакет #{packet.SequenceNum} от {packet.SenderId} | Тип: {typeName} | Значение: {packet.DataValue:F1}");

        // Рассылаем всем клиентам
        BroadcastPacketClientRpc(packet);
    }

    // ── CLIENT RPC — сервер → все клиенты ────────────────────
    [ClientRpc]
    private void BroadcastPacketClientRpc(GamePacket packet)
    {
        _receivedCount++;
        UpdateStats();

        float latency = Time.time - packet.Timestamp;

        string typeName = packet.PacketType switch
        {
            0 => "Чат",
            1 => "Действие",
            2 => "Данные",
            _ => "Неизвестный"
        };

        AddLog($"← #{packet.SequenceNum} от [{packet.SenderId}] | {typeName}: {packet.DataValue:F1} | {latency * 1000:F0}мс");
        Debug.Log($"[Клиент] Пакет #{packet.SequenceNum} получен. Задержка: {latency * 1000:F1}мс");
    }

    // ── УТИЛИТЫ ──────────────────────────────────────────────
    private bool IsConnected()
    {
        if (NetworkManager.Singleton == null ||
            (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost))
        {
            AddLog("⚠ Не подключено к сети!");
            return false;
        }
        return true;
    }

    private void AddLog(string message)
    {
        string time = System.DateTime.Now.ToString("HH:mm:ss");
        _log.Add($"[{time}] {message}");
        if (_log.Count > MAX_LOG) _log.RemoveAt(0);
        if (packetLogText != null)
            packetLogText.text = string.Join("\n", _log);
    }

    private void UpdateStats()
    {
        if (packetStatsText != null)
            packetStatsText.text = $"Отправлено: {_sentCount}  |  Получено: {_receivedCount}";
    }
}