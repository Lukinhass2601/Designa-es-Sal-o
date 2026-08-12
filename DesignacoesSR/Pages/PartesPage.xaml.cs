using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class PartesPage : ContentPage
{
    private readonly DatabaseService _database;

    public PartesPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        listaPartes.ItemsSource =
            await _database.GetPartesAsync();
    }

    private async void EditarParte_Clicked(
        object sender,
        EventArgs e)
    {
    }

    private async void ExcluirParte_Clicked(
        object sender,
        EventArgs e)
    {
    }
}