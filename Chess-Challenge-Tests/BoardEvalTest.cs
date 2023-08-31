using ChessChallenge.Chess;

public class BoardEvalTest
{
    [Fact]
    public void DefaultBoard()
    {
        var evaluation = GetEvaluation("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        Assert.Equal(0, evaluation);
    }

    [Fact]
    public void MissingWhiteQueen()
    {
        var evaluation = GetEvaluation("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNB1KBNR w KQkq - 0 1");

        Assert.Equal(-910, evaluation);
    }

    [Fact]
    public void MissingAllWhitePawnsQueen()
    {
        var evaluation = GetEvaluation("rnbqkbnr/pppppppp/8/8/8/8/11111111/RNBQKBNR w KQkq - 0 1");

        Assert.Equal(-746, evaluation);
    }

    [Fact]
    public void MissingBlackQueen()
    {
        var evaluation = GetEvaluation("rnb1kbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        Assert.Equal(910, evaluation);
    }


    [Fact]
    public void MissingAllBlackPawnsQueen()
    {
        var evaluation = GetEvaluation("rnbqkbnr/11111111/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        Assert.Equal(746, evaluation);
    }


    [Fact]
    public void E4Opening()
    {
        var evaluation = GetEvaluation("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");

        Assert.Equal(32, evaluation);
    }

    [Fact]
    public void BlackKingShortSideEval()
    {
        var evaluation = GetEvaluation("6k1/8/8/8/8/8/8/8 w - - 0 1");

        Assert.Equal(-10024, evaluation);
    }

    
    [Fact]
    public void OnlyShortSideCastledRookAndKing()
    {
        var evaluation = GetEvaluation("5rk1/8/8/8/8/8/8/5RK1 w - - 0 1");

        Assert.Equal(0, evaluation);
    }

    [Fact]
    public void CheckIndexCalculations()
    {
        for (int index = 0; index < 64; index++)
        {
            //var the_worst_lookup_index = ((63 - index) - ((63 - index) % 8) + (index % 8));
            var the_worst_lookup_index = 63 - index;
            Console.WriteLine($"Index: {index}, black index: {the_worst_lookup_index}");




            var index_row_number = index / 8;
            var index_column_number = index % 8;

            var lookup_row_number = true ? 7 - index_row_number : index_row_number;
            var lookup_column_number = index_column_number;

            var the_best_lookup_index = lookup_row_number * 8 + lookup_column_number;


            Assert.Equal(the_best_lookup_index, the_worst_lookup_index);
        }
        

    }


    [Fact]
    public void E4Response()
    {
        var e4_e5_eval = GetEvaluation("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2");
        var e4_a6_eval = GetEvaluation("rnbqkbnr/1ppppppp/p7/8/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2");

        Console.WriteLine(e4_e5_eval);
        Console.WriteLine(e4_a6_eval);

        Assert.True(e4_e5_eval > e4_a6_eval);
    }


    [Fact]
    public void E4E5Response()
    {
        var e4_e5_eval = GetEvaluation("rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2");
        Assert.Equal(0, e4_e5_eval);
    }

    [Fact]
    public void E4A5Response()
    {
        var e4_a6_eval = GetEvaluation("rnbqkbnr/1ppppppp/p7/8/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2");
        Assert.Equal(0, e4_a6_eval);
    }


    private int GetEvaluation(string fen)
    {
        Board board = new Board();
        board.LoadPosition(fen);

        ChessChallenge.API.Board botBoard = new(board);

        Console.WriteLine(botBoard.CreateDiagram());

        var bot = new MyBotPsv();
        return bot.EvaluateBoard(botBoard);
    }
}
