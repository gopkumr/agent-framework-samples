using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace SequenceAgent.AgentServices
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
            _agent = agent;
            _thread = _agent.GetNewThread();
        }

        public async ValueTask<string> HandleAsync(string message, IWorkflowContext context)
        {
            var prompt = $"Topic: {message}";

            var response = await _agent.RunAsync(prompt, _thread);

            await context.AddEventAsync(new ContentPreparedEvent(response.Text));

            return response.Text;
        }
    }
}
