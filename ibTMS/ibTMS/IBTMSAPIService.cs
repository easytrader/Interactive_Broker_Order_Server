using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace ibTMS
{
    public class IBTMSAPIService
    {
        private readonly HttpListener _listener = new HttpListener();
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        private string dynamo_password = "";

        public IBTMSAPIService()
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            _listener.Prefixes.Add("http://localhost:8080/TmesisAPI/");
            _listener.Start();

        }

        public string SendResponse(HttpListenerRequest request)
        {
            return string.Format("<HTML><BODY>TEST<br>{0}</BODY></HTML>", DateTime.Now);
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                if (ctx.Request.HttpMethod == "POST" && ctx.Request.Headers["Content-Type"] == "application/json")
                                {
                                    byte[] buf;

                                    // Parse Input
                                    Dictionary<string, string> inputData;
                                    using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                                    {
                                        inputData = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
                                    }

                                    // No input is a bad request
                                    if (inputData == null)
                                    {
                                        ctx.Response.StatusCode = 400;
                                    }

                                    // Baseline Security Check password
                                    else if (!CheckPassword(inputData["password"]))
                                    {
                                        buf = Encoding.UTF8.GetBytes(String.Format("Invalid Password {0}", inputData["password"]));
                                        ctx.Response.StatusCode = 401; // Unauthorized 
                                        ctx.Response.ContentLength64 = buf.Length;
                                        ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                    }

                                    else
                                    {
                                        // Process 
                                        switch (inputData["function"])
                                        {
                                            case "getAccountInfo":
                                                //EnableGetAccountInfo(ctx, inputData);
                                                Console.WriteLine("case getAccountInfo");
                                                GetAccountInfo(ctx, inputData);
                                                break;
                                            case "engetAccountInfo":
                                                EnableGetAccountInfo(ctx, inputData);
                                                break;
                                            case "ibTMSAlive":
                                                ctx.Response.StatusCode = 200;
                                                break;
                                            case "ibSocketConnected":
                                                IsIBSocketConnected(ctx, inputData);
                                                break;
                                            case "placeOrder":
                                                PlaceOrder(ctx, inputData);
                                                break;
                                            default:
                                                // Testing Connection
                                                SetSucessResponse(ctx, SendResponse(ctx.Request));
                                                break;
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                ctx.Response.StatusCode = 400;
                                // throw e;
                            } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

        #region APIFunction
        private void EnableGetAccountInfo(HttpListenerContext ctx, Dictionary<string, string> inputData)
        {
            //var res = "";
            Console.WriteLine("getAccountInfo");
			Ticket tk = CreateTicketFromInputData(inputData);
			TicketPoolManager.EnableGetAccountInfo(tk);

            //Wait 
            /*
            Thread.Sleep(50);
			Console.WriteLine("leo test in GetAccountInfo");
			Console.WriteLine("leo test in AccountManager.accountInfo.buyingPower");
			Console.WriteLine(AccountManager.accountInfo.buyingPower);
			*/
            SetSucessResponse(ctx, "");
        }

        private void IsIBSocketConnected(HttpListenerContext ctx, Dictionary<string, string> inputData)
        {
            //var res = "";
            bool IsConnected = false;
            //Console.WriteLine("IsIBSocketConnected");
            Ticket tk = CreateTicketFromInputData(inputData);
            IsConnected = TicketPoolManager.IsIBSocketConnected(tk);
            Console.WriteLine("leo test IsConnected:"+ IsConnected);
            
            if(IsConnected==true)
                SetSucessResponse(ctx, "");
            else
                SetFailResponse(ctx, "");
        }

        private void GetAccountInfo(HttpListenerContext ctx, Dictionary<string, string> inputData)
        {
            //var res = "";
            Console.WriteLine("getAccountInfo");
            //Ticket tk = CreateTicketFromInputData(inputData);
            //TicketPoolManager.EnableGetAccountInfo(tk);

            //Wait 
            
            //Thread.Sleep(50);
			Console.WriteLine("leo test in GetAccountInfo");
			Console.WriteLine("leo test in AccountManager.accountInfo.buyingPower");
			Console.WriteLine(AccountManager.accountInfo.buyingPower);
			
            SetSucessResponse(ctx, "");
        }

        private void PlaceOrder(HttpListenerContext ctx, Dictionary<string, string> inputData)
        {
            //var res = "";
            Console.WriteLine("placed order");
            Ticket tk = CreateTicketFromInputData(inputData);
            TicketPoolManager.SendTicket(tk);
            SetSucessResponse(ctx, "");
        }

        #endregion

        #region Helpers
        private void SetSucessResponse(HttpListenerContext ctx, string res)
        {
            var buf = Encoding.UTF8.GetBytes(res);
            ctx.Response.StatusCode = 200; // success
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        private void SetFailResponse(HttpListenerContext ctx, string res)
        {
            var buf = Encoding.UTF8.GetBytes(res);
            ctx.Response.StatusCode = 400; // success
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        private Ticket CreateTicketFromInputData(Dictionary<string, string> inputData)
        {
            TicketType tt = (TicketType)System.Enum.Parse(typeof(TicketType), inputData["action"]);
            long? timeout = null;
            if (inputData.ContainsKey("timeout"))
            {
                timeout = Convert.ToInt64(inputData["timeout"]);
            }
            return new Ticket(inputData["strategyName"], Convert.ToInt32(inputData["strategyOrderNumber"]), inputData["instrumentType"], inputData["symbol"], inputData["currency"], tt, Convert.ToDouble(inputData["quantity"]), timeout);
        }

        private bool CheckPassword(string password)
        {
            //dynamodb get password once
            try
            {
                string tableName = "password";

                var request = new GetItemRequest
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>() { { "key", new AttributeValue { S = "a" } } },
                };
                var response = client.GetItem(request);

                // Check the response.
                var result = response.Item;

                dynamo_password = result["password"].S;

                Console.WriteLine("dyanmodb get password: " + result["password"].S);
            }
            catch
            {
                Console.WriteLine("dynamodb get passsword fail");
            }

            if (password==dynamo_password)
                return true; //need a secure way to check password
            else
                return false;
        }
        #endregion
    }
}
