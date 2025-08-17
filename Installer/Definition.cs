using CFIT.Installer.Product;

namespace Installer
{
    public class Definition : ProductDefinition
    {
        public Config Config { get { return BaseConfig as Config; } }
        public WorkerManager WorkerManager { get { return BaseWorker as WorkerManager; } }

        public Definition(string[] args) : base(args)
        {

        }

        protected override void CreateConfig()
        {
            BaseConfig = new Config();
        }

        protected override void CreateWorker()
        {
            BaseWorker = new WorkerManager(Config);
        }

        protected override void CreateWindowBehavior()
        {
            base.CreateWindowBehavior();
            BaseBehavior.WelcomeLogoWidth = 192;
            BaseBehavior.WelcomeLogoResource = "Payload/icon";
        }

        protected override void CreatePageConfig()
        {
            PageBehaviors.Add(InstallerPages.CONFIG, new ConfigPage());
        }

        protected override void CreatePageSummary()
        {
            PageBehaviors.Add(InstallerPages.SUMMARY, new SummaryPage());
        }
    }
}
