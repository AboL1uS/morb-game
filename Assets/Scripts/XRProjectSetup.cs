using UnityEngine;
using System.Collections.Generic;

// ============================================================
// XR НАСТРОЙКА ПРОЕКТА — исправлено для Unity 6
// Задание 4: XR сборка — адаптация VR/AR/MR контроллеров
// ============================================================
public class XRProjectSetup : MonoBehaviour
{
    public enum RunMode { Desktop, VR, AR }

    [Header("Режим запуска")]
    [SerializeField] private RunMode currentMode = RunMode.Desktop;
    [SerializeField] private bool autoDetectXR = true;

    [Header("Объекты сцены")]
    [SerializeField] private GameObject xrRig;
    [SerializeField] private GameObject desktopCamera;

    [Header("Настройки VR")]
    [SerializeField] private float vrMoveSpeed = 5f;
    [SerializeField] private float vrSnapAngle = 45f;

    private bool _xrAvailable = false;
    private bool _snapCooled = true;

    // ============================================================
    // ИНИЦИАЛИЗАЦИЯ
    // ============================================================
    private void Awake()
    {
        if (autoDetectXR) DetectXRDevice();
        else ApplyMode(currentMode);
    }

    // ── ОПРЕДЕЛЕНИЕ XR УСТРОЙСТВА (Unity 6 API) ───────────────
    private void DetectXRDevice()
    {
        // Unity 6: используем GetSubsystems вместо устаревшего GetInstances
        var displays = new List<UnityEngine.XR.XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);

        _xrAvailable = displays.Count > 0 && displays[0].running;

        if (_xrAvailable)
        {
            Debug.Log("XR устройство обнаружено → VR режим");
            ApplyMode(RunMode.VR);
        }
        else
        {
            Debug.Log("XR устройство не обнаружено → Desktop режим");
            ApplyMode(RunMode.Desktop);
        }
    }

    // ── ПРИМЕНЕНИЕ РЕЖИМА ─────────────────────────────────────
    private void ApplyMode(RunMode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case RunMode.Desktop:
                if (desktopCamera != null) desktopCamera.SetActive(true);
                if (xrRig != null) xrRig.SetActive(false);
                Debug.Log("Режим: Desktop (клавиатура + мышь)");
                break;

            case RunMode.VR:
                if (desktopCamera != null) desktopCamera.SetActive(false);
                if (xrRig != null) xrRig.SetActive(true);
                LogXRDevices();
                Debug.Log("Режим: VR (XR контроллеры)");
                break;

            case RunMode.AR:
                if (desktopCamera != null) desktopCamera.SetActive(false);
                if (xrRig != null) xrRig.SetActive(true);
                Debug.Log("Режим: AR (камера + наложение)");
                break;
        }
    }

    // ── СПИСОК XR УСТРОЙСТВ (Unity 6 API) ─────────────────────
    private void LogXRDevices()
    {
        // Unity 6: используем GetDevices(List<T>) вместо GetAllDevices
        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);

        Debug.Log($"XR устройств найдено: {devices.Count}");
        foreach (var d in devices)
            Debug.Log($"  → {d.name} | {d.characteristics}");
    }

    // ============================================================
    // UPDATE — чтение XR контроллеров
    // ============================================================
    private void Update()
    {
        if (currentMode == RunMode.VR)
            HandleVRInput();
    }

    private void HandleVRInput()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();

        // ── Правый контроллер → выстрел + Snap Turn ───────────
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Right |
            UnityEngine.XR.InputDeviceCharacteristics.Controller,
            devices);

        if (devices.Count > 0)
        {
            var right = devices[0];

            // Триггер → выстрел
            if (right.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.triggerButton, out bool trigger) && trigger)
                OnVRShoot();

            // Джойстик → Snap Turn
            if (right.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 stick)
                && stick.magnitude > 0.8f)
                OnVRSnapTurn(stick.x);
        }

        // ── Левый контроллер → движение + пауза ───────────────
        devices.Clear();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Left |
            UnityEngine.XR.InputDeviceCharacteristics.Controller,
            devices);

        if (devices.Count > 0)
        {
            var left = devices[0];

            // Джойстик → движение
            if (left.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 stick)
                && stick.magnitude > 0.1f)
                OnVRMove(stick);

            // Кнопка X → пауза
            if (left.TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primaryButton, out bool xBtn) && xBtn)
                OnVRPause();
        }
    }

    // ============================================================
    // VR ДЕЙСТВИЯ
    // ============================================================

    // Триггер правого контроллера → выстрел заклинанием
    private void OnVRShoot()
    {
        Debug.Log("[VR] Триггер → выстрел");
        // SpellSystem.Instance?.FireVR();
    }

    // Snap Turn — мгновенный поворот на фиксированный угол
    // Комфортнее для VR чем плавный поворот (меньше укачивает)
    private void OnVRSnapTurn(float xAxis)
    {
        if (!_snapCooled) return;

        float angle = xAxis > 0 ? vrSnapAngle : -vrSnapAngle;
        if (xrRig != null)
            xrRig.transform.Rotate(0, angle, 0);

        _snapCooled = false;
        Invoke(nameof(ResetSnap), 0.3f);
        Debug.Log($"[VR] Snap Turn: {angle}°");
    }
    private void ResetSnap() => _snapCooled = true;

    // Левый джойстик → движение орба
    private void OnVRMove(Vector2 input)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        Vector3 move = new Vector3(input.x, 0, input.y) * vrMoveSpeed * Time.deltaTime;
        player.transform.Translate(move, Space.World);
    }

    // Кнопка X/A → пауза
    private void OnVRPause()
    {
        Debug.Log("[VR] Кнопка X → пауза");
    }

    // ── GIZMOS — зона отслеживания VR в редакторе ─────────────
    private void OnDrawGizmos()
    {
        if (currentMode != RunMode.VR) return;

        // Стандартная зона отслеживания 2×2 метра
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawCube(transform.position + Vector3.up * 1.25f, new Vector3(2f, 2.5f, 2f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 1.25f, new Vector3(2f, 2.5f, 2f));
    }
}