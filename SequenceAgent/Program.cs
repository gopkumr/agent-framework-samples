using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using SequenceAgent;

string Topic = "Azure Functions runtime versions overview";

Console.WriteLine("Starting Quiz Agent workflow, please enter a topic you want to learn about...");
Topic = Console.ReadLine() ?? Topic;

var endpoint = "https://gopa-racgp-poc-dev-resource.services.ai.azure.com/api/projects/gopa-racgp-poc-dev"
    ?? throw new InvalidOperationException("AZURE_FOUNDRY_PROJECT_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
var persistentAgentsClient = new PersistentAgentsClient(endpoint, new AzureCliCredential());

AIAgent WebContentDownloaderAgent = await CreateFoundryAgent(deploymentName, "WebContentDownloader", """
        - Use HostedWebSearchTool to extract readable content from the internet
        - Strip ads, navigation, and irrelevant sections
        - Return title and cleaned body text
        """, persistentAgentsClient, [new HostedWebSearchTool()]);

AIAgent QuizGeneratorAgent = await CreateFoundryAgent(deploymentName, "QuizGenerator", """
        - Generate 5 factual questions based on the content
        - Include concise answers
        - Format as a list of Q&A pairs
        .
        """, persistentAgentsClient);
AIAgent QuizPresenterAgent = await CreateFoundryAgent(deploymentName, "QuizPresenter", """
        - Present one question at a time
        - Wait for user input
        - Compare user response to expected answer (basic match or semantic similarity)
        - Provide feedback: correct/incorrect + explanation
        - Score the quiz and summarize performance

        """, persistentAgentsClient);

var inputExecutor = InputPort.Create<string, string>("Answer");
var WebContentDownloader = new WebContentDownloaderAgentExecutor(WebContentDownloaderAgent);
var QuizGenerator = new QuizGeneratorAgentExecutor(QuizGeneratorAgent);
var QuizPresenter = new QuizPresenterAgentExecutor(QuizPresenterAgent);


var workflow = new WorkflowBuilder(WebContentDownloader)
           .AddEdge(WebContentDownloader, QuizGenerator)
           .AddEdge(QuizGenerator, QuizPresenter)
           .AddEdge(QuizPresenter, inputExecutor)
           .AddEdge(inputExecutor, QuizPresenter)
           .WithOutputFrom(QuizPresenter)
           .Build();

//Console.WriteLine(workflow.ToDotString());

StreamingRun handle = await InProcessExecution.StreamAsync(workflow, Topic).ConfigureAwait(false);

await foreach (WorkflowEvent evt in handle.WatchStreamAsync().ConfigureAwait(false))
{
    if (evt is ContentPreparedEvent contentPreparedEvent)
    {
        Console.WriteLine("**************** Content Agent *************************");
        Console.Write(contentPreparedEvent.Content);
        Console.WriteLine("\n*******************************************************\n\n");
    }
    else if (evt is QuizQuestionAnswersEvent questionAnswersEvent)
    {
        Console.WriteLine("\n************** Question Answers Agent ************");
        Console.Write(questionAnswersEvent.QuestionAnswers);
        Console.WriteLine("\n**************************************************\n\n");
    }
    else if (evt is QuestionEvent questionEvent)
    {
        Console.WriteLine($"{questionEvent.Question}");
    }
    else if (evt is RequestInfoEvent requestInputEvt)
    {
        ExternalResponse response = HandleExternalRequest(requestInputEvt.Request);
        await handle.SendResponseAsync(response).ConfigureAwait(false);
    }
}

// Cleanup agents.
await persistentAgentsClient.Administration.DeleteAgentAsync(WebContentDownloader.Id);
await persistentAgentsClient.Administration.DeleteAgentAsync(QuizGenerator.Id);
await persistentAgentsClient.Administration.DeleteAgentAsync(QuizPresenter.Id);

Console.Write("Sample agents deleted, press ENTER to exit");
Console.ReadLine();


static async Task<AIAgent> CreateFoundryAgent(string deploymentName, string agentName, string agentInstructions, PersistentAgentsClient persistentAgentsClient, IList<AITool>? tools = null)
{
    var researcherAgentMetadata = await persistentAgentsClient.Administration.CreateAgentAsync(
        model: deploymentName,
        name: agentName,
        instructions: agentInstructions);
    var chatClient = persistentAgentsClient.AsIChatClient(researcherAgentMetadata.Value.Id);

    AIAgent researcher = new ChatClientAgent(chatClient, options: new()
    {
        Id = researcherAgentMetadata.Value.Id,
        Name = researcherAgentMetadata.Value.Name,
        Description = researcherAgentMetadata.Value.Description,
        Instructions = researcherAgentMetadata.Value.Instructions,
        ChatOptions = new() { Tools = tools ?? [] }
    });
    return researcher;
}

static ExternalResponse HandleExternalRequest(ExternalRequest request)
{
    if (request.DataIs<string>())
    {
        Console.Write($"Please provide your answer: ");
        string? answer = Console.ReadLine();
        return request.CreateResponse(answer);
    }
    throw new NotSupportedException($"Request {request.PortInfo.RequestType} is not supported");
}