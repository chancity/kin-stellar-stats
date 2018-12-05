namespace Kin.Horizon.Api.Poller.Database
{
    public partial class OverallStats
    {
        public int AppId { get; set; }
        public long ActiveUsers { get; set; }
        public long CreatedWallets { get; set; }
        public long Operations { get; set; }
        public long Payments { get; set; }
        public long PaymentVolume { get; set; }

        public App App { get; set; }
    }
}
