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
        m_StickyToggle.isOn = false;
        m_StickyToggle.onValueChanged.AddListener(OnToggle);
    }
    
    private void OnDisable()
    {
        m_StickyToggle.onValueChanged.RemoveListener(OnToggle);
    }

    private void OnToggle(bool isActive)
    {
        m_GameSetupManager.ToggleSticky(m_ChessPieceType, isActive);
    }
}
