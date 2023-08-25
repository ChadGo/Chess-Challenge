using ChessChallenge.Application;
using ChessChallenge.Chess;

namespace Chess_Challenge_Tests;

public class MyBotTest
{
    [Fact]
    public void ShouldCaptureQueen()
    {
        var move = GetMove("rnb1kbnr/ppp1pppp/8/8/7P/PP1q4/3PPPP1/RNBQKBNR w KQkq - 1 6");

        Assert.Equal("e2", move.StartSquare.Name);
        Assert.Equal("d3", move.TargetSquare.Name);

        //move.StartSquare.Name == "e2" && move.TargetSquare.Name == "d3"

    }

    private ChessChallenge.API.Move GetMove(string fen)
    {
        Board board = new Board();
        board.LoadPosition(fen);

        ChessChallenge.API.Board botBoard = new(board);

        Console.WriteLine(botBoard.CreateDiagram());

        var bot = new MyBot();
        var player = new ChessPlayer(bot, ChallengeController.PlayerType.MyBot);

        var bot2 = new MyBot();
        var player2 = new ChessPlayer(bot2, ChallengeController.PlayerType.MyBot);

        ChessChallenge.API.Timer timer = new(player.TimeRemainingMs, player2.TimeRemainingMs, Settings.GameDurationMilliseconds, Settings.IncrementMilliseconds);
        return player.Bot.Think(botBoard, timer);
    }
}
