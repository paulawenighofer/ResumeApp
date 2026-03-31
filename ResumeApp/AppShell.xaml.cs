namespace ResumeApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Views.EducationPage), typeof(Views.EducationPage));
            Routing.RegisterRoute(nameof(Views.ExperiencePage), typeof(Views.ExperiencePage));
            Routing.RegisterRoute(nameof(Views.SkillsPage), typeof(Views.SkillsPage));
            Routing.RegisterRoute(nameof(Views.ProjectsPage), typeof(Views.ProjectsPage));
        }
    }
}
