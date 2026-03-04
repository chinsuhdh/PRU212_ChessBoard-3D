using System;
using System.Linq;
using ChessModel;
using Random = System.Random;

namespace Player
{
    public class MinmaxPlayer : Player
    {

        // ... Khai báo biến (độ sâu suy nghĩ, bàn cờ, biến random để AI bớt máy móc...) ...
        private readonly int _depth;
        private readonly ChessBoard _board;
        private Move _bestMove;
        private readonly Random _rand;

        // Chức năng: Khởi tạo AI.
        // color: Phe của AI (Trắng/Đen).
        // depth: Độ sâu thuật toán (Độ khó). EX: 1 là Dễ, 3 là Khó.
        public MinmaxPlayer(ChessColor color, int depth) : base(color)
        {
            _depth = depth;
            _board = ChessBoard.Instance;
            _rand = new Random();
        }

        // Chức năng: Hàm chính để "hỏi" AI nước đi tiếp theo.
        // Nó kích hoạt thuật toán Minimax chạy, sau khi chạy xong biến _bestMove sẽ chứa nước đi ngon nhất.
        public override Move GetDesiredMove()
        {
            Minimax(_depth, float.MinValue, float.MaxValue, Color);
            return _bestMove;
        }

        // Chức năng: Thuật toán đệ quy Minimax với Alpha-Beta Pruning.
        // 1. Giả lập đi thử một nước.
        // 2. Gọi đệ quy chính nó để xem đối thủ sẽ đi thế nào.
        // 3. Tính điểm bàn cờ (GetEvaluationScore).
        // 4. Cắt tỉa (Pruning) các nhánh không cần thiết để chạy nhanh hơn.
        // 5. Trả về điểm số tốt nhất tìm được.
        private float Minimax(int depth, float alpha, float beta, ChessColor color)
        {
            if (_board.IsCheckMate)
                return _board.NextToPlay == ChessColor.White ? -100 : 100;

            if (_board.IsDraw())
                return 0;

            if (depth == 0)
                return _board.GetEvaluationScore();


            float value;
            var moves = _board.GetAllLegalMoves(color).OrderBy(item => _rand.Next());
            if (color == ChessColor.White)
            {
                value = float.MinValue;
                foreach (var move in moves)
                {
                    _board.Play(move, true);
                    var newValue = Minimax(depth - 1, alpha, beta, color.Reverse());
                    _board.Unplay();
                    alpha = Math.Max(alpha, newValue);
                    if (newValue > value)
                    {
                        value = newValue;
                        if (depth == _depth) _bestMove = move;
                    }

                    if (alpha >= beta)
                        break;
                }
            }
            else
            {
                value = float.MaxValue;
                foreach (var move in moves)
                {
                    _board.Play(move, true);
                    var newValue = Minimax(depth - 1, alpha, beta, color.Reverse());
                    _board.Unplay();
                    if (newValue < value)
                    {
                        value = newValue;
                        if (depth == _depth) _bestMove = move;
                    }

                    beta = Math.Min(beta, newValue);

                    if (alpha >= beta)
                        break;
                }
            }

            return value;
        }
    }
}