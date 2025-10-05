using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace SequenceAgent.AgentServices
{
    public class WorkflowService(string azureFoundryEndpoint)
    {
        private PersistentAgentsClient persistentAgentsClient;
        private AIAgent WebContentDownloaderAgent;
        private AIAgent QuizGeneratorAgent;
        private AIAgent QuizPresenterAgent;


        public async Task Initialize(string deploymentName)
        {
            persistentAgentsClient = new(azureFoundryEndpoint, new AzureCliCredential());
            WebContentDownloaderAgent = await CreateWebContentDownloaderAgent(deploymentName, persistentAgentsClient);
            QuizGeneratorAgent = await CreateQuizGeneratorAgent(deploymentName, persistentAgentsClient);
            QuizPresenterAgent = await CreateQuizPresenterAgent(deploymentName, persistentAgentsClient);
        }

        public async Task<Workflow> CreateQuestionAnswerWorkflow()
        {
            if (persistentAgentsClient == null || WebContentDownloaderAgent == null || QuizGeneratorAgent == null || QuizPresenterAgent == null)
            {
                throw new InvalidOperationException("WorkflowService is not initialized. Call Initialize() first.");
            }

            var inputExecutor = InputPort.Create<string, string>("Answer");
            var WebContentDownloader = new WebContentDownloaderAgentExecutor(WebContentDownloaderAgent);
            var QuizGenerator = new QuizGeneratorAgentExecutor(QuizGeneratorAgent);
            var QuizPresenter = new QuizPresenterAgentExecutor(QuizPresenterAgent);

            return new WorkflowBuilder(WebContentDownloader)
                       .AddEdge(WebContentDownloader, QuizGenerator)
                       .AddEdge(QuizGenerator, QuizPresenter)
                       .AddEdge(QuizPresenter, inputExecutor)
                       .AddEdge(inputExecutor, QuizPresenter)
                       .WithOutputFrom(QuizPresenter)
                       .Build();
        }

        public async Task CleanupAgents()
        {
            // Cleanup agents.
            await persistentAgentsClient.Administration.DeleteAgentAsync(WebContentDownloaderAgent.Id);
            await persistentAgentsClient.Administration.DeleteAgentAsync(QuizGeneratorAgent.Id);
            await persistentAgentsClient.Administration.DeleteAgentAsync(QuizPresenterAgent.Id);
        }


        private static async Task<AIAgent> CreateWebContentDownloaderAgent(string deploymentName, PersistentAgentsClient persistentAgentsClient)
        {
            return await CreateFoundryAgent(deploymentName, "WebContentDownloader", """
            - Use HostedWebSearchTool to extract readable content from the internet
            - Strip ads, navigation, and irrelevant sections
            - Return title and cleaned body text
            """, persistentAgentsClient, [new HostedWebSearchTool()]);
        }

        private static async Task<AIAgent> CreateQuizGeneratorAgent(string deploymentName, PersistentAgentsClient persistentAgentsClient)
        {
            JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(QuestionAnswer));
            var responeFormat = ChatResponseFormatJson.ForJsonSchema(
                schema: schema,
                schemaName: "QuestionAnswers",
                schemaDescription: "Collection of questions and answers");


            return await CreateFoundryAgent(deploymentName, "QuizGenerator", """
            - Generate 5 factual questions based on the content
            - Include concise answers
            - Format the response as a array of objects with 'question' and 'answer' fields
            """, persistentAgentsClient, responseFormat: responeFormat);

        }

        private static async Task<AIAgent> CreateQuizPresenterAgent(string deploymentName, PersistentAgentsClient persistentAgentsClient)
        {

            return await CreateFoundryAgent(deploymentName, "QuizPresenter", """
            - Present one question at a time
            - Wait for user input
            - Compare user response to expected answer (basic match or semantic similarity)
            - Provide feedback: correct/incorrect + explanation
            - Score the quiz and summarize performance

            """, persistentAgentsClient);
        }

        private static async Task<AIAgent> CreateFoundryAgent(string deploymentName, string agentName, string agentInstructions, PersistentAgentsClient persistentAgentsClient, IList<AITool>? tools = null, ChatResponseFormatJson? responseFormat = null)
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
                ChatOptions = new()
                {
                    Tools = tools ?? [],
                    ResponseFormat = responseFormat
                }
            });
            return researcher;
        }
    }


    public class QuestionAnswer
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
