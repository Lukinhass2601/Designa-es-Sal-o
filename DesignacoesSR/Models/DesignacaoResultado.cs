using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesignacoesSR.Models;

public class DesignacaoResultado :
    INotifyPropertyChanged
{
    public int ParteSemanaId { get; set; }

    public int ParteId { get; set; }

    public int Numero { get; set; }

    public string Parte { get; set; } =
        string.Empty;

    public string Descricao { get; set; } =
        string.Empty;

    public int DuracaoMinutos { get; set; }

    private int _participante1Id;

    public int Participante1Id
    {
        get => _participante1Id;

        set
        {
            if (_participante1Id == value)
                return;

            _participante1Id = value;

            OnPropertyChanged();
        }
    }

    private string _participante1 =
        string.Empty;

    public string Participante1
    {
        get => _participante1;

        set
        {
            if (_participante1 == value)
                return;

            _participante1 = value;

            OnPropertyChanged();
        }
    }

    private int _participante2Id;

    public int Participante2Id
    {
        get => _participante2Id;

        set
        {
            if (_participante2Id == value)
                return;

            _participante2Id = value;

            OnPropertyChanged();
            OnPropertyChanged(
                nameof(TemParticipante2));
        }
    }

    private string _participante2 =
        string.Empty;

    public string Participante2
    {
        get => _participante2;

        set
        {
            if (_participante2 == value)
                return;

            _participante2 = value;

            OnPropertyChanged();
            OnPropertyChanged(
                nameof(TemParticipante2));
        }
    }

    public bool TemParticipante2
    {
        get
        {
            return Participante2Id > 0 &&
                   !string.IsNullOrWhiteSpace(
                       Participante2);
        }
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propriedade = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propriedade));
    }
}