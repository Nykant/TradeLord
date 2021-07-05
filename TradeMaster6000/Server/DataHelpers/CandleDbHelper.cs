using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;
using OfficeOpenXml;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TradeMaster6000.Server.DataHelpers
{
    public class CandleDbHelper : ICandleDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        private readonly ILogger<CandleDbHelper> logger;
        public CandleDbHelper(IDbContextFactory<TradeDbContext> contextFactory, ILogger<CandleDbHelper> logger)
        {
            this.logger = logger;
            this.contextFactory = contextFactory;
        }

        public async Task MarkCandlesUsed(DateTime To, uint token)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                var candles = context.Candles.Where(x => x.InstrumentToken == token && x.Used == false);
                foreach(var candle in candles)
                {
                    if(candle.Timestamp < To)
                    {
                        candle.Used = true;
                        context.Candles.Update(candle);
                    }
                }
                await context.SaveChangesAsync();
            }
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
                                    Decimal.TryParse(value.ToString(), out decimal result3);
                                    candle.High = result3;
                                    break;
                                case 4:
                                    Decimal.TryParse(value.ToString(), out decimal result2);
                                    candle.Low = result2;
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
                        context.Candles.Add(candle);
                    }
                    context.SaveChanges();
                }
            }
        }

        public async Task<List<Candle>> GetAllCandles(uint instrumentToken)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.Where(x => x.InstrumentToken == instrumentToken).OrderBy(x => x.Timestamp).ToListAsync();
            }
        }

        public async Task<List<Candle>> GetUnusedCandles(uint instrumentToken)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.Where(x=>x.InstrumentToken == instrumentToken && !x.Used).OrderBy(x => x.Timestamp).ToListAsync();
            }
        }

        public List<Candle> GetCandles(uint instrumentToken, DateTime time)
        {
            try
            {
                using (var context = contextFactory.CreateDbContext())
                {
                    return context.Candles.Where(x => x.InstrumentToken == instrumentToken && DateTime.Compare(x.Timestamp, time) > 0).OrderBy(x => x.Timestamp).ToList();
                }
            }
            catch (Exception e)
            {
                logger.LogInformation(e.Message);
            }
            return default;
        }

        public async Task<Candle> GetCandle(DateTime time)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.FirstOrDefaultAsync(x => x.Timestamp.Hour == time.Hour && x.Timestamp.Minute == time.Minute);
            }
        }

        public async Task<Candle> GetLastCandle()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                var candles = await context.Candles.OrderBy(x => x.Timestamp).ToListAsync(); // debug det her!
                return candles[^1];
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
                var candles = context.Candles;
                foreach (var candle in candles)
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
        Task<List<Candle>> GetAllCandles(uint instrumentToken);
        Task<List<Candle>> GetUnusedCandles(uint instrumentToken);
        List<Candle> GetCandles(uint instrumentToken, DateTime time);
        Task<Candle> GetCandle(DateTime time);
        Task Flush();
        Task Add(List<Candle> candles);
        void LoadExcelCandles();
        Task MarkCandlesUsed(DateTime To, uint token);
        Task<Candle> GetLastCandle();
    }
}
