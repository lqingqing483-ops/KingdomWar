using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace KingdomWar.Game.Battle
{
    public class AnimationTest : MonoBehaviour
    {
        [Header("测试设置")]
        public GameObject unitPrefab; // 单位预制体
        public float testDelay = 3.0f; // 测试间隔时间
        public bool runBatchTest = false; // 是否运行批量测试
        public int batchTestCount = 5; // 批量测试次数
        public float animationStateCheckThreshold = 0.1f; // 动画状态检测阈值
        public int maxAnimationCheckAttempts = 10; // 最大动画状态检测尝试次数
        public float animationEventTimeout = 5.0f; // 动画事件超时时间

        [Header("测试动画列表")]
        public List<string> testAnimations = new List<string>
        {
            "Idle",
            "Walk",
            "Attack",
            "GetHit",
            "Death"
        };

        [Header("测试参数列表")]
        public List<AnimationParameter> testParameters = new List<AnimationParameter>
        {
            new AnimationParameter("IsMoving", AnimatorControllerParameterType.Bool, 0),
            new AnimationParameter("MoveSpeed", AnimatorControllerParameterType.Float, 1.0f),
            new AnimationParameter("Attack", AnimatorControllerParameterType.Bool, 0),
            new AnimationParameter("GetHit", AnimatorControllerParameterType.Bool, 0),
            new AnimationParameter("IsDead", AnimatorControllerParameterType.Bool, 0)
        };

        private Unit testUnit;
        private Animator animator;
        private List<TestResult> testResults = new List<TestResult>();
        private int currentTestIndex = 0;
        private bool isTesting = false;
        private float testStartTime;

        // 动画事件监听器
        private bool animationEventTriggered = false;
        private string lastTriggeredEvent = "";

        private void Start()
        {
            // 初始化测试
            InitializeTest();
        }

        private void Update()
        {
            if (isTesting)
            {
                CheckTestProgress();
            }
        }

        /// <summary>
        /// 初始化测试
        /// </summary>
        private void InitializeTest()
        {
            if (unitPrefab == null)
            {
                Debug.LogError("请设置单位预制体");
                return;
            }

            // 实例化测试单位
            GameObject unitObj = Instantiate(unitPrefab, transform.position, Quaternion.identity);
            testUnit = unitObj.GetComponent<Unit>();
            animator = unitObj.GetComponent<Animator>();

            if (testUnit == null || animator == null)
            {
                Debug.LogError("单位预制体缺少Unit组件或Animator组件");
                return;
            }

            // 初始化单位
            testUnit.Initialize(1, "TestUnit", 100, 10, 1.0f, 1.0f, 2.0f);

            // 注册动画事件监听器
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                AnimationClip clip = clipInfo[0].clip;
                if (clip != null)
                {
                    AnimationEvent[] events = clip.events;
                    foreach (AnimationEvent e in events)
                    {
                        Debug.Log($"发现动画事件: {e.functionName} 在时间: {e.time}");
                    }
                }
            }

            // 开始测试
            if (runBatchTest)
            {
                StartCoroutine(RunBatchTest());
            }
            else
            {
                StartCoroutine(RunSingleTest());
            }
        }

        /// <summary>
        /// 运行单步测试
        /// </summary>
        private IEnumerator RunSingleTest()
        {
            isTesting = true;

            // 测试1: 动画播放测试
            yield return TestAnimationPlayback();

            // 测试2: 动画切换测试
            yield return TestAnimationTransition();

            // 测试3: 参数控制测试
            yield return TestParameterControl();

            // 测试4: 动画事件测试
            yield return TestAnimationEvents();

            // 生成测试报告
            GenerateTestReport();

            isTesting = false;
        }

        /// <summary>
        /// 运行批量测试
        /// </summary>
        private IEnumerator RunBatchTest()
        {
            isTesting = true;

            for (int i = 0; i < batchTestCount; i++)
            {
                Debug.Log($"===== 批量测试第 {i + 1} 次 =====");

                // 测试1: 动画播放测试
                yield return TestAnimationPlayback();

                // 测试2: 动画切换测试
                yield return TestAnimationTransition();

                // 测试3: 参数控制测试
                yield return TestParameterControl();

                // 测试4: 动画事件测试
                yield return TestAnimationEvents();

                yield return new WaitForSeconds(testDelay);
            }

            // 生成测试报告
            GenerateTestReport();

            isTesting = false;
        }

        /// <summary>
        /// 测试动画播放
        /// </summary>
        private IEnumerator TestAnimationPlayback()
        {
            Debug.Log("开始测试: 动画播放测试");

            foreach (string animationName in testAnimations)
            {
                // 重置动画状态
                ResetAnimationState();
                yield return new WaitForSeconds(0.5f);

                // 触发动画
                TriggerAnimation(animationName);
                
                // 等待并验证动画状态
                bool success = false;
                bool callbackReceived = false;
                
                yield return StartCoroutine(WaitForAnimationStateCoroutine(animationName, testDelay, (result) => {
                    success = result;
                    callbackReceived = true;
                }));
                
                // 等待回调完成
                float waitTime = 0f;
                while (!callbackReceived && waitTime < testDelay)
                {
                    yield return null;
                    waitTime += Time.deltaTime;
                }
                
                testResults.Add(new TestResult(
                    "动画播放测试",
                    animationName,
                    success,
                    success ? $"成功播放动画: {animationName}" : $"失败: 无法播放动画 {animationName}"
                ));

                yield return new WaitForSeconds(testDelay);
            }

            Debug.Log("完成测试: 动画播放测试");
        }

        /// <summary>
        /// 测试动画切换
        /// </summary>
        private IEnumerator TestAnimationTransition()
        {
            Debug.Log("开始测试: 动画切换测试");

            // 测试正常切换
            for (int i = 0; i < testAnimations.Count; i++)
            {
                string fromAnim = testAnimations[i];
                string toAnim = testAnimations[(i + 1) % testAnimations.Count];

                // 先播放第一个动画
                TriggerAnimation(fromAnim);
                yield return new WaitForSeconds(testDelay / 2);

                // 切换到第二个动画
                TriggerAnimation(toAnim);
                
                // 等待并验证动画状态
                bool success = false;
                bool callbackReceived = false;
                
                yield return StartCoroutine(WaitForAnimationStateCoroutine(toAnim, testDelay, (result) => {
                    success = result;
                    callbackReceived = true;
                }));
                
                // 等待回调完成
                float waitTime = 0f;
                while (!callbackReceived && waitTime < testDelay)
                {
                    yield return null;
                    waitTime += Time.deltaTime;
                }
                
                testResults.Add(new TestResult(
                    "动画切换测试",
                    $"{fromAnim} -> {toAnim}",
                    success,
                    success ? $"成功从 {fromAnim} 切换到 {toAnim}" : $"失败: 无法从 {fromAnim} 切换到 {toAnim}"
                ));

                yield return new WaitForSeconds(testDelay / 2);
            }

            // 测试快速连续切换
            Debug.Log("测试快速连续切换动画");
            for (int i = 0; i < 5; i++)
            {
                string randomAnim1 = testAnimations[Random.Range(0, testAnimations.Count)];
                string randomAnim2 = testAnimations[Random.Range(0, testAnimations.Count)];
                
                TriggerAnimation(randomAnim1);
                yield return new WaitForSeconds(0.1f); // 极短间隔
                
                TriggerAnimation(randomAnim2);
                
                // 等待并验证动画状态
                bool success = false;
                bool callbackReceived = false;
                
                yield return StartCoroutine(WaitForAnimationStateCoroutine(randomAnim2, testDelay, (result) => {
                    success = result;
                    callbackReceived = true;
                }));
                
                // 等待回调完成
                float waitTime = 0f;
                while (!callbackReceived && waitTime < testDelay)
                {
                    yield return null;
                    waitTime += Time.deltaTime;
                }
                
                testResults.Add(new TestResult(
                    "动画切换测试(快速)",
                    $"{randomAnim1} -> {randomAnim2}",
                    success,
                    success ? $"成功快速从 {randomAnim1} 切换到 {randomAnim2}" : $"失败: 无法快速切换"
                ));
            }

            Debug.Log("完成测试: 动画切换测试");
        }

        /// <summary>
        /// 测试参数控制
        /// </summary>
        private IEnumerator TestParameterControl()
        {
            Debug.Log("开始测试: 参数控制测试");

            foreach (AnimationParameter param in testParameters)
            {
                // 测试正常值
                SetParameterValue(param);
                yield return new WaitForSeconds(1.0f);
                
                bool success = VerifyParameterValue(param);
                testResults.Add(new TestResult(
                    "参数控制测试",
                    $"{param.name} (正常)",
                    success,
                    success ? $"成功设置参数 {param.name} 为 {param.value}" : $"失败: 无法设置参数 {param.name}"
                ));

                // 测试极端值
                if (param.type == AnimatorControllerParameterType.Float)
                {
                    // 测试最大值
                    param.value = 100.0f;
                    SetParameterValue(param);
                    yield return new WaitForSeconds(1.0f);
                    success = VerifyParameterValue(param);
                    testResults.Add(new TestResult(
                        "参数控制测试",
                        $"{param.name} (最大值)",
                        success,
                        success ? $"成功设置参数 {param.name} 为 {param.value}" : $"失败: 无法设置参数 {param.name} 为最大值"
                    ));

                    // 测试最小值
                    param.value = -100.0f;
                    SetParameterValue(param);
                    yield return new WaitForSeconds(1.0f);
                    success = VerifyParameterValue(param);
                    testResults.Add(new TestResult(
                        "参数控制测试",
                        $"{param.name} (最小值)",
                        success,
                        success ? $"成功设置参数 {param.name} 为 {param.value}" : $"失败: 无法设置参数 {param.name} 为最小值"
                    ));
                }

                yield return new WaitForSeconds(testDelay);
            }

            Debug.Log("完成测试: 参数控制测试");
        }

        /// <summary>
        /// 测试动画事件
        /// </summary>
        private IEnumerator TestAnimationEvents()
        {
            Debug.Log("开始测试: 动画事件测试");

            // 测试攻击动画事件
            animationEventTriggered = false;
            lastTriggeredEvent = "";

            // 触发攻击动画
            TriggerAnimation("Attack");
            testStartTime = Time.time;

            // 等待动画事件触发
            float elapsedTime = 0f;
            while (elapsedTime < animationEventTimeout && !animationEventTriggered)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            bool success = animationEventTriggered;
            testResults.Add(new TestResult(
                "动画事件测试",
                "Attack",
                success,
                success ? $"成功触发动画事件: {lastTriggeredEvent}" : "失败: 动画事件未触发"
            ));

            Debug.Log("完成测试: 动画事件测试");
        }

        /// <summary>
        /// 触发动画
        /// </summary>
        private void TriggerAnimation(string animationName)
        {
            if (animator == null)
                return;

            switch (animationName)
            {
                case "Idle":
                    animator.SetBool("IsMoving", false);
                    animator.SetBool("Attack", false);
                    animator.SetBool("IsDead", false);
                    break;
                case "Walk":
                    animator.SetBool("IsMoving", true);
                    animator.SetFloat("MoveSpeed", 1.0f);
                    animator.SetBool("Attack", false);
                    animator.SetBool("IsDead", false);
                    break;
                case "Attack":
                    animator.SetBool("IsMoving", false);
                    animator.SetBool("Attack", true);
                    animator.SetBool("IsDead", false);
                    break;
                case "GetHit":
                    animator.SetBool("GetHit", true);
                    break;
                case "Death":
                    animator.SetBool("IsDead", true);
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// 等待并验证动画状态（协程版本）
        /// </summary>
        private IEnumerator WaitForAnimationStateCoroutine(string animationName, float timeout, System.Action<bool> callback)
        {
            bool success = false;
            int attempts = 0;
            float startTime = Time.time;

            while (attempts < maxAnimationCheckAttempts && Time.time - startTime < timeout)
            {
                if (IsAnimationPlaying(animationName))
                {
                    success = true;
                    break;
                }
                attempts++;
                yield return null; // 等待一帧
            }

            callback?.Invoke(success);
        }

        /// <summary>
        /// 检查动画是否正在播放
        /// </summary>
        private bool IsAnimationPlaying(string animationName)
        {
            if (animator == null)
                return false;

            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length == 0)
                return false;

            foreach (AnimatorClipInfo clip in clipInfo)
            {
                if (clip.clip.name.Contains(animationName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 重置动画状态
        /// </summary>
        private void ResetAnimationState()
        {
            if (animator == null)
                return;

            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveSpeed", 0f);
            animator.SetBool("Attack", false);
            animator.SetBool("GetHit", false);
            animator.SetBool("IsDead", false);
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        private void SetParameterValue(AnimationParameter param)
        {
            if (animator == null)
                return;

            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(param.name, param.value > 0);
                    break;
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.name, param.value);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.name, Mathf.RoundToInt(param.value));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(param.name);
                    break;
            }
        }

        /// <summary>
        /// 验证参数值
        /// </summary>
        private bool VerifyParameterValue(AnimationParameter param)
        {
            if (animator == null)
                return false;

            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    return animator.GetBool(param.name) == (param.value > 0);
                case AnimatorControllerParameterType.Float:
                    return Mathf.Approximately(animator.GetFloat(param.name), param.value);
                case AnimatorControllerParameterType.Int:
                    return animator.GetInteger(param.name) == Mathf.RoundToInt(param.value);
                case AnimatorControllerParameterType.Trigger:
                    // 触发器无法直接验证，返回true
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 检查测试进度
        /// </summary>
        private void CheckTestProgress()
        {
            // 可以添加进度显示逻辑
        }

        /// <summary>
        /// 生成测试报告
        /// </summary>
        private void GenerateTestReport()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("===== 动画测试报告 =====");
            report.AppendLine($"测试时间: {System.DateTime.Now}");
            report.AppendLine($"测试单位: {testUnit?.unitName}");
            report.AppendLine($"批量测试次数: {(runBatchTest ? batchTestCount : 1)}");
            report.AppendLine();

            int totalTests = testResults.Count;
            int passedTests = testResults.FindAll(r => r.passed).Count;

            report.AppendLine($"测试结果: {passedTests}/{totalTests} 通过");
            report.AppendLine();

            // 按测试类型分组
            Dictionary<string, List<TestResult>> resultsByType = new Dictionary<string, List<TestResult>>();
            foreach (TestResult result in testResults)
            {
                if (!resultsByType.ContainsKey(result.testType))
                {
                    resultsByType[result.testType] = new List<TestResult>();
                }
                resultsByType[result.testType].Add(result);
            }

            foreach (var pair in resultsByType)
            {
                report.AppendLine($"=== {pair.Key} ===");
                foreach (TestResult result in pair.Value)
                {
                    string status = result.passed ? "✓ 通过" : "✗ 失败";
                    report.AppendLine($"{status}: {result.testName}");
                    report.AppendLine($"  详情: {result.message}");
                }
                report.AppendLine();
            }

            // 输出到控制台
            Debug.Log(report.ToString());

            // 保存到文件
            string filePath = $"AnimationTestReport_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            System.IO.File.WriteAllText(filePath, report.ToString());
            Debug.Log($"测试报告已保存到: {filePath}");
        }

        /// <summary>
        /// 动画事件回调
        /// </summary>
        public void OnAnimationEvent(string eventName)
        {
            animationEventTriggered = true;
            lastTriggeredEvent = eventName;
            Debug.Log($"动画事件触发: {eventName}");
        }

        /// <summary>
        /// 模拟DealDamage事件
        /// </summary>
        public void DealDamage()
        {
            OnAnimationEvent("DealDamage");
        }

        /// <summary>
        /// 模拟FireProjectile事件
        /// </summary>
        public void FireProjectile()
        {
            OnAnimationEvent("FireProjectile");
        }
    }

    /// <summary>
    /// 动画参数类
    /// </summary>
    [System.Serializable]
    public class AnimationParameter
    {
        public string name;
        public AnimatorControllerParameterType type;
        public float value;

        public AnimationParameter(string name, AnimatorControllerParameterType type, float value)
        {
            this.name = name;
            this.type = type;
            this.value = value;
        }
    }

    /// <summary>
    /// 测试结果类
    /// </summary>
    public class TestResult
    {
        public string testType;
        public string testName;
        public bool passed;
        public string message;

        public TestResult(string testType, string testName, bool passed, string message)
        {
            this.testType = testType;
            this.testName = testName;
            this.passed = passed;
            this.message = message;
        }
    }
}
