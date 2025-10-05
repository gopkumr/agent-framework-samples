using Microsoft.Agents.AI.Workflows;
using SequenceAgent.AgentServices;
using SequenceAgent.UI;
using Spectre.Console;


// Initialize the chat application
var chatUI = new ChatUI();
var spinner = new ThinkingSpinner();
var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_ENDPOINT") ?? "https://[NAME].services.ai.azure.com/api/projects/[PROJECT]";
var deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
var workflowService = new WorkflowService(endpoint);


// Show welcome message
AnsiConsole.Clear();
UserInput.ShowWelcomeMessage();

// Wait for user to enter the topic to start
AnsiConsole.MarkupLine("[dim]Enter the topic to start chatting...[/]");
var topic = Console.ReadLine();

try
{
    await spinner.ShowSpinnerAsync(() => workflowService.Initialize(deploymentName), "Connecting agents 🤖 ...");
    AnsiConsole.Clear();

    // Add user message to chat
    chatUI.AddMessage(topic, isUser: true);
    chatUI.SetAIThinking(true, "🤖 Content Agent is generating content...");
    chatUI.DisplayChat();

    var workflow = await workflowService.CreateQuestionAnswerWorkflow();

    //Console.WriteLine(workflow.ToDotString());

    StreamingRun handle = await InProcessExecution.StreamAsync(workflow, topic).ConfigureAwait(false);

    await foreach (WorkflowEvent evt in handle.WatchStreamAsync().ConfigureAwait(false))
    {
        if (evt is ContentPreparedEvent contentPreparedEvent)
        {
            chatUI.AddMessage(contentPreparedEvent.Content, isUser: false, "Content Agent");
            chatUI.SetAIThinking(true, "🤖 Questions Agent is preparing questions...");
            chatUI.DisplayChat();
        }
        else if (evt is QuizQuestionAnswersEvent questionAnswersEvent)
        {
            //chatUI.AddMessage(questionAnswersEvent.QuestionAnswers, isUser: false, "Questions Agent");
            chatUI.SetAIThinking(true, "🤖 Presenter Agent is preparing to ask the first question...");
            chatUI.DisplayChat();
        }
        else if (evt is QuestionEvent questionEvent)
        {
            chatUI.AddMessage(questionEvent.Question, isUser: false, "Presenter Agent");
            chatUI.SetAIThinking(false);
            chatUI.DisplayChat();
        }
        else if (evt is RequestInfoEvent requestInputEvt)
        {
            ExternalResponse response = HandleExternalRequest(requestInputEvt.Request, chatUI);
            chatUI.DisplayChat();
            await handle.SendResponseAsync(response).ConfigureAwait(false);
            chatUI.SetAIThinking(true, "🤖 Presenter Agent is validating the answer...");
            chatUI.DisplayChat();
        }
    }

    //Delete agents
    await workflowService.CleanupAgents();

    chatUI.AddMessage("press ENTER to exit", isUser: false, isSystem: true);
    chatUI.DisplayChat();
    Console.ReadLine();

}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    AnsiConsole.MarkupLine("[red]An error occurred. Press any key to continue...[/]");
    Console.ReadKey(true);
}


static ExternalResponse HandleExternalRequest(ExternalRequest request, ChatUI chatUI)
{
    if (request.DataIs<string>())
    {
        var userMessage = UserInput.GetUserMessageFullWidth(chatUI);
        chatUI.AddMessage(userMessage, isUser: true);
        return request.CreateResponse(userMessage);
    }
    throw new NotSupportedException($"Request {request.PortInfo.RequestType} is not supported");
}

