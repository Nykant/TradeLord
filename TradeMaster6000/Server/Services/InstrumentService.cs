using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeMaster6000.Server.Data;
using TradeMaster6000.Shared;

namespace TradeMaster6000.Server.Services
{
    public class InstrumentService : IInstrumentService
    {
        private readonly List<TradeInstrument> instruments;
        public InstrumentService()
        {
            instruments = new List<TradeInstrument>() {
                new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4583169,
                    TradingSymbol = "ABBOTINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 5633,
                    TradingSymbol = "ACC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 6401,
                    TradingSymbol = "ADANIENT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 912129,
                    TradingSymbol = "ADANIGREEN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3861249,
                    TradingSymbol = "ADANIPORTS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2615553,
                    TradingSymbol = "ADANITRANS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2995969,
                    TradingSymbol = "ALKEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 325121,
                    TradingSymbol = "AMBUJACEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 40193,
                    TradingSymbol = "APOLLOHOSP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 60417,
                    TradingSymbol = "ASIANPAINT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 70401,
                    TradingSymbol = "AUROPHARMA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1510401,
                    TradingSymbol = "AXISBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4267265,
                    TradingSymbol = "BAJAJ-AUTO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4268801,
                    TradingSymbol = "BAJAJFINSV"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 78081,
                    TradingSymbol = "BAJAJHLDNG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 81153,
                    TradingSymbol = "BAJFINANCE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 579329,
                    TradingSymbol = "BANDHANBNK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 103425,
                    TradingSymbol = "BERGEPAINT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2714625,
                    TradingSymbol = "BHARTIARTL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2911489,
                    TradingSymbol = "BIOCON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 558337,
                    TradingSymbol = "BOSCHLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 134657,
                    TradingSymbol = "BPCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 140033,
                    TradingSymbol = "BRITANNIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2029825,
                    TradingSymbol = "CADILAHC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 177665,
                    TradingSymbol = "CIPLA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 5215745,
                    TradingSymbol = "COALINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3876097,
                    TradingSymbol = "COLPAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 197633,
                    TradingSymbol = "DABUR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2800641,
                    TradingSymbol = "DIVISLAB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3771393,
                    TradingSymbol = "DLF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 5097729,
                    TradingSymbol = "DMART"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 225537,
                    TradingSymbol = "DRREDDY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 232961,
                    TradingSymbol = "EICHERMOT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1207553,
                    TradingSymbol = "GAIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2585345,
                    TradingSymbol = "GODREJCP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 315393,
                    TradingSymbol = "GRASIM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2513665,
                    TradingSymbol = "HAVELLS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1850625,
                    TradingSymbol = "HCLTECH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 340481,
                    TradingSymbol = "HDFC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1086465,
                    TradingSymbol = "HDFCAMC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 341249,
                    TradingSymbol = "HDFCBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 119553,
                    TradingSymbol = "HDFCLIFE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 345089,
                    TradingSymbol = "HEROMOTOCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 348929,
                    TradingSymbol = "HINDALCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 359937,
                    TradingSymbol = "HINDPETRO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 356865,
                    TradingSymbol = "HINDUNILVR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1270529,
                    TradingSymbol = "ICICIBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 5573121,
                    TradingSymbol = "ICICIGI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4774913,
                    TradingSymbol = "ICICIPRULI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2883073,
                    TradingSymbol = "IGL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2865921,
                    TradingSymbol = "INDIGO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1346049,
                    TradingSymbol = "INDUSINDBK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 7458561,
                    TradingSymbol = "INDUSTOWER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 408065,
                    TradingSymbol = "INFY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 415745,
                    TradingSymbol = "IOC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 424961,
                    TradingSymbol = "ITC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3001089,
                    TradingSymbol = "JSWSTEEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4632577,
                    TradingSymbol = "JUBLFOOD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 492033,
                    TradingSymbol = "KOTAKBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2939649,
                    TradingSymbol = "LT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4561409,
                    TradingSymbol = "LTI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2672641,
                    TradingSymbol = "LUPIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 519937,
                    TradingSymbol = "M&M"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1041153,
                    TradingSymbol = "MARICO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2815745,
                    TradingSymbol = "MARUTI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2674433,
                    TradingSymbol = "MCDOWELL-N"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 1076225,
                    TradingSymbol = "MOTHERSUMI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 582913,
                    TradingSymbol = "MRF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 6054401,
                    TradingSymbol = "MUTHOOTFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3520257,
                    TradingSymbol = "NAUKRI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4598529,
                    TradingSymbol = "NESTLEIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3924993,
                    TradingSymbol = "NMDC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2977281,
                    TradingSymbol = "NTPC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 633601,
                    TradingSymbol = "ONGC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 617473,
                    TradingSymbol = "PEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2905857,
                    TradingSymbol = "PETRONET"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 648961,
                    TradingSymbol = "PGHH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 681985,
                    TradingSymbol = "PIDILITIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2730497,
                    TradingSymbol = "PNB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3834113,
                    TradingSymbol = "POWERGRID"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 738561,
                    TradingSymbol = "RELIANCE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4600577,
                    TradingSymbol = "SBICARD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 5582849,
                    TradingSymbol = "SBILIFE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 779521,
                    TradingSymbol = "SBIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 794369,
                    TradingSymbol = "SHREECEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 806401,
                    TradingSymbol = "SIEMENS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 857857,
                    TradingSymbol = "SUNPHARMA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 878593,
                    TradingSymbol = "TATACONSUM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 884737,
                    TradingSymbol = "TATAMOTORS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 895745,
                    TradingSymbol = "TATASTEEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2953217,
                    TradingSymbol = "TCS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3465729,
                    TradingSymbol = "TECHM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 897537,
                    TradingSymbol = "TITAN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 900609,
                    TradingSymbol = "TORNTPHARM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 4278529,
                    TradingSymbol = "UBL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2952193,
                    TradingSymbol ="ULTRACEMCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 2889473,
                    TradingSymbol = "UPL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 784129,
                    TradingSymbol = "VEDL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 969473,
                    TradingSymbol = "WIPRO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token = 3050241,
                    TradingSymbol = "YESBANK"
                }
            };
        }

        public List<TradeInstrument> GetInstruments()
        {
            return instruments;
        }
    }
    public interface IInstrumentService
    {
        List<TradeInstrument> GetInstruments();
    }
}
