using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IBApi;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections.Concurrent;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace ibTMS
{
    public class IBProcess
    {
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        private static string tableName = "ExampleTable";
        #region static
        #region staticField
        public static long ibCurrentTime;
        public static bool timeFlag = false;
        public static string managedAccounts = "";

        private static object lockATIBC = new object();
        private static int ibClientBufferingLine = 0;
        private static Queue<ManualResetEvent> ibClientWaitLine = new Queue<ManualResetEvent>();

        private static void AddToIbClientWaitLine(ManualResetEvent m)
        {
            lock (lockATIBC)
            {
                ibClientWaitLine.Enqueue(m);
            }
            m.WaitOne();
        }

        private static List<EWrapperImpl> ibClinetsResource = new List<EWrapperImpl>();
        private static Queue<EWrapperImpl> ibClients = new Queue<EWrapperImpl>();
        private static object lockIbClients = new object();
        /// <summary>
        /// Get the ibclient socket resource, requires release
        /// </summary>
        /// <returns></returns>
        public static void GetIbClient(Action<EWrapperImpl> action)
        {
            // if resource available
            if (ibClinetsResource.Count > ibClientBufferingLine)
            {
                lock (lockIbClients)
                {
                    if (ibClients.Count > 0)
                    {
                        var ib = ibClients.Dequeue();
                        action(ib);
                        FreeIbClients(ib);
                    }
                }
            }
            else
            {
                AddToIbClientWaitLine(new ManualResetEvent(false));
                // Resource ready
                lock (lockIbClients)
                {
                    var ib = ibClients.Dequeue();
                    ibClientBufferingLine--;
                    action(ib);
                    FreeIbClients(ib);
                }
            }
        }

        /// <summary>
        /// Required for GetIbClients, free the resource
        /// </summary>
        private static void FreeIbClients(EWrapperImpl ewi)
        {
            if (ibClientWaitLine.Count > 0)
            {
                ibClientBufferingLine++;
                ibClients.Enqueue(ewi);
                var m = ibClientWaitLine.Dequeue();
                m.Set();
                m.Dispose();
            }
            else
            {
                ibClients.Enqueue(ewi);
            }
        }

        private static object lockTickerId = new object();
        private static int tickerId = 0;
        public static int TickerId
        {
            get
            {
                lock (lockTickerId)
                {
                    tickerId++;
                    return tickerId;
                }
            }
        }

        /// <summary>
        /// Static Constructor initialize resources
        /// </summary>
        public static void IniIBProcess()
        {
            for (int i = 0; i < 32; i++)
            {
                var res = new EWrapperImpl();

                EClientSocket clientSocket = res.ClientSocket;
                EReaderSignal readerSignal = res.Signal;

                res.ClientSocket.eConnect("127.0.0.1", 7496, i);

                var reader = new EReader(clientSocket, readerSignal);
                reader.Start();
                //Once the messages are in the queue, an additional thread need to fetch them
                new Thread(() => { while (clientSocket.IsConnected()) { readerSignal.waitForSignal(); reader.processMsgs(); } }) { IsBackground = true }.Start();

                while (res.NextOrderId == 0)
                {
                    Console.WriteLine("Connecting to IB... id: " + i);
                    Thread.Sleep(100); //need to wait for it to connect to know the next valid order id
                }
                Console.WriteLine("\n* Client id: " + i + " Connected!\n");
                ibClients.Enqueue(res);
                ibClinetsResource.Add(res);
            }


			
			/*
            foreach (string accountName in AccountManager.AccountDictionary.Keys)
            {
                if (AccountManager.AccountDictionary[accountName].broker == "IB")
                {
                    managedAccounts.Add(accountName);
                }
            }
			
            AccountManager.subscribeIBUpdate(managedAccounts[0]);
            */
		}
        #endregion

        #region StaticMethods
        /// <summary>
        /// 
        /// </summary>
        public static void CleanUpStatic()
        {
            while (ibClientWaitLine.Count > 0)
            {
                var cur = ibClientWaitLine.Dequeue();
                cur.Set();
                cur.Dispose();
            }
        }

        /// <summary>
        /// Return the current IB server time
        /// </summary>
        public static DateTime IBCurrentTime
        {
            get
            {
                bool oldTimeFlag = timeFlag;
                IBProcess.GetIbClient((ibClient) =>
                {
                    ibClient.ClientSocket.reqCurrentTime();
                });
                while (oldTimeFlag == timeFlag)
                {
                    Thread.Sleep(100); // avoid over spinning
                }
                return (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(ibCurrentTime);
            }
        }

        /// <summary>
        /// Return the exchange for the given instrument type
        /// </summary>
        private static string GetExchange(string instrumentType, string symbol)
        {
            switch (instrumentType)
            {
                case "CASH":
                    return "IDEALPRO";
                default:
                    return "SMART";
            }
        }


        #endregion
        #endregion

        #region PrivateField
        private Ticket ts;
        private Guid gid;
        private ManualResetEvent mre = new ManualResetEvent(false);
        private bool isStop = false;
        private Thread runThread;
        private double quantityFilled;
        private double avgPrice;
        #endregion

        #region Property
        public double QuantityFilled
        {
            get { return quantityFilled; }
            set { quantityFilled = value; }
        }

        public Double AvgPrice
        {
            get { return avgPrice; }
            set { avgPrice = value; }
        }

        #endregion

        #region mainProcess
        /// <summary>
        /// Constructor, required guid
        /// </summary>
        /// <param name="gid"></param>
        public IBProcess(Guid gid)
        {
            this.gid = gid;
        }

        public void Run(Ticket ts)
        {
            Run(ts, null);
        }

		public void EnableGetInfo(Ticket ts)
		{
			//Run(ts, null);
			Console.WriteLine("leo test GetInfo ");
			
			AccountManager.subscribeIBUpdate("");
			
		}
        public bool IsIBSocketConnected(Ticket ts)
        {
            //Console.WriteLine("leo test IsIBSocketConnected ");
            bool IsConnected = false;
            IBProcess.GetIbClient((ibClient) =>
            {
                IsConnected = ibClient.ClientSocket.IsConnected();
            });
            return IsConnected;
        }
		/// <summary>
		/// This is required for processing all the runs, 
		/// Requiring starting a thread either from threadpool or task
		/// </summary>
		public void Run(Ticket ts, Action finish = null)
        {
            this.ts = ts;

            switch (ts.Action)
            {
                case TicketType.Long:
                    runThread = new Thread(() =>
                    {
                        LongAction();
                        if (finish != null)
                        {
                            finish();
                        }
                    });
                    break;
                case TicketType.Short:
                    runThread = new Thread(() =>
                    {
                        ShortAction();
                        if (finish != null)
                        {
                            finish();
                        }
                    });
                    break;
            }
            runThread.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            isStop = true;
            mre.Set();
            mre.Dispose();
        }
        #endregion

        #region Action
        /// <summary>
        /// Simple Long Action
        /// </summary>
        public void LongAction()
        {
            PlaceOrder("BUY", ts.Symbol, ts.InstrumentType, ts.Currency, ts.Quantity, ts.Timeout);
        }

        /// <summary>
        /// Simple Short Action
        /// </summary>
        public void ShortAction()
        {
            PlaceOrder("SELL", ts.Symbol, ts.InstrumentType, ts.Currency, ts.Quantity, ts.Timeout);
        }
        #endregion

        #region Helpers

        public static readonly int PenaltyTimeOut = 100;

        private static void CreateExampleTable()
        {
            Console.WriteLine("\n*** Creating table ***");
            var request = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>()
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = "N"
                },
                new AttributeDefinition
                {
                    AttributeName = "ReplyDateTime",
                    AttributeType = "N"
                }
            },
                KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = "HASH" //Partition key
                },
                new KeySchemaElement
                {
                    AttributeName = "ReplyDateTime",
                    KeyType = "RANGE" //Sort key
                }
            },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 6
                },
                TableName = tableName
            };

            var response = client.CreateTable(request);

            var tableDescription = response.TableDescription;
            Console.WriteLine("{1}: {0} \t ReadsPerSec: {2} \t WritesPerSec: {3}",
                      tableDescription.TableStatus,
                      tableDescription.TableName,
                      tableDescription.ProvisionedThroughput.ReadCapacityUnits,
                      tableDescription.ProvisionedThroughput.WriteCapacityUnits);

            string status = tableDescription.TableStatus;
            Console.WriteLine(tableName + " - " + status);

            //WaitUntilTableReady(tableName);
        }

        private void PlaceOrder(string action, string symbol, string instrumentType, string currency, double totalQuantity, long? timeout = null)
        {
            if (timeout == null)
            {
                timeout = 100;
            }

            var originTime = DateTime.UtcNow.Ticks;

            Order order = new Order()
            {
                OrderType = "MKT",
                Tif = "GTC",
                Action = action,
                TotalQuantity = ts.Quantity,
                OrderId = -1,
            };

            Contract contract = new Contract()
            {
                Symbol = symbol,
                SecType = instrumentType,
                Currency = currency,
                Exchange = GetExchange(instrumentType, symbol),
            };

            double totalQuantityFilled = 0;

            while (((totalQuantity - totalQuantityFilled) > 0) && DateTime.UtcNow.Ticks < (originTime + MilliSecToTick((long)timeout)))
            {
                order.TotalQuantity = totalQuantity - totalQuantityFilled;

                Console.WriteLine("TotalQuantity: " + order.TotalQuantity);
                IBProcess.GetIbClient((ibClient) =>
                {
                    order.OrderId = ibClient.NextOrderId;
                    ibClient.AddToIbs(order.OrderId, this);
                    ibClient.ClientSocket.placeOrder(order.OrderId, contract, order);
                    for (int i = 0; i < PenaltyTimeOut; i += 1)
                    {
                        if (QuantityFilled == order.TotalQuantity)
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(100); // avoid overspinning
                        }
                    }
                    if (QuantityFilled != order.TotalQuantity)
                    {
                        Console.WriteLine("Cancel order: " + order.OrderId);
                        ibClient.ClientSocket.cancelOrder(order.OrderId);
                    }
                    totalQuantityFilled += QuantityFilled;
                    QuantityFilled = 0;
                });
            }
            
            if (totalQuantityFilled > 0)
            {
                try
                {
                    
                    string tableName = "onlineOrderHistory";
                    //string accountName = "";

                    var request = new PutItemRequest
                    {
                        TableName = tableName,
                        Item = new Dictionary<string, AttributeValue>()
                        {
                              //{ "uid", new AttributeValue { N = "201" }},
                              {"uid", new AttributeValue { S =  System.Guid.NewGuid().ToString()}},
                              { "brokerName", new AttributeValue { S = "IB" }},
                              { "accountName", new AttributeValue { S = AccountManager.accountInfo.accountName }},
                              { "strategyName", new AttributeValue { S = ts.StrategyName }},
                              { "strategyOrderNumber", new AttributeValue { N = ts.StrategyOrderNumber.ToString() }},
                              { "instrumentType", new AttributeValue { S = ts.InstrumentType }},
                              { "symbol", new AttributeValue { S = ts.Symbol }},
                              { "currency", new AttributeValue { S = ts.Currency }},
                              { "action", new AttributeValue { S = ts.Action.ToString() }},
		                      { "quantity", new AttributeValue { N = totalQuantityFilled.ToString() }},
		                      { "orderPrice", new AttributeValue { N = avgPrice.ToString() }},
		                      { "orderTime", new AttributeValue { S = DateTime.UtcNow.ToString("o")}},
                            
                        }
                    };
                    client.PutItem(request);
                    
                }
                catch (AmazonDynamoDBException e) { Console.WriteLine(e.Message); }
                catch (AmazonServiceException e) { Console.WriteLine(e.Message); }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
        }

        private long MilliSecToTick(long milliSec)
        {
            return milliSec * 10000;
        }

        #endregion
    }
}

