using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace SequenceAgent
{
    public class ContentPreparedEvent(string content) : WorkflowEvent
    {
        public string Content { get; set; } = content;
    }

    public class WebContentDownloaderAgentExecutor : ReflectingExecutor<WebContentDownloaderAgentExecutor>, IMessageHandler<string, string>
    {
        private readonly AIAgent _agent;
        private readonly AgentThread _thread;

        public WebContentDownloaderAgentExecutor(AIAgent agent) : base(agent.Id)
        {
            this._agent = agent;
            this._thread = this._agent.GetNewThread();
        }

        public async ValueTask<string> HandleAsync(string message, IWorkflowContext context)
        {
            var prompt = $"Topic: {message}";

            var response = await this._agent.RunAsync(prompt, this._thread);

            await context.AddEventAsync(new ContentPreparedEvent(response.Text));

            return response.Text;
        }
    }
}
