using Domen.Enumi;

namespace Domen.Modeli
{
    [Serializable]
    public class Sto
    {
        public int BrojStola {  get; set; }
        public int BrojGostiju { get; set; }
        public StatusStola Status {  get; set; }
        public List<Porudzbina> Porudzbine = new List<Porudzbina>();

        public Sto(int brojStola, int brojGostiju, StatusStola status)
        {
            BrojStola = brojStola;
            BrojGostiju = brojGostiju;
            Status = status;
        }
    }
}
