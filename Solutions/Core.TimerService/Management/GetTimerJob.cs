using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OfficeDevPnP.TimerService.Enums;
using OfficeDevPnP.TimerService.Management.PipeBinds;

namespace OfficeDevPnP.TimerService.Management
{
    [Cmdlet(VerbsCommon.Get, "SPOTimerJob")]
    public class GetTimerJob : BaseCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipeline = true, Position = 0, HelpMessage = "The ID or Title of the list.")]
        public GuidPipeBind Identity;

        protected override void ExecuteCmdlet()
        {
            List<SPOTimerJob> timerJobs = new List<SPOTimerJob>();

            if (MyInvocation.BoundParameters.ContainsKey("Identity"))
            {
                WriteObject(jobs.JobCollection.FirstOrDefault(x => x.Id == Identity.Id));
            }
            else
            {
                WriteObject(jobs.JobCollection, true);
            }
        }
    }
}
