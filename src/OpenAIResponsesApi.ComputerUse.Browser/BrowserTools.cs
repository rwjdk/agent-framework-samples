using System.ComponentModel;
using Microsoft.Playwright;

namespace ComputerUse;

internal sealed class BrowserTools(IPage page)
{
    [Description("Navigate to a URL and return the loaded page's structure.")]
    public async Task<string> Navigate(string url)
    {
        Console.WriteLine($"Navigate to {url}");
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
        return await ReadPage();
    }

    [Description("Read the current page's accessibility tree, including headings, labeled fields, buttons, and status text. Use this to discover controls and verify results.")]
    public async Task<string> ReadPage()
    {
        string snapshot = await page.Locator("body").AriaSnapshotAsync();
        Console.WriteLine($"Page structure:{Environment.NewLine}{snapshot}");
        return $"URL: {page.Url}{Environment.NewLine}{snapshot}";
    }

    [Description("Fill a text field using its exact accessible label from the current page structure. Returns the updated page structure.")]
    public async Task<string> FillField(
        [Description("The exact label of the field, as observed on the page.")] string label,
        [Description("The text to enter into the field.")] string value)
    {
        Console.WriteLine($"Fill field '{label}' with '{value}'");
        ILocator field = page.GetByLabel(label, new() { Exact = true });
        await ShowCursorAsync(field);
        await field.FillAsync(value);
        return await ReadPage();
    }

    [Description("Click a button using its exact accessible name from the current page structure. Returns the updated page structure.")]
    public async Task<string> ClickButton(
        [Description("The exact accessible name of the button, as observed on the page.")] string name)
    {
        Console.WriteLine($"Click button '{name}'");
        ILocator button = page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
        await ShowCursorAsync(button);
        await button.ClickAsync();
        return await ReadPage();
    }

    [Description("Press a key in a field identified by its exact accessible label. Use Enter to submit a search. Returns the updated page structure.")]
    public async Task<string> PressKey(
        [Description("The exact accessible label of the field, as observed on the page.")] string label,
        [Description("The Playwright key name, for example Enter.")] string key)
    {
        ILocator field = page.GetByLabel(label, new() { Exact = true });
        await ShowCursorAsync(field);
        await field.PressAsync(key);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return await ReadPage();
    }

    private static async Task ShowCursorAsync(ILocator target)
    {
        await target.ScrollIntoViewIfNeededAsync();
        await target.EvaluateAsync("""
            async element => {
                let cursor = document.getElementById('agent-cursor');
                if (!cursor) {
                    cursor = document.createElement('div');
                    cursor.id = 'agent-cursor';
                    cursor.setAttribute('aria-hidden', 'true');
                    Object.assign(cursor.style, { position: 'fixed', left: '24px', top: '24px', zIndex: '2147483647', pointerEvents: 'none', color: '#7c3aed', filter: 'drop-shadow(0 2px 3px #0005)' });
                    cursor.innerHTML = '<svg width="28" height="34" viewBox="0 0 28 34"><path d="M2 2 L2 26 L9 20 L15 32 L21 29 L15 18 L25 18 Z" fill="currentColor" stroke="white" stroke-width="2"/></svg><span style="position:absolute;left:22px;top:24px;background:#7c3aed;color:white;border-radius:5px;padding:3px 7px;font:13px system-ui;white-space:nowrap">AI agent</span>';
                    document.body.appendChild(cursor);
                }
                const box = element.getBoundingClientRect();
                const left = `${box.left + box.width / 2}px`;
                const top = `${box.top + box.height / 2}px`;
                await cursor.animate([
                    { left: cursor.style.left || '24px', top: cursor.style.top || '24px' },
                    { left, top }
                ], { duration: 500, easing: 'ease-in-out', fill: 'forwards' }).finished;
                cursor.style.left = left;
                cursor.style.top = top;
                for (const animation of cursor.getAnimations()) animation.cancel();
                await cursor.animate([
                    { transform: 'scale(1)' },
                    { transform: 'scale(0.8)', offset: 0.5 },
                    { transform: 'scale(1)' }
                ], { duration: 250 }).finished;
            }
            """);
    }
}
