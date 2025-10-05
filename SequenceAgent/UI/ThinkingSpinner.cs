using Spectre.Console;

namespace SequenceAgent.UI;

public class ThinkingSpinner
{
    private readonly string[] _thinkingMessages =
    {
        "🤖 AI is thinking...",
        "🤖 Processing your request...",
        "🤖 Analyzing the information...",
        "🤖 Formulating a response...",
        "🤖 Considering different perspectives...",
        "🤖 Gathering relevant insights..."
    };

    private readonly Random _random = new();

    public async Task<T> ShowSpinnerInChatAsync<T>(Func<Task<T>> operation, ChatUI chatUI)
    {
        T result = default!;
        var messageIndex = 0;
        var maxMessages = _thinkingMessages.Length;

        // Start thinking animation
        chatUI.SetAIThinking(true, _thinkingMessages[messageIndex]);

        // Create a timer to cycle through thinking messages
        using var timer = new Timer(state =>
        {
            messageIndex = (messageIndex + 1) % maxMessages;
            chatUI.SetAIThinking(true, _thinkingMessages[messageIndex]);
            chatUI.DisplayChat();
        }, null, TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(800));

        try
        {
            // Execute the operation
            result = await operation();
        }
        finally
        {
            // Stop thinking animation
            chatUI.SetAIThinking(false);
        }

        return result;
    }

    public void ShowSpinnerInChat(ChatUI chatUI)
    {
        var messageIndex = 0;
        var maxMessages = _thinkingMessages.Length;

        // Start thinking animation
        chatUI.SetAIThinking(true, _thinkingMessages[messageIndex]);

        // Create a timer to cycle through thinking messages
        using var timer = new Timer(state =>
        {
            messageIndex = (messageIndex + 1) % maxMessages;
            chatUI.SetAIThinking(true, _thinkingMessages[messageIndex]);
            chatUI.DisplayChat();
        }, null, TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(800));
    }


    public async Task ShowSpinnerAsync(Func<Task<string>> operation)
    {
        var message = _thinkingMessages[_random.Next(_thinkingMessages.Length)];

        await AnsiConsole.Status()
            .StartAsync(message, async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                // Wait for the operation to complete
                await operation();
            });
    }

    public async Task<T> ShowSpinnerAsync<T>(Func<Task<T>> operation)
    {
        var message = _thinkingMessages[_random.Next(_thinkingMessages.Length)];
        T result = default!;

        await AnsiConsole.Status()
            .StartAsync(message, async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                // Wait for the operation to complete and capture result
                result = await operation();
            });

        return result;
    }

    public async Task ShowSpinnerAsync(Func<Task> operation, string? statusMessage=null)
    {
        var message = statusMessage ?? _thinkingMessages[_random.Next(_thinkingMessages.Length)];

        await AnsiConsole.Status()
            .StartAsync(message, async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                // Wait for the operation to complete
                await operation();
            });
    }

    public async Task ShowProgressAsync(Func<IProgress<string>, Task> operation)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]AI Processing[/]");
                task.IsIndeterminate = true;

                var progress = new Progress<string>(message =>
                {
                    task.Description = $"[green]{message}[/]";
                });

                await operation(progress);
                task.Value = 100;
            });
    }
}