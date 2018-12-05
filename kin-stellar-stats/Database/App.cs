using System.Collections.Generic;

namespace Kin.Horizon.Api.Poller.Database
{
    public partial class App
    {
        public App()
        {
            AppStats = new HashSet<AppStats>();
            OverallStats = new OverallStats();
            AppInfo = new AppInfo();
        }

        public int Id { get; set; }
        public string AppId { get; set; }
        public string FriendlyName { get; set; }

        public AppInfo AppInfo { get; set; }
        public OverallStats OverallStats { get; set; }
        public ICollection<AppStats> AppStats { get; set; }
    }
}
