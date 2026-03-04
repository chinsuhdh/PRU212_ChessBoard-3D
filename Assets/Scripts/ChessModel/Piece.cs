using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChessModel
{
    // Enum định nghĩa màu quân và loại quân
    public enum ChessColor
    {
        None,
        White,
        Black
    }

    public enum ChessType
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    public static class AddOnType
    {
        public static int Value(this ChessType type)
        {
            return type switch
            {
                ChessType.Pawn => 1,
                ChessType.Knight => 3,
                ChessType.Bishop => 3,
                ChessType.Rook => 5,
                ChessType.Queen => 9,
                ChessType.King => -1,
                ChessType.None => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static ChessColor Reverse(this ChessColor color)
        {
            return color switch
            {
                ChessColor.White => ChessColor.Black,
                ChessColor.Black => ChessColor.White,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
    }

    public class Piece
    {
        public ChessType Type { get; set; }
        public ChessColor Color { get; }
        public int Position { get; private set; }
        public int Value { get; }

        private static ChessBoard Board => ChessBoard.Instance;

        public Piece(ChessColor color, int position, ChessType type)
        {
            Color = color;
            Position = position;
            Type = type;
            Value = type.Value();
        }

        // Chức năng: Lấy danh sách nước đi HỢP LỆ
        public List<Move> GetLegalMoves()
        {
            var list = GetPseudoMoves();
            list = list.Where(move =>
            {
                Board.Play(move, true);
                var isCheck = Board.IsCheck(Color);
                Board.Unplay();
                return !isCheck;
            }).ToList();

            if (Board.IsCheck(Color))
                list.RemoveAll(move =>
                {
                    if (Color == ChessColor.White)
                        return move.StartPosition == 4 && (move.EndPosition == 2 || move.EndPosition == 6);
                    return move.StartPosition == 60 && (move.EndPosition == 58 || move.EndPosition == 62);
                });
            if (Board.IsThreatening(61, Board.NextToPlay.Reverse()))
                list.RemoveAll(move =>
                    move.EndPosition == 62 && move.StartPosition == 60 && move.Piece.Type == ChessType.King);
            if (Board.IsThreatening(59, Board.NextToPlay.Reverse()))
                list.RemoveAll(move =>
                    move.EndPosition == 58 && move.StartPosition == 60 && move.Piece.Type == ChessType.King);
            if (Board.IsThreatening(5, Board.NextToPlay.Reverse()))
                list.RemoveAll(move =>
                    move.EndPosition == 6 && move.StartPosition == 4 && move.Piece.Type == ChessType.King);
            if (Board.IsThreatening(3, Board.NextToPlay.Reverse()))
                list.RemoveAll(move =>
                    move.EndPosition == 2 && move.StartPosition == 4 && move.Piece.Type == ChessType.King);
            return list;
        }

        // Chức năng: Lấy danh sách nước đi THEO HÌNH HỌC
        public List<Move> GetPseudoMoves()
        {
            var legalPlacements = new List<int>();
            var legalMoves = new List<Move>();
            switch (Type)
            {
                case ChessType.Pawn:
                    switch (Color)
                    {
                        case ChessColor.Black:
                            // Đi thẳng 1 ô
                            if (Row > 0 && Board.IsEmpty((Row - 1) * 8 + Column))
                            {
                                legalPlacements.Add((Row - 1) * 8 + Column);
                                // Đi thẳng 2 ô từ vị trí đầu
                                if (Row == 6 && Board.IsEmpty((Row - 2) * 8 + Column))
                                    legalPlacements.Add((Row - 2) * 8 + Column);
                            }

                            // Ăn chéo phải (Chỉ ăn nếu có quân Trắng)
                            if (Row > 0 && Column != 7)
                            {
                                if (Board.Color((Row - 1) * 8 + Column + 1) == ChessColor.White)
                                    legalPlacements.Add((Row - 1) * 8 + Column + 1);
                            }

                            // Ăn chéo trái (Chỉ ăn nếu có quân Trắng)
                            if (Row > 0 && Column != 0)
                            {
                                if (Board.Color((Row - 1) * 8 + Column - 1) == ChessColor.White)
                                    legalPlacements.Add((Row - 1) * 8 + Column - 1);
                            }
                            break;

                        case ChessColor.White:
                            // Đi thẳng 1 ô
                            if (Row < 7 && Board.IsEmpty((Row + 1) * 8 + Column))
                            {
                                legalPlacements.Add((Row + 1) * 8 + Column);
                                // Đi thẳng 2 ô từ vị trí đầu
                                if (Row == 1 && Board.IsEmpty((Row + 2) * 8 + Column))
                                    legalPlacements.Add((Row + 2) * 8 + Column);
                            }

                            // Ăn chéo phải (Chỉ ăn nếu có quân Đen)
                            if (Row < 7 && Column != 7)
                            {
                                if (Board.Color((Row + 1) * 8 + Column + 1) == ChessColor.Black)
                                    legalPlacements.Add((Row + 1) * 8 + Column + 1);
                            }

                            // Ăn chéo trái (Chỉ ăn nếu có quân Đen)
                            if (Row < 7 && Column != 0)
                            {
                                if (Board.Color((Row + 1) * 8 + Column - 1) == ChessColor.Black)
                                    legalPlacements.Add((Row + 1) * 8 + Column - 1);
                            }
                            break;
                    }
                    break;

                case ChessType.Knight:
                    legalPlacements.AddRange(Enumerable.Range(0, 64).Where(pl =>
                        Math.Abs(pl / 8 - Row) == 1 && Math.Abs(pl % 8 - Column) == 2 ||
                        Math.Abs(pl / 8 - Row) == 2 && Math.Abs(pl % 8 - Column) == 1));

                    legalPlacements = legalPlacements.Where(p => Board.Color(p) != Color).ToList();
                    break;
                case ChessType.Bishop:
                    CheckDiagonals();
                    break;
                case ChessType.Rook:
                    CheckLines();
                    break;
                case ChessType.Queen:
                    CheckDiagonals();
                    CheckLines();
                    break;
                case ChessType.King:
                    if (Column != 7) Check(Row * 8 + Column + 1);
                    if (Column != 0) Check(Row * 8 + Column - 1);
                    if (Row != 7) Check((Row + 1) * 8 + Column);
                    if (Row != 0) Check((Row - 1) * 8 + Column);

                    if (Column != 7 && Row != 7) Check((Row + 1) * 8 + Column + 1);
                    if (Column != 0 && Row != 7) Check((Row + 1) * 8 + Column - 1);
                    if (Column != 0 && Row != 0) Check((Row - 1) * 8 + Column - 1);
                    if (Column != 7 && Row != 0) Check((Row - 1) * 8 + Column + 1);

                    switch (Color)
                    {
                        case ChessColor.Black:
                            if (Board.BlackLeftCastle && Board.IsEmpty(57)
                                                      && Board.IsEmpty(58)
                                                      && Board.IsEmpty(59))
                                legalPlacements.Add(58);

                            if (Board.BlackRightCastle && Board.IsEmpty(61)
                                                       && Board.IsEmpty(62))
                                legalPlacements.Add(62);
                            break;
                        case ChessColor.White:
                            if (Board.WhiteLeftCastle && Board.IsEmpty(1)
                                                      && Board.IsEmpty(2)
                                                      && Board.IsEmpty(3))
                                legalPlacements.Add(2);

                            if (Board.WhiteRightCastle && Board.IsEmpty(5)
                                                       && Board.IsEmpty(6))
                                legalPlacements.Add(6);
                            break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // --- [SỬA LỖI TẠI ĐÂY] ---
            // Hàm CheckDiagonals và CheckLines giờ sẽ kiểm tra IsTileBlocked
            // Nếu gặp hố (IsTileBlocked == true), vòng lặp sẽ BREAK (dừng ngay),
            // khiến quân cờ không thể đi xuyên qua hố.

            void CheckDiagonals()
            {
                var tileColor = Board.TileColor(Position);
                for (var i = Position + 9; i < Board.Board.Length; i += 9)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Board.TileColor(i) != tileColor || Check(i)) break;
                }

                for (var i = Position - 9; i >= 0; i -= 9)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Board.TileColor(i) != tileColor || Check(i)) break;
                }

                for (var i = Position + 7; i < 64; i += 7)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Board.TileColor(i) != tileColor || Check(i)) break;
                }

                for (var i = Position - 7; i >= 0; i -= 7)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Board.TileColor(i) != tileColor || Check(i)) break;
                }
            }

            void CheckLines()
            {
                for (var i = Position + 1; i < Row * 8 + 8; i++)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Check(i)) break;
                }

                for (var i = Position - 1; i >= Row * 8; i--)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Check(i)) break;
                }

                for (var i = Position + 8; i < 64; i += 8)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Check(i)) break;
                }

                for (var i = Position - 8; i >= 0; i -= 8)
                {
                    if (Board.IsTileBlocked(i)) break; // Gặp hố -> Dừng
                    if (Check(i)) break;
                }
            }
            // ---------------------------------------------------------

            bool Check(int position)
            {
                // Vẫn cần kiểm tra đích đến có bị chặn không (cho các quân nhảy như Mã/Vua)
                if (Board.IsTileBlocked(position)) return true; // Coi như có vật cản

                if (Board.IsEmpty(position))
                {
                    legalPlacements.Add(position);
                    return false;
                }

                if (Board.Color(position) != Color)
                    legalPlacements.Add(position);
                return true;
            }

            legalMoves.AddRange(legalPlacements.Select(position =>
                new Move(Position, position, this, Board.GetPiece(position))));
            return legalMoves;
        }

        private int Row => Position / 8;
        private int Column => Position % 8;

        public void MoveTo(int newPosition)
        {
            Position = newPosition;
        }

        public override string ToString()
        {
            return Position + ", " + Type + ", " + Color;
        }
    }
}