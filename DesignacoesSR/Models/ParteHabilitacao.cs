using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesignacoesSR.Models;

public class ParteHabilitacao :
    INotifyPropertyChanged
{
    public int ParteId { get; set; }

    public string Nome { get; set; } =
        string.Empty;

    private bool _selecionado;

    public bool Selecionado
    {
        get => _selecionado;

        set
        {
            if (_selecionado == value)
                return;

            _selecionado = value;

            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName]
        string? propriedade = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propriedade));
    }
}