using ChessModel;
using Random = System.Random;

namespace Player
{
    public class RandomPlayer : Player
    {
        // Chức năng: Khởi tạo bộ sinh số ngẫu nhiên.
        private readonly Random _rand;

        public RandomPlayer(ChessColor color) : base(color)
        {
            _rand = new Random();
        }

        // Chức năng: Chọn nước đi.
        // 1. Lấy danh sách tất cả nước đi hợp lệ hiện tại (GetAllLegalMoves).
        // 2. Random chọn 1 cái trong danh sách đó và trả về.
        public override Move GetDesiredMove()
        {
            var list = ChessBoard.Instance.GetAllLegalMoves(Color);
            return list[_rand.Next(list.Count)];
        }
    }
}