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

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        Application.runInBackground = true;
        PhotonNetwork.PhotonServerSettings.AppSettings.Protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;

        mainMenu.SetActive(true);
        firstMenu.SetActive(true);
        modeSelectionPanel.SetActive(false);
        lobbyPanel.SetActive(false);

        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);

        statusText.text = "Đang kết nối tới máy chủ Photon...";

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.SerializationRate = 15;
        PhotonNetwork.SendRate = 20;

        statusText.text = "Đã kết nối! Sẵn sàng chơi.";

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
    }

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
        Debug.Log("=> Đã vào phòng thành công!");

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
        if (mainMenu != null) mainMenu.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameMode"))
        {
            string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["GameMode"];
            if (mode == "PvP") boardManager.StartNormalGame_PvP();
            else if (mode == "ScorchedEarth") boardManager.StartScorchedEarthGame();
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

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList) cachedRoomList.Remove(info.Name);
            else cachedRoomList[info.Name] = info;
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
        if (isActive) UpdateRoomListUI();
    }

    private void UpdateRoomListUI()
    {
        foreach (Transform child in roomListContent) Destroy(child.gameObject);

        foreach (var kvp in cachedRoomList)
        {
            RoomInfo info = kvp.Value;
            if (info.CustomProperties.ContainsKey("GameMode"))
            {
                string roomMode = (string)info.CustomProperties["GameMode"];
                if (roomMode == selectedOnlineMode)
                {
                    GameObject newRoom = Instantiate(roomItemPrefab, roomListContent, false);
                    newRoom.GetComponent<RoomItem>().SetRoomInfo(info);
                }
            }
        }
    }

    public void CloseRoomListPanel()
    {
        if (roomListPanel != null) roomListPanel.SetActive(false);
    }
}