using CsvHelper;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using MyTradingApp.Stops;
using MyTradingApp.Stops.StopTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace MyTradingApp.StopsTestHarness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double EntryPrice = 12.50;

        private readonly StopManager _stopManager = new StopManager();
        private readonly List<double> _stops = new List<double>();
        private readonly Dictionary<string, BarCollection> _prices = new Dictionary<string, BarCollection>();
        private string _selectedStock;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Init();
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public List<string> Stocks { get; set; } = new List<string>();

        public List<TradeDirection> Directions { get; set; } = new List<TradeDirection>();

        public TradeDirection SelectedDirection
        {
            get => _stopManager.Position.Direction;
            set
            {
                _stopManager.Position.Direction = value;
                DrawChart();
            }
        }

        public string SelectedStock
        {
            get => _selectedStock;
            set
            {
                _selectedStock = value;
                LoadChartData();
                RefreshChartPriceSeries();
            }
        }

        private void LoadChartData()
        {
            if (!_prices.ContainsKey(SelectedStock))
            {
                var bars = ReadData($"{SelectedStock}.csv");
                _prices.Add(SelectedStock, bars);
            }

            _stopManager.SetHistoricalBars(_prices[SelectedStock]);
            DrawChart();
        }

        private void DrawChart()
        {
            if (SeriesCollection == null)
            {
                InitChart();
            }

            RefreshStopPriceSeries();
        }

        private void RefreshChartPriceSeries()
        {
            if (SeriesCollection == null)
            {
                InitChart();
            }

            SeriesCollection[0].Values.Clear();
            foreach (var item in _prices[SelectedStock])
            {
                var bar = item.Value;
                SeriesCollection[0].Values.Add(new OhlcPoint(bar.Open, bar.High, bar.Low, bar.Close));
            }

            var months = _stopManager.Bars.Keys.Select(x => x.ToString("MMM")).Distinct();
            Labels = months.ToArray();

            // Stop prices are based off the price
            RefreshStopPriceSeries();
        }

        private void RefreshStopPriceSeries()
        {
            _stops.Clear();
            foreach (var stop in _stopManager.Position.ExitStrategy.Stops)
            {
                stop.Reset();
            }

            foreach (var bar in _stopManager.Bars)
            {
                var stop = _stopManager.GetStop(bar.Key);
                _stops.Add(stop.Price);
            }

            SeriesCollection[1].Values.Clear();
            foreach (var stop in _stops)
            {
                SeriesCollection[1].Values.Add(stop);
            }
        }

        public double InitialTrailingStop
        {
            get => ((TrailingStop)_stopManager.Position.ExitStrategy.Stops[0]).Percentage;
            set
            {
                ((TrailingStop)_stopManager.Position.ExitStrategy.Stops[0]).Percentage = value;
                DrawChart();
            }
        }

        public double FloatingStopInitiateAt
        {
            get => ((StandardStop)_stopManager.Position.ExitStrategy.Stops[1]).InitiateAtGainPercentage.Value;
            set
            {
                var standardStop = (StandardStop)_stopManager.Position.ExitStrategy.Stops[1];
                standardStop.Reset();
                standardStop.InitiateAtGainPercentage = value;
                DrawChart();
            }
        }

        public double FloatingStopPercentage
        {
            get => ((StandardStop)_stopManager.Position.ExitStrategy.Stops[1]).InitialTrailPercentage;
            set
            {
                var standardStop = (StandardStop)_stopManager.Position.ExitStrategy.Stops[1];
                standardStop.Reset();
                standardStop.InitialTrailPercentage = value;
                DrawChart();
            }
        }

        public double ClosingStopInitiateAt
        {
            get => ((ClosingStop)_stopManager.Position.ExitStrategy.Stops[2]).InitiateAtGainPercentage.Value;
            set
            {
                ((ClosingStop)_stopManager.Position.ExitStrategy.Stops[2]).InitiateAtGainPercentage = value;
                DrawChart();
            }
        }

        public double ClosingStopProfitTarget
        {
            get => ((ClosingStop)_stopManager.Position.ExitStrategy.Stops[2]).ProfitTargetPercentage;
            set
            {
                ((ClosingStop)_stopManager.Position.ExitStrategy.Stops[2]).ProfitTargetPercentage = value;
                DrawChart();
            }
        }

        private static BarCollection ReadData(string fileName)
        {
            var bars = new BarCollection();
            using (var reader = new StreamReader(fileName))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var record = new Bar();
                    var records = csv.EnumerateRecords(record);
                    foreach (var r in records)
                    {
                        bars.Add(r.Date, new Bar
                        {
                            Date = r.Date,
                            Open = r.Open,
                            High = r.High,
                            Low = r.Low,
                            Close = r.Close,
                        });
                    }
                }
            }

            return bars;
        }

        private void Init()
        {
            Stocks.Add("ECOM");
            Stocks.Add("PLUG");
            Stocks.Add("INO");

            InitDirectionsList();

            var position = new Position
            {
                Direction = TradeDirection.Long,
                EntryPrice = EntryPrice,
                ExitStrategy = new AggressiveExitStrategy()
            };

            _stopManager.Position = position;
            SelectedStock = "ECOM";
        }

        private void InitDirectionsList()
        {
            foreach (TradeDirection item in Enum.GetValues(typeof(TradeDirection)))
            {
                Directions.Add(item);
            }
        }

        private void InitChart()
        {
            var chartValues = new ChartValues<OhlcPoint>();
            chartValues.AddRange(_stopManager.Bars.Values.Select(b => new OhlcPoint(b.Open, b.High, b.Low, b.Close)));

            var stopValues = new ChartValues<double>();
            stopValues.AddRange(_stops);

            SeriesCollection = new SeriesCollection
            {
                new OhlcSeries
                {
                    Title = "Price",
                    Values = chartValues,
                },
                new LineSeries
                {
                    Title = "Stop",
                    Values = stopValues
                }
            };

            var months = _stopManager.Bars.Keys.Select(x => x.ToString("MMM")).Distinct();
            Labels = months.ToArray();
            YFormatter = value => value.ToString("C");
        }
    }
}