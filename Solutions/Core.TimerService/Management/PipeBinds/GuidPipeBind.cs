using System;

namespace OfficeDevPnP.TimerService.Management.PipeBinds
{
    public sealed class GuidPipeBind
    {
        private readonly Guid _id;

        public GuidPipeBind(Guid guid)
        {
            _id = guid;
        }

        public GuidPipeBind(string id)
        {
            _id = new Guid(id);
        }

        public Guid Id
        {
            get { return _id; }
        }

        public GuidPipeBind()
        {
            _id = Guid.Empty;
        }

    }
}
