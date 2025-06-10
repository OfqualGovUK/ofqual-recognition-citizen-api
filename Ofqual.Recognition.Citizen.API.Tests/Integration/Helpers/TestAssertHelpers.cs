using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Helper;

public static class TestAssertHelpers
{
    public static void AssertDateTimeAlmostEqual(DateTime expected, DateTime actual, int toleranceMilliseconds = 10)
    {
        var difference = (expected - actual).Duration();
        Assert.True(difference < TimeSpan.FromMilliseconds(toleranceMilliseconds),
            $"DateTimes differ by more than {toleranceMilliseconds}ms. Expected: {expected:O}, Actual: {actual:O}");
    }
}
