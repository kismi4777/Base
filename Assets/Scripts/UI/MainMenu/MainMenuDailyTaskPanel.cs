using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuDailyTaskPanel : MainMenuOverlayPanel
{
    public struct TaskDefinition
    {
        public string Icon;
        public string TitleRu;
        public string TitleEn;
        public string DescRu;
        public string DescEn;
        public int Target;
        public int RewardXp;
        public Func<PlayerData, int> GetProgress;
        public Func<PlayerData, bool> IsClaimed;
        public Action<PlayerData> SetClaimed;
    }

    readonly TaskDefinition[] _tasks;
    readonly MainMenuToast _toast;
    readonly Action _onProgressChanged;

    int _taskIndex = -1;
    TMP_Text _bodyLabel;
    TMP_Text _progressLabel;
    Button _claimButton;

    public MainMenuDailyTaskPanel(Canvas canvas, MainMenuUiStyle style, TaskDefinition[] tasks,
        MainMenuToast toast, Action onProgressChanged, Action onClose)
        : base(canvas, style, onClose)
    {
        _tasks = tasks;
        _toast = toast;
        _onProgressChanged = onProgressChanged;
    }

    protected override string GetOverlayName() => "DailyTaskOverlay";

    protected override Vector2 GetPanelSize() => new Vector2(560f, 400f);

    protected override void BuildContent(Transform panel)
    {
        _bodyLabel = MainMenuUiFactory.CreateText("Body", panel.transform, Style, string.Empty, 20, FontStyles.Normal);
        _bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        MainMenuUiFactory.AddLayoutElement(_bodyLabel.gameObject, 120f);

        _progressLabel = MainMenuUiFactory.CreateText("Progress", panel.transform, Style, string.Empty, 22, FontStyles.Bold, Style.AccentBlue);
        _progressLabel.alignment = TextAlignmentOptions.Center;
        MainMenuUiFactory.AddLayoutElement(_progressLabel.gameObject, 36f);

        GameObject claimGo = MainMenuUiFactory.CreateUiObject("ClaimButton", panel.transform);
        MainMenuUiFactory.AddLayoutElement(claimGo, 48f);
        Image claimImage = claimGo.AddComponent<Image>();
        claimImage.color = Style.ConfirmColor;
        _claimButton = claimGo.AddComponent<Button>();
        _claimButton.targetGraphic = claimImage;
        TMP_Text claimLabel = MainMenuUiFactory.CreateText("Label", claimGo.transform, Style,
            MenuLocalization.Get("Забрать награду", "Claim reward"), 20, FontStyles.Bold);
        MainMenuUiFactory.StretchFullScreen(claimLabel.gameObject);
        claimLabel.raycastTarget = false;
        _claimButton.onClick.AddListener(ClaimReward);
    }

    public bool TryBindExtended(Transform canvasRoot)
    {
        if (!TryBindExisting(canvasRoot, GetOverlayName()))
            return false;

        Transform panel = Root.transform.Find("Panel");
        if (panel == null)
            return false;

        _bodyLabel = panel.Find("Body")?.GetComponent<TMP_Text>();
        _progressLabel = panel.Find("Progress")?.GetComponent<TMP_Text>();
        _claimButton = panel.Find("ClaimButton")?.GetComponent<Button>();
        if (_claimButton != null)
        {
            _claimButton.onClick.RemoveAllListeners();
            _claimButton.onClick.AddListener(ClaimReward);
        }

        return true;
    }

    public void OpenTask(int index)
    {
        _taskIndex = index;
        Open();
    }

    public override void Refresh()
    {
        TitleLabel.text = MenuLocalization.Get("ЗАДАНИЕ", "TASK");

        if (_taskIndex < 0 || _taskIndex >= _tasks.Length || !PlayerProgressUtility.HasSave)
            return;

        TaskDefinition task = _tasks[_taskIndex];
        PlayerData data = PlayerProgressUtility.Data;
        int progress = Mathf.Clamp(task.GetProgress(data), 0, task.Target);
        bool claimed = task.IsClaimed(data);
        bool complete = progress >= task.Target;

        TitleLabel.text = $"{task.Icon} {MenuLocalization.Get(task.TitleRu, task.TitleEn)}";
        _bodyLabel.text = MenuLocalization.Get(task.DescRu, task.DescEn);
        _progressLabel.text = $"{progress} / {task.Target}  •  XP {task.RewardXp}";

        _claimButton.interactable = complete && !claimed;
        _claimButton.GetComponentInChildren<TMP_Text>().text = claimed
            ? MenuLocalization.Get("Получено", "Claimed")
            : MenuLocalization.Get("Забрать награду", "Claim reward");
    }

    void ClaimReward()
    {
        if (_taskIndex < 0 || _taskIndex >= _tasks.Length || !PlayerProgressUtility.HasSave)
            return;

        TaskDefinition task = _tasks[_taskIndex];
        PlayerData data = PlayerProgressUtility.Data;
        int progress = task.GetProgress(data);

        if (progress < task.Target || task.IsClaimed(data))
            return;

        task.SetClaimed(data);
        PlayerProgressUtility.AddExperience(task.RewardXp);
        SaveManager.Instance.Save();

        _toast?.Show(MenuLocalization.Get(
            $"+{task.RewardXp} XP!",
            $"+{task.RewardXp} XP!"));

        _onProgressChanged?.Invoke();
        Refresh();
    }

    public new void Close(bool notify = true)
    {
        _taskIndex = -1;
        base.Close(notify);
    }
}
