using System.Collections.Generic;
using UnityEngine;

public class ScorchedEarthRules : MonoBehaviour
{
    // Cấu hình
    [Header("Settings")]
    public int CooldownTurns = 5; // Số lượt để ô Đỏ hồi phục

    // Số lượt để ô Vàng sập thành ô Đỏ (3 = 1 lượt chạy thoát)
    public int UnstableDuration = 3;

    // Trạng thái ô: 0=Bình thường, 1=Sắp sập (Vàng), 2=Đã sập (Đỏ)
    public int[] TileStates = new int[64];

    // Bộ đếm thời gian hồi phục cho từng ô (Dành cho ô Đỏ)
    private int[] _cooldowns = new int[64];

    // Bộ đếm thời gian sập (Dành cho ô Vàng)
    private int[] _unstableCooldowns = new int[64];

    public void ResetRules()
    {
        TileStates = new int[64];
        _cooldowns = new int[64];
        _unstableCooldowns = new int[64];
    }

    // Giai đoạn 1: Khi có quân bị ăn -> Đánh dấu là Sắp sập (Vàng)
    public void MarkTileUnstable(int position)
    {
        if (TileStates[position] == 0) // Chỉ đánh dấu nếu nó đang bình thường
        {
            TileStates[position] = 1; // 1 = Unstable (Vàng)
            _unstableCooldowns[position] = UnstableDuration;
        }
    }

    // Giai đoạn 2: Kết thúc lượt -> Tính toán logic
    public List<int> ProcessTurnLogic()
    {
        List<int> justCollapsedTiles = new List<int>();

        for (int i = 0; i < 64; i++)
        {
            // LOGIC 1: Nếu đang là Đỏ (Hố chết) -> Đếm ngược hồi phục
            if (TileStates[i] == 2)
            {
                _cooldowns[i]--;
                if (_cooldowns[i] <= 0)
                {
                    TileStates[i] = 0; // Hồi phục về bình thường
                }
            }
            // LOGIC 2: Nếu đang là Vàng (Sắp sập) -> Đếm ngược để hóa Đỏ
            else if (TileStates[i] == 1)
            {
                _unstableCooldowns[i]--;

                // Chỉ khi đếm ngược về 0 thì mới cho sập
                if (_unstableCooldowns[i] <= 0)
                {
                    TileStates[i] = 2; // Chuyển sang Đỏ (Sập hẳn)
                    _cooldowns[i] = CooldownTurns; // Bắt đầu tính thời gian hồi phục
                    justCollapsedTiles.Add(i); // Báo cáo ô này vừa sập
                }
            }
        }

        return justCollapsedTiles;
    }

    // Hàm lấy số lượt còn lại để hiển thị (cho TileScript)
    public int GetRemainingTurns(int position)
    {
        if (TileStates[position] == 2) return _cooldowns[position]; // Đỏ
        if (TileStates[position] == 1) return _unstableCooldowns[position]; // Vàng
        return 0;
    }

    // Hàm kiểm tra xem ô có bị chặn đường không
    public bool IsPathBlocked(int position)
    {
        return TileStates[position] == 2; // Chỉ chặn ô Đỏ
    }
}