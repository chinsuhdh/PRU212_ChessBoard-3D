using System.Collections.Generic;
using System.Linq;
using ChessModel;
using UnityEngine;

public class TileManager : MonoBehaviour
{

    private TileScript[] tileList; // Mảng chứa 64 script của 64 ô cờ
    private BoardManager boardManager;

    // Khởi tạo: Tìm tất cả các ô cờ con và lưu vào mảng tileList
    private void Awake()
    {
        tileList = gameObject.GetComponentsInChildren<TileScript>();
        boardManager = gameObject.GetComponentInParent<BoardManager>();
    }

    // Chức năng: Hàm trung gian.
    // Khi một ô cờ bị click, nó gọi hàm này, hàm này lại báo lên BoardManager: "Ê, ô số [tilePlacement] vừa bị bấm".
    public void clickTile(int tilePlacement)
    {
        boardManager.ClickTile(tilePlacement);
    }


    // Chức năng: Lấy GameObject của ô cờ dựa trên số thứ tự (0-63).
    public GameObject getTile(int position)
    {
        return tileList[position].gameObject;
    }

    // Chức năng: Tính toán vị trí thực tế (Vector3) để đặt quân cờ.
    // Vì tâm của ô cờ nằm ở dưới đất, nên cần cộng thêm một khoảng (offset) theo trục Y
    // để quân cờ đứng "trên" mặt ô chứ không bị chìm xuống đất.
    public Vector3 getCoordinatesByTilePlacement(int position)
    {
        Vector3 tileCoord = getTile(position).transform.position;
        // Cộng thêm offset dựa trên kích thước (Scale) của ô cờ
        tileCoord.x += (int) (5 * transform.localScale.x);
        tileCoord.y += (int) (5 * transform.localScale.y);
        tileCoord.z += (int) (5 * transform.localScale.z);
        return tileCoord;
    }

    // Chức năng: Hiển thị các nước đi hợp lệ (Gợi ý).
    // 1. Tắt đèn (Unhighlight) tất cả các ô cũ.
    // 2. Bật đèn (Highlight) các ô nằm trong danh sách nước đi (moves).
    public void updateLegalMoves(List<Move> moves)
    {
        foreach (var tile in tileList)
        {
            tile.UnHighlightTile();
        }
        // Chỉ bật sáng những ô đích (EndPosition) mà quân cờ có thể đi tới
        foreach (var position in moves.Select(move => move.EndPosition))
        {
            getTile(position).GetComponent<TileScript>().HighlightTile();
        }
    }
}
