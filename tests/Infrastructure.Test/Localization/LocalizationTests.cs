using System.Globalization;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Infrastructure.Test.Localization;

public class LocalizationTests
{
    private const string _testString = "testString";
    private const string _testStringInDutch = "testString in dutch";
    private const string _testStringInBelgianDutch = "testString in belgian dutch";
    private const string _testString2 = "testString2";
    private const string _testString2InDutch = "testString2 in dutch";

    private readonly IStringLocalizer _localizer;

    public LocalizationTests(IStringLocalizer<LocalizationTests> localizer) => _localizer = localizer;

    // there's no "en-US" folder
    // "nl-BE/test.po" only contains testString
    // "nl/test.po" contains both testString and testString2
    [Theory]
    [InlineData("en-US", _testString, _testString)]
    [InlineData("nl-NL", _testString, _testStringInDutch)]
    [InlineData("nl-BE", _testString, _testStringInBelgianDutch)]
    [InlineData("nl-NL", _testString2, _testString2InDutch)]
    [InlineData("nl-BE", _testString2, _testString2InDutch)]
    public void TranslateToCultureTest(string culture, string testString, string translatedString)
    {
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(culture);

        var result = _localizer[testString];

        Assert.Equal(translatedString, result);
    }
}