using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace SequenceAgent.AgentServices
{
    public class QuizQuestionAnswersEvent(string questionAnswers) : WorkflowEvent
    {
        public string QuestionAnswers { get; set; } = questionAnswers;
    }

    public class QuestionsAnswers(string questionAnswers)
    {
        public string QuestionAnswers { get; set; } = questionAnswers;
    }

    public class QuizGeneratorAgentExecutor : ReflectingExecutor<QuizGeneratorAgentExecutor>, IMessageHandler<string, QuestionsAnswers>
    {
        private readonly AIAgent _agent;
        private readonly AgentThread _thread;

        public QuizGeneratorAgentExecutor(AIAgent agent) : base(agent.Id)
        {
            _agent = agent;
            _thread = _agent.GetNewThread();
        }


        public async ValueTask<QuestionsAnswers> HandleAsync(string message, IWorkflowContext context)
        {
            var prompt = $"Content: {message}";

            var response = await _agent.RunAsync(prompt, _thread);

            await context.AddEventAsync(new QuizQuestionAnswersEvent(response.Text));

            return new QuestionsAnswers(response.Text);
        }
    }
}
