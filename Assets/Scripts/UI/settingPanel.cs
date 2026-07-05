using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using KingdomWar.Game;
namespace KingdomWar.UI
{
public class settingPanel : basePanel
{
    [Header("音量控制")]
    public Slider volumeSlider;
    public Text volumeText;
    
    [Header("帧率设置")]
    public Dropdown fpsDropdown;
    
    [Header("Exit Button")]
    public Button exitBtn;
    
    private int[] fpsOptions = { 30, 60, 90, 120 };
    
    protected override void Awake()
    {
        base.Awake();
        
        // 自动获取组件
        if (volumeSlider == null) volumeSlider = transform.Find("VolumeControl/VolumeSlider").GetComponent<Slider>();
        if (volumeText == null) volumeText = transform.Find("VolumeControl/VolumeText").GetComponent<Text>();
        if (fpsDropdown == null) fpsDropdown = transform.Find("FPSSetting/FPSDropdown").GetComponent<Dropdown>();
        if (exitBtn == null) exitBtn = transform.Find("ExitBtn").GetComponent<Button>();
    }
    
    protected override void Start()
    {
        base.Start();
        
        // initialize volume display
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioManager.Instance.GetBGMVolume();
            UpdateVolumeText(volumeSlider.value);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        // 初始化帧率选项
        if (fpsDropdown != null)
        {
            fpsDropdown.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (int fps in fpsOptions)
            {
                options.Add(new Dropdown.OptionData(fps + " FPS"));
            }
            fpsDropdown.AddOptions(options);
            
            // 加载保存的帧率设�?
            int savedFPS = PlayerPrefs.GetInt("GameFPS", 60);
            int fpsIndex = System.Array.IndexOf(fpsOptions, savedFPS);
            fpsDropdown.value = fpsIndex >= 0 ? fpsIndex : 1;
            
            fpsDropdown.onValueChanged.AddListener(OnFPSChanged);
        }
        
        // exit button listener
        if (exitBtn != null)
        {
            exitBtn.onClick.AddListener(OnExitGame);
        }
    }
    
    private void OnVolumeChanged(float value)
    {
        UpdateVolumeText(value);
        AudioManager.Instance.SetVolume(value);
    }
    
    private void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            volumeText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }
    
    private void OnFPSChanged(int index)
    {
        int targetFPS = fpsOptions[index];
        PlayerPrefs.SetInt("GameFPS", targetFPS);
        PlayerPrefs.Save();
        
        // 设置目标帧率
        Application.targetFrameRate = targetFPS;
    }
    
    private void OnExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    protected override void OnClickBtn()
    {
        UIManager.Instance.PopPanel();
    }
}

}
