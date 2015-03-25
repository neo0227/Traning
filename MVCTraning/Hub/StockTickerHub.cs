﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace MVCTraning.StockTicker
{
	[HubName("stockTickerMini")]
	public class StockTickerHub : Hub
	{
		private readonly StockTicker _stockTicker;

		public StockTickerHub() : this(StockTicker.Instance) { }

		public StockTickerHub(StockTicker stockTicker)
		{
			_stockTicker = stockTicker;
		}

		public string GetMarketState()
		{
			return _stockTicker.MarketState.ToString();
		}

		public void OpenMarket()
		{
			_stockTicker.OpenMarket();
		}

		public void CloseMarket()
		{
			_stockTicker.CloseMarket();
		}

		public void Reset()
		{
			_stockTicker.Reset();
		}

		public IEnumerable<Stock> GetAllStocks()
		{
			return _stockTicker.GetAllStocks();
		}
	}
}