using DesignacoesSR.Pages;

namespace DesignacoesSR;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
    nameof(HabilitacoesPage),
    typeof(HabilitacoesPage));

    }
}