using Microsoft.Azure.WebJobs;

namespace OnlineAdService.WebJob
{
    class Program
    {
        static void Main()
        {
            var host = new JobHost();
            host.RunAndBlock();
        }
    }
}
