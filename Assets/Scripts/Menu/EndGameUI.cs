using System;
using System.Collections;
using System.Collections.Generic;
using ChessModel;
using UnityEngine;
using UnityEngine.UI;

public class EndGameUI : MonoBehaviour
{

    public GameObject EndGameMenu;
    public MainMenuScpirt MenuScpirt;
    public BoardManager BoardManager;

    // SỬA Ở ĐÂY: Đổi thành public để có thể kéo thả từ Inspector
    // (Lưu ý: Nếu bạn dùng TextMeshPro, hãy đổi 'Text' thành 'TMP_Text' và thêm thư viện TMPro nhé)
    public Text winText;

    private void Start()
    {
        // Xóa dòng tìm Text tự động này đi vì mình sẽ kéo thả tay
        // _WinText = GetComponentInChildren<Text>(); 
        EndGameMenu.SetActive(false);
    }

    public void EndGameNull()
    {
        EndGameMenu.SetActive(true);
        winText.text = "Stalemate"; // Đổi tên biến
    }

    public void EndGameWin(ChessColor color)
    {
        EndGameMenu.SetActive(true);
        winText.text = color == ChessColor.White ? "White Wins!" : "Black Wins!"; // Đổi tên biến
    }

    public void RestartGame()
    {
        BoardManager.RestartGame();
        EndGameMenu.SetActive(false);
    }

    public void BackToMenu()
    {
        EndGameMenu.SetActive(false);
        MenuScpirt.BackToMenu();
    }
}