using JournalApp.Data;

namespace JournalApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            _ = JournalDatabase.InitAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "JournalApp" };
        }
    }
}
