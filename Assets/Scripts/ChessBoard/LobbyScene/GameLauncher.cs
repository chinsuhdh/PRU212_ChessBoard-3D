using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class GameLauncher : MonoBehaviourPunCallbacks
{
    [Header("--- KÉO THẢ UI TỪ CANVAS ---")]
    public GameObject mainMenu;
    public GameObject firstMenu;
    public GameObject modeSelectionPanel;
    public GameObject lobbyPanel;

    // --- MỚI THÊM: Tham chiếu đến bảng Luật Tiêu Thổ ---
    public GameObject scorchedTutorialPanel;

    [Header("--- THAM CHIẾU LOBBY ---")]
    public TMP_InputField roomNameInput;
    public TMP_Text statusText;

    [Header("--- BÀN CỜ ---")]
    public BoardManager boardManager;

    private string selectedOnlineMode = "";

    void Start()
    {
        mainMenu.SetActive(true);
        firstMenu.SetActive(true);
        modeSelectionPanel.SetActive(false);
        lobbyPanel.SetActive(false);

        // Đảm bảo bảng luật tắt lúc mới vào game
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);

        statusText.text = "Đang kết nối tới máy chủ Photon...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Đã kết nối! Sẵn sàng chơi.";
        PhotonNetwork.JoinLobby();
    }

    // ================= 0. HÀM CHO FIRST MENU =================

    public void GoToModeSelection()
    {
        firstMenu.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void BackToFirstMenu()
    {
        modeSelectionPanel.SetActive(false);
        firstMenu.SetActive(true);
    }

    // ================= 1. CÁC HÀM CHO 4 NÚT CHỌN MODE =================

    public void OnMode_PvP_Clicked()
    {
        selectedOnlineMode = "PvP";
        ShowLobby(); // PvP thường thì vào thẳng Lobby
    }

    // --- ĐÃ SỬA: Khi bấm Tiêu Thổ thì MỞ BẢNG LUẬT TRƯỚC ---
    public void OnMode_ScorchedEarth_Clicked()
    {
        selectedOnlineMode = "ScorchedEarth";
        modeSelectionPanel.SetActive(false); // Ẩn menu chọn chế độ

        if (scorchedTutorialPanel != null)
        {
            scorchedTutorialPanel.SetActive(true); // Mở bảng luật lên
        }
        else
        {
            ShowLobby(); // Đề phòng quên kéo UI thì vẫn vào được game
        }
    }

    // --- MỚI THÊM: Dành cho nút "ĐÃ HIỂU - CHIẾN THÔI" ---
    public void ProceedToScorchedEarthLobby()
    {
        if (scorchedTutorialPanel != null)
        {
            scorchedTutorialPanel.SetActive(false); // Tắt bảng luật đi
        }
        ShowLobby(); // Bây giờ mới gọi Lobby ra
    }
    // -----------------------------------------------------

    public void OnMode_PlayerVsAI_Clicked()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        StartOfflineGame("PlayerVsAI");
    }

    public void OnMode_AIvsAI_Clicked()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        StartOfflineGame("AIvsAI");
    }

    private void StartOfflineGame(string mode)
    {
        mainMenu.SetActive(false);

        if (mode == "PlayerVsAI")
        {
            // boardManager.StartGame_PlayerVsAI();  
        }
        else if (mode == "AIvsAI")
        {
            // boardManager.StartGame_AIvsAI();      
        }
    }

    // ================= 2. XỬ LÝ GIAO DIỆN LOBBY =================

    private void ShowLobby()
    {
        modeSelectionPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        roomNameInput.gameObject.SetActive(true);
        statusText.text = $"Chế độ: {selectedOnlineMode}\nHãy nhập tên phòng.";
    }

    public void BackToModeSelection()
    {
        lobbyPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false); // Đảm bảo bảng luật đã tắt
    }

    // ================= 3. PHOTON LOGIC (TẠO/VÀO/ĐỢI PHÒNG) =================

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text)) return;

        RoomOptions options = new RoomOptions { MaxPlayers = 2 };

        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps.Add("GameMode", selectedOnlineMode);
        options.CustomRoomProperties = roomProps;
        options.CustomRoomPropertiesForLobby = new string[] { "GameMode" };

        PhotonNetwork.CreateRoom(roomNameInput.text, options);
        statusText.text = "Đang tạo phòng...";
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text)) return;
        PhotonNetwork.JoinRoom(roomNameInput.text);
        statusText.text = "Đang tìm phòng...";
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            statusText.text = "Tạo phòng thành công!\nĐang chờ đối thủ tham gia...";
            roomNameInput.gameObject.SetActive(false);
        }
        else if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartNetworkGame();
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            StartNetworkGame();
        }
    }

    private void StartNetworkGame()
    {
        mainMenu.SetActive(false);

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameMode"))
        {
            string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["GameMode"];

            if (mode == "PvP") boardManager.StartNormalGame_PvP();
            else if (mode == "ScorchedEarth") boardManager.StartScorchedEarthGame();
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message) { statusText.text = "Lỗi tạo phòng: " + message; }
    public override void OnJoinRoomFailed(short returnCode, string message) { statusText.text = "Lỗi vào phòng: " + message; }

    // ================= 4. XỬ LÝ ĐỐI THỦ THOÁT GAME =================
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (boardManager != null && boardManager.playing)
        {
            boardManager.OpponentDisconnected();
        }
    }
}