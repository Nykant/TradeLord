﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradeMaster6000.Server.Data;

namespace TradeMaster6000.Server.data.migrations.trade
{
    [DbContext(typeof(TradeDbContext))]
    [Migration("20210629081220_DaTne")]
    partial class DaTne
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("TradeMaster6000.Shared.Candle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal>("Close")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("High")
                        .HasColumnType("decimal(65,30)");

                    b.Property<uint>("InstrumentToken")
                        .HasColumnType("int unsigned");

                    b.Property<DateTime>("Kill")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Low")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Open")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("TicksCount")
                        .HasColumnType("int");

                    b.Property<int>("Timeframe")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("Candles");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.MyTick", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("Flushtime")
                        .HasColumnType("datetime(6)");

                    b.Property<uint>("InstrumentToken")
                        .HasColumnType("int unsigned");

                    b.Property<decimal>("LTP")
                        .HasColumnType("decimal(65,30)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("Ticks");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.OrderUpdate", b =>
                {
                    b.Property<string>("OrderId")
                        .HasColumnType("varchar(255)");

                    b.Property<decimal>("AveragePrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("FilledQuantity")
                        .HasColumnType("int");

                    b.Property<uint>("InstrumentToken")
                        .HasColumnType("int unsigned");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<string>("Status")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("TriggerPrice")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("OrderId");

                    b.ToTable("OrderUpdates");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.TradeInstrument", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Exchange")
                        .HasColumnType("longtext");

                    b.Property<uint>("Token")
                        .HasColumnType("int unsigned");

                    b.Property<string>("TradingSymbol")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("TradeInstruments");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.TradeLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Log")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("TradeOrderId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("TradeOrderId");

                    b.ToTable("TradeLogs");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.TradeOrder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal>("Entry")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("EntryId")
                        .HasColumnType("longtext");

                    b.Property<string>("EntryStatus")
                        .HasColumnType("longtext");

                    b.Property<string>("ExitTransactionType")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsOrderFilling")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("PreSLMCancelled")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<int>("QuantityFilled")
                        .HasColumnType("int");

                    b.Property<bool>("RegularSlmPlaced")
                        .HasColumnType("tinyint(1)");

                    b.Property<decimal>("Risk")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("RxR")
                        .HasColumnType("int");

                    b.Property<string>("SLMId")
                        .HasColumnType("longtext");

                    b.Property<string>("SLMStatus")
                        .HasColumnType("longtext");

                    b.Property<bool>("SquaredOff")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("StopLoss")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("Target")
                        .HasColumnType("decimal(65,30)");

                    b.Property<bool>("TargetHit")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("TargetId")
                        .HasColumnType("longtext");

                    b.Property<bool>("TargetPlaced")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("TargetStatus")
                        .HasColumnType("longtext");

                    b.Property<string>("TradingSymbol")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("TransactionType")
                        .HasColumnType("int");

                    b.Property<decimal>("ZoneWidth")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("TradeOrders");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.Zone", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal>("Bottom")
                        .HasColumnType("decimal(65,30)");

                    b.Property<DateTime>("From")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("InstrumentSymbol")
                        .HasColumnType("longtext");

                    b.Property<int>("StayAway")
                        .HasColumnType("int");

                    b.Property<int>("SupplyDemand")
                        .HasColumnType("int");

                    b.Property<bool>("Tested")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("To")
                        .HasColumnType("datetime(6)");

                    b.Property<decimal>("Top")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("Tradeable")
                        .HasColumnType("int");

                    b.Property<int>("ZoneType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Zones");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.TradeLog", b =>
                {
                    b.HasOne("TradeMaster6000.Shared.TradeOrder", "TradeOrder")
                        .WithMany("TradeLogs")
                        .HasForeignKey("TradeOrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TradeOrder");
                });

            modelBuilder.Entity("TradeMaster6000.Shared.TradeOrder", b =>
                {
                    b.Navigation("TradeLogs");
                });
#pragma warning restore 612, 618
        }
    }
}