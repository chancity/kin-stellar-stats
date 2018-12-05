namespace Kin.Horizon.Api.Poller.Database
{
    public partial class AppInfo
    {
        public int AppId { get; set; }
        public string GooglePlay { get; set; }
        public string AppStore { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }

        public App App { get; set; }
    }
}
