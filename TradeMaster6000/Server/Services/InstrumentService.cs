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
                    Token =121345,
                    TradingSymbol = "3MINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1147137,
                    TradingSymbol = "AARTIDRUGS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1793,
                    TradingSymbol = "AARTIIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1378561,
                    TradingSymbol = "AAVAS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3329,
                    TradingSymbol = "ABB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4583169,
                    TradingSymbol = "ABBOTINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5533185,
                    TradingSymbol = "ABCAPITAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7707649,
                    TradingSymbol = "ABFRL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5633,
                    TradingSymbol = "ACC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1805569,
                    TradingSymbol = "ACCELYA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6401,
                    TradingSymbol = "ADANIENT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3861249,
                    TradingSymbol = "ADANIPORTS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4451329,
                    TradingSymbol = "ADANIPOWER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4617985,
                    TradingSymbol = "ADVENZYMES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =10241,
                    TradingSymbol = "AEGISCHEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2903809,
                    TradingSymbol = "AFFLE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3350017,
                    TradingSymbol = "AIAENG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2079745,
                    TradingSymbol = "AJANTPHARM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =375553,
                    TradingSymbol = "AKZOINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =20225,
                    TradingSymbol = "ALEMBICLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2995969,
                    TradingSymbol = "ALKEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3456257,
                    TradingSymbol = "ALLCARGO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4524801,
                    TradingSymbol = "ALOKINDS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =25601,
                    TradingSymbol = "AMARAJABAT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =325121,
                    TradingSymbol = "AMBUJACEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4492033,
                    TradingSymbol = "AMRUTANJAN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2941697,
                    TradingSymbol = "APARINDS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6483969,
                    TradingSymbol = "APLLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =40193,
                    TradingSymbol = "APOLLOHOSP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =41729,
                    TradingSymbol = "APOLLOTYRE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1376769,
                    TradingSymbol = "ASAHIINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5166593,
                    TradingSymbol = "ASHOKA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =54273,
                    TradingSymbol = "ASHOKLEY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =60417,
                    TradingSymbol = "ASIANPAINT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =386049,
                    TradingSymbol = "ASTERDM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3691009,
                    TradingSymbol = "ASTRAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =425729,
                    TradingSymbol = "ATFL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =67329,
                    TradingSymbol = "ATUL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5436929,
                    TradingSymbol = "AUBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =70401,
                    TradingSymbol = "AUROPHARMA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2031617,
                    TradingSymbol = "AVANTIFEED"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1510401,
                    TradingSymbol = "AXISBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4267265,
                    TradingSymbol = "BAJAJ-AUTO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4999937,
                    TradingSymbol = "BAJAJCON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3848705,
                    TradingSymbol = "BAJAJELEC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4268801,
                    TradingSymbol = "BAJAJFINSV"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =78081,
                    TradingSymbol = "BAJAJHLDNG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =81153,
                    TradingSymbol = "BAJFINANCE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =85761,
                    TradingSymbol = "BALKRISIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =86529,
                    TradingSymbol = "BALMLAWRIE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =89601,
                    TradingSymbol = "BANARISUG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =579329,
                    TradingSymbol = "BANDHANBNK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1195009,
                    TradingSymbol = "BANKBARODA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2928385,
                    TradingSymbol = "BANKBEES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1214721,
                    TradingSymbol = "BANKINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =94209,
                    TradingSymbol = "BASF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =94977,
                    TradingSymbol = "BATAINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4589313,
                    TradingSymbol = "BAYERCROP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =97281,
                    TradingSymbol = "BBTC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =548865,
                    TradingSymbol = "BDL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =98049,
                    TradingSymbol = "BEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =101121,
                    TradingSymbol = "BEML"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =103425,
                    TradingSymbol = "BERGEPAINT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =108033,
                    TradingSymbol = "BHARATFORG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =981505,
                    TradingSymbol = "BHARATRAS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2714625,
                    TradingSymbol = "BHARTIARTL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =112129,
                    TradingSymbol = "BHEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2911489,
                    TradingSymbol = "BIOCON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4931841,
                    TradingSymbol = "BLISSGVS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =126721,
                    TradingSymbol = "BLUEDART"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2127617,
                    TradingSymbol = "BLUESTARCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =131329,
                    TradingSymbol = "BOMDYEING"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =558337,
                    TradingSymbol = "BOSCHLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =134657,
                    TradingSymbol = "BPCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3887105,
                    TradingSymbol = "BRIGADE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =140033,
                    TradingSymbol = "BRITANNIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5013761,
                    TradingSymbol = "BSE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1790465,
                    TradingSymbol = "BSOFT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2029825,
                    TradingSymbol = "CADILAHC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =87553,
                    TradingSymbol = "CAMS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2763265,
                    TradingSymbol = "CANBK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =149249,
                    TradingSymbol = "CANFINHOME"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =999937,
                    TradingSymbol = "CAPLIPOINT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =152321,
                    TradingSymbol = "CARBORUNIV"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7452929,
                    TradingSymbol = "CARERATING"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =320001,
                    TradingSymbol = "CASTROLIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2931713,
                    TradingSymbol = "CCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3905025,
                    TradingSymbol = "CEATLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3812865,
                    TradingSymbol = "CENTRALBK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =160001,
                    TradingSymbol = "CENTURYTEX"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3849985,
                    TradingSymbol = "CERA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =160769,
                    TradingSymbol = "CESC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2187777,
                    TradingSymbol = "CHALET"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =163073,
                    TradingSymbol = "CHAMBLFERT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =69121,
                    TradingSymbol = "CHEMCON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =175361,
                    TradingSymbol = "CHOLAFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5565441,
                    TradingSymbol = "CHOLAHLDNG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =177665,
                    TradingSymbol = "CIPLA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5215745,
                    TradingSymbol = "COALINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5506049,
                    TradingSymbol = "COCHINSHIP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2955009,
                    TradingSymbol = "COFORGE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3876097,
                    TradingSymbol = "COLPAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1215745,
                    TradingSymbol = "CONCOR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =189185,
                    TradingSymbol = "COROMANDEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1131777,
                    TradingSymbol = "CREDITACC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =193793,
                    TradingSymbol = "CRISIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4376065,
                    TradingSymbol = "CROMPTON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3831297,
                    TradingSymbol = "CSBBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1459457,
                    TradingSymbol = "CUB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =486657,
                    TradingSymbol = "CUMMINSIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =197633,
                    TradingSymbol = "DABUR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4577537,
                    TradingSymbol = "DBCORP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4630017,
                    TradingSymbol = "DBL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3513601,
                    TradingSymbol = "DCBBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =207617,
                    TradingSymbol = "DCMSHRIRAM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5105409,
                    TradingSymbol = "DEEPAKNTR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3851265,
                    TradingSymbol = "DELTACORP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4536833,
                    TradingSymbol = "DEN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4933377,
                    TradingSymbol = "DFMFOODS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6248705,
                    TradingSymbol = "DHANUKA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5591041,
                    TradingSymbol = "DIAMONDYD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2800641,
                    TradingSymbol = "DIVISLAB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3771393,
                    TradingSymbol = "DLF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5097729,
                    TradingSymbol = "DMART"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =225537,
                    TradingSymbol = "DRREDDY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =232961,
                    TradingSymbol = "EICHERMOT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =235265,
                    TradingSymbol = "EIHOTEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4818433,
                    TradingSymbol = "ENDURANCE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1256193,
                    TradingSymbol = "ENGINERSIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =251137,
                    TradingSymbol = "EPL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4314113,
                    TradingSymbol = "EQUITAS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5415425,
                    TradingSymbol = "ERIS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =244481,
                    TradingSymbol = "ESABINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =245249,
                    TradingSymbol = "ESCORTS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =173057,
                    TradingSymbol = "EXIDEIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =258049,
                    TradingSymbol = "FACT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7689729,
                    TradingSymbol = "FCONSUMER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1253889,
                    TradingSymbol = "FDC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =261889,
                    TradingSymbol = "FEDERALBNK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =265729,
                    TradingSymbol = "FINCABLES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =958465,
                    TradingSymbol = "FINEORG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3520001,
                    TradingSymbol = "FLUOROCHEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =304641,
                    TradingSymbol = "FMGOETZE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2962689,
                    TradingSymbol = "FORCEMOT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3735553,
                    TradingSymbol = "FORTIS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4704769,
                    TradingSymbol = "FRETAIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3661825,
                    TradingSymbol = "FSL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =277761,
                    TradingSymbol = "GABRIEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2259969,
                    TradingSymbol = "GAEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1207553,
                    TradingSymbol = "GAIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =336641,
                    TradingSymbol = "GALAXYSURF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =281601,
                    TradingSymbol = "GARFIBRES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2012673,
                    TradingSymbol = "GEPIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3526657,
                    TradingSymbol = "GESHIP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4296449,
                    TradingSymbol = "GET&D"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =70913,
                    TradingSymbol = "GICRE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =403457,
                    TradingSymbol = "GILLETTE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =295169,
                    TradingSymbol = "GLAXO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1895937,
                    TradingSymbol = "GLENMARK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1332225,
                    TradingSymbol = "GMDCLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =401921,
                    TradingSymbol = "GMMPFAUDLR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3463169,
                    TradingSymbol = "GMRINFRA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =302337,
                    TradingSymbol = "GODFRYPHLP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =36865,
                    TradingSymbol = "GODREJAGRO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2585345,
                    TradingSymbol = "GODREJCP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2796801,
                    TradingSymbol = "GODREJIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4576001,
                    TradingSymbol = "GODREJPROP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =308481,
                    TradingSymbol = "GOODYEAR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5051137,
                    TradingSymbol = "GPPL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3039233,
                    TradingSymbol = "GRANULES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =151553,
                    TradingSymbol = "GRAPHITE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =315393,
                    TradingSymbol = "GRASIM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =316161,
                    TradingSymbol = "GREAVESCOT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1020673,
                    TradingSymbol = "GREENPLY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3471361,
                    TradingSymbol = "GRINDWELL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1401601,
                    TradingSymbol = "GRSE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =319233,
                    TradingSymbol = "GSFC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3378433,
                    TradingSymbol = "GSPL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =324353,
                    TradingSymbol = "GUJALKALI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2713345,
                    TradingSymbol = "GUJGASLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1124097,
                    TradingSymbol = "GULFOILLUB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =589569,
                    TradingSymbol = "HAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =12289,
                    TradingSymbol = "HAPPSTMNDS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4647425,
                    TradingSymbol = "HATHWAY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =996353,
                    TradingSymbol = "HATSUN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2513665,
                    TradingSymbol = "HAVELLS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1850625,
                    TradingSymbol = "HCLTECH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =340481,
                    TradingSymbol = "HDFC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1086465,
                    TradingSymbol = "HDFCAMC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =341249,
                    TradingSymbol = "HDFCBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =119553,
                    TradingSymbol = "HDFCLIFE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =342017,
                    TradingSymbol = "HEG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =592897,
                    TradingSymbol = "HEIDELBERG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1177089,
                    TradingSymbol = "HERITGFOOD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =345089,
                    TradingSymbol = "HEROMOTOCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5619457,
                    TradingSymbol = "HFCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =428033,
                    TradingSymbol = "HGINFRA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =348929,
                    TradingSymbol = "HINDALCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4592385,
                    TradingSymbol = "HINDCOPPER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =359937,
                    TradingSymbol = "HINDPETRO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =356865,
                    TradingSymbol = "HINDUNILVR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =364545,
                    TradingSymbol = "HINDZINC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3116289,
                    TradingSymbol = "HNDFDS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =874753,
                    TradingSymbol = "HONAUT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3669505,
                    TradingSymbol = "HSCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5331201,
                    TradingSymbol = "HUDCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =655873,
                    TradingSymbol = "HUHTAMAKI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3699201,
                    TradingSymbol = "IBREALEST"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7712001,
                    TradingSymbol = "IBULHSGFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1270529,
                    TradingSymbol = "ICICIBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5573121,
                    TradingSymbol = "ICICIGI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4774913,
                    TradingSymbol = "ICICIPRULI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3717889,
                    TradingSymbol = "ICRA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =377857,
                    TradingSymbol = "IDBI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3677697,
                    TradingSymbol = "IDEA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2863105,
                    TradingSymbol = "IDFCFIRSTB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =56321,
                    TradingSymbol = "IEX"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =380161,
                    TradingSymbol = "IFBIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2883073,
                    TradingSymbol = "IGL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3343617,
                    TradingSymbol = "IIFLWAM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =387073,
                    TradingSymbol = "INDHOTEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =387841,
                    TradingSymbol = "INDIACEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2745857,
                    TradingSymbol = "INDIAMART"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3663105,
                    TradingSymbol = "INDIANB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2865921,
                    TradingSymbol = "INDIGO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2989313,
                    TradingSymbol = "INDOCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1346049,
                    TradingSymbol = "INDUSINDBK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7458561,
                    TradingSymbol = "INDUSTOWER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4865,
                    TradingSymbol = "INEOSSTYRO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4159745,
                    TradingSymbol = "INFIBEAM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =408065,
                    TradingSymbol = "INFY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =408833,
                    TradingSymbol = "INGERRAND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3384577,
                    TradingSymbol = "INOXLEISUR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2393089,
                    TradingSymbol = "IOB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =415745,
                    TradingSymbol = "IOC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5225729,
                    TradingSymbol = "IOLCP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =418049,
                    TradingSymbol = "IPCALAB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3920129,
                    TradingSymbol = "IRB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3484417,
                    TradingSymbol = "IRCTC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =637185,
                    TradingSymbol = "ISEC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =852225,
                    TradingSymbol = "ISGEC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =424961,
                    TradingSymbol = "ITC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4940545,
                    TradingSymbol = "ITDC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =428801,
                    TradingSymbol = "ITI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1442049,
                    TradingSymbol = "J&KBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3382017,
                    TradingSymbol = "JAGRAN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1316609,
                    TradingSymbol = "JAICORPLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =441857,
                    TradingSymbol = "JBCHEPHARM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1149697,
                    TradingSymbol = "JCHAC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =449537,
                    TradingSymbol = "JINDALPOLY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =774145,
                    TradingSymbol = "JINDALSAW"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1723649,
                    TradingSymbol = "JINDALSTEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3453697,
                    TradingSymbol = "JKLAKSHMI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3036161,
                    TradingSymbol = "JKPAPER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3491073,
                    TradingSymbol = "JMFINANCIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3149825,
                    TradingSymbol = "JSLHISAR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4574465,
                    TradingSymbol = "JSWENERGY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3041281,
                    TradingSymbol = "JSWHL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3001089,
                    TradingSymbol = "JSWSTEEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =828673,
                    TradingSymbol = "JTEKTINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4632577,
                    TradingSymbol = "JUBLFOOD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =931073,
                    TradingSymbol = "JUBLPHARMA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7670273,
                    TradingSymbol = "JUSTDIAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3877377,
                    TradingSymbol = "JYOTHYLAB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =464385,
                    TradingSymbol = "KALPATPOWR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =306177,
                    TradingSymbol = "KANSAINER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =470529,
                    TradingSymbol = "KARURVYSYA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3394561,
                    TradingSymbol = "KEC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3407361,
                    TradingSymbol = "KEI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3031297,
                    TradingSymbol = "KENNAMET"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5359617,
                    TradingSymbol = "KIRLOSENG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3912449,
                    TradingSymbol = "KNRCON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3871745,
                    TradingSymbol = "KOLTEPATIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =492033,
                    TradingSymbol = "KOTAKBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2707713,
                    TradingSymbol = "KRBL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =498945,
                    TradingSymbol = "KSB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3832833,
                    TradingSymbol = "KSCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2061825,
                    TradingSymbol = "KTKBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6386689,
                    TradingSymbol = "L&TFH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2983425,
                    TradingSymbol = "LALPATHLAB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4923905,
                    TradingSymbol = "LAURUSLABS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =667137,
                    TradingSymbol = "LEMONTREE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =511233,
                    TradingSymbol = "LICHSGFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =416513,
                    TradingSymbol = "LINDEINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2939649,
                    TradingSymbol = "LT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4561409,
                    TradingSymbol = "LTI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4752385,
                    TradingSymbol = "LTTS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2672641,
                    TradingSymbol = "LUPIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =519937,
                    TradingSymbol = "M&M"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3400961,
                    TradingSymbol = "M&MFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2912513,
                    TradingSymbol = "MAHABANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3823873,
                    TradingSymbol = "MAHINDCIE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =98561,
                    TradingSymbol = "MAHLOG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =533761,
                    TradingSymbol = "MAHSCOOTER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =534529,
                    TradingSymbol = "MAHSEAMLES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6281729,
                    TradingSymbol = "MAITHANALL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4879617,
                    TradingSymbol = "MANAPPURAM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1041153,
                    TradingSymbol = "MARICO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2815745,
                    TradingSymbol = "MARUTI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =50945,
                    TradingSymbol = "MASFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5728513,
                    TradingSymbol = "MAXHEALTH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =130305,
                    TradingSymbol = "MAZDOCK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2674433,
                    TradingSymbol = "MCDOWELL-N"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7982337,
                    TradingSymbol = "MCX"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2452737,
                    TradingSymbol = "METROPOLIS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =548353,
                    TradingSymbol = "MFSL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4488705,
                    TradingSymbol = "MGL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4437249,
                    TradingSymbol = "MHRIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =630529,
                    TradingSymbol = "MIDHANI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6629633,
                    TradingSymbol = "MINDACORP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3623425,
                    TradingSymbol = "MINDAIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3675137,
                    TradingSymbol = "MINDTREE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4596993,
                    TradingSymbol = "MMTC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5332481,
                    TradingSymbol = "MOIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1076225,
                    TradingSymbol = "MOTHERSUMI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3826433,
                    TradingSymbol = "MOTILALOFS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1152769,
                    TradingSymbol = "MPHASIS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =582913,
                    TradingSymbol = "MRF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =584449,
                    TradingSymbol = "MRPL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6054401,
                    TradingSymbol = "MUTHOOTFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =91393,
                    TradingSymbol = "NAM-INDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1003009,
                    TradingSymbol = "NATCOPHARM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1629185,
                    TradingSymbol = "NATIONALUM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3520257,
                    TradingSymbol = "NAUKRI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3756033,
                    TradingSymbol = "NAVINFLUOR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =8042241,
                    TradingSymbol = "NBCC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2538753,
                    TradingSymbol = "NEOGEN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3944705,
                    TradingSymbol = "NESCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4598529,
                    TradingSymbol = "NESTLEIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3612417,
                    TradingSymbol = "NETWORK18"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3564801,
                    TradingSymbol = "NFL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3031041,
                    TradingSymbol = "NH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4454401,
                    TradingSymbol = "NHPC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =102145,
                    TradingSymbol = "NIACL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2707457,
                    TradingSymbol = "NIFTYBEES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2949633,
                    TradingSymbol = "NIITLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =619777,
                    TradingSymbol = "NILKAMAL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2197761,
                    TradingSymbol = "NLCINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3924993,
                    TradingSymbol = "NMDC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2977281,
                    TradingSymbol = "NTPC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5181953,
                    TradingSymbol = "OBEROIRLTY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2748929,
                    TradingSymbol = "OFSS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4464129,
                    TradingSymbol = "OIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3802369,
                    TradingSymbol = "OMAXE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =633601,
                    TradingSymbol = "ONGC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =760833,
                    TradingSymbol = "ORIENTELEC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7977729,
                    TradingSymbol = "ORIENTREF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3689729,
                    TradingSymbol = "PAGEIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =617473,
                    TradingSymbol = "PEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4701441,
                    TradingSymbol = "PERSISTENT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2905857,
                    TradingSymbol = "PETRONET"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3660545,
                    TradingSymbol = "PFC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =676609,
                    TradingSymbol = "PFIZER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =648961,
                    TradingSymbol = "PGHH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =240641,
                    TradingSymbol = "PGHL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3725313,
                    TradingSymbol = "PHOENIXLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =681985,
                    TradingSymbol = "PIDILITIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6191105,
                    TradingSymbol = "PIIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2929921,
                    TradingSymbol = "PILANIINVS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2730497,
                    TradingSymbol = "PNB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4840449,
                    TradingSymbol = "PNBHOUSING"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2402561,
                    TradingSymbol = "PNCINFRA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2455041,
                    TradingSymbol = "POLYCAB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6583809,
                    TradingSymbol = "POLYMED"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3834113,
                    TradingSymbol = "POWERGRID"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4724993,
                    TradingSymbol = "POWERINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5197313,
                    TradingSymbol = "PRESTIGE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2259201,
                    TradingSymbol = "PRIVISCL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5376257,
                    TradingSymbol = "PSB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5344513,
                    TradingSymbol = "PSPPROJECT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3365633,
                    TradingSymbol = "PVR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2813441,
                    TradingSymbol = "RADICO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3926273,
                    TradingSymbol = "RAIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1894657,
                    TradingSymbol = "RAJESHEXPO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =720897,
                    TradingSymbol = "RALLIS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =523009,
                    TradingSymbol = "RAMCOCEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1174273,
                    TradingSymbol = "RAMCOIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3443457,
                    TradingSymbol = "RATNAMANI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =731905,
                    TradingSymbol = "RAYMOND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4708097,
                    TradingSymbol = "RBLBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =733697,
                    TradingSymbol = "RCF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3930881,
                    TradingSymbol = "RECLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3649281,
                    TradingSymbol = "REDINGTON"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6201601,
                    TradingSymbol = "RELAXO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =738561,
                    TradingSymbol = "RELIANCE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5202689,
                    TradingSymbol = "RESPONIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =962817,
                    TradingSymbol = "RITES"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4968961,
                    TradingSymbol = "ROSSARI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =32769,
                    TradingSymbol = "ROUTE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6585345,
                    TradingSymbol = "RUPA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2445313,
                    TradingSymbol = "RVNL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =758529,
                    TradingSymbol = "SAIL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =369153,
                    TradingSymbol = "SANOFI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4600577,
                    TradingSymbol = "SBICARD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5582849,
                    TradingSymbol = "SBILIFE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =779521,
                    TradingSymbol = "SBIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =258817,
                    TradingSymbol = "SCHAEFFLER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =780289,
                    TradingSymbol = "SCI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3659777,
                    TradingSymbol = "SEQUENT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4911105,
                    TradingSymbol = "SFL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1277953,
                    TradingSymbol = "SHARDACROP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4544513,
                    TradingSymbol = "SHILPAMED"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3024129,
                    TradingSymbol = "SHOPERSTOP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =794369,
                    TradingSymbol = "SHREECEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3005185,
                    TradingSymbol = "SHRIRAMCIT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =806401,
                    TradingSymbol = "SIEMENS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5504257,
                    TradingSymbol = "SIS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4834049,
                    TradingSymbol = "SJVN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =815617,
                    TradingSymbol = "SKFINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3539457,
                    TradingSymbol = "SOBHA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =940033,
                    TradingSymbol = "SOLARA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3412993,
                    TradingSymbol = "SOLARINDS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2927361,
                    TradingSymbol = "SPANDANA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2930177,
                    TradingSymbol = "SPICEJET"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =837889,
                    TradingSymbol = "SRF"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1102337,
                    TradingSymbol = "SRTRANSFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1887745,
                    TradingSymbol = "STAR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5399297,
                    TradingSymbol = "STARCEMENT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2383105,
                    TradingSymbol = "STLTECH"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =851713,
                    TradingSymbol = "SUDARSCHEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4378881,
                    TradingSymbol = "SUMICHEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7426049,
                    TradingSymbol = "SUNCLAYLTD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =854785,
                    TradingSymbol = "SUNDARMFIN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =856321,
                    TradingSymbol = "SUNDRMFAST"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =857857,
                    TradingSymbol = "SUNPHARMA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4516097,
                    TradingSymbol = "SUNTECK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3431425,
                    TradingSymbol = "SUNTV"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2992385,
                    TradingSymbol = "SUPRAJIT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =860929,
                    TradingSymbol = "SUPREMEIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6936321,
                    TradingSymbol = "SWANENERGY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =866305,
                    TradingSymbol = "SWARAJENG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3197185,
                    TradingSymbol = "SWSOLAR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6192641,
                    TradingSymbol = "SYMPHONY"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2622209,
                    TradingSymbol = "SYNGENE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =5143553,
                    TradingSymbol = "TASTYBITE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =871681,
                    TradingSymbol = "TATACHEM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =185345,
                    TradingSymbol = "TATACOFFEE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =952577,
                    TradingSymbol = "TATACOMM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =878593,
                    TradingSymbol = "TATACONSUM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =414977,
                    TradingSymbol = "TATAINVEST"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =876289,
                    TradingSymbol = "TATAMETALI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =884737,
                    TradingSymbol = "TATAMOTORS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =877057,
                    TradingSymbol = "TATAPOWER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =895745,
                    TradingSymbol = "TATASTEEL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2708481,
                    TradingSymbol = "TCI"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1068033,
                    TradingSymbol = "TCNSBRANDS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2953217,
                    TradingSymbol = "TCS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3255297,
                    TradingSymbol = "TEAMLEASE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3465729,
                    TradingSymbol = "TECHM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =1649921,
                    TradingSymbol = "TECHNOE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =889601,
                    TradingSymbol = "THERMAX"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4360193,
                    TradingSymbol = "THYROCARE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3634689,
                    TradingSymbol = "TIMKEN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =897537,
                    TradingSymbol = "TITAN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =900609,
                    TradingSymbol = "TORNTPHARM"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3529217,
                    TradingSymbol = "TORNTPOWER"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =502785,
                    TradingSymbol = "TRENT"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =6549505,
                    TradingSymbol = "TRITURBINE"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =907777,
                    TradingSymbol = "TTKPRESTIG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3637249,
                    TradingSymbol = "TV18BRDCST"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2170625,
                    TradingSymbol = "TVSMOTOR"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4278529,
                    TradingSymbol = "UBL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2873089,
                    TradingSymbol = "UCOBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =269569,
                    TradingSymbol = "UFLEX"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4369665,
                    TradingSymbol = "UJJIVAN"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3898369,
                    TradingSymbol = "UJJIVANSFB"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2952193,
                    TradingSymbol = "ULTRACEMCO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2752769,
                    TradingSymbol = "UNIONBANK"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2889473,
                    TradingSymbol = "UPL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =134913,
                    TradingSymbol = "UTIAMC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =84481,
                    TradingSymbol = "VALIANTORG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =987393,
                    TradingSymbol = "VARROC"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4843777,
                    TradingSymbol = "VBL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =784129,
                    TradingSymbol = "VEDL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3932673,
                    TradingSymbol = "VGUARD"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4445185,
                    TradingSymbol = "VINATIORGA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =947969,
                    TradingSymbol = "VIPIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =7496705,
                    TradingSymbol = "VMART"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =951809,
                    TradingSymbol = "VOLTAS"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2226177,
                    TradingSymbol = "VRLLOG"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =953345,
                    TradingSymbol = "VSTIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =530689,
                    TradingSymbol = "VTL"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =4330241,
                    TradingSymbol = "WABCOINDIA"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =3026177,
                    TradingSymbol = "WELCORP"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =2880769,
                    TradingSymbol = "WELSPUNIND"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =969473,
                    TradingSymbol = "WIPRO"
                },
new TradeInstrument
                {
                    Exchange = "NSE",
                    Token =975873,
                    TradingSymbol = "ZEEL"
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
