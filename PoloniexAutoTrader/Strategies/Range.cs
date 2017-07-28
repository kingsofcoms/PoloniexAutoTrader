﻿using Jojatekok.PoloniexAPI;
using Jojatekok.PoloniexAPI.MarketTools;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PoloniexAutoTrader.Strategies
{
    class Range : Strategy
    {
        double topBuyPrice;
        double topSellPrice;
        double currentTickerPrice;
        string lineSeperator = "-------------------";

        public Range(string strategyName, MarketPeriod marketSeries, CurrencyPair symbol, bool? buy, bool? sell, double volume) : base(strategyName, marketSeries, symbol, buy, sell, volume)
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
            await RangeBot();
        }

        public override void OnTick(TickerChangedEventArgs ticker)
        {
            // Tticker prices
            if (ticker.CurrencyPair == Symbol)
            {

                Debug.WriteLine("TOP BUY " + ticker.MarketData.OrderTopBuy);

                Debug.WriteLine("TOP SELL " + ticker.MarketData.OrderTopSell);

                topBuyPrice = ticker.MarketData.OrderTopBuy;
                topSellPrice = ticker.MarketData.OrderTopSell;
                currentTickerPrice = ticker.MarketData.PriceLast;
            }
        }

        public override void OnStop()
        {
            Debug.WriteLine(StrategyName + " Stopped");
        }

        public async Task RangeBot()
        {

            DateTime startdate = new DateTime(2017, 1, 1);
            DateTime enddate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);

            // Find % profit / loss of trades
            var opentrades = await Client.PoloniexClient.Trading.GetTradesAsync(Symbol, startdate, enddate);

            foreach (var trade in opentrades)
            {
                double percentChange = Math.Round(((currentTickerPrice - trade.PricePerCoin) / trade.PricePerCoin) * 100,4);

                if (trade.Type == OrderType.Buy && percentChange >= 0.05)
                {
                    // Close position if > 5% profit
                    string buyPercentageData = string.Format("{0} | Entry Price {1} | Profit {2}%" + "\n" + lineSeperator + "\n", trade.Type, trade.PricePerCoin, percentChange);
                    Debug.WriteLine(buyPercentageData);
                    outputData.Strategy1Output.Text += buyPercentageData;
                }
                else if (trade.Type == OrderType.Sell && percentChange <= -0.05)
                {
                    // Close position if > 5% profit
                    string sellPercentageData = string.Format("{0} | Entry Price {1} | Profit {2}%" + "\n" + lineSeperator + "\n", trade.Type, trade.PricePerCoin, percentChange);
                    Debug.WriteLine(sellPercentageData);
                    outputData.Strategy1Output.Text += sellPercentageData;
                }
            }

            var candleInfo = await Client.PoloniexClient.Markets.GetChartDataAsync(Symbol, MarketSeries, startdate, enddate);
            var candleindex = candleInfo.Count() - 1;

            int period = 20;
            double rangeHigh = 0;
            double rangeLow = 0;
            double ABR = Indicators.Indicator.ABR(candleInfo, candleindex, period);
            double SMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, candleindex, period)[0];
            double bBandTop = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, candleindex, period)[1];
            double bBandBottom = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, candleindex, period)[2];

            // Last 20 High / Low
            for (int i = candleindex - 1; i >= candleindex - period; i--)
            {
                rangeHigh += candleInfo[i].High;
                rangeLow += candleInfo[i].Low;
            }

            double rangeHighAvg = Math.Round(rangeHigh,7) / period;
            double rangeLowAvg = Math.Round(rangeLow,7) / period;

            // Output IBS to datawindow
            outputData.Strategy1Output.Text += "Range High Avg" + "\n" + rangeHighAvg + "\n" + lineSeperator + "\n";
            outputData.Strategy1Output.Text += "Range Low Avg" + "\n" + rangeLowAvg + "\n" + lineSeperator + "\n";

            // ABR
            outputData.Strategy1Output.Text += "ABR Function" + "\n" + ABR.ToStringNormalized() + "\n" + lineSeperator + "\n";

            // Bollinger Bands
            outputData.Strategy1Output.Text += "B Band Top" + "\n" + bBandTop + "\n" + lineSeperator + "\n";
            outputData.Strategy1Output.Text += "SMA" + "\n" + SMA + "\n" + lineSeperator + "\n";
            outputData.Strategy1Output.Text += "B Band Bottom" + "\n" + bBandBottom + "\n" + lineSeperator + "\n";


            // Output IBS to datawindow

            // 0.15% fee
            if ((bool)Buy)
             {
                     if (candleInfo[candleindex].Close < rangeLowAvg)
                     {
                         //double pricePerCoinBuy = await currentPrice.CurrentMarketPrice(Symbol, OrderType.Buy);
                         var volume = quantity.TotalToQuantity(Total, topBuyPrice);
                         await marketOrder.ExecuteMarketOrder(Symbol, OrderType.Buy, volume);
                        // Output IBS to datawindow

                         string tradeOutputBuy = DateTime.Now + "\n" + "Volume = " + volume + "\n" + OrderType.Buy + "\n" + "Range Low = " + rangeLowAvg + "\n" + lineSeperator + "\n";
                         Debug.WriteLine(tradeOutputBuy);
                         outputData.Strategy1Output.Text += tradeOutputBuy;
                     }
                 }
             
             // 0.25% fee
             if ((bool)Sell)
             {
                if (candleInfo[candleindex].Close > rangeHighAvg)
                {
                    var quoteBalance = balance.GetBalance(Symbol.QuoteCurrency);
                    var balanceQuote = quoteBalance.Result;

                    if (balanceQuote > minOrderSize)
                     {
                         //double pricePerCoinSell = await currentPrice.CurrentMarketPrice(Symbol, OrderType.Sell);
                         var volume = quantity.TotalToQuantity(Total, topSellPrice);
                         await marketOrder.ExecuteMarketOrder(Symbol, OrderType.Sell, volume);

                         // Output IBS to datawindow
                         string tradeOutputSell = DateTime.Now + "\n" + "Volume = " + volume + "\n" + OrderType.Sell + "\n" + "Range High = " + rangeHighAvg + "\n" + lineSeperator + "\n";
                         Debug.WriteLine(tradeOutputSell);
                        outputData.Strategy1Output.Text += tradeOutputSell;
                    }
                 }
             }
        }

    }
}
