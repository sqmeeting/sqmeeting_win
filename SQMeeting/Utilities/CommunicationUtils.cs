using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.Utilities
{
    class CommunicationUtils
    {
        public static string GetSecret(string plainText, string salt)
        {
            var hashed = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(plainText + salt));
            StringBuilder buildSHA1 = new StringBuilder();
            foreach (byte b in hashed)
            {
                buildSHA1.Append(b.ToString("X2"));
            }
            return buildSHA1.ToString().ToLower();
        }

        public static string MakeURLWithParams(string originURL, List<KeyValuePair<string, string>> parameters)
        {
            StringBuilder urlBuilder = new StringBuilder(originURL);
            urlBuilder.Append("?");
            foreach (var p in parameters)
            {
                urlBuilder = urlBuilder.Append(p.Key).Append("=").Append(p.Value).Append("&");
            }
            urlBuilder.Remove(urlBuilder.Length - 1, 1);
            return urlBuilder.ToString();
        }
    }
}
