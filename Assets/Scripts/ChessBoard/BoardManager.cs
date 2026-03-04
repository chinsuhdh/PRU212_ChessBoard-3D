using Photon.Pun; // Đã thêm thư viện Photon
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChessModel;
using Exploder;
using Exploder.Utils;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private TileManager _tileManager;
    private PieceManager _pieceManager;
    private ObjectPool _objectPool;
    public PromotionUIScript _promotionScript;
    public EndGameUI EndGameUI;

    [Header("UI Mới thêm")]
    public GameObject pauseMenuUI; // Kéo GameObject PauseMenu vào đây

    private ChessBoard _chessBoard;

    private Dictionary<ChessColor, Player.Player> _players;

    private List<Move> _legalMoves;

    public static bool _humainPlayer;
    private bool _firstClick;

    public bool playing { get; set; }
    private bool paused;

    public GameObject whiteCam;
    public GameObject menuCam;

    // Biến kiểm tra mode chơi
    public bool IsScorchedEarthMode = false;

    // Biến lưu xem máy này đang cầm quân Trắng hay Đen
    private ChessColor myColor;

    // Tham chiếu đến Script luật mới
    private ScorchedEarthRules _scorchedRules;

    private Dictionary<Piece, GameObject> _map;

    private void Start()
    {
        _tileManager = GetComponentInChildren<TileManager>();
        _pieceManager = GetComponentInChildren<PieceManager>();
        _objectPool = GetComponentInChildren<ObjectPool>();

        // --- [LẤY COMPONENT AN TOÀN] ---
        _scorchedRules = GetComponent<ScorchedEarthRules>();
        if (_scorchedRules == null) _scorchedRules = GetComponentInChildren<ScorchedEarthRules>();
        if (_scorchedRules == null) Debug.LogError("CHƯA GẮN SCRIPT [ScorchedEarthRules]!!");

        _chessBoard = new ChessBoard(this);
        _chessBoard.Rock += RockDone;

        _map = new Dictionary<Piece, GameObject>(32);

        _humainPlayer = false;
        _firstClick = true;

        playing = false;
        paused = true;

        _legalMoves = new List<Move>();

        _chessBoard.InitializeBoard();

        // Lưu màu gốc cho các ô
        for (int i = 0; i < 64; i++)
        {
            var tile = _tileManager.getTile(i);
            if (tile != null) tile.GetComponent<TileScript>().SaveOriginalColor();
        }

        foreach (var piece in _chessBoard.Board)
        {
            if (piece.Type != ChessType.None)
                _map.Add(piece, createPieceOnPlacement(piece.Type, piece.Color, piece.Position));
            if (piece.Color == ChessColor.Black) _map[piece].transform.Rotate(0, 180, 0);
        }
    }

    private void SwapCam()
    {
        whiteCam.SetActive(!whiteCam.activeInHierarchy);
    }

    public void RestartGame()
    {
        _chessBoard.InitializeBoard();

        // Reset luật Tiêu Thổ khi chơi lại
        if (_scorchedRules != null) _scorchedRules.ResetRules();

        FragmentPool.Instance.DeactivateFragments();
        FragmentPool.Instance.DestroyFragments();
        FragmentPool.Instance.Reset(ExploderSingleton.Instance.Params);

        foreach (var piece in _map)
        {
            piece.Value.SetActive(false);
        }

        _humainPlayer = false;
        _legalMoves.Clear();
        _tileManager.updateLegalMoves(_legalMoves);
        _firstClick = true;

        _map.Clear();
        foreach (var piece in _chessBoard.Board)
        {
            if (piece.Type == ChessType.None) continue;
            GameObject gameObjectPiece = createPieceOnPlacement(piece.Type, piece.Color, piece.Position);
            gameObjectPiece.GetComponent<PiecePieces>().ResetMovement();
            _map.Add(piece, gameObjectPiece);
        }
        playing = true;
    }

    // --- Hàm bắt đầu game Tiêu Thổ ---
    public void StartScorchedEarthGame()
    {
        IsScorchedEarthMode = true;
        RestartGame();

        Dictionary<ChessColor, Player.Player> players = new Dictionary<ChessColor, Player.Player>();
        players.Add(ChessColor.White, null);
        players.Add(ChessColor.Black, null);
        InitialisePlay(players);
    }

    // --- Hàm bắt đầu game thường ---
    public void StartNormalGame_PvP()
    {
        IsScorchedEarthMode = false;

        // Dọn dẹp màu mè cũ nếu có
        if (_scorchedRules != null)
        {
            _scorchedRules.ResetRules();
            for (int i = 0; i < 64; i++)
            {
                var tile = _tileManager.getTile(i).GetComponent<TileScript>();
                tile.SetBrokenVisual(0);
                tile.SetCooldownNumber(0);
            }
        }

        RestartGame();
        Dictionary<ChessColor, Player.Player> players = new Dictionary<ChessColor, Player.Player>();
        players.Add(ChessColor.White, null);
        players.Add(ChessColor.Black, null);
        InitialisePlay(players);
    }

    public void InitialisePlay(Dictionary<ChessColor, Player.Player> players)
    {
        _players = players;

        // --- LẬP TRÌNH MẠNG: CHIA MÀU ---
        if (PhotonNetwork.InRoom)
        {
            // Nếu là chủ phòng thì cầm Trắng, khách cầm Đen
            myColor = PhotonNetwork.IsMasterClient ? ChessColor.White : ChessColor.Black;
            whiteCam.SetActive(myColor == ChessColor.White); // Xoay camera cho đúng màu
        }
        else
        {
            myColor = ChessColor.White; // Chơi offline mặc định
            if (players[ChessColor.White] != null && players[ChessColor.Black] == null)
                whiteCam.SetActive(false);
        }
        // --------------------------------

        menuCam.SetActive(false);
        GetComponent<AudioSource>().Play();
        playing = true;
        paused = true;
    }

    private void MovePiece(GameObject piece, Move move, bool rock = false)
    {
        int position = move.EndPosition;

        // --- Logic: Đánh dấu Đất Lún (Vàng) khi ăn quân ---
        if (IsScorchedEarthMode && move.Eat && _scorchedRules != null)
        {
            _scorchedRules.MarkTileUnstable(position);

            var tileObj = _tileManager.getTile(position).GetComponent<TileScript>();
            if (tileObj != null)
            {
                tileObj.SetBrokenVisual(1); // Vàng
                // Cập nhật số ngay lập tức
                tileObj.SetCooldownNumber(_scorchedRules.GetRemainingTurns(position));
            }
        }
        // --------------------------------------------------

        if (move.Eat)
        {
            _pieceManager.AttackWithPiece(piece, _tileManager.getCoordinatesByTilePlacement(position), _tileManager.getCoordinatesByTilePlacement(move.EatenPiece.Position), _map[move.EatenPiece]);
        }
        else
        {
            _pieceManager.MovePiece(piece, _tileManager.getCoordinatesByTilePlacement(position), rock);
        }
    }

    public void ClickTile(int placement)
    {
        if (!_humainPlayer || !playing) return;

        if (_firstClick)
        {
            if ((_legalMoves = _chessBoard.GetMoveFromPosition(placement)).Any())
            {
                _firstClick = false;
            }
        }
        else
        {
            _legalMoves = _legalMoves.Where(move => move.EndPosition.Equals(placement)).ToList();
            if (_legalMoves.Count == 1)
            {
                var move = _legalMoves[0];
                _humainPlayer = false;

                // --- [PHOTON] BÁO CÁO NƯỚC ĐI CHO MÁY BÊN KIA ---
                if (PhotonNetwork.InRoom)
                {
                    GetComponent<PhotonView>().RPC("RPC_ReceiveMove", RpcTarget.Others, move.StartPosition, move.EndPosition);
                }
                // ------------------------------------------------

                _chessBoard.Play(move);
                MovePiece(_map[move.Piece], move);
                _legalMoves.Clear();
                _firstClick = true;
            }
            else
            {
                _legalMoves = _chessBoard.GetMoveFromPosition(placement);
            }
        }

        _tileManager.updateLegalMoves(_legalMoves);
    }

    private GameObject createPieceOnPlacement(ChessType pieceType, ChessColor color, int position)
    {
        var pieceObject = _objectPool.getPooledPiece(pieceType, color, _tileManager.getCoordinatesByTilePlacement(position));
        return pieceObject;
    }

    public void NextTurn()
    {
        if (paused) return;

        // --- Logic Tiêu Thổ: Cuối lượt ---
        if (IsScorchedEarthMode && _scorchedRules != null)
        {
            // A. Tính toán logic
            List<int> collapsedTiles = _scorchedRules.ProcessTurnLogic();

            // B. Cập nhật hình ảnh + số
            for (int i = 0; i < 64; i++)
            {
                var state = _scorchedRules.TileStates[i];
                var tileScript = _tileManager.getTile(i).GetComponent<TileScript>();

                if (tileScript != null)
                {
                    tileScript.SetBrokenVisual(state);
                    tileScript.SetCooldownNumber(_scorchedRules.GetRemainingTurns(i));
                }
            }

            // C. Giết quân
            foreach (int tileIndex in collapsedTiles)
            {
                Piece pieceOnTrap = _chessBoard.GetPiece(tileIndex);
                if (pieceOnTrap != null && pieceOnTrap.Type != ChessType.None)
                {
                    if (_map.ContainsKey(pieceOnTrap))
                    {
                        GameObject obj = _map[pieceOnTrap];
                        StartCoroutine(obj.GetComponent<PiecePieces>().Die());
                        _map.Remove(pieceOnTrap);
                    }

                    _chessBoard.Board[tileIndex] = new Piece(ChessColor.None, tileIndex, ChessType.None);

                    if (pieceOnTrap.Type == ChessType.King)
                    {
                        EndGameWin(pieceOnTrap.Color.Reverse(), pieceOnTrap);
                        return;
                    }
                }
            }
        }
        // ---------------------------------

        var nextToPlay = _chessBoard.NextToPlay;

        var currentPlayer = _players[nextToPlay];
        if (currentPlayer == null)
        {
            // LẬP TRÌNH MẠNG: CHỈ CHO CLICK NẾU TỚI LƯỢT CỦA MÀU MÌNH CẦM
            if (PhotonNetwork.InRoom)
            {
                _humainPlayer = (nextToPlay == myColor);
            }
            else
            {
                // Chơi offline 1 máy 2 người thì đổi cam qua lại
                whiteCam.SetActive(nextToPlay == ChessColor.White);
                _humainPlayer = true;
            }
        }
        else
        {
            var move = currentPlayer.GetDesiredMove();
            MovePiece(_map[move.Piece], move);
            _chessBoard.Play(move);
        }
    }

    private void Update()
    {
        if (playing && paused)
        {
            paused = false;
            NextTurn();
        }

        if (!playing && !paused)
        {
            paused = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwapCam();
        }

        // --- MỚI THÊM: BẬT/TẮT PAUSE MENU BẰNG NÚT ESC ---
        if (Input.GetKeyDown(KeyCode.Escape) && playing)
        {
            if (pauseMenuUI != null)
            {
                pauseMenuUI.SetActive(!pauseMenuUI.activeSelf);
            }
        }
    }

    private void RockDone(object sender, Move move)
    {
        MovePiece(_map[move.Piece], move, true);
    }

    public void Promotion(Piece piece)
    {
        StartCoroutine(PromotionWait(piece));
        playing = false;
    }

    private IEnumerator PromotionWait(Piece piece)
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => _map[piece].GetComponent<PiecePieces>()._arrived);
        if (_players[_chessBoard.NextToPlay.Reverse()] == null)
        {
            // Trong tương lai nếu muốn popup phong cấp đồng bộ thì cần sửa thêm logic ở đây
            _promotionScript.show(piece);
        }
        else GivePromotion(piece, ChessType.Queen);
    }

    public void GivePromotion(Piece piece, ChessType chessType)
    {
        switch (chessType)
        {
            case ChessType.Bishop:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Bishop, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
            case ChessType.Rook:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Rook, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
            case ChessType.Queen:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Queen, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
            case ChessType.Knight:
                _map[piece].SetActive(false);
                _map[piece] = _objectPool.getPooledPiece(ChessType.Knight, piece.Color, _tileManager.getCoordinatesByTilePlacement(piece.Position));
                break;
        }
        _chessBoard.PromotePawn(piece, chessType);
        playing = true;
    }

    public void EndGameWin(ChessColor color, Piece piece)
    {
        StartCoroutine(EndGameWin2(color, piece));
    }

    public void EndGameNull(Piece piece)
    {
        StartCoroutine(EndGameNull2(piece));
    }

    private IEnumerator EndGameWin2(ChessColor color, Piece piece)
    {
        playing = false;
        yield return new WaitForSeconds(1f);
        if (_map.ContainsKey(piece))
        {
            yield return new WaitUntil(() => _map[piece].GetComponent<PiecePieces>()._arrived);
        }
        EndGameUI.EndGameWin(color);
    }

    private IEnumerator EndGameNull2(Piece piece)
    {
        playing = false;
        yield return new WaitForSeconds(1f);
        if (_map.ContainsKey(piece))
        {
            yield return new WaitUntil(() => _map[piece].GetComponent<PiecePieces>()._arrived);
        }
        EndGameUI.EndGameNull();
    }

    // ================= PHOTON RPC (NHẬN TÍN HIỆU TỪ MÁY KHÁC) =================

    [PunRPC] // Đánh dấu để Photon nhận diện
    public void RPC_ReceiveMove(int startPosition, int endPosition)
    {
        // 1. Lấy danh sách nước đi hợp lệ của đối thủ ở vị trí start
        var legalMovesForOpponent = _chessBoard.GetMoveFromPosition(startPosition);

        // 2. Tìm đúng nước đi khớp với endPosition mà mạng vừa gửi sang
        var move = legalMovesForOpponent.FirstOrDefault(m => m.EndPosition == endPosition);

        if (move != null)
        {
            // 3. Thực thi
            _chessBoard.Play(move);
            MovePiece(_map[move.Piece], move);

            // Dọn dẹp highlight nếu có
            _legalMoves.Clear();
            _tileManager.updateLegalMoves(_legalMoves);
        }
    }

    // ================= MỚI THÊM: XỬ LÝ ĐẦU HÀNG & MẤT KẾT NỐI =================

    // Hàm 1: Gọi khi đối thủ bị rớt mạng hoặc tự ý bấm thoát
    public void OpponentDisconnected()
    {
        if (playing)
        {
            playing = false;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false); // Tắt menu pause nếu đang mở

            Debug.Log("Đối thủ đã thoát. Bạn thắng!");

            // Ép gọi hàm Thắng cờ cho màu của mình (myColor)
            EndGameUI.EndGameWin(myColor);
        }
    }

    // Hàm 2: Dùng để gán vào nút "Quit/Đầu hàng" trong PauseMenu
    public void SurrenderAndLeaveGame()
    {
        // 1. Rời khỏi phòng Photon để báo cho máy kia biết mình đã thoát
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // 2. Load lại toàn bộ Scene để dọn dẹp bàn cờ cũ, quay về Menu chính
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}