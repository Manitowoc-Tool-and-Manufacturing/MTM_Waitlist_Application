namespace MTM_Waitlist_Application
{
    public partial class AppShell : Shell
    {
        public AppShell(Page dashboardPage)
        {
            InitializeComponent();
            DashboardContent.Content = dashboardPage;
        }
    }
}
