using ChessChallenge.Chess;

public class IterativeDeepeningBotTest
{
    [Fact]
    public void DefaultBoard()
    {
        var move = GetMove("rnb1kbnr/pppp1ppp/4pq2/8/8/2N1P3/PPPP1PPP/R1BQKBNR w KQkq - 1 3");

        Assert.Equal("", move.ToString());
    }


    private ChessChallenge.API.Move GetMove(string fen)
    {
        Board board = new Board();
        board.LoadPosition(fen);

        ChessChallenge.API.Board botBoard = new(board);

        Console.WriteLine(botBoard.CreateDiagram());

        var bot = new ChadBot();
        return bot.Think(botBoard, new ChessChallenge.API.Timer(60000));
    }
}
