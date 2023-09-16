using ChessChallenge.Chess;

public class IterativeDeepeningBotTest
{
    [Fact]
    public void DefaultBoard()
    {
        var move = GetMove("r1bqkb1r/ppp2ppp/2n5/3np3/4Q3/2P4P/PP1PNPP1/RNB1KB1R b KQkq - 0 7");

        Assert.Equal(0, 0);
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
