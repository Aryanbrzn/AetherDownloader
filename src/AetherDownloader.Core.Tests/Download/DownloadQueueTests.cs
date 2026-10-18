using AetherDownloader.Core.Download;
using AetherDownloader.Core.Models;
using FluentAssertions;

namespace AetherDownloader.Core.Tests.Download;

public sealed class DownloadQueueTests
{
    [Fact]
    public async Task EnqueueAsync_MaxParallelTwo_NeverExceedsTwoConcurrent()
    {
        var sync = new object();
        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var queue = new DownloadQueue(2, async (_, _, ct) =>
        {
            lock (sync)
            {
                currentConcurrent++;
                if (currentConcurrent > maxConcurrent)
                {
                    maxConcurrent = currentConcurrent;
                }
            }

            await gate.Task.WaitAsync(ct);
            lock (sync)
            {
                currentConcurrent--;
            }

            return new DownloadResult { Success = true, ExitCode = 0 };
        });

        var requests = Enumerable.Range(0, 4).Select(CreateRequest).ToList();
        var enqueueTasks = requests
            .Select(r => queue.EnqueueAsync(r, null, CancellationToken.None))
            .ToList();

        await Task.Delay(100);
        maxConcurrent.Should().BeLessThanOrEqualTo(2);

        gate.TrySetResult();
        await Task.WhenAll(enqueueTasks);

        maxConcurrent.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void Constructor_MaxParallelOutOfRange_Throws()
    {
        var act = () => new DownloadQueue(0, (_, _, _) => Task.FromResult(new DownloadResult { Success = true, ExitCode = 0 }));
        act.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => new DownloadQueue(5, (_, _, _) => Task.FromResult(new DownloadResult { Success = true, ExitCode = 0 }));
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static DownloadRequest CreateRequest(int index) =>
        new()
        {
            JobId = Guid.NewGuid(),
            Url = $"https://example.com/video{index}",
            FormatSelection = new FormatSelection { Mode = FormatSelectionMode.BestMerge },
            OutputDirectory = Path.GetTempPath(),
            FileNameTemplate = $"video{index}",
        };
}
