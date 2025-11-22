using System.Collections;
using TMPro;
using UnityEngine;

public class TurnBannerHUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_PlayerLabel;

    [SerializeField]
    private TMP_Text m_OrientationLabel;

    [SerializeField]
    private TMP_Text m_BannerArrow;

    [SerializeField]
    private TMP_Text m_MiniArrow;

    [SerializeField]
    private float m_SlideDistance = 32f;

    [SerializeField]
    private float m_FadeDuration = 0.35f;

    [SerializeField]
    private float m_MinHoldDuration = 0.6f;

    private CanvasGroup m_CanvasGroup;
    private RectTransform m_RectTransform;
    private Coroutine m_AnimationRoutine;
    private Vector2 m_DefaultAnchoredPosition;
    private bool m_IsFlipped;

    private void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();
        m_RectTransform = transform as RectTransform;
        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = 0f;
        }
        if (m_RectTransform != null)
        {
            m_DefaultAnchoredPosition = m_RectTransform.anchoredPosition;
        }

        if (m_OrientationLabel != null)
        {
            m_OrientationLabel.text = string.Empty;
        }
    }

    private void OnEnable()
    {
        EventsManager.OnBoardFlipState.AddListener(OnBoardFlipStateChanged, true);
        EventsManager.OnTurnChanged.AddListener(OnTurnChanged, true);
        UpdateLabels(PuckController.IsWhiteTurn);
    }

    private void OnDisable()
    {
        EventsManager.OnBoardFlipState.RemoveListener(OnBoardFlipStateChanged);
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
        if (m_AnimationRoutine != null)
        {
            StopCoroutine(m_AnimationRoutine);
            m_AnimationRoutine = null;
        }
    }

    private void OnBoardFlipStateChanged(bool isFlipped)
    {
        m_IsFlipped = isFlipped;
        UpdateLabels(PuckController.IsWhiteTurn);
        UpdateArrows();
        StartAnimationIfNeeded();
    }

    private void OnTurnChanged(bool isWhiteTurn)
    {
        UpdateLabels(isWhiteTurn);
        UpdateArrows();
        StartAnimationIfNeeded();
    }

    private void UpdateLabels(bool isWhiteTurn)
    {
        if (m_PlayerLabel != null)
        {
            m_PlayerLabel.text = isWhiteTurn ? "White's turn" : "Black's turn";
        }

        if (m_OrientationLabel != null)
        {
            m_OrientationLabel.text = string.Empty;
        }
    }

    private void UpdateArrows()
    {
        float zRotation = m_IsFlipped ? 180f : 0f;

        if (m_BannerArrow != null)
        {
            RotateArrow(m_BannerArrow.rectTransform, zRotation);
        }

        if (m_MiniArrow != null)
        {
            RotateArrow(m_MiniArrow.rectTransform, zRotation);
        }
    }

    private static void RotateArrow(RectTransform arrow, float zRotation)
    {
        if (arrow == null)
        {
            return;
        }

        arrow.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private void StartAnimationIfNeeded()
    {
        if (m_RectTransform == null || m_CanvasGroup == null)
        {
            return;
        }

        if (m_AnimationRoutine != null)
        {
            return;
        }

        m_AnimationRoutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector2 hiddenPosition = m_DefaultAnchoredPosition + new Vector2(0f, m_SlideDistance);
        float halfFade = Mathf.Max(0.05f, m_FadeDuration * 0.5f);

        m_RectTransform.anchoredPosition = hiddenPosition;
        m_CanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < m_FadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_FadeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            m_RectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, m_DefaultAnchoredPosition, eased);
            m_CanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * (m_FadeDuration / halfFade)));
            yield return null;
        }

        m_RectTransform.anchoredPosition = m_DefaultAnchoredPosition;
        m_CanvasGroup.alpha = 1f;

        float holdTime = Mathf.Max(BoardFlipper.GetFlipDuration(), m_MinHoldDuration);
        yield return new WaitForSeconds(holdTime);

        elapsed = 0f;
        while (elapsed < m_FadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_FadeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            m_RectTransform.anchoredPosition = Vector2.Lerp(m_DefaultAnchoredPosition, hiddenPosition, eased);
            m_CanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(t * (m_FadeDuration / halfFade)));
            yield return null;
        }

        m_RectTransform.anchoredPosition = hiddenPosition;
        m_CanvasGroup.alpha = 0f;
        m_AnimationRoutine = null;
    }
}
