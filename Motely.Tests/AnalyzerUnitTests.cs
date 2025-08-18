using static VerifyXunit.Verifier;

namespace Motely.Tests;

public sealed class AnalyzerUnitTests
{

    [Fact]
    public async Task TestAnalyzer_UNITTEST_Seed()
    {
        // Arrange
        string seed = "UNITTEST";



        // Act
        var actualOutput = GetAnalyzerOutput(seed);

        // Assert using Verify - this will create a nice diff view
        await Verify(actualOutput)
            .UseFileName($"analyzer_output_{seed}")
            .DisableRequireUniquePrefix();
    }

    [Fact]
    public async Task TestAnalyzer_ALEEB_Seed()
    {
        // Arrange
        string seed = "ALEEB";

        // Act
        var actualOutput = GetAnalyzerOutput(seed);

        // Assert using Verify - this will create a nice diff view
        await Verify(actualOutput)
            .UseFileName($"analyzer_output_{seed}")
            .DisableRequireUniquePrefix();
    }

    private string GetAnalyzerOutput(string seed)
    {
        return SeedAnalyzer.Analyze(new(seed, MotelyDeck.Red, MotelyStake.White)).ToString();
    }

    // This method is now only used by other tests that don't use Verify yet
    private void AssertOutputsMatch(string expected, string actual, string seed)
    {
        // Normalize line endings
        expected = expected.Replace("\r\n", "\n").Trim();
        actual = actual.Replace("\r\n", "\n").Trim();

        // Split into lines for detailed comparison
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        // First check line count
        Assert.Equal(expectedLines.Length, actualLines.Length);

        // Compare line by line for better error messages
        for (int i = 0; i < expectedLines.Length; i++)
        {
            var expectedLine = expectedLines[i].TrimEnd();
            var actualLine = actualLines[i].TrimEnd();

            if (expectedLine != actualLine)
            {
                // Provide detailed error message showing the difference
                var message = $"Seed {seed} - Line {i + 1} mismatch:\n" +
                              $"Expected: {expectedLine}\n" +
                              $"Actual:   {actualLine}";
                Assert.Fail(message);
            }
        }
    }

    [Fact]
    public void TestAnalyzer_PackContentsFormat()
    {
        // Test that pack contents are formatted correctly
        string seed = "UNITTEST";
        var output = GetAnalyzerOutput(seed);

        // Check that packs have the correct format: "Pack Name - Card1, Card2"
        Assert.Contains("Buffoon Pack - ", output);
        Assert.Contains("Arcana Pack - ", output);
        Assert.Contains("Standard Pack - ", output);

        // Check that Mega packs DON'T have the "(pick 2)" suffix (Immolate doesn't use it)
        Assert.Contains("Mega Standard Pack - ", output);
        Assert.Contains("Mega Arcana Pack - ", output);
        Assert.Contains("Mega Celestial Pack - ", output);
    }

    [Fact]
    public void TestAnalyzer_TagsNotActivated()
    {
        // Test that tags are just listed, not "activated" to show their packs
        string seed = "UNITTEST";
        var output = GetAnalyzerOutput(seed);

        // Check first ante has Speed Tags but no extra packs from them
        var lines = output.Split('\n');
        bool inAnte1 = false;
        int packCount = 0;

        foreach (var line in lines)
        {
            if (line.Contains("==ANTE 1=="))
            {
                inAnte1 = true;
            }
            else if (line.Contains("==ANTE 2=="))
            {
                break;
            }
            else if (inAnte1 && line.Trim().StartsWith("Buffoon Pack") ||
                     line.Trim().StartsWith("Arcana Pack") ||
                     line.Trim().StartsWith("Celestial Pack") ||
                     line.Trim().StartsWith("Spectral Pack") ||
                     line.Trim().StartsWith("Standard Pack") ||
                     line.Trim().StartsWith("Jumbo") ||
                     line.Trim().StartsWith("Mega"))
            {
                packCount++;
            }
        }

        // Ante 1 should have exactly 4 packs
        Assert.Equal(4, packCount);
    }
}