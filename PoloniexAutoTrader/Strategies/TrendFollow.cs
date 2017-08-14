using Jojatekok.PoloniexAPI;
using Jojatekok.PoloniexAPI.MarketTools;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PoloniexAutoTrader.Strategies
{
    class TrendFollow : Strategy
    {
        double topBuyPrice;
        double topSellPrice;
        static IList<IMarketChartData> candleInfo;
        string lineSeperator = "\n" + "-------------------" + "\n";
        string newline = Environment.NewLine;

        public TrendFollow(string strategyName, MarketPeriod marketSeries, CurrencyPair symbol, bool? buy, bool? sell, double volume) : base(strategyName, marketSeries, symbol, buy, sell, volume)
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
            await TrendBot();
        }

        public override void OnTick(TickerChangedEventArgs ticker)
        {
            // Ticker data
            if (ticker.CurrencyPair == Symbol)
            {
                topBuyPrice = ticker.MarketData.OrderTopBuy;
                topSellPrice = ticker.MarketData.OrderTopSell;
            }
        }

        public override void OnStop()
        {
            Debug.WriteLine(StrategyName + " Stopped");
        }

        public async Task TrendBot()
        {

            DateTime startdate = new DateTime(2017, 1, 1);
            DateTime enddate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);

            // Get historical data
            candleInfo = await Client.PoloniexClient.Markets.GetChartDataAsync(Symbol, MarketSeries, startdate, enddate);
            // Get data index -1
            var index = candleInfo.Count() - 1;
            // Set MA Period
            int period = 50;
            // Set MA Period 2
            int slowPeriod = 20;
            // Lookback period
            int lookBack = 5;

            // ABR Calculation
            double ABR = Indicators.Indicator.ABR(candleInfo, index, period);
            // Last SMA value
            double SMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index, period)[0];
            // Previous SMA value
            double previousSMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index - 1, period)[0];
            // Last SMA 2 value
            double SMA2 = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index, slowPeriod)[0];
            // Previous SMA 2 value
            double previousSMA2 = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index - 1, slowPeriod)[0];

            // CLOSE UNDER / OVER SMA
            // SMA last > previous = Bullish
            bool smaIsRising = Indicators.Indicator.IsRising(candleInfo, index, lookBack, period);
            // SMA last < previous = Bearish
            bool smaIsFalling = Indicators.Indicator.IsFalling(candleInfo, index, lookBack, period);

            // MA CROSSOVER
            // SMA previous under SMA 2 previous, SMA over SMA2 last value
            bool bullishCross = BullishCrossver(SMA, SMA2, previousSMA, previousSMA2);
            // SMA previous over SMA 2 previous, SMA under SMA2 last value
            bool bearishCross = Bearishrossver(SMA, SMA2, previousSMA, previousSMA2);

            // Candle close under SMA previous & Then close over SMA last.
            var bullishSignal = candleInfo[index - 1].Low < previousSMA && candleInfo[index].Close > SMA;
            // Candle close over SMA previous & Then close under SMA last.
            var bearishSignal = candleInfo[index - 1].High > previousSMA && candleInfo[index].Close < SMA;

            // Output Test Index to datawindow
            string smaIndexTest = string.Format("{0} SMA at Index {1} {2} {1} Candle Close at Index {1} {3} {1} {4} {5}", Symbol, newline, SMA, candleInfo[index].Close, candleInfo[index].Time, lineSeperator);
            string smaIndexTest2 = string.Format("{0} SMA at Index {1} {2} {1} Candle Close at Index {1} {3} {1} {4} {5}", Symbol, newline, SMA, candleInfo[index - 1].Close, candleInfo[index - 1].Time, lineSeperator);

            // ABR
            string abrString = string.Format("{0} ABR = {1}{2}", Symbol, ABR.ToStringNormalized(), lineSeperator);
            outputData.Strategy1Output.Text += abrString;


            // Output IBS to datawindow

            // 0.15% fee
            if ((bool)Buy)
            {
                if (bullishSignal && smaIsRising)
                {
                    var volume = quantity.TotalToQuantity(Total, topBuyPrice);
                    await marketOrder.ExecuteMarketOrder(Symbol, OrderType.Buy, volume);
                    // Output IBS to datawindow

                    string tradeOutputBuy = DateTime.Now + "\n" + "Volume = " + volume + "\n" + OrderType.Buy + "\n" + lineSeperator + "\n";
                    Debug.WriteLine(tradeOutputBuy);
                    outputData.Strategy1Output.Text += tradeOutputBuy;
                }
            }

            // 0.25% fee
            if ((bool)Sell)
            {
                if (bearishSignal && smaIsFalling)
                //if (bearishCross)
                {
                    var quoteBalance = balance.GetBalance(Symbol.QuoteCurrency);
                    var balanceQuote = quoteBalance.Result;

                    if (balanceQuote > minOrderSize)
                    {
                        var volume = quantity.TotalToQuantity(Total, topSellPrice);
                        await marketOrder.ExecuteMarketOrder(Symbol, OrderType.Sell, volume);

                        // Output IBS to datawindow
                        string tradeOutputSell = DateTime.Now + "\n" + "Volume = " + volume + "\n" + OrderType.Sell + "\n" + lineSeperator + "\n";
                        Debug.WriteLine(tradeOutputSell);
                        outputData.Strategy1Output.Text += tradeOutputSell;
                    }
                }
            }
        }

        // Check if Fast SMA has crossed above slow SMA
        static bool BullishCrossver(double SMA, double SMA2, double previousSMA, double previousSMA2)
        {
            // CROSS OVER
            if ((previousSMA < previousSMA2) && SMA >= SMA2)
            {
                return true;
            }
            return false;
        }

        // Check if Fast SMA has cross below slow SMA
        static bool Bearishrossver(double SMA, double SMA2, double previousSMA, double previousSMA2)
        {
            // CROSS OVER
            if ((previousSMA > previousSMA2) && SMA <= SMA2)
            {
                return true;
            }
            return false;
        }

    }
}