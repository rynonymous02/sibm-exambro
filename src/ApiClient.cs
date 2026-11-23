using System;
using System.IO;
using System.Net;
using System.Text;

namespace SibmExambro
{
    public static class ApiClient
    {
        static ApiClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        public static bool GetSessionInfo(string baseUrl, string tokenOrCode, out string judul, out string message)
        {
            judul = "";
            message = "";
            try
            {
                string url = baseUrl.TrimEnd('/') + "/api/ujian/session-info?token=" + Uri.EscapeDataString(tokenOrCode.Trim());
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("X-SIBM-Exam-App", "SIBM-Desktop-Exambro");
                request.UserAgent = "SIBM-Desktop-ExamBrowser/1.0 (Windows NT; SIBM-Exambro)";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    if (json.Contains("\"status\":\"success\""))
                    {
                        judul = ExtractJsonField(json, "judul");
                        return true;
                    }
                    else
                    {
                        message = ExtractJsonMessage(json, "Sesi ujian tidak ditemukan.");
                        return false;
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        string errBody = reader.ReadToEnd();
                        message = ExtractJsonMessage(errBody, "Kode sesi ujian tidak ditemukan atau tidak aktif.");
                    }
                }
                else
                {
                    message = "Tidak dapat terhubung ke server SIBM: " + wex.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool VerifySupervisorPin(string baseUrl, string token, string pin, string namaPeserta, string deviceName, out string message)
        {
            message = "";
            try
            {
                string url = baseUrl.TrimEnd('/') + "/api/ujian/verify-pin";
                string jsonPayload = string.Format(
                    "{{\"pin\":\"{0}\",\"token\":\"{1}\",\"nama_peserta\":\"{2}\",\"device_name\":\"{3}\"}}",
                    EscapeJson(pin), EscapeJson(token), EscapeJson(namaPeserta), EscapeJson(deviceName)
                );

                string response = PostJson(url, jsonPayload);
                if (response.Contains("\"status\":\"success\""))
                {
                    message = "PIN Pengawas Valid.";
                    return true;
                }
                else
                {
                    message = ExtractJsonMessage(response, "Kode pengawas tidak valid.");
                    return false;
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        string errBody = reader.ReadToEnd();
                        message = ExtractJsonMessage(errBody, "PIN salah atau sudah kedaluwarsa.");
                    }
                }
                else
                {
                    message = wex.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static void SendActivityLog(string baseUrl, string token, string tipe, string keterangan, string namaPeserta, string deviceName)
        {
            try
            {
                string url = baseUrl.TrimEnd('/') + "/api/ujian/log";
                string jsonPayload = string.Format(
                    "{{\"token\":\"{0}\",\"tipe\":\"{1}\",\"keterangan\":\"{2}\",\"nama_peserta\":\"{3}\",\"device_name\":\"{4}\"}}",
                    EscapeJson(token), EscapeJson(tipe), EscapeJson(keterangan), EscapeJson(namaPeserta), EscapeJson(deviceName)
                );

                PostJson(url, jsonPayload);
            }
            catch (Exception)
            {
                // Silently ignore log sending errors to avoid interrupting exam
            }
        }

        private static string PostJson(string url, string json)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("X-SIBM-Exam-App", "SIBM-Desktop-Exambro");
            request.UserAgent = "SIBM-Desktop-ExamBrowser/1.0 (Windows NT; SIBM-Exambro)";

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            request.ContentLength = bytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }

        private static string ExtractJsonField(string json, string field)
        {
            try
            {
                string search = "\"" + field + "\":\"";
                int idx = json.IndexOf(search);
                if (idx >= 0)
                {
                    int start = idx + search.Length;
                    int end = json.IndexOf("\"", start);
                    if (end > start)
                    {
                        return json.Substring(start, end - start);
                    }
                }
            }
            catch (Exception) { }
            return "";
        }

        private static string ExtractJsonMessage(string json, string defaultMsg)
        {
            try
            {
                int idx = json.IndexOf("\"message\":");
                if (idx >= 0)
                {
                    int start = json.IndexOf("\"", idx + 10);
                    if (start >= 0)
                    {
                        int end = json.IndexOf("\"", start + 1);
                        if (end > start)
                        {
                            return json.Substring(start + 1, end - start - 1);
                        }
                    }
                }
            }
            catch (Exception) { }
            return defaultMsg;
        }
    }
}
