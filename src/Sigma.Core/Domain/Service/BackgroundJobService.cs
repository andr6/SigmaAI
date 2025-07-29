using Coravel.Queuing.Interfaces;
using Sigma.Core.Domain.Interface;

namespace Sigma.Core.Domain.Service
{
    public class BackgroundJobService
    {
        private readonly IQueue _queue;
        public BackgroundJobService(IQueue queue)
        {
            _queue = queue;
        }

        public void Enqueue(Func<Task> job)
        {
            _queue.QueueAsyncTask(job);
        }
    }
}
