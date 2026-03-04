using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;

public class RoomItem : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    private string roomName;

    // Hàm này để đổ dữ liệu từ Photon vào UI
    public void SetRoomInfo(RoomInfo info)
    {
        roomName = info.Name;
        roomNameText.text = info.Name;
        playerCountText.text = info.PlayerCount + "/" + info.MaxPlayers;

        // Nếu phòng đầy thì có thể đổi màu hoặc ẩn nút Join (tùy bạn)
        if (info.PlayerCount >= info.MaxPlayers) playerCountText.text += " (FULL)";
    }

    public void OnClickJoin()
    {
        PhotonNetwork.JoinRoom(roomName);
    }
}