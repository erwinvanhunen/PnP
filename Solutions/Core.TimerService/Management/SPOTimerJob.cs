using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeDevPnP.TimerService.Enums;

namespace OfficeDevPnP.TimerService.Management
{
    public class SPOTimerJob
    {
        public string Title { get; set; }
        public DateTime LastRun { get; set; }
        public Guid Id { get; set; }

        public bool IsRunning { get; set; }

        public bool Enabled { get; set; }

        public ScheduleType ScheduleType { get; set; }
    }
}
