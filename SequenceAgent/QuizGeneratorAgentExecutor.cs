using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace SequenceAgent
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
            this._agent = agent;
            this._thread = this._agent.GetNewThread();
        }


        public async ValueTask<QuestionsAnswers> HandleAsync(string message, IWorkflowContext context)
        {
            var prompt = $"Content: {message}";

            var response = await this._agent.RunAsync(prompt, this._thread);

            await context.AddEventAsync(new QuizQuestionAnswersEvent(response.Text));

            return new QuestionsAnswers(response.Text);
        }
    }
}
