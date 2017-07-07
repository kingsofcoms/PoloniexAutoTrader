using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jojatekok.PoloniexAPI;
using System.Diagnostics;
using System.Windows.Threading;

namespace PoloniexAutoTrader.Market
{
    public class TickerData
    {
        Client Client = new Client();

        public TickerData()
        {
            Client.PoloniexClient.Live.OnTickerChanged += Live_OnTickerChanged;
        }


        async void Live_OnTickerChanged(object sender, TickerChangedEventArgs ticker)
        {
            if (TickerDataTask == null)
            {
                Debug.WriteLine("No task set!");
                return;
            }

            if (IsRunning && !IsReentrant)
            {
                // previous task hasn't completed
                Debug.WriteLine("Ticker Task already running");
                return;
            }

            try
            {
                // we're running it now
                IsRunning = true;

                Debug.WriteLine("Ticker Running Task");
                await TickerDataTask.Invoke();
                Debug.WriteLine("Ticker Task Completed");
            }
            catch (Exception)
            {
                Debug.WriteLine("Ticker Task Failed");
            }
            finally
            {
                // allow it to run again
                IsRunning = false;
            }
        }

        public bool IsReentrant { get; set; }
        public bool IsRunning { get; private set; }

        public Func<Task> TickerDataTask { get; set; }
    }
}
