using System.ServiceProcess;

namespace OfficeDevPnP.TimerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[] 
            { 
                new SPOTimerService() 
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
