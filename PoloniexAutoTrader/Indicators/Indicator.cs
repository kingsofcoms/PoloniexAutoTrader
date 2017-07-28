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

        // EMA Calculation
        // Initial SMA: 10 Period sum / 10
        // Multipilier: (2 / (Time periods +1) ) = (2 / (10+1) ) = 0.1818 (18.18%)
        // EMA: {Close - EMA (previous day) } x multiplier + EMA (previous day).

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

            double high = 0; // [6]
            double low = 0; // [1]
            
            double fib382 = 0; // [2]
            double fib500 = 0; // [3]
            double fib618 = 0; // [4]
            double fib1618 = 0; // [5]

            for (var i = index; i > (index - period); i--)
            {
                high = Math.Max(value[index - period].High, value[i].High);
                low = Math.Min(value[index - period].High, value[i].Low);

                fib618negative = low - (high - low) * 0.618;
                fib382 = low + (high - low) * 0.382;
                fib500 = low + (high - low) * 0.5;
                fib618 = low + (high - low) * 0.618;
                fib1618 = low + (high - low) * 1.618;
            }

            return new[] {
                fib618negative, low, fib382, fib500, fib618, fib1618, high
            };
        }

        // TO DO
        // RSI Divergence
        // Correlation

        // Get Average Volume Quote
        public static double Correlation(this IList<IMarketChartData> value, IList<IMarketChartData> value2, int index, int period)
        {
            double correlation = 0;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;
            double x, y, x2, y2;

            for (var i = index; i > (index - period); i--)
            {
                x = value[index].Close;
                y = value2[index].Close;

                x2 = x * x;
                y2 = y * y;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x2;
                sumY2 += y2;
            }

            correlation = (period * (sumXY) - sumX * sumY) / Math.Sqrt((period * sumX2 - sumX * sumX) * (period * sumY2 - sumY * sumY));
            return correlation;
        }

    }
}
