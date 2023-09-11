using System;
using System.Linq;
using ChessChallenge.API;
using static System.Formats.Asn1.AsnWriter;

public class IterativeDeepeningBot : IChessBot
{
    private bool isWhite;
    private const int TRANSPOSITION_TABLE_SIZE = 1 << 20;
    private TranspositionTableResult[] transpositionTable = new TranspositionTableResult[TRANSPOSITION_TABLE_SIZE];
    private Move bestMove;
    private int startingPly;
    private int currentPly;
    private Board TheBoard;
    private Move[] BestMovesByPly;
    private Timer _timer;
    private bool CancelSearch = false;

    public Move Think(Board board, Timer timer)
    {
        CancelSearch = false;
        BestMovesByPly = new Move[10];
        TheBoard = board;
        _timer = timer;
        isWhite = board.IsWhiteToMove;
        startingPly = TheBoard.PlyCount;

        Move bestMove = IterativeDeepening();

        Console.WriteLine($"{board.CreateDiagram()}");
        Console.WriteLine($"{board.GetFenString()}");

        return bestMove;
    }

    private Move IterativeDeepening()
    {
        bestMove = Move.NullMove;
        int depth = 1;
        while (!CancelSearch)
        {
            if (CancelSearch)
            {
                break;
            }

            bestMove = GetBestMove(depth++);
        }
        Console.WriteLine($"******************** Selected best move: {bestMove}, piece type: {bestMove.MovePieceType}");
        return bestMove;
    }

    bool MoveTimedOut() { return _timer.MillisecondsElapsedThisTurn > 3000; }
    int OrderSearch(Move move)
    {
        //move.IsCapture || move.IsCastles || move.IsEnPassant || move.IsPromotion;
        //return 1;

        int value = 0;

        if (BestMovesByPly[currentPly] != null && move == BestMovesByPly[currentPly])
        {
            value =+ 100;
        }

        if(move.IsCapture || move.IsCastles || move.IsEnPassant || move.IsPromotion)
        {
            value = +1;
        }

        return value;

    }
    Move[] GetLegalMoves() { return TheBoard.GetLegalMoves().OrderByDescending(OrderSearch).ToArray(); }

    private Move GetBestMove(int depth)
    {
        bestMove = Move.NullMove;
        int bestScore = isWhite ? int.MinValue : int.MaxValue;
        Console.WriteLine($"Depth Test: {depth}");
        foreach (Move move in GetLegalMoves())
        {
            TheBoard.MakeMove(move);
            int score = MiniMax(depth, int.MinValue, int.MaxValue, !isWhite, 0);
            TheBoard.UndoMove(move);

            if (CancelSearch)
            {
                break;
            }

            if (isWhite && score > bestScore)
            {
                Console.WriteLine($"******************** Updated best move for white.  Move: {move}, score: {score}, depth: {depth}");
                bestScore = score;
                bestMove = move;
            }
            else if (!isWhite && score < bestScore)
            {
                Console.WriteLine($"******************** Updated best move for black.  Move: {move}, score: {score}, depth: {depth}");
                bestScore = score;
                bestMove = move;
            }
        }
        
        Console.WriteLine($"******************** Selected best move: {bestMove}, depth: {depth}, piece type: {bestMove.MovePieceType}");
        return bestMove;
    }

    bool IsGameOver()
    {
        return TheBoard.IsDraw() || TheBoard.IsInCheckmate();
    }

    int MiniMax(int depth, int alpha, int beta, bool maximizing, int plyFromStart)
    {
        currentPly = plyFromStart;

        var boardKey = TheBoard.ZobristKey;

        if (depth == 0 || IsGameOver())
        {
            return EvaluateBoard(TheBoard);

        }

        var ttIndex = boardKey % TRANSPOSITION_TABLE_SIZE;
        var cachedResult = transpositionTable[ttIndex];

        if(cachedResult != null)
        {
            if (cachedResult.Key == boardKey)
            {
                if (cachedResult.Depth >= depth)
                {
                    //Console.WriteLine("Cache Hit!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    return cachedResult.Eval;
                }
                else
                {
                    //Console.WriteLine("Not deep enough!");
                }
            }
            else
            {
                //Console.WriteLine("Collision boom!");
            }
        }
        else
        {
            //Console.WriteLine("Cache Miss!");
        }

        var legalMoves = TheBoard.GetLegalMoves().OrderByDescending(OrderSearch);

        if (maximizing)
        {
            int maxEval = int.MinValue;
            Move currentBestMove = Move.NullMove;
            foreach (Move move in legalMoves)
            {
                TheBoard.MakeMove(move);
                int eval = MiniMax(calculateNewDepth(depth, move), alpha, beta, false, plyFromStart + 1);
                TheBoard.UndoMove(move);


                if (MoveTimedOut())
                {
                    CancelSearch = true;
                    //Console.WriteLine("Breaking out after " + timer.MillisecondsElapsedThisTurn);
                    //return maximizing ? int.MinValue : int.MaxValue;
                    return int.MinValue;
                }

                if (maxEval < eval)
                {
                    maxEval = eval;
                    currentBestMove = move;
                    BestMovesByPly[plyFromStart] = move;
                }
                //maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }           

            transpositionTable[ttIndex] = new TranspositionTableResult(boardKey, depth, maxEval, true);

            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            Move currentBestMove = Move.NullMove;
            foreach (Move move in legalMoves)
            {
                TheBoard.MakeMove(move);
                int eval = MiniMax(calculateNewDepth(depth, move), alpha, beta, true, plyFromStart + 1);
                TheBoard.UndoMove(move);

                if (MoveTimedOut())
                {
                    CancelSearch = true;
                    //Console.WriteLine("Breaking out after " + timer.MillisecondsElapsedThisTurn);
                    //return maximizing ? int.MinValue : int.MaxValue;
                    return int.MaxValue;
                }


                if (minEval > eval)
                {
                    minEval = eval;
                    BestMovesByPly[plyFromStart] = move;
                }

                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            transpositionTable[ttIndex] = new TranspositionTableResult(boardKey, depth, minEval, false);
            return minEval;
        }
    }

    static int calculateNewDepth(int currentDepth, Move move)
    {
        if(currentDepth == 0 && move.IsCapture)
        {
            return currentDepth;
        }
        else
        {
            return currentDepth - 1;
        }
    }

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    static int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };


    public int EvaluateBoard(Board board)
    {
        var score = 0;

        score += EvaluateBoardRawMaterial(board);
        score += GetPSVEvaluation(board);

        return score;
    }

    static int EvaluateBoardRawMaterial(Board board)
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


            score += pieceList.IsWhitePieceList ? piecesValue : -piecesValue;
        }

        return score;
    }

    static int GetPSVEvaluation(ChessChallenge.API.Board board)
    {
        var evaluation = 0;

        foreach (ChessChallenge.API.PieceType pieceType in Enum.GetValues(typeof(ChessChallenge.API.PieceType)))
        {
            bool[] colors = { true, false };
            foreach (var isWhiteColor in colors)
            {
                var pieceBitBoard = board.GetPieceBitboard(pieceType, isWhiteColor);
                while (pieceBitBoard > 0)
                {
                    var index = ChessChallenge.API.BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitBoard);
                    var lookupTable = board.PlyCount < 30 ? mg_tables : eg_tables;

                    int[] pieceSquareTable = mg_tables[(int) pieceType - 1];
                    var index_row_number = index / 8;
                    var index_column_number = index % 8;

                    var lookup_row_number = isWhiteColor ? 7 - index_row_number : index_row_number;
                    var lookup_column_number = index_column_number;

                    var lookup_index = lookup_row_number * 8 + lookup_column_number;
                    evaluation += (isWhiteColor ? 1 : -1) * pieceSquareTable[lookup_index];
                }
            }
        }

        return evaluation;
    }


    public class TranspositionTableResult
    {
        public ulong Key { get; }
        public int Depth { get; }
        public int Eval { get; }
        public bool Maximizing { get; }

        public TranspositionTableResult(ulong key, int depth, int eval, bool maximizing)
        {
            Key = key;
            Depth = depth;
            Eval = eval;
            Maximizing = maximizing;
        }
    }

    static int[] mg_pawn_table = {
          0,   0,   0,   0,   0,   0,  0,   0,
         98, 134,  61,  95,  68, 126, 34, -11,
         -6,   7,  26,  31,  65,  56, 25, -20,
        -14,  13,   6,  21,  23,  12, 17, -23,
        -27,  -2,  -5,  12,  17,   6, 10, -25,
        -26,  -4,  -4, -10,   3,   3, 33, -12,
        -35,  -1, -20, -23, -15,  24, 38, -22,
          0,   0,   0,   0,   0,   0,  0,   0,
    };

    static int[] eg_pawn_table = {
          0,   0,   0,   0,   0,   0,   0,   0,
        178, 173, 158, 134, 147, 132, 165, 187,
         94, 100,  85,  67,  56,  53,  82,  84,
         32,  24,  13,   5,  -2,   4,  17,  17,
         13,   9,  -3,  -7,  -7,  -8,   3,  -1,
          4,   7,  -6,   1,   0,  -5,  -1,  -8,
         13,   8,   8,  10,  13,   0,   2,  -7,
          0,   0,   0,   0,   0,   0,   0,   0,
    };

    static int[] mg_knight_table = {
        -167, -89, -34, -49,  61, -97, -15, -107,
         -73, -41,  72,  36,  23,  62,   7,  -17,
         -47,  60,  37,  65,  84, 129,  73,   44,
          -9,  17,  19,  53,  37,  69,  18,   22,
         -13,   4,  16,  13,  28,  19,  21,   -8,
         -23,  -9,  12,  10,  19,  17,  25,  -16,
         -29, -53, -12,  -3,  -1,  18, -14,  -19,
        -105, -21, -58, -33, -17, -28, -19,  -23,
    };

    static int[] eg_knight_table = {
        -58, -38, -13, -28, -31, -27, -63, -99,
        -25,  -8, -25,  -2,  -9, -25, -24, -52,
        -24, -20,  10,   9,  -1,  -9, -19, -41,
        -17,   3,  22,  22,  22,  11,   8, -18,
        -18,  -6,  16,  25,  16,  17,   4, -18,
        -23,  -3,  -1,  15,  10,  -3, -20, -22,
        -42, -20, -10,  -5,  -2, -20, -23, -44,
        -29, -51, -23, -15, -22, -18, -50, -64,
    };

    static int[] mg_bishop_table = {
        -29,   4, -82, -37, -25, -42,   7,  -8,
        -26,  16, -18, -13,  30,  59,  18, -47,
        -16,  37,  43,  40,  35,  50,  37,  -2,
         -4,   5,  19,  50,  37,  37,   7,  -2,
         -6,  13,  13,  26,  34,  12,  10,   4,
          0,  15,  15,  15,  14,  27,  18,  10,
          4,  15,  16,   0,   7,  21,  33,   1,
        -33,  -3, -14, -21, -13, -12, -39, -21,
    };

    static int[] eg_bishop_table = {
        -14, -21, -11,  -8, -7,  -9, -17, -24,
         -8,  -4,   7, -12, -3, -13,  -4, -14,
          2,  -8,   0,  -1, -2,   6,   0,   4,
         -3,   9,  12,   9, 14,  10,   3,   2,
         -6,   3,  13,  19,  7,  10,  -3,  -9,
        -12,  -3,   8,  10, 13,   3,  -7, -15,
        -14, -18,  -7,  -1,  4,  -9, -15, -27,
        -23,  -9, -23,  -5, -9, -16,  -5, -17,
    };

    static int[] mg_rook_table = {
         32,  42,  32,  51, 63,  9,  31,  43,
         27,  32,  58,  62, 80, 67,  26,  44,
         -5,  19,  26,  36, 17, 45,  61,  16,
        -24, -11,   7,  26, 24, 35,  -8, -20,
        -36, -26, -12,  -1,  9, -7,   6, -23,
        -45, -25, -16, -17,  3,  0,  -5, -33,
        -44, -16, -20,  -9, -1, 11,  -6, -71,
        -19, -13,   1,  17, 16,  7, -37, -26,
    };

    static int[] eg_rook_table = {
        13, 10, 18, 15, 12,  12,   8,   5,
        11, 13, 13, 11, -3,   3,   8,   3,
         7,  7,  7,  5,  4,  -3,  -5,  -3,
         4,  3, 13,  1,  2,   1,  -1,   2,
         3,  5,  8,  4, -5,  -6,  -8, -11,
        -4,  0, -5, -1, -7, -12,  -8, -16,
        -6, -6,  0,  2, -9,  -9, -11,  -3,
        -9,  2,  3, -1, -5, -13,   4, -20,
    };

    static int[] mg_queen_table = {
        -28,   0,  29,  12,  59,  44,  43,  45,
        -24, -39,  -5,   1, -16,  57,  28,  54,
        -13, -17,   7,   8,  29,  56,  47,  57,
        -27, -27, -16, -16,  -1,  17,  -2,   1,
         -9, -26,  -9, -10,  -2,  -4,   3,  -3,
        -14,   2, -11,  -2,  -5,   2,  14,   5,
        -35,  -8,  11,   2,   8,  15,  -3,   1,
         -1, -18,  -9,  10, -15, -25, -31, -50,
    };


    static int[] eg_queen_table = {
         -9,  22,  22,  27,  27,  19,  10,  20,
        -17,  20,  32,  41,  58,  25,  30,   0,
        -20,   6,   9,  49,  47,  35,  19,   9,
          3,  22,  24,  45,  57,  40,  57,  36,
        -18,  28,  19,  47,  31,  34,  39,  23,
        -16, -27,  15,   6,   9,  17,  10,   5,
        -22, -23, -30, -16, -16, -23, -36, -32,
        -33, -28, -22, -43,  -5, -32, -20, -41,
    };

    static int[] mg_king_table = {
        -65,  23,  16, -15, -56, -34,   2,  13,
         29,  -1, -20,  -7,  -8,  -4, -38, -29,
         -9,  24,   2, -16, -20,   6,  22, -22,
        -17, -20, -12, -27, -30, -25, -14, -36,
        -49,  -1, -27, -39, -46, -44, -33, -51,
        -14, -14, -22, -46, -44, -30, -15, -27,
          1,   7,  -8, -64, -43, -16,   9,   8,
        -15,  36,  12, -54,   8, -28,  24,  14,
    };


    static int[] eg_king_table = {
        -74, -35, -18, -18, -11,  15,   4, -17,
        -12,  17,  14,  17,  17,  38,  23,  11,
         10,  17,  23,  15,  20,  45,  44,  13,
         -8,  22,  24,  27,  26,  33,  26,   3,
        -18,  -4,  21,  24,  27,  23,   9, -11,
        -19,  -3,  11,  21,  23,  16,   7,  -9,
        -27, -11,   4,  13,  14,   4,  -5, -17,
        -53, -34, -21, -11, -28, -14, -24, -43
    };

    static int[][] mg_tables = {
        mg_pawn_table,
        mg_knight_table,
        mg_bishop_table,
        mg_rook_table,
        mg_queen_table,
        mg_king_table
    };

    static int[][] eg_tables =
    {
        eg_pawn_table,
        eg_knight_table,
        eg_bishop_table,
        eg_rook_table,
        eg_queen_table,
        eg_king_table
    };

}