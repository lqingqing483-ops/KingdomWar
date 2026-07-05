using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KingdomWar.Game;
namespace KingdomWar.Load
{
public class AsyncSceneLoader : MonoBehaviour
{

    private Slider loadingSlider;
    private Text progressText;
    private string sceneName = SceneNames.MainMenu;

    private float minLoadingTime = 3f; // ��С����ʱ��
    private float progressSmoothSpeed = 2f; // ����ƽ���ٶ�

    private float loadingProgress = 0f;
    private float smoothProgress = 0f;
    private float loadingTimer = 0f;

    private void Awake()
    {
        loadingSlider = transform.Find("loadingSlider").GetComponent<Slider>();
        progressText = transform.Find("progressText").GetComponent<Text>();
    }

    void Start()
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        loadingSlider.value = 0f;
        progressText.text = "加载�?.. 0%";
        loadingProgress = 0f;
        smoothProgress = 0f;
        loadingTimer = 0f;

        // ��ʼ�첽����
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        bool isReadyToActivate = false;

        // ����ѭ��
        while (!asyncOperation.isDone)
        {
            loadingTimer += Time.deltaTime;

            // ������ʵ���ؽ���
            if (asyncOperation.progress >= 0.9f)
            {
                loadingProgress = 1f; // ������ɣ��ȴ�����?
                isReadyToActivate = true;
            }
            else
            {
                // ȷ������ʱ������ΪminLoadingTime
                float timeBasedProgress = Mathf.Clamp01(loadingTimer / minLoadingTime);
                loadingProgress = Mathf.Max(asyncOperation.progress, timeBasedProgress * 0.9f);
            }

            // ƽ��������ʾ
            smoothProgress = Mathf.Lerp(smoothProgress, loadingProgress, Time.deltaTime * progressSmoothSpeed);

            // ����UI
            loadingSlider.value = smoothProgress;
            progressText.text = $"加载�?.. {smoothProgress * 100:F0}%";

            if (smoothProgress >= 0.99)
            {
                progressText.text = "Tap anywhere to enter the game";
            }

            // ����Ƿ�׼���ü����?
            if (isReadyToActivate)
            {
                // �ȴ��û�����
                if (Input.anyKeyDown)
                {
                    asyncOperation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }
}

}
