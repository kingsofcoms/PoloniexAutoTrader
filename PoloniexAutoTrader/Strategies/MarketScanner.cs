using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jojatekok.PoloniexAPI;
using Jojatekok.PoloniexAPI.MarketTools;
using System.Diagnostics;

namespace PoloniexAutoTrader.Strategies
{
    class MarketScanner : Strategy
    {
        double topBuyPrice;
        double topSellPrice;
        double currentTickerPrice;
        string lineSeperator = "\n" + "-------------------" + "\n";
        string newline = Environment.NewLine;
        static IList<IMarketChartData> candleInfo;
        static CurrencyPair newSymbol;
        private double dailyVolume;

        public MarketScanner(string strategyName, MarketPeriod marketSeries, CurrencyPair symbol, bool? buy, bool? sell, double total) : base(strategyName, marketSeries, symbol, buy, sell, total)
        {
        }
       
        public override void OnStart()
        {
            // Output window
            outputData = new Strategy1Data();
            outputData.Show();
            outputData.Title = StrategyName;
        }

        public override void OnTick(TickerChangedEventArgs ticker)
        {
            // Last ticker price
            if (ticker.MarketData.Volume24HourBase > 2000)
            {
                //Debug.WriteLine("TOP BUY " + ticker.MarketData.OrderTopBuy);
                //Debug.WriteLine("TOP SELL " + ticker.MarketData.OrderTopSell);

                topBuyPrice = ticker.MarketData.OrderTopBuy;
                topSellPrice = ticker.MarketData.OrderTopSell;
                currentTickerPrice = ticker.MarketData.PriceLast;
                dailyVolume = ticker.MarketData.Volume24HourBase;
            }
        }

        public override async Task OnBar()
        {
            await MarketScan();
        }

        // Percentage Change
        public async Task MarketScan()
        {
            // Scan all pairs
            // Find pairs > 50 btc volume
            // Find pairs low ABR / high ABR
            // Find all pairs with bullish / bearish cross
            // Find all pairs above / below SMA

            bool bullishSignal = false;
            bool bearishSignal = false;

            DateTime startdate = new DateTime(2017, 1, 1);
            DateTime enddate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);

            // Get Market Summary
            var allSymbols = await Client.PoloniexClient.Markets.GetSummaryAsync();

            // Create list of symbols > 2000 btc daily volume
            List<CurrencyPair> allSymbolsList = new List<CurrencyPair>();

            // Clear list
            allSymbolsList.Clear();

            // Loop through all symbols list
            foreach (var data in allSymbols)
            {
                // Only add to list if daily volume > 2000 btc
                if (data.Value.Volume24HourBase > 2000)
                {
                    allSymbolsList.Add(data.Key);
                }
            }

            // Use each item in list of Symbols with volume > 2000 btc
            for (int i = 0; i < allSymbolsList.Count; i++)
            {
                newSymbol = allSymbolsList[i];

                // Get historical data
                candleInfo = await Client.PoloniexClient.Markets.GetChartDataAsync(newSymbol, MarketSeries, startdate, enddate);
                // Get data index -1
                var index = candleInfo.Count() - 1;
                // Set MA Period
                int period = 20;
                // Set MA Period 2
                int slowPeriod = 20;
                // Lookback period
                int lookBack = 5;

                // ABR Calculation
                double ABR = Indicators.Indicator.ABR(candleInfo, index, period);
                // Average Volume
                double avgVolume = Indicators.Indicator.AverageVolumeBase(candleInfo, index, period);
                // Last SMA value
                double SMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index, period)[0];
                // Previous SMA value
                double previousSMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index - 1, period)[0];
                // Last SMA 2 value
                double SMA2 = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index, slowPeriod)[0];
                // Previous SMA 2 value
                double previousSMA2 = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index - 1, slowPeriod)[0];

                // Fib Levels
                // 0.618
                double fib618 = Indicators.Indicator.FibLevels(candleInfo, index, slowPeriod)[4];
                // Minus 0.618
                double minusfib618 = Indicators.Indicator.FibLevels(candleInfo, index, slowPeriod)[0];

                // CLOSE UNDER / OVER SMA
                // SMA last > previous = Bullish
                bool smaIsRising = IsRising(index, lookBack, period);
                // Rising Test
                string risingString = string.Format("{0} Rising = {1}{2}", newSymbol, smaIsRising, lineSeperator);
                outputData.Strategy1Output.Text += risingString;
                // Falling Test                
                // SMA last < previous = Bearish
                bool smaIsFalling = IsFalling(index, lookBack, period);
                string fallingString = string.Format("{0} Falling = {1}{2}", newSymbol, smaIsFalling, lineSeperator);
                outputData.Strategy1Output.Text += fallingString;

                // MA CROSSOVER
                // SMA previous under SMA 2 previous, SMA over SMA2 last value
                bool bullishCross = BullishCrossver(SMA, SMA2, previousSMA, previousSMA2);
                // SMA previous over SMA 2 previous, SMA under SMA2 last value
                bool bearishCross = Bearishrossver(SMA, SMA2, previousSMA, previousSMA2);

                // Candle close under SMA previous & Then close over SMA last.
                if (candleInfo[index].Close > SMA && smaIsRising)
                {
                    bullishSignal = true;
                }
                else
                {
                    bullishSignal = false;
                }
                // Candle close over SMA previous & Then close under SMA last.
                if (candleInfo[index].Close < SMA && smaIsFalling)
                {
                    bearishSignal = true;
                }
                else
                {
                    bearishSignal = false;
                }

                // Output IBS to datawindow
                // **** Testing (index - 1) in indicator or using (index -1) in strategy ****
                string smaIndexTest = string.Format("{0} SMA at Index {1} {2} {1} Candle Close at Index {1} {3} {1} {4} {5}", newSymbol, newline, SMA, candleInfo[index].Close, candleInfo[index].Time, lineSeperator);
                string smaIndexTest2 = string.Format("{0} SMA at Index {1} {2} {1} Candle Close at Index {1} {3} {1} {4} {5}", newSymbol, newline, SMA, candleInfo[index-1].Close, candleInfo[index-1].Time, lineSeperator);

                //Debug.WriteLine(smaIndexTest);
                //Debug.WriteLine(smaIndexTest2);

                // ABR
                string abrString = string.Format("{0} ABR = {1}{2}", newSymbol, ABR.ToStringNormalized(), lineSeperator);
                outputData.Strategy1Output.Text += abrString;

                //Bullish Signal
                string bullSignal = string.Format("{0} Bullish = {1}{2}", newSymbol, bullishSignal, lineSeperator);
                outputData.Strategy1Output.Text += bullSignal;

                // Bearish Signal
                string bearSignal = string.Format("{0} Bearish = {1}{2}", newSymbol, bearishSignal, lineSeperator);
                outputData.Strategy1Output.Text += bearSignal;

                // Fib levels
                string plus618Fib = string.Format("{0} 0.618 = {1}{2}", newSymbol, fib618, lineSeperator);
                outputData.Strategy1Output.Text += plus618Fib;

                string minus618Fib = string.Format("{0} Minus 0.618 = {1}{2}", newSymbol, minusfib618, lineSeperator);
                outputData.Strategy1Output.Text += minus618Fib;

                // Current Volume over average
                if (dailyVolume > avgVolume)
                {
                    string avgVol = string.Format("{0} Higher than average volume{1}", newSymbol, lineSeperator);
                    outputData.Strategy1Output.Text += avgVol;
                }                               
            }
        }

        // Check to see if SMA value is higher than SMA Value (x) bars ago (lookback period)
        bool IsRising(int index, int lookBack, int period)
        {
            double SMA = 0;
            double previousSMA = 0;

            //for (var i = index; i > Math.Max(index - lookBack, -1); i--)
            for (var i = index; i > (index - lookBack); i--)
            {
                // Last SMA value
                SMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index, period)[0];
                // Previous SMA value
                previousSMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index - i, period)[0];

                if (SMA > previousSMA)
                {
                    return true;
                }                              
            }
            return false;
        }

        // Check to see if SMA value is lower than SMA Value (x) bars ago (lookback period)
        bool IsFalling(int index, int lookBack, int period)
        {
            double SMA = 0;
            double previousSMA = 0;

            //for (var i = index; i > Math.Max(index - lookBack, -1); i--)
            for (var i = index; i > (index - lookBack); i--)
            {
                // Last SMA value
                SMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index, period)[0];
                // Previous SMA value
                previousSMA = Indicators.Indicator.GetBollingerBandsWithSimpleMovingAverage(candleInfo, index - i, period)[0];

                if (SMA < previousSMA)
                {
                    return true;
                }               
            }

            return false;

            // Output to debug to check values are correct
            //Debug.WriteLine("{4} Is Falling = {0} | SMA {1} | Previous SMA {2} | {3} Bars ago", smaIsFalling, SMA.ToStringNormalized(), previousSMA.ToStringNormalized(), lookBack, newSymbol);
        }

        // Check if Fast SMA has crossed above slow SMA
        static bool BullishCrossver(double SMA, double SMA2, double previousSMA, double previousSMA2)
        {
            // CROSS OVER
            if (previousSMA < previousSMA2 && SMA >= SMA2)
            {
                return true;
            }

            return false;

            // Output to debug to check values are correct
            //Debug.WriteLine("{1} BullishCross = {0}", bullishCross, newSymbol);
        }

        // Check if Fast SMA has cross below slow SMA
        static bool Bearishrossver(double SMA, double SMA2, double previousSMA, double previousSMA2)
        {
            // CROSS OVER
            if (previousSMA > previousSMA2 && SMA <= SMA2)
            {
                return true;
            }

            return false;

            // Output to debug to check values are correct
            // Debug.WriteLine("{1} BearishCross = {0}", bearishCross, newSymbol);
        }


        public override void OnStop()
        {

        }
    }
}
