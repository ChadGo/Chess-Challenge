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
            int score = MiniMax(board, 4, int.MinValue, int.MaxValue, !isWhite);
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
        var score = 0;

        score += EvaluateBoardRawMaterial(board);
        score += EvaluateKingPositions(board);

        return score;
    }

    int EvaluateBoardRawMaterial(Board board)
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

            //Console.WriteLine($"pieceType: {pieceType}, pieceValue: {pieceValue}, count: {pieceList.Count}, pieceValue: {piecesValue}, white: {pieceList.IsWhitePieceList}");

            score += pieceList.IsWhitePieceList ? piecesValue : -piecesValue;
        }


        //Console.WriteLine("EvaluateBoard: " + score);
        return score;
    }

    int GetMaterialEvaluation(Board board, bool white)
    {
        var allPieces = board.GetAllPieceLists();

        var score = 0;
        foreach (var pieceList in allPieces)
        {
            var pieceType = pieceList.TypeOfPieceInList;
            var pieceValue = pieceValues[(int)pieceType];
            var piecesValue = pieceValue * pieceList.Count;

            if (white && pieceList.IsWhitePieceList)
            {
                score += piecesValue;
            }
            else if (!white && !pieceList.IsWhitePieceList)
            {
                score -= piecesValue;
            }
        }

        return score;
    }

    int EvaluateKingPositions(Board board)
    {
        var score = 0;

        int[] earlyMidGameKingEncouragement = new int[]
        {
            -80, -70, -70, -70, -70, -70, -70, -80,
            -60, -60, -60, -60, -60, -60, -60, -60,
            -40, -50, -50, -60, -60, -50, -50, -40,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
             20,  20,  -5,  -5,  -5,  -5,  20,  20,
             20,  30,  10,   0,   0,  10,  30,  20
        };

        int[] lateGameKingEncouragement = new int[]
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
             -5,   0,   5,   5,   5,   5,   0,  -5,
            -10,  -5,  20,  30,  30,  20,  -5, -10,
            -15, -10,  35,  45,  45,  35, -10, -15,
            -20, -15,  30,  40,  40,  30, -15, -20,
            -25, -20,  20,  25,  25,  20, -20, -25,
            -30, -25,   0,   0,   0,   0, -25, -30,
            -50, -30, -30, -30, -30, -30, -30, -50
        };

        // Approximate the amount of material left on either side by the
        // plyCount.
        var plyCount = board.PlyCount;
        var earlyGamePlyRatio = 100/(100 + plyCount);
        var lateGamePlyRatio = 1/Math.Max(1, 100 - plyCount);

        var blackKingPosition = board.GetKingSquare(false).Index;
        var whiteKingPosition = board.GetKingSquare(true).Index;

        var whiteKingScore = earlyGamePlyRatio * earlyMidGameKingEncouragement[whiteKingPosition] +
            lateGamePlyRatio * lateGameKingEncouragement[whiteKingPosition];

        // TODO: Black indexes need massaging.  They are looked up from white's perspective currently.
        var blackKingScore = earlyGamePlyRatio * earlyMidGameKingEncouragement[blackKingPosition] +
            lateGamePlyRatio * lateGameKingEncouragement[blackKingPosition];

        score += whiteKingScore;
        //score -= blackKingScore;

        //Console.WriteLine($"King eval: white: {whiteKingScore}, black: {blackKingScore}");

        return score;
    }
}