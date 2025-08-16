using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Phase1PieceButton : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_PieceCountText;

    [SerializeField]
    private ChessPieceType m_ChessPieceType;

    [SerializeField]
    private bool m_IsWhite;

    [SerializeField]
    private Button m_Button;

    [SerializeField]
    private GameObject m_PuckPrefab;
    [SerializeField]
    private Transform m_PuckSpawnTransform;


    private int m_PieceCount = 0;
    private bool m_IsSticky;
    
    private void OnEnable()
    {
        EventsManager.OnPieceSetupData.AddListener(OnPieceSetup, true);
        m_Button.onClick.AddListener(OnButtonPress);
    }
    
    private void OnDisable()
    {
        EventsManager.OnPieceSetupData.RemoveListener(OnPieceSetup);
        m_Button.onClick.RemoveListener(OnButtonPress);
        m_PieceCount = 0;
        m_PieceCountText.text = "X 0";
    }

    private void OnButtonPress()
    {
        if (m_PieceCount == 0)
        {
            return;
        }

        if (PuckController.IsWhiteTurn != m_IsWhite)
        {
            return;
        }

        float diameter = m_PuckPrefab.GetComponent<CircleCollider2D>().radius * 2f * m_PuckPrefab.transform.localScale.x;
        Vector3 spawnPos = m_PuckSpawnTransform.position + m_PuckSpawnTransform.up * diameter;

        PuckController puckController = Instantiate(m_PuckPrefab, spawnPos, Quaternion.identity).GetComponent<PuckController>();
        puckController.Init(m_ChessPieceType, m_IsSticky, m_IsWhite);
        m_PieceCount--;
        m_PieceCountText.text = $"X {m_PieceCount}";
    }
    
    private void OnPieceSetup(PieceSetupData[] pieceSetupData)
    {
        if (pieceSetupData == null)
        {
            return;
        }
        
        for (int i = 0; i < pieceSetupData.Length; i++)
        {
            if (pieceSetupData[i].Type == m_ChessPieceType)
            {
                if (m_IsWhite)
                {
                    m_PieceCountText.text = $"X {pieceSetupData[i].WhiteCount}";
                    m_PieceCount = pieceSetupData[i].WhiteCount;
                }
                else
                {
                    m_PieceCountText.text = $"X {pieceSetupData[i].BlackCount}";
                    m_PieceCount = pieceSetupData[i].BlackCount;
                }

                m_IsSticky = pieceSetupData[i].Sticky;
                return;
            }
        }
    }
}
