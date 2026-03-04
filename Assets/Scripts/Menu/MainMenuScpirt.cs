using System;
using System.Collections;
using System.Collections.Generic;
using ChessModel;
using Player;
using UnityEngine;
using Photon.Pun; // THÊM THƯ VIỆN NÀY ĐỂ XỬ LÝ THOÁT PHÒNG

public class MainMenuScpirt : MonoBehaviour
{
    // Các tham chiếu đến các Panel UI (kéo thả trong Inspector)
    public GameObject firstMenu;      // Màn hình chính (Start/Exit)
    public GameObject modeSelection;  // Màn hình chọn chế độ (PvP, PvE...)
    public GameObject colorSelection; // Màn hình chọn phe (Trắng/Đen)
    public GameObject aiDifficulty;   // Màn hình chọn độ khó AI
    public GameObject pauseMenu;      // Menu tạm dừng (ESC)

    // --- [NEW] Tham chiếu đến bảng hướng dẫn (Scorched Earth Tutorial Panel) ---
    public GameObject scorchedTutorialPanel;
    // --------------------------------------------------------------------------

    // Tham chiếu đến BoardManager để bắt đầu game
    public BoardManager boardManager;

    // Biến lưu trạng thái cài đặt game trước khi bắt đầu
    private GameMode _gameMode;
    private Dictionary<ChessColor, Player.Player> _players; // Danh sách người chơi (Ai cầm quân nào)
    private ChessColor _playerColor; // Màu quân mà người chơi đang chọn

    // Chức năng: Khởi tạo mặc định khi bật game.
    // Ẩn hết các menu con, chỉ hiện menu chính.
    void Start()
    {
        firstMenu.SetActive(true);
        modeSelection.SetActive(false);
        colorSelection.SetActive(false);
        aiDifficulty.SetActive(false);
        pauseMenu.SetActive(false);

        // --- [NEW] Đảm bảo bảng hướng dẫn tắt khi bắt đầu ---
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);
        // ---------------------------------------------------------

        _players = new Dictionary<ChessColor, Player.Player>
        {
            [ChessColor.Black] = null,
            [ChessColor.White] = null
        };

        _playerColor = ChessColor.White;
    }

    // Chức năng: Kiểm tra nút bấm mỗi khung hình.
    // Dùng để bật/tắt Menu Tạm Dừng (Pause Menu) bằng nút ESC.
    private void Update()
    {
        // Nếu đang chơi mà bấm ESC -> Tạm dừng
        if (boardManager.playing && Input.GetKeyDown(KeyCode.Escape))
        {
            boardManager.playing = false; // Dừng logic bàn cờ
            pauseMenu.SetActive(true); // Hiện bảng Pause
        }
        // Nếu đang Pause mà bấm ESC -> Chơi tiếp
        else if (pauseMenu.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            boardManager.playing = true;
            pauseMenu.SetActive(false);
        }
    }

    // Chức năng: Nút "Resume" trong Pause Menu -> Tiếp tục chơi.
    public void Resume()
    {
        boardManager.playing = true;
        pauseMenu.SetActive(false);
    }

    // Chức năng: Nút "Restart" -> Chơi lại ván mới ngay lập tức.
    public void RestartGame()
    {
        // Nếu đang chơi Tiêu Thổ thì Restart vẫn giữ Tiêu Thổ, ngược lại thì thường
        // Logic này BoardManager đã tự xử lý trong hàm RestartGame của nó
        boardManager.RestartGame();
        pauseMenu.SetActive(false);
    }

    // ================= CÁCH FIX MỚI CHO NÚT BACK TO MENU =================

    public void BackToMenu()
    {
        // 1. Rời phòng mạng (Bắt buộc để ván sau không bị lỗi kẹt trong phòng cũ)
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // 2. Tắt menu Pause
        if (pauseMenu != null) pauseMenu.SetActive(false);

        // 3. Đổi Camera và Nhạc
        boardManager.menuCam.SetActive(true);
        boardManager.GetComponent<AudioSource>().Stop();

        // 4. ĐÁNH THỨC MAIN MENU: Bật lại chính object chứa script này để Coroutine được phép chạy
        gameObject.SetActive(true);

        // 5. Tạm thời ẩn các nút của FirstMenu đi để màn hình trống trong 2 giây camera bay
        firstMenu.SetActive(false);

        // Phát nhạc menu
        if (GetComponent<AudioSource>() != null) GetComponent<AudioSource>().Play();
        boardManager.whiteCam.SetActive(true);

        // 6. Bây giờ Coroutine đã có thể chạy an toàn!
        StartCoroutine(TravelToMenu());
    }

    // Coroutine: Hiệu ứng chờ 2 giây rồi mới quay về Menu chính (để camera bay về đẹp mắt).
    private IEnumerator TravelToMenu()
    {
        yield return new WaitForSeconds(2f);

        boardManager.RestartGame();
        boardManager.playing = false;

        // === [SỬA LỖI ĐÈ GIAO DIỆN Ở ĐÂY] ===
        firstMenu.SetActive(true);               // CHỈ bật FirstMenu
        modeSelection.SetActive(false);          // ÉP TẮT bảng chọn Mode
        colorSelection.SetActive(false);         // ÉP TẮT bảng chọn màu
        aiDifficulty.SetActive(false);           // ÉP TẮT bảng độ khó
        if (scorchedTutorialPanel != null)
            scorchedTutorialPanel.SetActive(false); // Tắt luôn bảng luật Tiêu Thổ
                                                    // =====================================

        _players = new Dictionary<ChessColor, Player.Player>
        {
            [ChessColor.Black] = null,
            [ChessColor.White] = null
        };

        _playerColor = ChessColor.White;
    }
    // =====================================================================

    // --- [NEW] Hàm này KHÔNG bắt đầu game ngay nữa, mà chỉ hiện bảng Hướng dẫn ---
    // Gắn hàm này vào nút "Scorched Earth Mode" ở Menu chọn chế độ
    public void OpenScorchedEarthTutorial()
    {
        if (scorchedTutorialPanel != null)
        {
            scorchedTutorialPanel.SetActive(true); // Hiện bảng hướng dẫn lên
        }
        else
        {
            // Nếu quên gắn Panel trong Unity Editor thì chạy luôn game (để đỡ lỗi)
            PlayScorchedEarthModeNow();
        }
    }

    // --- [NEW] Hàm này mới thực sự bắt đầu game ---
    // Gắn hàm này vào nút "ĐÃ HIỂU - CHIẾN THÔI" trong bảng hướng dẫn
    public void PlayScorchedEarthModeNow()
    {
        // 1. Ẩn bảng hướng dẫn
        if (scorchedTutorialPanel != null) scorchedTutorialPanel.SetActive(false);

        // 2. Ẩn UI Menu chính
        firstMenu.SetActive(false);
        modeSelection.SetActive(false);

        // 3. Tắt nhạc Menu
        GetComponent<AudioSource>().Stop();

        // 4. Gọi hàm logic bắt đầu game Tiêu Thổ bên BoardManager
        boardManager.StartScorchedEarthGame();
    }
    // -------------------------------------------------------

    public void SelectColorWhite()
    {
        SelectColor(ChessColor.Black);
    }

    public void SelectColorBlack()
    {
        SelectColor(ChessColor.White);
    }

    private void SelectColor(ChessColor color)
    {
        _playerColor = color;
        colorSelection.SetActive(false);
        ShowAiDifficulty();
    }

    private void ShowColorSelection()
    {
        if (_gameMode == GameMode.Pvai)
        {
            colorSelection.SetActive(true);
        }
        else
        {
            ShowAiDifficulty();
            _playerColor = ChessColor.White;
        }
    }

    // --- [MODIFIED] Sửa hàm StartGame để đảm bảo tắt chế độ Tiêu Thổ khi chơi thường ---
    private void StartGame()
    {
        GetComponent<AudioSource>().Stop();

        // Quan trọng: Tắt cờ hiệu Tiêu Thổ để không bị vỡ ô khi chơi thường
        boardManager.IsScorchedEarthMode = false;

        // Reset luật chơi (nếu có script ScorchedEarthRules)
        var rules = boardManager.GetComponent<ScorchedEarthRules>();
        if (rules != null) rules.ResetRules();

        // Reset visual các ô về màu gốc
        boardManager.InitialisePlay(_players);
    }
    // -----------------------------------------------------------------------------------

    private void ShowAiDifficulty()
    {
        if (_gameMode == GameMode.Pvai || _gameMode == GameMode.Aivai)
        {
            aiDifficulty.SetActive(true);
        }
        else
        {
            StartGame();
        }
    }

    public void SelectDifficultyRandom()
    {
        AssignDifficulty(Difficulty.Random);
    }

    public void SelectDifficultyEasy()
    {
        AssignDifficulty(Difficulty.Easy);
    }

    public void SelectDifficultyMedium()
    {
        AssignDifficulty(Difficulty.Medium);
    }

    public void SelectDifficultyHard()
    {
        AssignDifficulty(Difficulty.Hard);
    }

    private void AssignDifficulty(Difficulty difficulty)
    {
        Player.Player player = difficulty == Difficulty.Random ? new RandomPlayer(_playerColor) : (Player.Player)new MinmaxPlayer(_playerColor, (int)difficulty);

        if (_playerColor == ChessColor.White)
        {
            _players[ChessColor.White] = player;
        }
        else
        {
            _players[ChessColor.Black] = player;
        }

        if (_gameMode == GameMode.Aivai && _playerColor == ChessColor.White)
        {
            _playerColor = ChessColor.Black;
        }
        else
        {
            StartGame();
            aiDifficulty.SetActive(false);
        }
    }

    private void SelectGameMode(GameMode gameMode)
    {
        modeSelection.SetActive(false);
        _gameMode = gameMode;
        ShowColorSelection();
    }

    public void SelectPvpGameMode()
    {
        SelectGameMode(GameMode.Pvp);
    }

    public void SelectPvAiGameMode()
    {
        SelectGameMode(GameMode.Pvai);
    }

    public void SelectAivaiGameMode()
    {
        SelectGameMode(GameMode.Aivai);
    }

    public void ToMainMenuFromModeSelection()
    {
        firstMenu.SetActive(true);
        modeSelection.SetActive(false);
    }

    public void ToGameModeSelectionFromMainMenu()
    {
        firstMenu.SetActive(false);
        modeSelection.SetActive(true);
    }

    public void Exit()
    {
        Application.Quit();
    }

    // Enum định nghĩa các chế độ chơi
    public enum GameMode
    {
        Pvp,  // Player vs Player
        Pvai, // Player vs AI
        Aivai // AI vs AI
    }

    // Enum định nghĩa độ khó (số càng cao Minimax duyệt càng sâu)
    private enum Difficulty
    {
        Random,
        Easy = 1,
        Medium = 2,
        Hard = 3
    }
}