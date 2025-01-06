using Domen.Enumi;

namespace Domen.Modeli
{
    [Serializable]
    public class Sto
    {
        private int BrojStola {  get; set; }
        private int BrojGostiju { get; set; }
        private StatusStola Status {  get; set; }
        private List<Porudzbina> Porudzbine = new List<Porudzbina>();
    }
}
