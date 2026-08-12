using System.ComponentModel;

namespace DesignacoesSR.Models;

public class ParteHabilitacao : INotifyPropertyChanged
{
    public int ParteId { get; set; }

    public string Nome { get; set; } = string.Empty;

    private bool _selecionado;

    public bool Selecionado
    {
        get => _selecionado;
        set
        {
            _selecionado = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Selecionado)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}