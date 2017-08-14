using Jojatekok.PoloniexAPI;
using Jojatekok.PoloniexAPI.MarketTools;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PoloniexAutoTrader.Strategies
{
    class PercentageTrader : Strategy
    {
        double topBuyPrice;
        double topSellPrice;
        double tickerPricePercentageChange;
        double tickerPriceLast;
        bool symbolOk;
        string lineSeperator = "\n" + "-------------------" + "\n";
        string newLine = Environment.NewLine;

        public PercentageTrader(string strategyName, MarketPeriod marketSeries, CurrencyPair symbol, bool? buy, bool? sell, double volume) : base(strategyName, marketSeries, symbol, buy, sell, volume)
        {
        }

        public override void OnStart()
        {
            // Output window
            outputData = new Strategy1Data();
            outputData.Show();
            outputData.Title = StrategyName + " " + Symbol;
        }

        public override async Task OnBar()
        {
            await PercentChange();
        }

        public override void OnTick(TickerChangedEventArgs ticker)
        {
            // Last ticker price        

            if (ticker.CurrencyPair == Symbol)
            {
                symbolOk = true;

                topBuyPrice = ticker.MarketData.OrderTopBuy;
                topSellPrice = ticker.MarketData.OrderTopSell;

                tickerPricePercentageChange = ticker.MarketData.PriceChangePercentage;
                tickerPriceLast = ticker.MarketData.PriceLast;
            }
        }

        // Percentage Change
        public async Task PercentChange()
        {
            DateTime startdate = new DateTime(2017, 1, 1);
            DateTime enddate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);

            var CandleSeries = Client.PoloniexClient.Markets.GetChartDataAsync(Symbol, MarketSeries, startdate, enddate);
            var candleSeries = CandleSeries.Result;
            var index = candleSeries.Count() - 1;

            //Percent Drop Calc
            if (symbolOk)
            {
                double lastClose = candleSeries[index].Close;

                //double percentChange = Math.Round(((tickerPricePercentageChange - lastClose) / tickerPricePercentageChange) * 100, 4);
                double percentChange = Math.Round(((tickerPriceLast - lastClose) / tickerPriceLast) * 100, 4);

                // DT Now
                string timeNow = DateTime.Now.ToString();

                // Output IBS to datawindow 
                string percentChangeStr = string.Format("{0} Percentage Change = {1} | {2}{3}", Symbol, percentChange, timeNow, lineSeperator);
                string tickerChangeStr = string.Format("{0} Ticker Change Change = {1} | {2}{3}", Symbol, tickerPricePercentageChange, timeNow, lineSeperator);
                string lastCloseStr = string.Format("{0} Last Close = {1} | {2}{3}", Symbol, lastClose, timeNow, lineSeperator);
                string lastTickerStr = string.Format("{0} Last Ticker Price = {1} | {2}{3}", Symbol, tickerPriceLast, timeNow, lineSeperator);

                outputData.Dispatcher.Invoke(new Action(() =>
               {                                  
                   outputData.Strategy1Output.Text += percentChangeStr;
                   outputData.Strategy1Output.Text += tickerChangeStr;
                   outputData.Strategy1Output.Text += lastCloseStr;
                   outputData.Strategy1Output.Text += lastTickerStr;
               }));


                //  ************** TRADING LOGIC **************
                try
                {
                    if ((bool)Buy)
                    {
                        //if (CandleSeries[index].Close > CandleSeries[index].Open && percentChange <= 1)
                        if (percentChange <= -50)
                        {
                            //double pricePerCoinBuy = await currentPrice.CurrentMarketPrice(Symbol, OrderType.Buy);
                            var volume = quantity.TotalToQuantity(Total, topBuyPrice);

                            await marketOrder.ExecuteMarketOrder(Symbol, OrderType.Buy, volume);

                            // Output IBS to datawindow

                            string tradeOutputBuy = DateTime.Now + "\n" + "Volume = " + volume + "\n" + OrderType.Buy + "\n" + "% Change = " + percentChange + "\n" + lineSeperator + "\n";
                            Debug.WriteLine(tradeOutputBuy);
                            outputData.Strategy1Output.Text += tradeOutputBuy;
                        }
                    }


                    if ((bool)Sell)
                    {
                        if (percentChange >= 100)
                        {
                            var quoteBalance = balance.GetBalance(Symbol.QuoteCurrency);
                            var balanceQuote = quoteBalance.Result;

                            if (balanceQuote > minOrderSize)
                            {
                                if (balanceQuote > minOrderSize)
                                {
                                    //double pricePerCoinSell = await currentPrice.CurrentMarketPrice(Symbol, OrderType.Sell);
                                    var volume = quantity.TotalToQuantity(Total, topSellPrice);

                                    await marketOrder.ExecuteMarketOrder(Symbol, OrderType.Sell, volume);

                                    // Output IBS to datawindow
                                    string tradeOutputSell = DateTime.Now + "\n" + "Volume = " + volume + "\n" + OrderType.Sell + "\n" + "% Change = " + percentChange + "\n" + lineSeperator + "\n";
                                    Debug.WriteLine(tradeOutputSell);
                                    outputData.Strategy1Output.Text += tradeOutputSell;
                                }
                            }
                        }
                    }
                }

                catch (Exception error)
                {
                    Debug.WriteLine(error.Message);
                    //MessageDialogue.Show("Order Error" + error.Message);
                }
            }
        }

        public override void OnStop()
        {

        }
    }
}
