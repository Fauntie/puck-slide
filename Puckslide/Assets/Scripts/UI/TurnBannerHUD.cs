using System.Collections;
using TMPro;
using UnityEngine;

public class TurnBannerHUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_PlayerLabel;

    [SerializeField]
    private float m_SlideDistance = 32f;

    [SerializeField]
    private float m_FadeDuration = 0.35f;

    [SerializeField]
    private float m_MinHoldDuration = 0.6f;

    [SerializeField]
    private CanvasGroup m_WaitingOverlay;

    private CanvasGroup m_CanvasGroup;
    private RectTransform m_RectTransform;
    private Coroutine m_AnimationRoutine;
    private Vector2 m_DefaultAnchoredPosition;
    private bool m_IsLocalTurn;

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
    }

    private void OnEnable()
    {
        EventsManager.OnTurnChanged.AddListener(OnTurnChanged, true);
        EventsManager.OnLobbySnapshot.AddListener(OnLobbySnapshot, true);
        UpdateLabels(PuckController.IsWhiteTurn);
        ApplyMutedState();
    }

    private void OnDisable()
    {
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
        EventsManager.OnLobbySnapshot.RemoveListener(OnLobbySnapshot);
        if (m_AnimationRoutine != null)
        {
            StopCoroutine(m_AnimationRoutine);
            m_AnimationRoutine = null;
        }
    }

    private void OnTurnChanged(bool isWhiteTurn)
    {
        UpdateLabels(isWhiteTurn);
        ApplyMutedState();
        StartAnimationIfNeeded();
    }

    private void OnLobbySnapshot(LobbySnapshot _)
    {
        ApplyMutedState();
    }

    private void UpdateLabels(bool isWhiteTurn)
    {
        if (m_PlayerLabel != null)
        {
            m_PlayerLabel.text = isWhiteTurn ? "White's turn" : "Black's turn";
        }
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

        yield return new WaitForSeconds(m_MinHoldDuration);

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

    private void ApplyMutedState()
    {
        m_IsLocalTurn = PuckController.IsWhiteTurn == LobbyState.LocalIsWhitePlayer;

        if (m_WaitingOverlay != null)
        {
            m_WaitingOverlay.alpha = m_IsLocalTurn ? 0f : 1f;
            m_WaitingOverlay.blocksRaycasts = !m_IsLocalTurn;
            m_WaitingOverlay.interactable = false;
        }
    }
}
