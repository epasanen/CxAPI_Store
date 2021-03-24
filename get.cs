using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace CxAPI_Store
{
    class get
    {
        public bool get_Http(resultClass token, string path, int timeout = 30, string version = "v=1.0")
        {
            token.status = -1;
            try
            {
                HttpClient client = Configuration._HttpClient(token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", String.Format("application/json;{0}",version));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.bearer_token);
                client.Timeout = new TimeSpan(0, 0, timeout);
                var response = client.GetAsync(path).Result;
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        if (token.debug)
                        {
                            Console.WriteLine(String.Format("Results found: {0}", path));
                        }
                        token.op_result = response.Content.ReadAsStringAsync().Result;

                        token.status = 0;
                        return true;
                    }
                    else
                    {
                        Console.Error.Write(response);
                        return false;
                    }

                }
                else
                {
                    Console.Error.Write("null returned get_http");
                    return false;
                }
            }
            catch (Exception ex)
            {
                token.status = -1;
                token.statusMessage = ex.Message;
                Console.Error.WriteLine("get_http {0}", ex.Message);
            }
            return false;
        }

    }
    class post
    {
        public bool post_Http(resultClass token, string path, object JsonObject)
        {
            token.status = -1;
            try
            {
                HttpClient client = Configuration._HttpClient(token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json;v=1.0");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.bearer_token);
                var content = new StringContent(JsonConvert.SerializeObject(JsonObject), Encoding.UTF8, "application/json");
                client.Timeout = new TimeSpan(0, 0, 60);
                var result = client.PostAsync(path, content).Result;
                if (result != null)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        if (token.debug)
                        {
                            Console.WriteLine(String.Format("Results found: {0}", path));
                        }
                        token.op_result = result.Content.ReadAsStringAsync().Result;
                        token.status = 0;
                        return true;
                    }
                    else
                    {
                        Console.Error.Write(result);
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                token.status = -1;
                token.statusMessage = ex.Message;
                Console.Error.WriteLine("post_http {0}", ex.Message);
            }
            return false;
        }
    }

}
