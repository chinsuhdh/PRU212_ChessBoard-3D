using System.Collections;
using System.Collections.Generic;
using ChessModel;
using UnityEngine;
using UnityEngine.AI;

public class PieceManager : MonoBehaviour
{
    private BoardManager _boardManager;
    public GameObject explosion;

    // Khởi tạo: Tìm và lưu tham chiếu đến BoardManager cha
    void Awake()
    {
        _boardManager = GetComponentInParent<BoardManager>();
    }

    // Chức năng: Ra lệnh cho một quân cờ (piece) di chuyển đến vị trí (placement)
    // rock: biến kiểm tra xem có phải nước đi Nhập thành (Castling) hay không
    public void MovePiece(GameObject piece, Vector3 placement, bool rock = false)
    {
        piece.GetComponent<PiecePieces>().Move(placement, rock);
    }

    // Chức năng: Ra lệnh cho một quân cờ tấn công kẻ địch tại vị trí enemyPlacement
    public void AttackWithPiece(GameObject piece, Vector3 placement, Vector3 enemyPlacement, GameObject enemy)
    {
        piece.GetComponent<PiecePieces>().Attack(placement, enemyPlacement, enemy);
    }

    // Chức năng: Được gọi khi quân cờ đã hoàn thành xong mọi hoạt ảnh (đi/đánh)
    // Để báo cho BoardManager biết là "Xong rồi, chuyển lượt đi"
    public void FinishedAnim()
    {
        _boardManager.NextTurn();
    }
}