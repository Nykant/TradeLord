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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics;

namespace TradeMaster6000.Server.DataHelpers
{
    public class CandleDbHelper : ICandleDbHelper
    {
        private readonly IDbContextFactory<TradeDbContext> contextFactory;
        private readonly IInstrumentHelper instrumentHelper;
        private readonly ILogger<CandleDbHelper> logger;
        public CandleDbHelper(IDbContextFactory<TradeDbContext> contextFactory, ILogger<CandleDbHelper> logger, IInstrumentHelper instrumentHelper)
        {
            this.logger = logger;
            this.contextFactory = contextFactory;
            this.instrumentHelper = instrumentHelper;
        }

        public async Task DeleteCandles(List<Candle> candles)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.Candles.RemoveRange(candles);
                await context.SaveChangesAsync();
            }
        }

        //public async Task MarkCandlesTransformed(List<Candle> candles)
        //{
        //    for(int i = 0, n = candles.Count; i < n; i++)
        //    {
        //        candles[i].Transformed = true;
        //    }

        //    using (var context = contextFactory.CreateDbContext())
        //    {
        //        context.Candles.UpdateRange(candles);
        //        await context.SaveChangesAsync();
        //    }
        //}

        public async Task MarkCandlesUsed(List<Candle> candles)
        {
            for (int i = 0, n = candles.Count; i < n; i++)
            {
                candles[i].Used = true;
            }

            using (var context = contextFactory.CreateDbContext())
            {
                context.Candles.UpdateRange(candles);
                await context.SaveChangesAsync();
            }
        }

        public async Task LoadExcelCandles()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<TradeInstrument> instruments = await instrumentHelper.GetTradeInstruments();
            FileInfo existingFile = new FileInfo(@"C:\Users\Christian\Documents\excel_candles\Sheet1.xlsx");
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
                        if (value != null)
                        {
                            switch (col)
                            {
                                case 1:
                                    if (value.ToString() == "stockname" || value.ToString() == "BANKNIFTY")
                                    {
                                        goto Skip;
                                    }
                                    uint token = instruments.Find(x => x.TradingSymbol == value.ToString()).Token;
                                    candle.InstrumentToken = token;
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
                    Skip:;
                }
            }

            existingFile = new FileInfo(@"C:\Users\Christian\Documents\excel_candles\Sheet2.xlsx");
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
                        if (value != null)
                        {
                            switch (col)
                            {
                                case 1:
                                    if (value.ToString() == "stockname" || value.ToString() == "BANKNIFTY")
                                    {
                                        goto Skip;
                                    }
                                    uint token = instruments.Find(x => x.TradingSymbol == value.ToString()).Token;
                                    candle.InstrumentToken = token;
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
                    Skip:;
                }
            }

            existingFile = new FileInfo(@"C:\Users\Christian\Documents\excel_candles\Sheet3.xlsx");
            using (ExcelPackage package = new ExcelPackage(existingFile))
            {
                for(int i = 0, n = package.Workbook.Worksheets.Count; i < n; i++)
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[i];
                    int colCount = worksheet.Dimension.End.Column;
                    int rowCount = worksheet.Dimension.End.Row;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        Candle candle = new Candle();
                        for (int col = 1; col <= colCount; col++)
                        {
                            var value = worksheet.Cells[row, col].Value;
                            if (value != null)
                            {
                                switch (col)
                                {
                                    case 1:
                                        if (value.ToString() == "stockname")
                                        {
                                            goto Skip;
                                        }
                                        uint token = instruments.Find(x => x.TradingSymbol == value.ToString()).Token;
                                        candle.InstrumentToken = token;
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
                        Skip:;
                    }
                }
            }

            using (var context = contextFactory.CreateDbContext())
            {
                await context.Candles.BulkInsertAsync(excelCandles);
                await context.BulkSaveChangesAsync();
            }

            stopwatch.Stop();
            logger.LogInformation($"excel candles loaded - elapsed time: {stopwatch.ElapsedMilliseconds}");
        }

        public async Task<List<Candle>> GetCandles(int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.Timeframe == timeframe);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }

        public async Task<List<Candle>> GetCandles(uint instrumentToken)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.InstrumentToken == instrumentToken);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }

        public async Task<List<Candle>> GetAll5minCandles()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.Timeframe == 5);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }

        public async Task<List<Candle>> GetUnusedCandles(uint instrumentToken, int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.InstrumentToken == instrumentToken && x.Used == false && x.Timeframe == timeframe);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }
        public async Task<List<Candle>> GetUnusedNonBaseCandles()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.Used == false && x.Timeframe != 1);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }
        public async Task<List<Candle>> GetNonBaseCandles()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.Timeframe != 1);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }
        public async Task<List<Candle>> GetCandlesBefore(uint instrumentToken, DateTime time, int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.InstrumentToken == instrumentToken && DateTime.Compare(x.Timestamp, time) < 0 && x.Timeframe == timeframe);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }


        public async Task<List<Candle>> GetCandlesAfter(uint instrumentToken, DateTime time, int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking().Where(x => x.InstrumentToken == instrumentToken && DateTime.Compare(x.Timestamp, time) > 0 && x.Timeframe == timeframe);
                return await candles.OrderBy(x => x.Timestamp).ToListAsync();
            }
        }

        public async Task<Candle> GetCandle(DateTime time, int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.AsNoTracking().FirstOrDefaultAsync(x => x.Timestamp.Hour == time.Hour && x.Timestamp.Minute == time.Minute && x.Timeframe == timeframe);
            }
        }

        public async Task<Candle> GetLastCandle(int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                IQueryable<Candle> candles = context.Candles.AsNoTracking();
                var candleslist = await candles.Where(x => x.Timeframe == timeframe).OrderBy(x => x.Timestamp).ToListAsync();
                return candleslist[^1];
            }
        }

        public async Task<Candle> AddCandle(Candle candle)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                EntityEntry<Candle> entry = context.Candles.Add(candle);
                await context.SaveChangesAsync();
                return entry.Entity;
            }
        }

        public async Task Add(List<Candle> candles)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(50000));
                await context.Candles.BulkInsertAsync(candles);
                await context.BulkSaveChangesAsync();
            }
        }

        public async Task Flush()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.Candles.RemoveRange(context.Candles.Where(x => DateTime.Compare(x.Kill, DateTime.Now) < 0));
                await context.SaveChangesAsync();
            }
        }

        public async Task MarkAllCandlesUnused()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                List<Candle> candles = context.Candles.Where(x => x.Used == true).ToList();

                for(int i = 0, n = candles.Count; i < n; i++)
                {
                    candles[i].Used = false;
                }
                context.Candles.UpdateRange(candles);

                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Candle>> GetUnusedCandles(int timeframe)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Candles.Where(x => x.Timeframe == timeframe && !x.Used).ToListAsync();
            }
        }

        //public async Task<List<Candle>> GetUntransformedCandles(int timeframe)
        //{
        //    using (var context = contextFactory.CreateDbContext())
        //    {
        //        return await context.Candles.Where(x => x.Timeframe == timeframe && !x.Transformed).ToListAsync();
        //    }
        //}

        public async Task Update(List<Candle> candles)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(50000));
                await context.Candles.BulkUpdateAsync(candles);
                await context.BulkSaveChangesAsync();
            }
        }

    }
    public interface ICandleDbHelper
    {
        Task Update(List<Candle> candles);
        Task MarkAllCandlesUnused();
        Task<Candle> AddCandle(Candle candle);
        Task<List<Candle>> GetCandles(uint instrumentToken);
        Task<List<Candle>> GetCandles(int timeframe);
        Task<List<Candle>> GetAll5minCandles();
        Task<List<Candle>> GetUnusedCandles(uint instrumentToken, int timeframe);
        Task<List<Candle>> GetUnusedCandles(int timeframe);
        Task<List<Candle>> GetCandlesBefore(uint instrumentToken, DateTime time, int timeframe);
        Task<List<Candle>> GetCandlesAfter(uint instrumentToken, DateTime time, int timeframe);
        Task<Candle> GetCandle(DateTime time, int timeframe);
        Task Flush();
        Task Add(List<Candle> candles);
        Task LoadExcelCandles();
        Task MarkCandlesUsed(List<Candle> candles);
        Task<Candle> GetLastCandle(int timeframe);
        Task DeleteCandles(List<Candle> candles);
        //Task MarkCandlesTransformed(List<Candle> candles);
        Task<List<Candle>> GetUnusedNonBaseCandles();
        Task<List<Candle>> GetNonBaseCandles();
    }
}
