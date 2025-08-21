using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gley.TrafficSystem;

[DisallowMultipleComponent]
public class AlphaPulse : MonoBehaviour
{
    [Header("目标组件")]
    public Image targetRawImage;
    public TextMeshProUGUI[] targetTexts;

    public PlayerComponent player;

    [Header("闪烁参数 (0–255)")]
    [Range(0, 255)] public int minAlphaByte = 0;
    [Range(0, 255)] public int maxAlphaByte = 150;
    public float frequency = 1.0f;
    public bool randomizePhase = true;

    [Header("稳态控制")]
    [Tooltip("当状态切到false后，透明度在该时长内平滑降为0")]
    public float fadeOutDuration = 0.35f;
    [Tooltip("状态切换的最小持续时间，避免极短时间横跳")]
    public float stateDebounce = 0.2f;

    private Color _rawInitColor;
    private Color[] _textInitColors;
    private bool _hasRaw, _hasTexts;
    private float _phaseOffset;

    // 去抖逻辑
    private bool _logicalActive;      // 去抖后的稳定状态
    private float _stateTimer;        // 当前状态与稳定状态不一致时累计的时间

    // 渐隐
    private float _currentAlpha;      // 实际应用的alpha
    private float _alphaVel;          // SmoothDamp用速度

    void Awake()
    {
        if (targetRawImage == null)
            targetRawImage = GetComponent<Image>();

        if (targetTexts == null || targetTexts.Length == 0)
            targetTexts = GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);

        _hasRaw = targetRawImage != null;
        _hasTexts = targetTexts != null && targetTexts.Length > 0;

        if (_hasRaw) _rawInitColor = targetRawImage.color;

        if (_hasTexts)
        {
            _textInitColors = new Color[targetTexts.Length];
            for (int i = 0; i < targetTexts.Length; i++)
                if (targetTexts[i] != null)
                    _textInitColors[i] = targetTexts[i].color;
        }

        _phaseOffset = randomizePhase ? Random.value * Mathf.PI * 2f : 0f;

        // 初始完全透明，避免启用瞬间闪一下
        _currentAlpha = 0f;
        ApplyAlpha(_currentAlpha);
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        bool desiredActive = (player != null && player.isBehindSpeeding);

        // --- 去抖动（状态必须稳定持续 stateDebounce 秒才生效） ---
        if (desiredActive != _logicalActive)
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= stateDebounce)
            {
                _logicalActive = desiredActive;
                _stateTimer = 0f;
            }
        }
        else
        {
            _stateTimer = 0f;
        }

        // --- 计算目标alpha ---
        float targetAlpha;
        if (_logicalActive)
        {
            // 激活时使用正弦“呼吸”波形（不做额外平滑，以保持节奏）
            targetAlpha = CalcAlpha(Time.time);
            _currentAlpha = targetAlpha; // 直接跟随呼吸波
        }
        else
        {
            // 非激活时平滑降到0
            float smooth = Mathf.Max(0.01f, fadeOutDuration);
            targetAlpha = 0f;
            _currentAlpha = Mathf.SmoothDamp(_currentAlpha, targetAlpha, ref _alphaVel, smooth);
        }

        // 应用
        ApplyAlpha(_currentAlpha);
    }

    private float CalcAlpha(float time)
    {
        float t = (Mathf.Sin((time * frequency * 2f * Mathf.PI) + _phaseOffset) + 1f) * 0.5f;

        // 转换成 0–255，再映射到 0–1
        float minA = minAlphaByte / 255f;
        float maxA = maxAlphaByte / 255f;
        return Mathf.Lerp(minA, maxA, t);
    }

    private void ApplyAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (_hasRaw)
        {
            var c = _rawInitColor;
            c.a = alpha;
            targetRawImage.color = c;
        }

        if (_hasTexts)
        {
            for (int i = 0; i < targetTexts.Length; i++)
            {
                if (targetTexts[i] == null) continue;
                var c = _textInitColors[i];
                c.a = alpha;
                targetTexts[i].color = c;
            }
        }
    }
}
