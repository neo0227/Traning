﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCTraning.StockTicker
{
	public class Stock
	{
		private decimal _price;
		[Key]
		[Required] 
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int StockID { set; get; }

		[MaxLength(10)]
		[Required] 
		public string Symbol { get; set; }

		[NotMapped]
		public decimal DayLow { get; private set; }

		[NotMapped]
		public decimal DayHigh { get; private set; }

		[NotMapped]
		public decimal LastChange { get; private set; }

		[Required] 
		public decimal Price
		{
			get
			{
				return _price;
			}
			set
			{
				if (_price == value)
				{
					return;
				}

				LastChange = value - _price;
				_price = value;

				if (DayOpen == 0)
				{
					DayOpen = _price;
				}
				if (_price < DayLow || DayLow == 0)
				{
					DayLow = _price;
				}
				if (_price > DayHigh)
				{
					DayHigh = _price;
				}
			}
		}

		[NotMapped] 
		public decimal DayOpen { get; private set; }

		[NotMapped] 
		public decimal Change
		{
			get
			{
				return Price - DayOpen;
			}
		}

		[NotMapped] 
		public double PercentChange
		{
			get
			{
				return (double)Math.Round(Change / Price, 4);
			}
		}
	}

	public class StockDBContext : DbContext
	{
		public StockDBContext() : base("StockDBContext") { }
		public DbSet<Stock> Stocks { get; set; }
	}

}
