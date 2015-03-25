using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace MVCTraning.StockTicker
{
	public class StockTicker
	{
		// Singleton instance
		private readonly static Lazy<StockTicker> _instance = new Lazy<StockTicker>(() => new StockTicker(GlobalHost.ConnectionManager.GetHubContext<StockTickerHub>().Clients));

		private readonly ConcurrentDictionary<string, Stock> _stocks = new ConcurrentDictionary<string, Stock>();

		private readonly object _updateStockPricesLock = new object();
		private readonly object _marketStateLock = new object();

		//stock can go up or down by a percentage of this factor on each change
		private readonly double _rangePercent = 0.002;

		private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(250);
		private readonly Random _updateOrNotRandom = new Random();

		private Timer _timer;
		private volatile bool _updatingStockPrices = false;
		private volatile MarketState _marketState;

		public MarketState MarketState
		{
			get { return _marketState; }
			private set { _marketState = value; }
		}

		private StockDBContext stocksContext = new StockDBContext();

		private StockTicker(IHubConnectionContext<dynamic> clients)
		{
			Clients = clients;
			LoadDefaultStocks();
		}

		public static StockTicker Instance
		{
			get
			{
				return _instance.Value;
			}
		}

		private IHubConnectionContext<dynamic> Clients
		{
			get;
			set;
		}

		public IEnumerable<Stock> GetAllStocks()
		{
			return _stocks.Values;
		}

		public void OpenMarket()
		{
			lock (_marketStateLock)
			{
				if (MarketState != MarketState.Open)
				{
					_timer = new Timer(UpdateStockPrices, null, _updateInterval, _updateInterval);
					MarketState = MarketState.Open;
					BroadcastMarketStateChange(MarketState.Open);
				}
			}
		}

		public void CloseMarket()
		{
			lock (_marketStateLock)
			{
				if (MarketState == MarketState.Open)
				{
					if (_timer != null)
					{
						_timer.Dispose();
					}
					MarketState = MarketState.Closed;
					BroadcastMarketStateChange(MarketState.Closed);
				}
			}
		}

		public void Reset()
		{
			lock (_marketStateLock)
			{
				if (MarketState != MarketState.Closed)
				{
					throw new InvalidOperationException("Market must be closed before it can be reset.");
				}
				LoadDefaultStocks();
				BroadcastMarketReset();
			}
		}

		private void LoadDefaultStocks()
		{
			_stocks.Clear();

			List<Stock> stocks = stocksContext.Stocks.ToList();
			stocks.ForEach(Obj => _stocks.TryAdd(Obj.Symbol, Obj));

		}


		private void UpdateStockPrices(object state)
		{
			// This function must be re-entrant as it's running as a timer interval handler
			lock (_updateStockPricesLock)
			{
				if (!_updatingStockPrices)
				{
					_updatingStockPrices = true;

					foreach (var stock in _stocks.Values)
					{
						if (TryUpdateStockPrice(stock))
						{
							BroadcastStockPrice(stock);
						}
					}

					_updatingStockPrices = false;
				}
			}
		}

		private bool TryUpdateStockPrice(Stock stock)
		{
			// Randomly choose whether to update this stock or not
			var r = _updateOrNotRandom.NextDouble();
			if (r > .1)
			{
				return false;
			}

			// Update the stock price by a random factor of the range percent
			var random = new Random((int)Math.Floor(stock.Price));
			var percentChange = random.NextDouble() * _rangePercent;
			//var pos = random.NextDouble() > .51;
			var pos = random.NextDouble() > 0.71;
			var change = Math.Round(stock.Price * (decimal)percentChange, 2);
			change = pos ? change : -change;

			stock.Price += change;
			return true;
		}

		private void BroadcastStockPrice(Stock stock)
		{
			Clients.All.updateStockPrice(stock);
		}


		private void BroadcastMarketStateChange(MarketState marketState)
		{
			switch (marketState)
			{
				case MarketState.Open:
					Clients.All.marketOpened();
					break;
				case MarketState.Closed:
					Clients.All.marketClosed();
					break;
				default:
					break;
			}
		}

		private void BroadcastMarketReset()
		{
			Clients.All.marketReset();
		}

	}

	public enum MarketState
	{
		Closed,
		Open
	}

}