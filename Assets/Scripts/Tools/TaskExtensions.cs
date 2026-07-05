using System.Threading.Tasks;

namespace KingdomWar.Tools
{
    public static class TaskExtensions
    {
        public static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
        }
    }
}
