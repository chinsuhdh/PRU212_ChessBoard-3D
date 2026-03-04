using ChessModel;

namespace Player
{
    public abstract class Player
    {
        protected readonly ChessColor Color;// Màu quân của người chơi này

        // Chức năng: Khởi tạo cơ bản, gán màu quân.
        protected Player(ChessColor color)
        {
            Color = color;
        }

        // Chức năng: Phương thức trừu tượng (Abstract).
        // Bắt buộc các lớp con (MinmaxPlayer, RandomPlayer) phải tự viết nội dung cho hàm này.
        // Mục đích: Trả về một nước đi (Move) mà người chơi muốn thực hiện.
        public abstract Move GetDesiredMove();
    }
}