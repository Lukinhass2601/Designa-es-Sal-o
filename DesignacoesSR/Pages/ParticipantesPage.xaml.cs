using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class ParticipantesPage : ContentPage
{
    private readonly DatabaseService _database;

    public ParticipantesPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        listaParticipantes.ItemsSource =
            await _database.GetParticipantesAsync();
    }

    private async void EditarParticipante_Clicked(
        object sender,
        EventArgs e)
    {
    }

    private async void ExcluirParticipante_Clicked(
        object sender,
        EventArgs e)
    {
    }
}