using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

public class GameLauncher : MonoBehaviourPunCallbacks
{
    [Header("--- KÉO THẢ UI TỪ CANVAS ---")]
    public GameObject mainMenu;
    public GameObject firstMenu;
    public GameObject modeSelectionPanel;
    public GameObject lobbyPanel;

    // Tham chiếu đến bảng Luật Tiêu Thổ
    public GameObject scorchedTutorialPanel;

    [Header("--- DANH SÁCH PHÒNG ---")]
    public GameObject roomListPanel;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    [Header("--- THAM CHIẾU LOBBY ---")]
    public TMP_InputField roomNameInput;
    public TMP_Text statusText;

    [Header("--- BÀN CỜ ---")]
    public BoardManager boardManager;

    private string selectedOnlineMode = "";

    // Biến lưu trữ danh sách phòng từ Server gửi về
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        // Giúp game tiếp tục xử lý lệnh mạng ngay cả khi người chơi chuyển sang tab khác trên trình duyệt
        // Đây là lệnh "sống còn" cho các game Multiplayer chạy trên nền tảng WebGL
        Application.runInBackground = true;

        // Thiết lập trạng thái UI ban đầu
        mainMenu.SetActive(true);
        firstMenu.SetActive(true);
        modeSelectionPanel.SetActive(false);
        lobbyPanel.SetActive(false);

        // Đảm bảo các bảng phụ luôn tắt lúc mới vào game để tránh đè màn hình
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);

        statusText.text = "Đang kết nối tới máy chủ Photon...";

        // Kiểm tra trạng thái kết nối để tránh gọi Connect trùng lặp
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // ĐÃ SỬA LỖI 2: Xử lý xung đột trạng thái khi vào Lobby
    public override void OnConnectedToMaster()
    {
        // --- TỐI ƯU CHO WEBGL (BẮT ĐẦU) ---
        // Giảm tần suất gửi dữ liệu một chút để phù hợp với môi trường trình duyệt, 
        // giúp tránh tình trạng bị nghẽn mạng (Network Congestion).
        PhotonNetwork.SerializationRate = 15; // Mặc định thường là 20
        PhotonNetwork.SendRate = 20;          // Mặc định thường là 30
                                              // --- TỐI ƯU CHO WEBGL (KẾT THÚC) ---

        statusText.text = "Đã kết nối! Sẵn sàng chơi.";

        // ĐÃ SỬA LỖI 2: Xử lý xung đột trạng thái khi vào Lobby
        // Chỉ gọi lệnh JoinLobby nếu chưa ở trong sảnh và KHÔNG PHẢI đang trong quá trình tự động vào sảnh
        if (!PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            Debug.Log("Đang tiến hành tham gia Lobby...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Đã vào Lobby thành công! Sẵn sàng nhận danh sách phòng.");
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
        ShowLobby();
    }

    public void OnMode_ScorchedEarth_Clicked()
    {
        selectedOnlineMode = "ScorchedEarth";
        modeSelectionPanel.SetActive(false);

        if (scorchedTutorialPanel != null)
        {
            scorchedTutorialPanel.SetActive(true);
        }
        else
        {
            ShowLobby();
        }
    }

    public void ProceedToScorchedEarthLobby()
    {
        if (scorchedTutorialPanel != null)
        {
            scorchedTutorialPanel.SetActive(false);
        }
        ShowLobby();
    }

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

        string modeDisplay = selectedOnlineMode == "PvP" ? "PvP" : "Cờ Tiêu Thổ";
        statusText.text = $"Chế độ: {modeDisplay}\nHãy tạo phòng hoặc bấm 'Phòng' để tìm.";

        if (roomListPanel != null && roomListPanel.activeSelf)
        {
            UpdateRoomListUI();
        }
    }

    public void BackToModeSelection()
    {
        lobbyPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);
    }

    // ================= 3. PHOTON LOGIC =================

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
        Debug.Log("=> Đã vào phòng thành công! Số người hiện tại: " + PhotonNetwork.CurrentRoom.PlayerCount);

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            statusText.text = "Tạo phòng thành công!\nĐang chờ đối thủ tham gia...";
            roomNameInput.gameObject.SetActive(false);
            if (roomListPanel != null) roomListPanel.SetActive(false);
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
        Debug.Log("=> Bắt đầu khởi tạo bàn cờ!");

        // 1. TẮT TOÀN BỘ UI CỦA MENU VÀ LOBBY TRƯỚC KHI VÀO GAME
        if (mainMenu != null) mainMenu.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (firstMenu != null) firstMenu.SetActive(false);
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);

        // 2. KIỂM TRA MODE VÀ GỌI BÀN CỜ
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameMode"))
        {
            string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["GameMode"];
            Debug.Log("=> Chế độ chơi của phòng này là: " + mode);

            if (mode == "PvP")
            {
                boardManager.StartNormalGame_PvP();
            }
            else if (mode == "ScorchedEarth")
            {
                boardManager.StartScorchedEarthGame();
            }
        }
        else
        {
            Debug.LogError("=> LỖI TỚI TỪ PHOTON: Phòng này bị mất dữ liệu GameMode!");
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message) { statusText.text = "Lỗi tạo phòng: " + message; }
    public override void OnJoinRoomFailed(short returnCode, string message) { statusText.text = "Lỗi vào phòng: " + message; }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (boardManager != null && boardManager.playing)
        {
            boardManager.OpponentDisconnected();
        }
    }

    // ================= 5. QUẢN LÝ DANH SÁCH PHÒNG =================

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("<color=green>Photon gửi về danh sách chứa: " + roomList.Count + " phòng.</color>");
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        if (roomListPanel != null && roomListPanel.activeSelf)
        {
            UpdateRoomListUI();
        }
    }

    public void OnClickShowRoomsButton()
    {
        bool isActive = !roomListPanel.activeSelf;
        roomListPanel.SetActive(isActive);

        if (isActive)
        {
            UpdateRoomListUI();
        }
    }

    private void UpdateRoomListUI()
    {
        Debug.Log("<color=yellow>Đang cập nhật UI. Tổng số phòng lưu trong Cache: " + cachedRoomList.Count + "</color>");

        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in cachedRoomList)
        {
            RoomInfo info = kvp.Value;
            Debug.Log($"Kiểm tra phòng: {info.Name} | Số người: {info.PlayerCount}/{info.MaxPlayers}");

            if (info.CustomProperties.ContainsKey("GameMode"))
            {
                string roomMode = (string)info.CustomProperties["GameMode"];
                Debug.Log($"Mode phòng này là: {roomMode} | Mode bạn đang chọn là: {selectedOnlineMode}");

                if (roomMode == selectedOnlineMode)
                {
                    Debug.Log("=> KHỚP MODE! Bắt đầu tạo UI cho phòng này ra màn hình.");

                    // Thêm tham số false vào Instantiate để xử lý UI Scale tự động tốt nhất
                    GameObject newRoom = Instantiate(roomItemPrefab, roomListContent, false);

                    // Reset lại vị trí Z tránh bị khuất Camera
                    newRoom.transform.localPosition = new Vector3(newRoom.transform.localPosition.x, newRoom.transform.localPosition.y, 0);
                    newRoom.transform.localScale = Vector3.one;

                    newRoom.GetComponent<RoomItem>().SetRoomInfo(info);
                    newRoom.GetComponent<RoomItem>().joinButton.onClick.AddListener(() => {
                        newRoom.GetComponent<RoomItem>().OnClickJoin();
                    });
                }
            }
            else
            {
                Debug.Log("=> LỖI: Phòng này bị thiếu thuộc tính GameMode!");
            }
        }
    }

    public void CloseRoomListPanel()
    {
        if (roomListPanel != null)
        {
            roomListPanel.SetActive(false);
        }
    }
}