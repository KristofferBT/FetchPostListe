using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Web.Script.Serialization;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;

namespace FetchPostListe
{


    class PostListe
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Input command (/y /d /YM /Missing (Year/Day/YearMonth))");
            string command = Console.ReadLine();
            string day = "";
            int year = 2018;
            int month = 1;
            int daysback = 31;
            string mode = "day";
            switch (command)
            {
                case "/y":
                    Console.WriteLine("Input year to import: ");
                    year = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("Importing " + year);
                    mode = "year";
                    break;

                case "/d":
                    Console.WriteLine("Input day to import(yyyy-DD-mm): ");
                    day = Console.ReadLine();
                    Console.WriteLine("Importing " + day);
                    mode = "Day";
                    break;

                case "/YM":
                    Console.WriteLine("Input year: ");
                    year = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("Input month: ");
                    month = Convert.ToInt32(Console.ReadLine());
                    mode = "YM";
                    break;

                case "/Missing":
                    Console.WriteLine("Type how many days back to check: ");
                    daysback = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("Importing data from " + daysback + " days back");
                    mode = "Missing";
                    break;

                    


                default:
                    break;
            }

            if(mode == "YM")
            {
                GetDatesMonth(year, month);
            }
            if(mode == "Day")
            {
                FetchData(day);
            }
            if(mode == "year")
            {
                GetDatesYear(year);
            }

            if(mode == "Missing")
            {
                Console.WriteLine("Got into Missing mode");
                //Get missing dates from sql proc
                var connection = System.Configuration.ConfigurationManager.ConnectionStrings["PostListe"].ConnectionString;
                using (SqlConnection db = new SqlConnection(connection))
                {
                    db.Open();

                    SqlCommand GetMissingDates = new SqlCommand(@"[PostListe].[dbo].[GetMissingDays]", db);
                    Console.WriteLine(GetMissingDates);
                    GetMissingDates.Parameters.AddWithValue("@DaysBack", daysback);
                    GetMissingDates.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter MissingDatesResult = new System.Data.SqlClient.SqlDataAdapter(GetMissingDates);
                    DataSet ds = new DataSet();
                    MissingDatesResult.Fill(ds);

                    Console.WriteLine(ds.Tables[0].DefaultView);

                    foreach (DataTable table in ds.Tables)
                    {
                        foreach (DataRow dr in table.Rows)
                        {
                            Console.WriteLine(dr["DateLookup"].ToString());
                            string CleanDate = dr["DateLookup"].ToString().Replace(" 00.00.00","");
                            FetchData(CleanDate);
                        }
                        
                    }

                    db.Close();
                }
            }

           Console.ReadKey();
        }


        public static string GetDatesMonth(int year, int month)
        {
            DateTime dt = new DateTime(year, month, 1);
            while (dt.Month == month)
            {
                Console.WriteLine(dt.ToString("yyyy-MM-dd"));
                //Console.ReadKey();
                FetchData((dt.ToString("yyyy-MM-dd")));
                Console.WriteLine("Done with " + dt.ToString("yyyy-MM-dd"));
                dt = dt.AddDays(1);
                Console.WriteLine("Next date:" + dt.ToString("yyyy-MM-dd"));
               // Console.ReadKey();
                //return (dt.ToString("yyyy-MM-dd"));
            }
            
            return (dt.ToString("yyyy-MM-dd"));

        }

        public static string GetDatesYear(int year)
        {
            DateTime dt = new DateTime(year,1, 1);
            while (dt.Year == year)
            {
                Console.WriteLine(dt.ToString("yyyy-MM-dd"));
                //Console.ReadKey();
                FetchData((dt.ToString("yyyy-MM-dd")));
                Console.WriteLine("Done with " + dt.ToString("yyyy-MM-dd"));
                dt = dt.AddDays(1);
                Console.WriteLine("Next date:" + dt.ToString("yyyy-MM-dd"));

            }

            return (dt.ToString("yyyy-MM-dd"));

        }

        private static void FetchData(string Day)
        {
            var connection = ConfigurationManager.ConnectionStrings["PostListe"].ConnectionString;

            using (SqlConnection db = new SqlConnection(connection))
            {
                db.Open();

                string Url = "https://postliste.porsgrunn.kommune.no/api/postliste/" + Day + "/" + Day + "/";
                Console.WriteLine("Importing data from: " + Url);
                //string Url = "https://postliste.porsgrunn.kommune.no/api/postliste/2018-04-12/2018-04-12/";
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(Url);
                httpWebRequest.Method = WebRequestMethods.Http.Get;
                httpWebRequest.Accept = "application/json";

                string JsonResponse;

                var response = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    JsonResponse = sr.ReadToEnd();
                }
                if (JsonResponse.Length > 100)
                {
                    Console.WriteLine("Response contains data");


                    //string Date = DateTime.Now.AddDays(-4).ToString("yyyy-MM-ddT00:00:00");
                    //Date = Date.Replace('.', ':');
                    string Date = Day + "T00:00:00";
                    Console.WriteLine(Date);
                    JsonResponse = JsonResponse.Replace("\"Extensions\": {}", "\"Extensions\": null");

                    Dictionary<string, object> header = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonResponse);
                    Dictionary<string, object> responsData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(header["response"].ToString());
                    Dictionary<string, object> responsData1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(responsData[Date.ToString()].ToString());


                    Console.WriteLine("Utgående post");

                    //Sjekke om JsonResponse inneholder utgående post
                    if (JsonResponse.Contains("\"U\": [") == true)
                    {
                        string Retning = "U";
                        List<PostListeClass> Utmail = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PostListeClass>>(responsData1["U"].ToString());
                        int UtMailArrayCounter = 0;
                        foreach (PostListeClass email in Utmail)
                        {
                            
                            Console.WriteLine("Sak");
                            if (email.SakKontaktEpost == null)
                            {
                                email.SakKontaktEpost = "null";
                            }

                            SqlCommand InsertSak = new SqlCommand(@"INSERT INTO [dbo].[PostListeSak]
           ([EksternId]
           ,[Retning]
           ,[Avdeling]
           ,[SakTittel]
           ,[SakNr]
           ,[SakId]
           ,[Tittel]
           ,[DokumentNr]
           ,[LopeNr]
           ,[DokumentDato]
           ,[JournalDato]
           ,[PubliseringsDato]
           ,[SakKontaktNavn]
           ,[SakKontaktEpost])
            
            Values (
                    @EksternId
                    ,'Utgående'
                    ,@Avdeling
                    ,@SakTittel
                    ,@SakNr
                    ,@SakId
                    ,@Tittel
                    ,@DokumentNr
                    ,@LopeNr
                    ,@DokumentDato
                    ,@JournalDato
                    ,@PubliseringsDato
                    ,@SakKontaktNavn
                    ,@SakKontaktEpost
                    )", db);
                            InsertSak.Parameters.AddWithValue("@EksternId", email.Id);
                            InsertSak.Parameters.AddWithValue("@Avdeling", email.Avdeling);
                            InsertSak.Parameters.AddWithValue("@SakTittel", email.SakTittel);
                            InsertSak.Parameters.AddWithValue("@SakNr", email.SakNr);
                            InsertSak.Parameters.AddWithValue("@SakId", email.SakId);
                            InsertSak.Parameters.AddWithValue("@Tittel", email.Tittel);
                            InsertSak.Parameters.AddWithValue("@DokumentNr", email.DokumentNr);
                            InsertSak.Parameters.AddWithValue("@LopeNr", email.LopeNr);
                            InsertSak.Parameters.AddWithValue("@DokumentDato", email.DokumentDato);
                            InsertSak.Parameters.AddWithValue("@JournalDato", email.JournalDato);
                            InsertSak.Parameters.AddWithValue("@PubliseringsDato", email.PubliseringsDato);
                            InsertSak.Parameters.AddWithValue("@SakKontaktNavn", email.SakKontaktNavn);
                            InsertSak.Parameters.AddWithValue("@SakKontaktEpost", email.SakKontaktEpost);
                            InsertSak.ExecuteNonQuery();

                            Console.WriteLine(email.Id);
                            Console.WriteLine(email.SakTittel);
                            Console.WriteLine(email.Avdeling);

                            //Avsendere
                            if (email.Avsendere != null)
                            {
                                Console.WriteLine("Avsendere");
                                foreach (object Avsender in email.Avsendere)
                                {
                                    if (Avsender != null)
                                    {
                                        Console.WriteLine(email.SakNr);
                                        SqlCommand insertAvsender = new SqlCommand(@"
INSERT INTO [dbo].[PostListeKommunikasjon]
           ([SakEksternId]
           ,[KommunikasjonsType]
           ,[Navn])
     VALUES
           (@SakEksternId
           ,@KommunikasjonsType
           ,@Navn)", db);

                                        insertAvsender.Parameters.AddWithValue("@SakEksternId", email.Id);
                                        insertAvsender.Parameters.AddWithValue("@KommunikasjonsType", "Avsender");
                                        if (Avsender == null)
                                        {
                                            insertAvsender.Parameters.AddWithValue("@Navn", "NULL");

                                        }
                                        else
                                        {
                                            insertAvsender.Parameters.AddWithValue("@Navn", Avsender);
                                        }
                                        
                                        insertAvsender.ExecuteNonQuery();

                                        Console.WriteLine("Avsender = " + Avsender.ToString());

                                    }
                                    else
                                    {
                                        SqlCommand insertAvsender = new SqlCommand(@"
INSERT INTO [dbo].[PostListeKommunikasjon]
           ([SakEksternId]
           ,[KommunikasjonsType]
           ,[Navn])
     VALUES
           (@SakEksternId
           ,@KommunikasjonsType
           ,@Navn)", db);

                                        insertAvsender.Parameters.AddWithValue("@SakEksternId", email.Id);
                                        insertAvsender.Parameters.AddWithValue("@KommunikasjonsType", "Avsender");
                                        insertAvsender.Parameters.AddWithValue("@Navn", email.Avdeling);
                                        insertAvsender.ExecuteNonQuery();
                                    }


                                }
                            }

                            //Mottakere
                            if (email.Mottakere != null)
                            {
                                Console.WriteLine("Mottakere");
                                foreach (object Mottaker in email.Mottakere)
                                {
                                    string MottakerClean = "";
                                    if (Mottaker != null)
                                    {
                                        Console.WriteLine("Mottaker = " + Mottaker.ToString());
                                        MottakerClean = Mottaker.ToString().Replace("'", "");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Mottaker = " + email.Avdeling.ToString());
                                    }

                                    SqlCommand insertMottaker = new SqlCommand(@"
INSERT INTO [dbo].[PostListeKommunikasjon]
           ([SakEksternId]
           ,[KommunikasjonsType]
           ,[Navn])
     VALUES
           (@SakEksternId
           ,@KommunikasjonsType
           ,@Navn)", db);

                                    insertMottaker.Parameters.AddWithValue("@SakEksternId", email.Id);
                                    insertMottaker.Parameters.AddWithValue("@KommunikasjonsType", "Mottaker");
                                    insertMottaker.Parameters.AddWithValue("@Navn", MottakerClean);
                                    insertMottaker.ExecuteNonQuery();

                                }
                            }
                            //Dokumenter
                            if (email.Dokumenter != null)
                            {
                                Console.WriteLine("Dokumenter");
                                int DokumentArrayCount = 0;
                                foreach (Dokumenter docs in email.Dokumenter)
                                {
                                    
                                    SqlCommand insertDokument = new SqlCommand(@"
INSERT INTO [dbo].[PostListeDokumenter]
           ([SakEksternId]
           ,[DokumentId]
           ,[DokumentTittel]
           ,[Filendelse]
           ,[URL])
     VALUES
           (@SakEksternId
           ,@DokumentId
           ,@DokumentTittel
           ,@Filendelse
           ,@Url)", db);
                                    insertDokument.Parameters.AddWithValue("@SakEksternId", email.Id);
                                    insertDokument.Parameters.AddWithValue("@DokumentId", docs.Id);
                                    insertDokument.Parameters.AddWithValue("@DokumentTittel", docs.Tittel);
                                    insertDokument.Parameters.AddWithValue("@Filendelse", docs.Filendelse);
                                    insertDokument.Parameters.AddWithValue("@Url", "https://postliste.porsgrunn.kommune.no/api/postliste/"+Day+ "/_//"+Retning+"/"+UtMailArrayCounter+"/"+DokumentArrayCount);

                                    insertDokument.ExecuteNonQuery();

                                    Console.WriteLine("ID = " + docs.Id);
                                    Console.WriteLine("Tittel = " + docs.Tittel);
                                    Console.WriteLine("Filendelse = " + docs.Filendelse);
                                    Console.WriteLine("Offentlig = " + docs.Offentlig);
                                    Console.WriteLine("Tilgjengelig = " + docs.Tilgjengelig);
                                    DokumentArrayCount = DokumentArrayCount + 1;
                                }
                            }
                            UtMailArrayCounter = UtMailArrayCounter + 1;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ingen utgående post");
                    }



                    Console.WriteLine("\n");

                    db.Close();
                    db.Open();
                    Console.WriteLine("\n\n\n");
                    Console.WriteLine("Inngående post");
                    Console.WriteLine("\n\n\n");
                    if (JsonResponse.Contains("\"I\": [") == true)
                    {
                        string Retning = "I";


                        List<PostListeClass> Innmail = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PostListeClass>>(responsData1["I"].ToString());
                        int MailArrayCounter = 0;
                        foreach (PostListeClass email in Innmail)
                        {
                            Console.WriteLine("Sak");
                            if (email.SakKontaktEpost == null)
                            {
                                email.SakKontaktEpost = "null";
                            }

                            SqlCommand InsertSak = new SqlCommand(@"INSERT INTO [dbo].[PostListeSak]
           ([EksternId]
           ,[Retning]
           ,[Avdeling]
           ,[SakTittel]
           ,[SakNr]
           ,[SakId]
           ,[Tittel]
           ,[DokumentNr]
           ,[LopeNr]
           ,[DokumentDato]
           ,[JournalDato]
           ,[PubliseringsDato]
           ,[SakKontaktNavn]
           ,[SakKontaktEpost])
            
            Values (
                    @EksternId
                    ,'Inngående'
                    ,@Avdeling
                    ,@SakTittel
                    ,@SakNr
                    ,@SakId
                    ,@Tittel
                    ,@DokumentNr
                    ,@LopeNr
                    ,@DokumentDato
                    ,@JournalDato
                    ,@PubliseringsDato
                    ,@SakKontaktNavn
                    ,@SakKontaktEpost
                    )", db);
                            InsertSak.Parameters.AddWithValue("@EksternId", email.Id);
                            InsertSak.Parameters.AddWithValue("@Avdeling", email.Avdeling);
                            InsertSak.Parameters.AddWithValue("@SakTittel", email.SakTittel);
                            InsertSak.Parameters.AddWithValue("@SakNr", email.SakNr);
                            InsertSak.Parameters.AddWithValue("@SakId", email.SakId);
                            InsertSak.Parameters.AddWithValue("@Tittel", email.Tittel);
                            InsertSak.Parameters.AddWithValue("@DokumentNr", email.DokumentNr);
                            InsertSak.Parameters.AddWithValue("@LopeNr", email.LopeNr);
                            InsertSak.Parameters.AddWithValue("@DokumentDato", email.DokumentDato);
                            InsertSak.Parameters.AddWithValue("@JournalDato", email.JournalDato);
                            InsertSak.Parameters.AddWithValue("@PubliseringsDato", email.PubliseringsDato);
                            InsertSak.Parameters.AddWithValue("@SakKontaktNavn", email.SakKontaktNavn);
                            InsertSak.Parameters.AddWithValue("@SakKontaktEpost", email.SakKontaktEpost);
                            InsertSak.ExecuteNonQuery();

                            Console.WriteLine(email.Id);
                            Console.WriteLine(email.SakTittel);
                            Console.WriteLine(email.Avdeling);

                            //Avsendere
                            if (email.Avsendere != null)
                            {
                                Console.WriteLine("Avsendere");
                                foreach (object Avsender in email.Avsendere)
                                {
                                    if (Avsender != null)
                                    {
                                        Console.WriteLine(email.SakNr);
                                        SqlCommand insertAvsender = new SqlCommand(@"
INSERT INTO [dbo].[PostListeKommunikasjon]
           ([SakEksternId]
           ,[KommunikasjonsType]
           ,[Navn])
     VALUES
           (@SakEksternId
           ,@KommunikasjonsType
           ,@Navn)", db);

                                        insertAvsender.Parameters.AddWithValue("@SakEksternId", email.Id);
                                        insertAvsender.Parameters.AddWithValue("@KommunikasjonsType", "Avsender");
                                        if (Avsender == null)
                                        {
                                            insertAvsender.Parameters.AddWithValue("@Navn", "NULL");

                                        }
                                        else
                                        {
                                            insertAvsender.Parameters.AddWithValue("@Navn", Avsender);
                                        }

                                        insertAvsender.ExecuteNonQuery();

                                        Console.WriteLine("Avsender = " + Avsender.ToString());

                                    }
                                    else
                                    {
                                        SqlCommand insertAvsender = new SqlCommand(@"
                                INSERT INTO [dbo].[PostListeKommunikasjon]
                                           ([SakEksternId]
                                           ,[KommunikasjonsType]
                                           ,[Navn])
                                     VALUES
                                           (@SakEksternId
                                           ,@KommunikasjonsType
                                           ,@Navn)", db);

                                        insertAvsender.Parameters.AddWithValue("@SakEksternId", email.Id);
                                        insertAvsender.Parameters.AddWithValue("@KommunikasjonsType", "Avsender");
                                        insertAvsender.Parameters.AddWithValue("@Navn", email.Avdeling);
                                        insertAvsender.ExecuteNonQuery();
                                    }


                                }
                            }

                            //Mottakere
                            if (email.Mottakere != null)
                            {
                                Console.WriteLine("Mottakere");
                                foreach (object Mottaker in email.Mottakere)
                                {
                                    string MottakerClean = "";
                                    if (Mottaker != null)
                                    {
                                        Console.WriteLine("Mottaker = " + Mottaker.ToString());
                                        MottakerClean = Mottaker.ToString().Replace("'", "");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Mottaker = " + email.Avdeling.ToString());
                                    }

                                    SqlCommand insertMottaker = new SqlCommand(@"
INSERT INTO [dbo].[PostListeKommunikasjon]
           ([SakEksternId]
           ,[KommunikasjonsType]
           ,[Navn])
     VALUES
           (@SakEksternId
           ,@KommunikasjonsType
           ,@Navn)", db);

                                    insertMottaker.Parameters.AddWithValue("@SakEksternId", email.Id);
                                    insertMottaker.Parameters.AddWithValue("@KommunikasjonsType", "Mottaker");
                                    insertMottaker.Parameters.AddWithValue("@Navn", MottakerClean);
                                    insertMottaker.ExecuteNonQuery();

                                }
                            }
                            //Dokumenter
                            if (email.Dokumenter != null)
                            {
                                Console.WriteLine("Dokumenter");
                                int DokumentArrayCount = 0;
                                foreach (Dokumenter docs in email.Dokumenter)
                                {

                                    SqlCommand insertDokument = new SqlCommand(@"
INSERT INTO [dbo].[PostListeDokumenter]
           ([SakEksternId]
           ,[DokumentId]
           ,[DokumentTittel]
           ,[Filendelse]
           ,[URL])
     VALUES
           (@SakEksternId
           ,@DokumentId
           ,@DokumentTittel
           ,@Filendelse
           ,@Url)", db);
                                    insertDokument.Parameters.AddWithValue("@SakEksternId", email.Id);
                                    insertDokument.Parameters.AddWithValue("@DokumentId", docs.Id);
                                    insertDokument.Parameters.AddWithValue("@DokumentTittel", docs.Tittel);
                                    insertDokument.Parameters.AddWithValue("@Filendelse", docs.Filendelse);
                                    insertDokument.Parameters.AddWithValue("@Url", "https://postliste.porsgrunn.kommune.no/api/postliste/" + Day + "/_//" + Retning + "/" + MailArrayCounter + "/" + DokumentArrayCount);

                                    insertDokument.ExecuteNonQuery();

                                    Console.WriteLine("ID = " + docs.Id);
                                    Console.WriteLine("Tittel = " + docs.Tittel);
                                    Console.WriteLine("Filendelse = " + docs.Filendelse);
                                    Console.WriteLine("Offentlig = " + docs.Offentlig);
                                    Console.WriteLine("Tilgjengelig = " + docs.Tilgjengelig);
                                    DokumentArrayCount++;
                                }
                            }


                            Console.WriteLine("\n");
                            MailArrayCounter++;
                        }

                    }else
                    {
                        Console.WriteLine("Ingen inngående post");
                    }
                }
                else
                {
                    Console.WriteLine("Empty response");
                }
            }
            

        }
        
        }
}

      
    
    

