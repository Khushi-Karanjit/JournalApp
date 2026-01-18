using JournalApp.Data;

using JournalApp.Services;

namespace JournalApp
{
    public partial class App : Application
    {
        public App(JournalDatabaseService databaseService)
        {
            InitializeComponent();
            _ = databaseService.EnsureInitializedAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "JournalApp" };
        }
    }
}
