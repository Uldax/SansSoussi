using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.Security;
using System.Data;
using System.Text;

namespace SansSoussi.Controllers
{
    public class HomeController : Controller
    {
        SqlConnection _dbConnection;
        public HomeController()
        {
             _dbConnection = new SqlConnection(WebConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString);
        }

        public ActionResult Index()
        {
            ViewBag.Message = "Parce que marcher devrait se faire SansSoussi";

            return View();
        }

        public ActionResult Comments()
        {
            List<string> comments = new List<string>();

            //Get current user from default membership provider
            MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
            if (user != null)
            {
                //Add request param : https://msdn.microsoft.com/fr-fr/library/system.data.sqlclient.sqlcommand.prepare(v=vs.110).aspx
                SqlCommand cmd = new SqlCommand(null, _dbConnection);
                // Create and prepare an SQL statement.
                cmd.CommandText ="Select Comment from Comments where UserId = @user_id";
                SqlParameter idParam = new SqlParameter("@user_id", SqlDbType.UniqueIdentifier);
                idParam.Value = user.ProviderUserKey;
                cmd.Parameters.Add(idParam);
                _dbConnection.Open();
                // Call Prepare after setting the Commandtext and Parameters.
                cmd.Prepare();
                SqlDataReader rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    comments.Add(rd.GetString(0));
                }

                rd.Close();
                _dbConnection.Close();
            }
            return View(comments);
        }

        [HttpPost]
        //Do not allow html content
        [ValidateInput(true)]
        //XSRF protection
        [ValidateAntiForgeryToken]
        public ActionResult Comments(string comment)
        {
            string status = "success";
            try
            {
                //Get current user from default membership provider
                MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
                if (user != null)
                {
                    // Retrait des caractéres parasites (Double vérif avec ValidateInput)
                    //comment = RemoveTroublesCharacters(comment);
                    if (comment.Length < 1)
                        throw new MyException("Votre message est vide");

                    //add new comment to db
                    //clean the string and prepare request must be good 
                    SqlCommand cmd = new SqlCommand(
                    "Insert into [Comments] ([UserId], [CommentId], [Comment]) Values (@user_id, @comment_id, @comment)", _dbConnection);

                    cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier));
                    cmd.Parameters.Add(new SqlParameter("@comment_id", SqlDbType.UniqueIdentifier));
                    //La méthode SqlCommand.Prepare requiert que tous les paramètres de longueur variable aient une valeur Size explicitement définie différente de zéro
                    cmd.Parameters.Add(new SqlParameter("@comment", SqlDbType.VarChar, 256));

                    cmd.Parameters["@user_id"].Value = user.ProviderUserKey;
                    cmd.Parameters["@comment_id"].Value = Guid.NewGuid();
                    cmd.Parameters["@comment"].Value = comment;

                    _dbConnection.Open();
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
              
                }
                else
                {
                    throw new MyException("Vous devez vous connecter");
                }
            }
            catch (MyException ex)
            {
                status = ex.Message;
            }
            catch (Exception ex) //Récupération des erreurs (SQL & autres) -> ne pas afficher les informations
            {
                status = "Erreur durant l'envoie de votre message";
            }
            finally
            {
                _dbConnection.Close();
            }

            return Json(status);
        }

        public ActionResult Search(string searchData)
        {
            List<string> searchResults = new List<string>();

            //Get current user from default membership provider
            MembershipUser user = Membership.Provider.GetUser(HttpContext.User.Identity.Name, true);
            if (user != null)
            {
                if (!string.IsNullOrEmpty(searchData))
                {
                    SqlCommand cmd = new SqlCommand("Select Comment from Comments where UserId = @user_id and Comment like '%' + @search_data + '%'", _dbConnection);

                    cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier));
                    cmd.Parameters.Add(new SqlParameter("@search_data", SqlDbType.VarChar, 256));

                    cmd.Parameters["@user_id"].Value = user.ProviderUserKey;
                    cmd.Parameters["@search_data"].Value = searchData;

                    _dbConnection.Open();
                    cmd.Prepare();
                    SqlDataReader rd = cmd.ExecuteReader();


                    while (rd.Read())
                    {
                        searchResults.Add(rd.GetString(0));
                    }

                    rd.Close();
                    _dbConnection.Close();
                }
            }
            return View(searchResults);
        }

        [HttpGet]
        public ActionResult Emails()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Emails(object form)
        {
            List<string> searchResults = new List<string>();

            HttpCookie cookie = HttpContext.Request.Cookies["username"];
            
            List<string> cookieString = new List<string>();

            //Decode the cookie from base64 encoding
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(cookie.Value);
            string strCookieValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);

            //get user role base on cookie value
            string[] roles = Roles.GetRolesForUser(strCookieValue);
            bool isAdmin = roles.Contains("admin");

            if (isAdmin)
            {
                SqlCommand cmd = new SqlCommand("Select Email from aspnet_Membership", _dbConnection);
                _dbConnection.Open();
                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    searchResults.Add(rd.GetString(0));
                }
                rd.Close();
                _dbConnection.Close();
            }


            return Json(searchResults);
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Error()
        {
            return View();
        }

        public ActionResult JsError()
        {
            return Json("Erreur durant l'envoie de votre message");
        }

        // Résumé :
        //     Retire les caractères pouvant causser des soucis du la chaine de caractères
        //
        // Paramètres :
        //   inputString:
        //     Chaine à traiter
        public static string RemoveTroublesCharacters(string inputString)
        {
            if (inputString == null) return null;

            string WhiteListCaractere = "abcdefghijklmnopqrstuvwxyz0123456789.!:?,'";

            StringBuilder outString = new StringBuilder();
            char caract;

            for (int i = 0; i < inputString.Length; i++)
            {
                caract = inputString[i];

                // 5 pour StringComparison.OrdinalIgnoreCase
                if (WhiteListCaractere.IndexOf(caract, 5) != -1)
                    outString.Append(caract);
            }
            return outString.ToString();
        }

        private class MyException : Exception
        {
            public MyException()
            { }

            public MyException(string message) : base(message)
            { }

            public MyException(string message, Exception innerException) : base(message, innerException)
            { }
        }
    }
}
