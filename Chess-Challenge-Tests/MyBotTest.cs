using System.Runtime.InteropServices;
using ChessChallenge.Application;
using ChessChallenge.Chess;
using static MyBot;

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

    [Fact]
    public void ShouldMate()
    {
        var move = GetMove("3Q4/1P6/8/7k/8/8/8/K5Q1 w - - 0 1");

        Assert.Equal("d3", move.TargetSquare.Name);
    }

    [Fact]
    public void CalcMaxTranspositionTableSize()
    {
        const int sizeInMb = 256;
        int sizeBytes = sizeInMb * 1024 * 1024;
        //var instance = new TranspositionTableResult(123456789UL, 5, 30000, true);
        var singleEntrySize = Marshal.SizeOf(typeof(TranspositionTableResult));
        int numEntries = sizeBytes / singleEntrySize;

        //New max entries: 11184810, old: 8388608, prev: 1048576
        Assert.Equal(11184810, numEntries); // New theoretical max using all 256 MB of TT space
        Assert.Equal(8388608, 1 << 23); // Greatest power of 2 that is less than numEntries
        Assert.Equal(1048576, 1 << 20); // Previous value for max TT entries

        Console.WriteLine($"New max entries: {numEntries}, old: {1 << 23}, prev: {1 << 20}");
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

        var playerTimeRemaining = int.MaxValue;
        var player2TimeRemaining = int.MaxValue;

        ChessChallenge.API.Timer timer = new(playerTimeRemaining, player2TimeRemaining, Settings.GameDurationMilliseconds, Settings.IncrementMilliseconds);
        return player.Bot.Think(botBoard, timer);
    }
}
