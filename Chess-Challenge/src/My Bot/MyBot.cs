using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    private bool isWhite;

    public Move Think(Board board, Timer timer)
    {
        isWhite = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        var bestMove = GetBestMove(board, moves);

        return (bestMove == Move.NullMove) ? moves[0] : bestMove;
    }

    private Move GetBestMove(Board board, Move[] legalMoves)
    {
        Move bestMove = Move.NullMove;
        int bestScore = isWhite ? int.MinValue : int.MaxValue;

        foreach(Move move in legalMoves)
        {
            board.MakeMove(move);
            int score = MiniMax(board, 4, int.MinValue, int.MaxValue, false);
            board.UndoMove(move);

            if (isWhite && score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
            else if (!isWhite && score < bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    static bool IsGameOver(Board board)
    {
        return board.IsDraw() || board.IsInCheckmate();
    }

    int MiniMax(Board board, int depth, int alpha, int beta, bool maximizing)
    {
        if (depth == 0 || IsGameOver(board))
        {
            return EvaluateBoard(board);
        }

        var legalMoves = board.GetLegalMoves();

        if (maximizing)
        {
            int maxEval = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = MiniMax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = MiniMax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return minEval;
        }
    }

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    static int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };


    int EvaluateBoard(Board board)
    {
        /// Gets an array of all the piece lists. In order these are:
        /// Pawns(white), Knights (white), Bishops (white), Rooks (white), Queens (white), King (white),
        /// Pawns (black), Knights (black), Bishops (black), Rooks (black), Queens (black), King (black)
        var allPieces = board.GetAllPieceLists();

        var score = 0;
        foreach (var pieceList in allPieces)
        {
            var pieceType = pieceList.TypeOfPieceInList;
            var pieceValue = pieceValues[(int)pieceType];
            var piecesValue = pieceValue * pieceList.Count;

            score += (pieceList.IsWhitePieceList && isWhite ? piecesValue : -piecesValue); 
        }
        return score;
    }
}