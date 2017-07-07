using Jojatekok.PoloniexAPI;
using Jojatekok.PoloniexAPI.MarketTools;
using PoloniexAutoTrader.Market;
using PoloniexAutoTrader.Strategies;
using PoloniexAutoTrader.Timers;
using PoloniexAutoTrader.Trading;
using PoloniexAutoTrader.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PoloniexAutoTrader.Indicators
{
    public static class Indicator
    {
        public static double[] GetBollingerBandsWithSimpleMovingAverage(this IList<IMarketChartData> value, int index, int period)
        {
            var closes = new List<double>(period);
            for (var i = index; i > Math.Max(index - period, -1); i--)
            {
                closes.Add(value[i].Close);
            }

            var simpleMovingAverage = closes.Average();
            var stDevMultiplied = Math.Sqrt(closes.Average(x => Math.Pow(x - simpleMovingAverage, 2))) * 2;

            return new[] {
                simpleMovingAverage,
                simpleMovingAverage + stDevMultiplied,
                simpleMovingAverage - stDevMultiplied
            };
        }

        // ABR = Average Bar Range
        public static double ABR(this IList<IMarketChartData> value, int index, int period)
        {
            double range_adr = 0;
            // for (var i = index; i > index - period; i--)
            for (var i = index; i > Math.Max(index - period, -1); i--)
            {
                range_adr += (value[i].High - value[i].Low);
            }

            range_adr /= period;
            return range_adr;
        }

        // IBS = Internal Bar Strength
        public static double IBS(this IList<IMarketChartData> value, int index)
        {
            var ibs = ((value[index].Close - value[index - 1].Low) / (value[index].High - value[index].Low)) * 100;
            return ibs;
        }

        // ROC = Rate of Change
        public static double ROC(this IList<IMarketChartData> value, int index, int period)
        {
            double roc = 0;
            // for (var i = index; i > index - period; i--)
            for (var i = index; i > Math.Max(index - period, -1); i--)
            {
                roc += Math.Round(((value[1].Close - value[i].Close) / value[1].Close) * 100, 4);
            }

            roc /= period;
            return roc;
        }

        // Range High and Low - Average High over (x) periods / Average Low over (x) Periods
        public static double[] RanageHighLow(this IList<IMarketChartData> value, int index, int period)
        {
            var highs = new List<double>(period);
            var lows = new List<double>(period);
            for (var i = index; i > (index - period); i--)
            {
                highs.Add(value[i].High);
                lows.Add(value[i].Low);
            }

            var rangeHigh = highs.Average();
            var rangeLow = lows.Average();

            return new[] {
                rangeHigh,
                rangeLow
            };
        }

        // ZigZag Indicator
        // Get Max High and Min Low - Find Support and resistance
        public static double[] SupportResistance(this IList<IMarketChartData> value, int index, int period)
        {
            double resistance = 0;
            double support = 0;

            for (var i = index; i > (index - period); i--)
            {
                resistance = Math.Max(value[index - period].High, value[i].High);
                support = Math.Min(value[index - period].High, value[i].Low);
            }

            return new[] {
                support,
                resistance
            };
        }

        // Get Average Volume Quote
        public static double AverageVolumeQuote(this IList<IMarketChartData> value, int index, int period)
        {
            double averageVolumeQuote = 0;
            // for (var i = index; i > index - period; i--)
            for (var i = index; i > Math.Max(index - period, -1); i--)
            {
                averageVolumeQuote += value[i].VolumeQuote;
            }

            averageVolumeQuote /= period;
            return averageVolumeQuote;
        }

        // Get Average Volume Base
        public static double AverageVolumeBase(this IList<IMarketChartData> value, int index, int period)
        {
            double averageVolumeBase = 0;
            // for (var i = index; i > index - period; i--)
            for (var i = index; i > Math.Max(index - period, -1); i--)
            {
                averageVolumeBase += value[i].VolumeBase;
            }

            averageVolumeBase /= period;
            return averageVolumeBase;
        }

        // Get Fibonacci levels from Max High and Max Low of period
        public static double[] FibLevels(this IList<IMarketChartData> value, int index, int period)
        {
            double fib618negative = 0; // Array index [0]

            double high = 0; // [1]
            double low = 0; // [2]
            
            double fib382 = 0; // [3]
            double fib500 = 0; // [4]
            double fib618 = 0; // [5]
            double fib1618 = 0; //[6]

            for (var i = index; i > (index - period); i--)
            {
                high = Math.Max(value[index - period].High, value[i].High);
                low = Math.Min(value[index - period].High, value[i].Low);

                //var high = value[i].High;
                //var low = value[i].Low;

                fib618negative = low - (high - low) * 0.618;
                fib382 = low + (high - low) * 0.382;
                fib500 = low + (high - low) * 0.5;
                fib618 = low + (high - low) * 0.618;
                fib1618 = low + (high - low) * 1.618;
            }

            return new[] {
                fib618negative, high, low, fib382, fib500, fib618, fib1618
            };
        }

        // TO DO
        // RSI Divergence
        // 
    }
}
