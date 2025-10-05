using Spectre.Console;
using Spectre.Console.Rendering;

namespace SequenceAgent.UI;

public class ChatMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public bool IsSystem { get; set; }
    public string? Name { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ChatUI
{
    private readonly List<ChatMessage> _messages = new();
    private const int MaxDisplayMessages = 15; // Increased for full-screen view
    private bool _isAIThinking = false;
    private string _thinkingMessage = "🤖 AI is thinking...";

    public void AddMessage(string content, bool isUser, string? name = null, bool isSystem = false)
    {
        _messages.Add(new ChatMessage
        {
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.Now,
            Name = name,
            IsSystem = isSystem
        });
        _isAIThinking = false; // Clear thinking state when message is added
    }

    public void SetAIThinking(bool isThinking, string thinkingMessage = "🤖 AI is thinking...")
    {
        _isAIThinking = isThinking;
        _thinkingMessage = thinkingMessage;
    }

    public void DisplayChat()
    {
        AnsiConsole.Clear();

        // Create a full-screen conversation layout
        var chatContent = CreateChatContent();
        var conversationPanel = new Panel(chatContent)
            .Header("[bold yellow]🤖 AI Leaning Workflow  - Conversation[/]")
            .BorderColor(Color.Yellow)
            .Padding(1, 1)
            .Expand();

        AnsiConsole.Write(conversationPanel);
    }

    public void DisplayChatForInput()
    {
        AnsiConsole.Clear();

        // Get terminal height to calculate optimal sizing
        var terminalHeight = Console.WindowHeight;
        var inputSectionHeight = 6; // Reserve more space for input section
        var conversationHeight = Math.Max(10, terminalHeight - inputSectionHeight - 4);

        // Create the main layout with conversation taking most space
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Conversation").Size(conversationHeight),
                new Layout("InputArea").Size(inputSectionHeight)
            );

        // Conversation area - full width
        var chatContent = CreateChatContent();
        layout["Conversation"].Update(
            new Panel(chatContent)
                .Header("[bold yellow]🤖 AI Leaning Workflow - Conversation[/]")
                .BorderColor(Color.Yellow)
                .Padding(1, 1)
                .Expand()
        );

        // Input area - prepare for input
        layout["InputArea"].Update(
            new Panel(new Markup("[dim]Ready to type your message...[/]"))
                .Header("[bold green]Your Turn[/]")
                .BorderColor(Color.Green)
                .Padding(1, 1)
                .Expand()
        );

        AnsiConsole.Write(layout);
    }

    private IRenderable CreateChatContent()
    {
        if (!_messages.Any())
        {
            return new Markup("[dim]No messages yet. Start the conversation![/]");
        }

        var recentMessages = _messages.TakeLast(MaxDisplayMessages);
        var lines = new List<string>();

        foreach (var message in recentMessages)
        {
            var timeStamp = message.Timestamp.ToString("HH:mm");
            var prefix = message.IsSystem ? "[yellow]System[/]" : message.IsUser ? $"[green]You[/]" : $"[blue]🤖 {message.Name ?? "AI"}[/]";
            var content = message.Content.EscapeMarkup();

            // Add the message with proper spacing
            lines.Add($"[dim]{timeStamp}[/] {prefix}: {content}");
            lines.Add(""); // Add spacing between messages
        }

        // Add thinking indicator if AI is thinking
        if (_isAIThinking)
        {
            var currentTime = DateTime.Now.ToString("HH:mm");
            lines.Add($"[dim]{currentTime}[/] [yellow]{_thinkingMessage}[/]");
            lines.Add("");
        }

        // Remove the last empty line
        if (lines.Any())
            lines.RemoveAt(lines.Count - 1);

        return new Markup(string.Join("\n", lines));
    }

    public void ShowThinkingSpinner(string message = "AI is thinking...")
    {
        AnsiConsole.Status()
            .Start(message, ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                // This will be handled by the caller's async operation
                // The spinner will automatically stop when the context exits
            });
    }
}