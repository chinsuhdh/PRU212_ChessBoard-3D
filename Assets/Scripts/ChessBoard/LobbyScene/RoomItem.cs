using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;

public class RoomItem : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text gameModeText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private string roomName;

    // Hàm này để đổ dữ liệu từ Photon vào UI
    public void SetRoomInfo(RoomInfo info)
    {
        roomName = info.Name;
        roomNameText.text = info.Name;

        // Lấy thông tin Mode từ Custom Properties của phòng
        if (info.CustomProperties.ContainsKey("GameMode"))
        {
            string mode = (string)info.CustomProperties["GameMode"];
            gameModeText.text = mode == "PvP" ? "PvP" : "Cờ Tiêu Thổ";
        }
        else
        {
            gameModeText.text = "Không xác định";
        }

        // Kiểm tra và hiển thị số lượng người
        if (info.PlayerCount >= info.MaxPlayers)
        {
            playerCountText.text = info.PlayerCount + "/" + info.MaxPlayers + " (FULL)";
            playerCountText.color = Color.red;
            joinButton.interactable = false;
        }
        else
        {
            playerCountText.text = info.PlayerCount + "/" + info.MaxPlayers;
            playerCountText.color = Color.white;
            joinButton.interactable = true;
        }
    }

    public void OnClickJoin()
    {
        // 1. Kiểm tra xem nếu đang ở trong phòng rồi thì không cho bấm nữa
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Lỗi: Bạn đã ở sẵn trong một phòng rồi!");
            return;
        }

        // 2. Chỉ thực hiện Join khi Photon đang rảnh rỗi và sẵn sàng
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Đang kết nối vào phòng: " + roomName);

            // Tắt nút ngay lập tức để chống bấm đúp (Spam click)
            if (joinButton != null)
            {
                joinButton.interactable = false;
            }

            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            Debug.LogWarning("Photon chưa sẵn sàng. Trạng thái hiện tại: " + PhotonNetwork.NetworkClientState);
        }
    }
}