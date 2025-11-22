using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StickySetup : MonoBehaviour
{
    [SerializeField]
    private GameSetupManager m_GameSetupManager;
    [SerializeField]
    private ChessPieceType m_ChessPieceType;

    [SerializeField]
    private Toggle m_StickyToggle;

    private void OnEnable()
    {
        Refresh();
        m_GameSetupManager.CountsChanged += Refresh;
        m_StickyToggle.onValueChanged.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        m_StickyToggle.onValueChanged.RemoveListener(OnToggle);
        m_GameSetupManager.CountsChanged -= Refresh;
    }

    private void OnToggle(bool isActive)
    {
        m_GameSetupManager.ToggleSticky(m_ChessPieceType, isActive);
    }

    private void Refresh()
    {
        if (m_GameSetupManager == null)
        {
            return;
        }

        m_StickyToggle.isOn = m_GameSetupManager.GetSticky(m_ChessPieceType);
        m_StickyToggle.interactable = m_GameSetupManager.IsLocalHost;
    }
}
