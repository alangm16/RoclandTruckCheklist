namespace RoclandTruckCheck.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("ChecklistPage", typeof(Views.ChecklistPage));
            Routing.RegisterRoute("TipoAcceso", typeof(Views.TipoAcceso));
        }
    }
}
