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
    public void MissingBackQueen()
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
