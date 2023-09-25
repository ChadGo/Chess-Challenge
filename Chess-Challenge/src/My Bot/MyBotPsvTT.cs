using System;
using System.Linq;
using System.Numerics;
using ChessChallenge.API;

public class MyBotPsvTT : IChessBot
{
    private bool isWhite;
    private const int TRANSPOSITION_TABLE_SIZE = 1 << 20;
    private TranspositionTableResult[] transpositionTable = new TranspositionTableResult[TRANSPOSITION_TABLE_SIZE];


    public MyBotPsvTT()
    {
        mg_tables = new short[][]
        {
            DecodeDecimalArray(mg_pawn_table_encoded),
            DecodeDecimalArray(mg_knight_table_encoded),
            DecodeDecimalArray(mg_bishop_table_encoded),
            DecodeDecimalArray(mg_rook_table_encoded),
            DecodeDecimalArray(mg_queen_table_encoded),
            DecodeDecimalArray(mg_king_table_encoded)
        };

        eg_tables = new short[][]
        {
            DecodeDecimalArray(eg_pawn_table_encoded),
            DecodeDecimalArray(eg_knight_table_encoded),
            DecodeDecimalArray(eg_bishop_table_encoded),
            DecodeDecimalArray(eg_rook_table_encoded),
            DecodeDecimalArray(eg_queen_table_encoded),
            DecodeDecimalArray(eg_king_table_encoded)
        };

    }

    private short[] DecodeDecimalArray(Decimal[] encodedDecimals)
    {

        BigInteger encodedValue = BigInteger.Parse(string.Concat(encodedDecimals.Select(d => d.ToString().Substring(2))));
        byte[] byteArray = encodedValue.ToByteArray();
        short[] result = new short[byteArray.Length];

        Buffer.BlockCopy(byteArray, 0, result, 0, byteArray.Length);

        return result;
    }


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

        foreach (Move move in legalMoves.OrderByDescending(move => move.IsCapture || move.IsCastles || move.IsEnPassant || move.IsPromotion))
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
        var boardKey = board.ZobristKey;

        if (depth == 0 || IsGameOver(board))
        {
            return EvaluateBoard(board);

        }

        var ttIndex = boardKey % TRANSPOSITION_TABLE_SIZE;
        var cachedResult = transpositionTable[ttIndex];

        if (cachedResult != null)
        {
            if (cachedResult.Key == boardKey)
            {
                if (cachedResult.Depth >= depth)
                {
                    //Console.WriteLine("Cache Hit!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    return cachedResult.Eval;
                }
            }
        }

        var legalMoves = board.GetLegalMoves().OrderByDescending(move => move.IsCapture || move.IsCastles || move.IsEnPassant || move.IsPromotion);

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

            transpositionTable[ttIndex] = new TranspositionTableResult(boardKey, depth, maxEval, true);

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
            transpositionTable[ttIndex] = new TranspositionTableResult(boardKey, depth, minEval, false);
            return minEval;
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

                    var pieceSquareTable = mg_tables[(int)pieceType - 1];
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

    //static int[] mg_pawn_table = {
    //      0,   0,   0,   0,   0,   0,  0,   0,
    //     98, 134,  61,  95,  68, 126, 34, -11,
    //     -6,   7,  26,  31,  65,  56, 25, -20,
    //    -14,  13,   6,  21,  23,  12, 17, -23,
    //    -27,  -2,  -5,  12,  17,   6, 10, -25,
    //    -26,  -4,  -4, -10,   3,   3, 33, -12,
    //    -35,  -1, -20, -23, -15,  24, 38, -22,
    //      0,   0,   0,   0,   0,   0,  0,   0,
    //};

    //static int[] eg_pawn_table = {
    //      0,   0,   0,   0,   0,   0,   0,   0,
    //    178, 173, 158, 134, 147, 132, 165, 187,
    //     94, 100,  85,  67,  56,  53,  82,  84,
    //     32,  24,  13,   5,  -2,   4,  17,  17,
    //     13,   9,  -3,  -7,  -7,  -8,   3,  -1,
    //      4,   7,  -6,   1,   0,  -5,  -1,  -8,
    //     13,   8,   8,  10,  13,   0,   2,  -7,
    //      0,   0,   0,   0,   0,   0,   0,   0,
    //};

    //static int[] mg_knight_table = {
    //    -167, -89, -34, -49,  61, -97, -15, -107,
    //     -73, -41,  72,  36,  23,  62,   7,  -17,
    //     -47,  60,  37,  65,  84, 129,  73,   44,
    //      -9,  17,  19,  53,  37,  69,  18,   22,
    //     -13,   4,  16,  13,  28,  19,  21,   -8,
    //     -23,  -9,  12,  10,  19,  17,  25,  -16,
    //     -29, -53, -12,  -3,  -1,  18, -14,  -19,
    //    -105, -21, -58, -33, -17, -28, -19,  -23,
    //};

    //static int[] eg_knight_table = {
    //    -58, -38, -13, -28, -31, -27, -63, -99,
    //    -25,  -8, -25,  -2,  -9, -25, -24, -52,
    //    -24, -20,  10,   9,  -1,  -9, -19, -41,
    //    -17,   3,  22,  22,  22,  11,   8, -18,
    //    -18,  -6,  16,  25,  16,  17,   4, -18,
    //    -23,  -3,  -1,  15,  10,  -3, -20, -22,
    //    -42, -20, -10,  -5,  -2, -20, -23, -44,
    //    -29, -51, -23, -15, -22, -18, -50, -64,
    //};

    //static int[] mg_bishop_table = {
    //    -29,   4, -82, -37, -25, -42,   7,  -8,
    //    -26,  16, -18, -13,  30,  59,  18, -47,
    //    -16,  37,  43,  40,  35,  50,  37,  -2,
    //     -4,   5,  19,  50,  37,  37,   7,  -2,
    //     -6,  13,  13,  26,  34,  12,  10,   4,
    //      0,  15,  15,  15,  14,  27,  18,  10,
    //      4,  15,  16,   0,   7,  21,  33,   1,
    //    -33,  -3, -14, -21, -13, -12, -39, -21,
    //};

    //static int[] eg_bishop_table = {
    //    -14, -21, -11,  -8, -7,  -9, -17, -24,
    //     -8,  -4,   7, -12, -3, -13,  -4, -14,
    //      2,  -8,   0,  -1, -2,   6,   0,   4,
    //     -3,   9,  12,   9, 14,  10,   3,   2,
    //     -6,   3,  13,  19,  7,  10,  -3,  -9,
    //    -12,  -3,   8,  10, 13,   3,  -7, -15,
    //    -14, -18,  -7,  -1,  4,  -9, -15, -27,
    //    -23,  -9, -23,  -5, -9, -16,  -5, -17,
    //};

    //static int[] mg_rook_table = {
    //     32,  42,  32,  51, 63,  9,  31,  43,
    //     27,  32,  58,  62, 80, 67,  26,  44,
    //     -5,  19,  26,  36, 17, 45,  61,  16,
    //    -24, -11,   7,  26, 24, 35,  -8, -20,
    //    -36, -26, -12,  -1,  9, -7,   6, -23,
    //    -45, -25, -16, -17,  3,  0,  -5, -33,
    //    -44, -16, -20,  -9, -1, 11,  -6, -71,
    //    -19, -13,   1,  17, 16,  7, -37, -26,
    //};

    //static int[] eg_rook_table = {
    //    13, 10, 18, 15, 12,  12,   8,   5,
    //    11, 13, 13, 11, -3,   3,   8,   3,
    //     7,  7,  7,  5,  4,  -3,  -5,  -3,
    //     4,  3, 13,  1,  2,   1,  -1,   2,
    //     3,  5,  8,  4, -5,  -6,  -8, -11,
    //    -4,  0, -5, -1, -7, -12,  -8, -16,
    //    -6, -6,  0,  2, -9,  -9, -11,  -3,
    //    -9,  2,  3, -1, -5, -13,   4, -20,
    //};

    //static int[] mg_queen_table = {
    //    -28,   0,  29,  12,  59,  44,  43,  45,
    //    -24, -39,  -5,   1, -16,  57,  28,  54,
    //    -13, -17,   7,   8,  29,  56,  47,  57,
    //    -27, -27, -16, -16,  -1,  17,  -2,   1,
    //     -9, -26,  -9, -10,  -2,  -4,   3,  -3,
    //    -14,   2, -11,  -2,  -5,   2,  14,   5,
    //    -35,  -8,  11,   2,   8,  15,  -3,   1,
    //     -1, -18,  -9,  10, -15, -25, -31, -50,
    //};


    //static int[] eg_queen_table = {
    //     -9,  22,  22,  27,  27,  19,  10,  20,
    //    -17,  20,  32,  41,  58,  25,  30,   0,
    //    -20,   6,   9,  49,  47,  35,  19,   9,
    //      3,  22,  24,  45,  57,  40,  57,  36,
    //    -18,  28,  19,  47,  31,  34,  39,  23,
    //    -16, -27,  15,   6,   9,  17,  10,   5,
    //    -22, -23, -30, -16, -16, -23, -36, -32,
    //    -33, -28, -22, -43,  -5, -32, -20, -41,
    //};

    //static int[] mg_king_table = {
    //    -65,  23,  16, -15, -56, -34,   2,  13,
    //     29,  -1, -20,  -7,  -8,  -4, -38, -29,
    //     -9,  24,   2, -16, -20,   6,  22, -22,
    //    -17, -20, -12, -27, -30, -25, -14, -36,
    //    -49,  -1, -27, -39, -46, -44, -33, -51,
    //    -14, -14, -22, -46, -44, -30, -15, -27,
    //      1,   7,  -8, -64, -43, -16,   9,   8,
    //    -15,  36,  12, -54,   8, -28,  24,  14,
    //};


    //static int[] eg_king_table = {
    //    -74, -35, -18, -18, -11,  15,   4, -17,
    //    -12,  17,  14,  17,  17,  38,  23,  11,
    //     10,  17,  23,  15,  20,  45,  44,  13,
    //     -8,  22,  24,  27,  26,  33,  26,   3,
    //    -18,  -4,  21,  24,  27,  23,   9, -11,
    //    -19,  -3,  11,  21,  23,  16,   7,  -9,
    //    -27, -11,   4,  13,  14,   4,  -5, -17,
    //    -53, -34, -21, -11, -28, -14, -24, -43
    //};

    static short[][] mg_tables;
    //    = {
    //    //mg_pawn_table,
    //    //mg_knight_table,
    //    //mg_bishop_table,
    //    //mg_rook_table,
    //    //mg_queen_table,
    //    //mg_king_table
    //};

    static short[][] eg_tables;
    //=
    //{
    //    //eg_pawn_table,
    //    //eg_knight_table,
    //    //eg_bishop_table,
    //    //eg_rook_table,
    //    //eg_queen_table,
    //    //eg_king_table
    //};

    decimal[] mg_pawn_table_encoded = new decimal[] {
0.5281171908439357134103180170m,
0.8865990595315857998869949161m,
0.5440025088417148940892015302m,
0.4118787500666823943524082405m,
0.2502113748847173429566213424m,
0.0476735579862764112555634553m,
0.7858095221046448991861193141m,
0.4621180003731826435819575879m,
0.3972210208217849961371255464m,
0.643361552491085824m,
};
    decimal[] eg_pawn_table_encoded = new decimal[] {
0.5282381034379772579889694058m,
0.4335008491291735896268963686m,
0.2142802865075899115088806166m,
0.6548317393886815190688051530m,
0.0525610042223940148810034202m,
0.8838443990982551343899520130m,
0.1459736794698964146687830258m,
0.5232593050030714743448875122m,
0.6435290372340532562679412119m,
0.865596827804893184m,
};
    decimal[] mg_knight_table_encoded = new decimal[] {
0.1797089653680531265533663309m,
0.9326409285531661933326648024m,
0.0577524102248420771824476000m,
0.4551268117682873650230085009m,
0.2419018885868915555738755748m,
0.8565235109508342702112883910m,
0.5579348371858218627161621516m,
0.4544761051144845296893559426m,
0.1901901962079875478044478259m,
0.7558556709004707258856348089m,
0.3763292789387618012440559599m,
0.3m,
};
    decimal[] eg_knight_table_encoded = new decimal[] {
0.1795964985271192885610056180m,
0.9537809427636716092233734275m,
0.5480847111803724485906782695m,
0.4932349675856033737963387290m,
0.1350303415452828345803900646m,
0.7275274002814592773237866906m,
0.3455376992045513116688819713m,
0.9655142747297677044945784533m,
0.5782677127491098835627856772m,
0.9475663190442105008175524893m,
0.9717047557027132810047383136m,
0.6m,
};
    decimal[] mg_bishop_table_encoded = new decimal[] {
0.1797144506550160396191461007m,
0.6972102430580282784860470891m,
0.7880708725821908590510949048m,
0.1394404656289939315802365735m,
0.7036018167404267063444612553m,
0.6997129678253797233493474523m,
0.7790836887143531349576334938m,
0.0412737982319544568731993067m,
0.3265629258986321674267097715m,
0.4415201466094741967390798949m,
0.4892500714647445172315311305m,
0.9m,
};
    decimal[] eg_bishop_table_encoded = new decimal[] {
0.1797254243262484412791613290m,
0.8772378029139784397761746152m,
0.5096228228929802868177038675m,
0.2809871146817401178977274641m,
0.2701585550877523947519930978m,
0.3703836629529173416382190180m,
0.6922136269677335114990733605m,
0.7556246171318405550981539637m,
0.2688394443089125847972340018m,
0.7621140632130186292445421813m,
0.0902112836954406641210569521m,
0.8m,
};
    decimal[] mg_rook_table_encoded = new decimal[] {
0.1797007353867113566787087424m,
0.5555216230865807461035520222m,
0.7555382144164664550357607232m,
0.4387730766070379042717543680m,
0.8144818089989236979673144118m,
0.8646601953691853357457624234m,
0.4454180868929200360149734181m,
0.8817254929184529062352129715m,
0.8245055411504240494381732384m,
0.1562777824570254328023024007m,
0.3394653350294158044052494748m,
0.8m,
};
    decimal[] eg_rook_table_encoded = new decimal[] {
0.1797144524548150146613957777m,
0.7456939115704549984045045135m,
0.5023156099979719802794647053m,
0.2118664504590457038418197800m,
0.6877574631453896673476697504m,
0.0814680464786324717567211660m,
0.3411280022749057033125552239m,
0.2504083865394393208226518830m,
0.6539787530349016590963293714m,
0.1209326848811123657542912131m,
0.8368017923113229100800186778m,
0.9m,
};
    decimal[] mg_queen_table_encoded = new decimal[] {
0.1796349021908566681540284307m,
0.8560595033025183384762616855m,
0.2535982979508435231385557219m,
0.0755636311190669649705810115m,
0.9735206704127913619440298840m,
0.0057239422798879657521166883m,
0.1703485832629585823320791258m,
0.4826665266915909858351343058m,
0.2071045193517026472398488918m,
0.2337806197722501416820939937m,
0.0763309219252584483495562442m,
0.0m,
};
    decimal[] eg_queen_table_encoded = new decimal[] {
0.1796595902095756238283495371m,
0.0267218161874918141119181813m,
0.8915576629857105914513962261m,
0.1280889477192231032585827903m,
0.6082293609836236626433448831m,
0.3589034711596569247153144600m,
0.4140675089652527369219794842m,
0.5209339558561161282534689027m,
0.4988090799534863306410657278m,
0.1023865075580040392585432373m,
0.5914420082649215356288047512m,
0.7m,
};
    decimal[] mg_king_table_encoded = new decimal[] {
0.3840391485879738464833731646m,
0.5791856841261702807301274774m,
0.2858351627487408006902954195m,
0.6048692692139850450668619073m,
0.4456359269995736768647101752m,
0.2567240214405747777480104427m,
0.9463765929627204240300966707m,
0.8527225419803135045684985583m,
0.1991669922768688484411133436m,
0.6631247835244628732541544340m,
0.5746053332180667727347647m,
};
    decimal[] eg_king_table_encoded = new decimal[] {
0.1796541039180951060828574684m,
0.4852067124780928641588229463m,
0.2566084966161116573981696750m,
0.4186579452102552891947750213m,
0.8356420370682990558561432355m,
0.7438984680956906095473810712m,
0.0943087629617497101221608541m,
0.7927697791276282237658764937m,
0.5072855737594382798472292186m,
0.2343196522837426436484028834m,
0.5801867026644651501904763282m,
0.2m,
};

}