using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace SequenceAgent
{
    public class QuestionEvent(string question) : WorkflowEvent
    {
        public string Question { get; set; } = question;
    }

    public class QuizPresenterAgentExecutor :
        ReflectingExecutor<QuizPresenterAgentExecutor>,
        IMessageHandler<QuestionsAnswers>,
        IMessageHandler<string>
    {
        private readonly AIAgent _agent;
        private readonly AgentThread _thread;
        private int _questions = 1;

        public QuizPresenterAgentExecutor(AIAgent agent) : base(agent.Id)
        {
            this._agent = agent;
            this._agent = agent;
            this._thread = this._agent.GetNewThread();
        }

        public async ValueTask HandleAsync(string message, IWorkflowContext context)
        {
            var prompt = $"Here is the answer: {message}. Validate the answer";

            if (_questions >= 5)
            {
                prompt += ".This is the last answer. Prepare a summary of the result and end the quiz. Ask no more questions.";
            }
            else
                prompt += "and ask the next question.";

            var response = await this._agent.RunAsync(prompt, this._thread);
            await context.AddEventAsync(new QuestionEvent(response.Text));

            if (_questions >= 5)
            {
                await context.YieldOutputAsync(response.Text);
                return;
            }
                       
            _questions++;
            await context.SendMessageAsync(response.Text);
        }

        public async ValueTask HandleAsync(QuestionsAnswers message, IWorkflowContext context)
        {
            _questions = 1;

            var prompt = $"Here are the questions and answers: {message.QuestionAnswers}";

            var response = await this._agent.RunAsync(prompt, this._thread);

            await context.AddEventAsync(new QuestionEvent(response.Text));

            await context.SendMessageAsync(response.Text);
        }
    }
}
