using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Exit: 버튼에 연결하여 사용합니다.
/// Android에서는 버튼 클릭 시 저장을 수행한 뒤 Application.Quit()으로 종료합니다.
/// 에디터/다른 플랫폼에서는 저장만 수행하고 로그를 남깁니다.
/// </summary>
public class UI_Exit : MonoBehaviour
{
    [SerializeField] private Button _exitButton;

    private void Awake()
    {
        if (_exitButton == null)
        {
            _exitButton = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitButtonPressed);
            _exitButton.onClick.AddListener(OnExitButtonPressed);
        }
    }

    public void OnExitButtonPressed()
    {
        // Save immediately before exit
        try
        {
            SaveManager.Instance?.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UI_Exit: Save failed before exit - {ex}");
        }

        // Quit on Android builds; in Editor leave a log to avoid stopping play mode
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.Quit();
#else
        Debug.Log("UI_Exit: Exit requested (not running on Android or in Editor). Save performed.");
#endif
    }

    private void OnDestroy()
    {
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(OnExitButtonPressed);
    }
}
