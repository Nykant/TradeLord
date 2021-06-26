using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;
using OfficeOpenXml;
using System.IO;

namespace TradeMaster6000.Server.DataHelpers
{
    public class CandleDbHelper : ICandleDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;

        public CandleDbHelper(IDbContextFactory<TradeDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void LoadExcelCandles()
        {
            FileInfo existingFile = new FileInfo(@"C:\Users\Christian\Documents\excel_candles\ACC-a-lot.xlsx");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            List<Candle> excelCandles = new List<Candle>();
            using (ExcelPackage package = new ExcelPackage(existingFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int colCount = worksheet.Dimension.End.Column;
                int rowCount = worksheet.Dimension.End.Row;
                for (int row = 2; row <= rowCount; row++)
                {
                    Candle candle = new Candle();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var value = worksheet.Cells[row, col].Value;
                        if(value != null)
                        {
                            switch (col)
                            {
                                case 1:
                                    UInt32.TryParse(value.ToString(), out uint result);
                                    candle.InstrumentToken = result;
                                    break;
                                case 2:
                                    Decimal.TryParse(value.ToString(), out decimal result1);
                                    candle.Open = result1;
                                    break;
                                case 3:
                                    Decimal.TryParse(value.ToString(), out decimal result2);
                                    candle.High = result2;
                                    break;
                                case 4:
                                    Decimal.TryParse(value.ToString(), out decimal result3);
                                    candle.Low = result3;
                                    break;
                                case 5:
                                    Decimal.TryParse(value.ToString(), out decimal result4);
                                    candle.Close = result4;
                                    break;
                                case 6:
                                    candle.Timestamp = DateTime.FromOADate((double)value);
                                    break;
                            }
                        }
                    }
                    excelCandles.Add(candle);
                }
                using (var context = contextFactory.CreateDbContext())
                { 
                    foreach(var candle in excelCandles)
                    {
                        context.Add(candle);
                    }
                    context.SaveChanges();
                }
            }
        }

        public async Task<List<Candle>> GetCandles()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.ToListAsync();
            }
        }

        public async Task<List<Candle>> GetCandles(uint instrumentToken)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.Where(x=>x.InstrumentToken == instrumentToken).ToListAsync();
            }
        }

        public async Task<Candle> GetCandle(DateTime time)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.FirstOrDefaultAsync(x => x.Timestamp.Hour == time.Hour && x.Timestamp.Minute == time.Minute);
            }
        }

        public async Task<Candle> AddCandle(Candle candle)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                var added = await context.Candles.AddAsync(candle);
                await context.SaveChangesAsync();
                return added.Entity;
            }
        }

        public async Task Add(List<Candle> candles)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                foreach(var candle in candles)
                {
                    await context.Candles.AddAsync(candle);
                }
                
                await context.SaveChangesAsync();
            }
        }

        public async Task Flush()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                foreach (var candle in context.Candles)
                {
                    if (DateTime.Compare(candle.Kill, DateTime.Now) < 0)
                    {
                        context.Remove(candle);
                    }
                }
                await context.SaveChangesAsync();
            }
        }
    }
    public interface ICandleDbHelper
    {
        Task<Candle> AddCandle(Candle candle);
        Task<List<Candle>> GetCandles();
        Task<List<Candle>> GetCandles(uint instrumentToken);
        Task Flush();
        Task Add(List<Candle> candles);
        void LoadExcelCandles();
    }
}
