using Microsoft.Playwright.NUnit;

namespace E2ETests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class SwaggerUiTests : PageTest
{
    private const string BaseUrl = "http://localhost:5037";

    [Test]
    public async Task SwaggerUI_ShouldLoad()
    {
        await Page.GotoAsync($"{BaseUrl}/swagger/index.html");

        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Swagger"));
    }

    [Test]
    public async Task SwaggerUI_ShouldDisplayApiTitle()
    {
        await Page.GotoAsync($"{BaseUrl}/swagger/index.html");

        var titleLocator = Page.Locator(".title");
        await Expect(titleLocator).ToContainTextAsync("Unified Patient Access API");
    }
}
