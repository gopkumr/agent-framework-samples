using Spectre.Console;

namespace SequenceAgent.UI;

public class UserInput
{
    public static string GetUserMessage()
    {
        // Create a full-width input prompt
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]💬 Your message:[/] ")
                .PromptStyle("green")
                .AllowEmpty()
        );

        return input.Trim();
    }

    public static string GetUserMessageWithValidation()
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]💬 Your message:[/] ")
                .PromptStyle("green")
                .Validate(input =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return ValidationResult.Error("[red]Please enter a message[/]");

                    if (input.Length > 500)
                        return ValidationResult.Error("[red]Message too long (max 500 characters)[/]");

                    return ValidationResult.Success();
                })
        );

        return input.Trim();
    }

    public static string GetUserMessageFullWidth(ChatUI chatUI)
    {
        // Display the full-height conversation
        chatUI.DisplayChat();

        // Add a simple rule separator for input
        AnsiConsole.Write(new Rule("[green]Your Turn[/]") { Justification = Justify.Left });

        // Get user input with clean styling
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]💬[/] ")
                .PromptStyle("green")
                .Validate(input =>
                {
                    if (string.IsNullOrWhiteSpace(input))
                        return ValidationResult.Error("[red]Please enter a message[/]");

                    if (input.Length > 500)
                        return ValidationResult.Error("[red]Message too long (max 500 characters)[/]");

                    return ValidationResult.Success();
                })
        );

        return input.Trim();
    }

    public static bool ConfirmQuit()
    {
        return AnsiConsole.Confirm("[red]Are you sure you want to quit?[/]");
    }

    public static void ShowWelcomeMessage()
    {
        var panel = new Panel(new Markup(
            "[bold blue]Welcome to AI Learning Assistant! 🤖[/]\n\n" +
            "• Type the [green] Topic [/] you want to learn and press Enter to start\n" +
            "• Enjoy your conversation! 💬"))
            .Header("[green]Getting Started[/]")
            .BorderColor(Color.Green)
            .Padding(1, 1)
            .Expand();

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public static void ShowGoodbyeMessage()
    {
        var panel = new Panel(new Markup(
            "[bold yellow]Thanks for chatting![/]\n\n" +
            "Hope you enjoyed your conversation with the AI assistant. 👋"))
            .Header("[blue]Goodbye[/]")
            .BorderColor(Color.Blue)
            .Padding(1, 1);

        AnsiConsole.Write(panel);
    }
}