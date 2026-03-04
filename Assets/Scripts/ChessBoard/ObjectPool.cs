using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChessModel;
using Exploder.Utils;
using UnityEngine;

public class ObjectPool : MonoBehaviour {

    // Danh sách các "kho chứa" riêng biệt cho từng loại quân
    private MaterialManager _materialManager;
    
    private const int BoardsToInitialise = 1;

    public GameObject PawnPiece;
    private List<GameObject> pooledPawn;
    public GameObject RookPiece;
    private List<GameObject> pooledRook;
    public GameObject BishopPiece;
    private List<GameObject> pooledBishop;
    public GameObject KnightPiece;
    private List<GameObject> pooledKnignt;
    public GameObject KingPiece;
    private List<GameObject> pooledKing;
    public GameObject QueenPiece;
    private List<GameObject> pooledQueen;

    // Chức năng: Tạo sẵn các quân cờ (Instantiate) khi game bắt đầu và thêm vào danh sách (List), sau đó tắt (SetActive false) để ẩn đi.
    // objectToPool: Loại quân cần tạo (Prefab gốc).
    // pooledObjects: Danh sách để lưu trữ.
    private void instantiateLootObjetcts(GameObject objectToPool, ref List<GameObject> pooledObjects)
    {
        pooledObjects = new List<GameObject>();
        GameObject tmp;
        int piecesToInitialise = 0;
        if (objectToPool == PawnPiece)
            piecesToInitialise = 16 * BoardsToInitialise;
        else if(objectToPool == RookPiece || objectToPool == BishopPiece || objectToPool == KnightPiece || objectToPool == QueenPiece)
            piecesToInitialise = 6 * BoardsToInitialise;
        else if(objectToPool == KingPiece)
            piecesToInitialise = 2 * BoardsToInitialise;
        for (int i = 0; i < piecesToInitialise; i++)
        {
            tmp = Instantiate(objectToPool, new Vector3(), Quaternion.identity, transform);
            tmp.SetActive(false);
            pooledObjects.Add(tmp);
        }
    }

    // Chức năng: Lấy một quân cờ từ trong kho ra để sử dụng ("Spawn").
    // 1. Tìm trong kho loại quân tương ứng.
    // 2. Gọi MaterialManager để sơn đúng màu.
    // 3. Đặt vào vị trí placement.
    // 4. Bật lên (SetActive true).
    public GameObject getPooledPiece(ChessType chessType, ChessColor color, Vector3 placement)
    {
        GameObject ChessPiece; 
        switch (chessType)
        {
            case ChessType.Bishop:
                ChessPiece = GetPooledObject(pooledBishop);
                break;
            case ChessType.King:
                ChessPiece = GetPooledObject(pooledKing);
                break;
            case ChessType.Knight:
                ChessPiece = GetPooledObject(pooledKnignt);
                break;
            case ChessType.Pawn:
                ChessPiece = GetPooledObject(pooledPawn);
                break;
            case ChessType.Queen:
                ChessPiece = GetPooledObject(pooledQueen);
                break;
            case ChessType.Rook:
                ChessPiece = GetPooledObject(pooledRook);
                break;
            default:
                return null;
        }
        ChessPiece = _materialManager.changeMaterial(ChessPiece , chessType, color);
        
        ChessPiece.GetComponent<PiecePieces>().IsWhite = color==ChessColor.White;
        ChessPiece.transform.position = placement;
        ChessPiece.SetActive(true);
        ExploderSingleton.Instance.CrackObject(ChessPiece);
        return ChessPiece;
    }

    // Chức năng: Tìm trong danh sách một quân cờ đang "rảnh" (không active) để tái sử dụng.
    // Nếu tất cả đều đang bận, có thể cần logic mở rộng thêm (nhưng ở đây game cờ vua số lượng quân cố định nên không lo thiếu).
    public GameObject GetPooledObject(List<GameObject> pooledObjects)
    {
        return pooledObjects.Find(pooledObject => !pooledObject.activeInHierarchy);
    }

    // Chức năng: Khởi chạy đầu tiên. Gọi hàm tạo sẵn quân cờ cho tất cả các loại (Tốt, Xe, Mã, Vua, Hậu).
    void Start()
    {
        _materialManager = GetComponent<MaterialManager>();
        
        instantiateLootObjetcts(PawnPiece, ref pooledPawn);
        instantiateLootObjetcts(BishopPiece, ref pooledBishop);
        instantiateLootObjetcts(RookPiece, ref pooledRook);
        instantiateLootObjetcts(KingPiece, ref pooledKing);
        instantiateLootObjetcts(KnightPiece, ref pooledKnignt);
        instantiateLootObjetcts(QueenPiece, ref pooledQueen);
    }
}
