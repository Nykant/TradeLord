using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Data
{
    public class TradeDbContext : DbContext
    {
        public TradeDbContext(DbContextOptions<TradeDbContext> options) : base(options)
        {
        }

        public DbSet<TradeOrder> TradeOrders { get; set; }
        public DbSet<TradeLog> TradeLogs { get; set; }
        public DbSet<TradeInstrument> TradeInstruments { get; set; }
        public DbSet<Candle> Candles { get; set; }
        public DbSet<MyTick> Ticks { get; set; }
        public DbSet<OrderUpdate> OrderUpdates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TradeOrder>().Ignore(e => e.TokenSource);
            modelBuilder.Entity<TradeOrder>().Ignore(e => e.Instrument);
        }
    }
}
