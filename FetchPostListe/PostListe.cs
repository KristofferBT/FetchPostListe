using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FetchPostListe
{
    public class Extensions
    {
    }

    public class Dokumenter
    {
        public string Id { get; set; }
        public string Tittel { get; set; }
        public string Filendelse { get; set; }
        public bool Offentlig { get; set; }
        public bool Tilgjengelig { get; set; }
        public Extensions Extensions { get; set; }
    }


    public class PostListeClass
    {
        public string Id { get; set; }
        public string Avdeling { get; set; }
        public string SakTittel { get; set; }
        public string SakNr { get; set; }
        public string SakId { get; set; }
        public string Tittel { get; set; }
        public string DokumentNr { get; set; }
        public string DokumentType { get; set; }
        public string LopeNr { get; set; }
        public List<object> Avsendere { get; set; }
        public List<string> Mottakere { get; set; }
        public DateTime DokumentDato { get; set; }
        public DateTime JournalDato { get; set; }
        public DateTime PubliseringsDato { get; set; }
        public bool Offentlig { get; set; }
        public object Hjemmel { get; set; }
        public object Arkivkode { get; set; }
        public string SakKontaktNavn { get; set; }
        public object SakKontaktEpost { get; set; }
        public List<Dokumenter> Dokumenter { get; set; }
        public object Extensions { get; set; }
    }


}

public class RootObject
{
    public string uri { get; set; }
    public String response { get; set; }
}

