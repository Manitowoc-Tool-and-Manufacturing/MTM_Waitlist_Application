namespace MTM_Waitlist_Application
{
    public partial class AppShell : Shell
    {
        public AppShell(Page dashboardPage, Page setupTechPage)
        {
            InitializeComponent();
            DashboardContent.Content = dashboardPage;
            SetupTechContent.Content = setupTechPage;
        }
    }
}
