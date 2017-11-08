using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using WealthLab;
using WealthLab.Indicators;

namespace WealthLabTest
{
    public class TestStrategyScript : WealthScript
    {
        private double riskStop = 0.3;
        private double profitSave = 0.4;
        private double[] keyLevel = { 105750, 108950, 109900 };                     // Ключевые уровни, относительно которых будет вестись торговля
        private int[] barException = { };                         // Исключение сделки из результата (ввести бар на котором совершена сделка, если сделка на bar + 1, если сделка на bar + 0, то вводим номер следующего после сделки бара)
        private static DateTime fromDate = new DateTime(2015, 03, 30, 10, 00, 00);      // Дата с которой начнут поступать сигналы (yyyy,mm,dd,hh,mm,ss )
        private static DateTime beforeDate = new DateTime(2017, 10, 24, 23, 50, 00);        // Дата до которой будут поступать сигналы (yyyy,mm,dd,hh,mm,ss )
        private bool longTrade = true;
        private bool shortTrade = true;
        private double percentageRejection = 0.002; // Отклонение от уровня, на котром разрешены сигналы 
                                                    /////////////////////////////////////////////////////


        public int FindFractalHIgh(int i, double period, List<double> H_tmp)
        {
            int P = (int)Math.Floor(period / 2) * 2 + 1;
            H_tmp.Add((double)Bars.High[i]);
            if (i >= period)
            {
                int s = (int)(i - period + 1 + (int)Math.Floor(period / 2));
                double val_h = 0;
                for (int j = i - (int)period; j < i; j++)
                {
                    if (H_tmp[j] > val_h)
                        val_h = H_tmp[j];
                }
                double h = Bars.High[s];
                if (val_h == h)
                    return s;
            }
            return -1;
        }
        public int FindFractalLow(int i, double period, List<double> L_tmp)
        {
            int P = (int)Math.Floor(period / 2) * 2 + 1;
            L_tmp.Add((double)Bars.Low[i]);
            if (i >= period)
            {
                int s = (int)(i - period + 1 + (int)Math.Floor(period / 2));

                double val_l = L_tmp[i - (int)period];
                for (int j = i - (int)period; j < i; j++)
                {
                    if (L_tmp[j] < val_l)
                        val_l = L_tmp[j];
                }
                double l = Bars.Low[s];
                if (val_l == l)
                    return s;
            }
            return -1;
        }
        //////////////////////////////////////////////////////////////


        public int StartBarForFindBSY(int bar)              // Метод для определния с какого бара искать БСУ, если в графике меньше 540 баров, то с 0, если больше, то текущий бар - 540 баров назад
        {
            int countBarForFind = 200;                     // Как глубоко будем искать БСУ
            if (bar - countBarForFind > 0)
                return bar - countBarForFind;
            return 0;
        }
        public string AirLevel(int bar, out int bsy)
        {
            if (bar > 1 && Bars.High[bar] == Bars.High[bar - 2] && Bars.High[bar] == Bars.High[bar - 1])
            {
                bsy = bar - 2;
                return "Short";
            }
            else if (bar > 1 && Bars.Low[bar] == Bars.Low[bar - 2] && Bars.Low[bar] == Bars.Low[bar - 1])
            {
                bsy = bar - 2;
                return "Long";
            }
            bsy = 0;
            return "Nothing";
        }
        public bool BSYAndPBY1MirrorForLong(int bar, List<int> listHighFractal, out int bsy) // Для зеркального уровня, позиция в лонг
        {
            int fine = StartBarForFindBSY(bar);
            for (int temp = bar - 1; temp >= fine; temp--)
            {
                if (Bars.High[temp] == Bars.Low[bar] && listHighFractal.Contains(temp))     // $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                {
                    bsy = temp;
                    return true;
                }
            }
            bsy = 0;
            return false;
        }
        public bool BSYAndPBY1MirrorForShort(int bar, List<int> listLowFractal, out int bsy) // Для зеркального уровня, позиция в шорт
        {
            int fine = StartBarForFindBSY(bar);
            for (int temp = bar - 1; temp >= fine; temp--)
            {
                if (Bars.Low[temp] == Bars.High[bar] && listLowFractal.Contains(temp))     // $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                {
                    bsy = temp;
                    return true;
                }
            }
            bsy = 0;
            return false;
        }
        public bool BSYAndBPY1High(int bar, List<int> listHighFractal, out int bsy)               // Повторяющийся уровень для шорта
        {
            int fine = StartBarForFindBSY(bar);
            for (int temp = bar - 1; temp >= fine; temp--)
            {
                if (Bars.High[temp] == Bars.High[bar] && listHighFractal.Contains(temp)) // $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                {
                    bsy = temp;
                    return true;
                }
            }
            bsy = 0;
            return false;
        }
        public bool BSYAndBPY1Low(int bar, List<int> listLowFractal, out int bsy)                // Повторяющийся уровень для лонга
        {
            int fine = StartBarForFindBSY(bar);
            for (int temp = bar - 1; temp >= fine; temp--)
            {
                if (Bars.Low[temp] == Bars.Low[bar] && listLowFractal.Contains(temp))      // $$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
                {
                    bsy = temp;
                    return true;
                }
            }
            bsy = 0;
            return false;
        }
        public bool BPY1AndBPY2High(int bar)              // Подтверждение повторяющегося уровеня для шорта (БПУ2)
        {
            if (Bars.High[bar] >= (Bars.High[bar - 1] - (Bars.High[bar - 1] / 2500)) && Bars.High[bar] <= Bars.High[bar - 1])
                return true;
            return false;
        }
        public bool BPY1AndBPY2Low(int bar)               // Подтверждение повторяющегося уровеня для лонга (БПУ2)   
        {
            if (Bars.Low[bar] <= (Bars.Low[bar - 1] + (Bars.Low[bar - 1] / 2500)) && Bars.Low[bar] >= Bars.Low[bar - 1])
                return true;
            return false;
        }
        public string FindModelRepeatLevel(int bar, List<int> listHighFractal, List<int> listLowFractal, out int BSY)                // Начало анализа баров для поиска повторяющегося уровня 
        {
            if (BSYAndBPY1High(bar - 1, listHighFractal, out BSY) && BPY1AndBPY2High(bar))
            {
                //	DrawHorzLine( PricePane, bar, Color.Green, WealthLab.LineStyle.Solid, 1 );
                return "Short";
            }
            if (BSYAndBPY1Low(bar - 1, listLowFractal, out BSY) && BPY1AndBPY2Low(bar))
            {
                //	DrawHorzLine( PricePane, bar, Color.Green, WealthLab.LineStyle.Solid, 1 );
                return "Long";
            }
            return "Nothing";
        }
        public string FindModelMirrorLevel(int bar, List<int> listHighFractal, List<int> listLowFractal, out int BSY)                // Начало анализа баров для поиска зеркального уровня 
        {
            if (BSYAndPBY1MirrorForShort(bar - 1, listLowFractal, out BSY) && BPY1AndBPY2High(bar))
                return "Short";
            if (BSYAndPBY1MirrorForLong(bar - 1, listHighFractal, out BSY) && BPY1AndBPY2Low(bar))
                return "Long";
            return "Nothing";
        }
        /*	public bool DefenitionAreaNearLevel(int bar) // Сигналы во все стороны (без разделения на/под уровнем)
            {
                for (int i = 0; i < keyLevel.Length; i++)
                {
                    if((Close[bar] < (keyLevel[i] + keyLevel[i] * 0.01)) && Close[bar] > (keyLevel[i] - keyLevel[i] * 0.01))
                        return true;
                }
                return false;
            }*/
        public bool DateStart(int bar)
        {
            int barToStart = 0;
            int barBeforeExecute = Bars.Count;
            if (DateTimeToBar(fromDate, false) != -1)
                barToStart = DateTimeToBar(fromDate, false);
            if (DateTimeToBar(beforeDate, false) != -1)
                barBeforeExecute = DateTimeToBar(beforeDate, false);
            if (barToStart <= bar && bar <= barBeforeExecute)
                return true;
            return false;
        }
        public string DefenitionAreaNearLevel(int bar)
        {
            for (int i = 0; i < keyLevel.Length; i++)
            {
                if ((Close[bar] < (keyLevel[i] + keyLevel[i] * percentageRejection)) && Close[bar] > keyLevel[i]) // для трейда только возле уровня +- 0.2% && Bars.Low[bar - 1] < (keyLevel[i] + Bars.Low[bar - 1] / 500))
                    return "Long";
                if ((Close[bar] > (keyLevel[i] - keyLevel[i] * percentageRejection)) && Close[bar] < keyLevel[i]) // для трейда только возле уровня +- 0.2% && Bars.Low[bar - 1] > (keyLevel[i] - Bars.Low[bar - 1] / 500))
                    return "Short";
            }
            return "Nothing";
        }
        public bool ExceptionBars(int bar)
        {
            for (int i = 0; i < barException.Length; i++)
            {
                if (bar == barException[i] - 1)
                    return false;
            }
            return true;
        }
        public void DrawingLevel()
        {
            for (int i = 0; i < keyLevel.Length; i++)
            {
                DrawHorzLine(PricePane, keyLevel[i], Color.Black, WealthLab.LineStyle.Solid, 2);
                DrawHorzLine(PricePane, (keyLevel[i] + keyLevel[i] * percentageRejection), Color.Blue, WealthLab.LineStyle.Solid, 2);
                DrawHorzLine(PricePane, (keyLevel[i] - keyLevel[i] * percentageRejection), Color.Blue, WealthLab.LineStyle.Solid, 2);
            }
        }
        public double AveragePrice(int i)
        {
            const int period = 5;
            double sum = 0d;
            for (int bar = i - 5; bar >= i - period; bar--)
                sum = sum + Bars.Close[bar];
            double avg = sum / period;
            return avg;
        }
        public void DrawLineToBSYAndBPY(int barBsy, int barBpy1, int barBpy2, string str, Color color) // Метод для рисования линий к БСУ и БПУ (первое значение откуда, второе куда(бар и значение(вертикальное)))
        {
            DrawLine(PricePane, barBsy, Bars.High[barBsy], barBpy2, Bars.High[barBpy2] * 1.002, color, WealthLab.LineStyle.Dotted, 2);
            DrawLine(PricePane, barBpy1, Bars.High[barBpy1], barBpy2, Bars.High[barBpy2] * 1.002, color, WealthLab.LineStyle.Dotted, 2);
            DrawLine(PricePane, barBpy2, Bars.High[barBpy2], barBpy2, Bars.High[barBpy2] * 1.002, color, WealthLab.LineStyle.Dotted, 2);
            for (int i = 0; i < str.Length; i++)
            {
                AnnotateChart(PricePane, ReverseArrayFramework(str).Substring(i, 1), barBpy2, Bars.High[barBpy2] * 1.002 + i * 30, color);
            }
        }
        static string ReverseArrayFramework(string str)
        {
            char[] arr = str.ToCharArray();
            Array.Reverse(arr);
            return new String(arr);
        }
        //Pushed indicator StrategyParameter statements

        private StrategyParameter strategyParameter1;
        private StrategyParameter strategyParameter2;
        private StrategyParameter strategyParameter3;
        private StrategyParameter strategyParameter4;
        private StrategyParameter strategyParameter5;
        public TestStrategyScript()
        {
            strategyParameter1 = CreateParameter("Fractals", 15, 3, 100, 3);
            //	strategyParameter2 = CreateParameter("SMA", 4, 1, 10, 2);
            //	strategyParameter3 = CreateParameter("Sdvig", 3, 1, 4, 1);
            strategyParameter4 = CreateParameter("Stop", 0.002, 0.001, 0.01, 0.001);
            strategyParameter5 = CreateParameter("Profit", 0.006, 0.002, 0.01, 0.001);
        }

        protected override void Execute()
        {
            List<double> H_tmp = new List<double>();
            List<double> L_tmp = new List<double>();

            List<int> listHighFractal = new List<int>();
            List<int> listLowFractal = new List<int>();
            //	DrawingLevel();


            double fractalPeriod = strategyParameter1.Value;
            int smaPeriod = 4;// strategyParameter2.ValueInt;
            int sdvig = 3;//strategyParameter3.ValueInt;
            double stop = strategyParameter4.Value;
            double profit = strategyParameter5.Value;



            DataSeries smaFast = EMA.Series(Close, smaPeriod, EMACalculation.Modern);
            
            for (int bar = 0; bar < Bars.Count; bar++)
            {
                int fractalUp = FindFractalHIgh(bar, fractalPeriod, H_tmp);
                int fractalDown = FindFractalLow(bar, fractalPeriod, L_tmp);
                if (fractalUp != -1)
                {
                    listHighFractal.Add(fractalUp);
                    //		SetBackgroundColor(fractalUp, Color.Black);
                    for (int i = fractalUp - sdvig >= 0 ? fractalUp - sdvig : 0; i < fractalUp; i++)
                    {
                        if (Bars.High[i] > smaFast[i + sdvig])
                        {
                            listHighFractal.Add(i);
                            //			SetBackgroundColor(i, Color.Yellow);
                        }

                    }
                    for (int i = fractalUp + sdvig <= Bars.Count - 1 ? fractalUp + sdvig : Bars.Count - 1; i > fractalUp; i--)
                    {
                        if (Bars.High[i] > smaFast[i - sdvig])
                        {
                            listHighFractal.Add(i);
                            //			SetBackgroundColor(i, Color.Yellow);
                        }
                    }
                }
                if (fractalDown != -1)
                {
                    listLowFractal.Add(fractalDown);
                    //		SetBackgroundColor(fractalDown, Color.BlueViolet);
                    for (int i = fractalDown - sdvig >= 0 ? fractalDown - sdvig : 0; i < fractalDown - 1; i++)
                    {
                        if (Bars.Low[i] < smaFast[i + sdvig])
                        {
                            listLowFractal.Add(i);
                            //			SetBackgroundColor(i, Color.Coral);
                        }

                    }
                    for (int i = fractalDown + sdvig <= Bars.Count - 1 ? fractalDown + sdvig : Bars.Count - 1; i > fractalDown; i--)
                    {
                        if (Bars.Low[i] < smaFast[i - sdvig])
                        {
                            listLowFractal.Add(i);
                            //			SetBackgroundColor(i, Color.Coral);
                        }
                    }
                }
            }
            for (int bar = 3; bar < Bars.Count - 3; bar++)
            {
                if (bar == 53)
                {
                }
                if (IsLastPositionActive)
                {
                    if (LastActivePosition.PositionType == PositionType.Short)
                    {
                        if (LastActivePosition.MFEAsOfBarPercent(bar) >= profitSave)
                        {
                            CoverAtStop(bar + 1, LastPosition, LastPosition.EntryPrice - LastPosition.EntryPrice * 0.00119, "Безубыток");
                            //	CoverAtStop(bar + 1, LastPosition, LastPosition.EntryPrice, "Безубыток");
                            CoverAtLimit(bar + 1, LastPosition, LastPosition.EntryPrice * (1 - profit) - LastPosition.EntryPrice * 0.00119, "Profit 0.6% (1к3)");
                        }
                        else
                        {
                            // Cover the short position if prices move against us by 0.2% or 1 to 3 profit
                            CoverAtStop(bar + 1, LastPosition, LastPosition.EntryPrice * (1 + stop) - LastPosition.EntryPrice * 0.00119, "0.2% Stop");
                            CoverAtLimit(bar + 1, LastPosition, LastPosition.EntryPrice * (1 - profit) - LastPosition.EntryPrice * 0.00119, "Profit 0.6% (1к3)");
                        }
                    }
                    else
                    {

                        if (LastActivePosition.MFEAsOfBarPercent(bar) >= profitSave)
                        {
                            SellAtStop(bar + 1, LastPosition, LastPosition.EntryPrice + LastPosition.EntryPrice * 0.00119, "Безубыток");
                            //	SellAtStop(bar + 1, LastPosition, LastPosition.EntryPrice, "Безубыток");
                            SellAtLimit(bar + 1, LastPosition, LastPosition.EntryPrice * (1 + profit) + LastPosition.EntryPrice * 0.00119, "Profit 0.6% (1к3)");
                        }
                        else
                        {
                            // Cover the short position if prices move against us by 0.2% or 1 to 3 profit
                            SellAtStop(bar + 1, LastPosition, LastPosition.EntryPrice * (1 - stop) + LastPosition.EntryPrice * 0.00119, "0.2% Stop");
                            SellAtLimit(bar + 1, LastPosition, LastPosition.EntryPrice * (1 + profit) + LastPosition.EntryPrice * 0.00119, "Profit 0.6% (1к3)");
                        }
                    }
                }
                else
                {
                    int bsy;
                    if (FindModelRepeatLevel(bar, listHighFractal, listLowFractal, out bsy) == "Short")// && DateStart(bar)) //&& DefenitionAreaNearLevel(bar) == "Short" && ExceptionBars(bar) && shortTrade)
                    {
                        //	PrintDebug("Повторяющийся уровень: БСУ - " + bsy + " / БПУ1 - " + (bar - 1) + " / БПУ2 - " + bar + " / Уровень = " + Bars.High[bsy] + " / Люфт = " + Bars.High[bsy] * 0.9996 + " | Снятие заявки от люфта = " + ((Bars.High[bsy] * 0.9996) * 0.996));
                        DrawLineToBSYAndBPY(bsy, bar - 1, bar, "Повторяющийся", Color.Fuchsia);
                        //		ShortAtMarket(bar + 0, "Шорт от повторяющегося уровня сопротивления"); // Если заход на bar + 1, то вход на баре сразу после БПУ2
                        ShortAtLimit(bar + 0, Bars.High[bar - 1] * 0.9996, "Шорт от повторяющегося уровня сопротивления");  // Для захода по люфту, если на bar + 0 - то заход на БПУ2
                        continue;
                    }
                    if (FindModelMirrorLevel(bar, listHighFractal, listLowFractal, out bsy) == "Short")// && DateStart(bar))// && DefenitionAreaNearLevel(bar) == "Short" && ExceptionBars(bar) && shortTrade)
                    {
                        //		PrintDebug("Зеркальный уровень: БСУ - " + bsy + " / БПУ1 - " + (bar - 1) + " / БПУ2 - " + bar + " / Уровень = " + Bars.Low[bsy] + " / Люфт = " + Bars.Low[bsy] * 0.9996 + " | Снятие заявки от люфта = " + ((Bars.Low[bsy] * 0.9996) * 0.996));
                        DrawLineToBSYAndBPY(bsy, bar - 1, bar, "Зеркальный", Color.Blue);
                        //		ShortAtMarket(bar + 0, "Шорт от зеркального уровня сопротивления"); // Если заход на bar + 1, то вход на баре сразу после БПУ2
                        ShortAtLimit(bar + 0, Bars.High[bar - 1] * 0.9996, "Шорт от зеркального уровня сопротивления");   // Для захода по люфту, если на bar + 0 - то заход на БПУ2
                        continue;
                    }
                    if (AirLevel(bar, out bsy) == "Short")// && DateStart(bar))//&& DefenitionAreaNearLevel(bar) == "Nothing" && ExceptionBars(bar) && shortTrade)
                    {
                        //		PrintDebug("Воздушный уровень: БСУ - " + bsy + " / БПУ1 - " + (bar - 1) + " / БПУ2 - " + bar + " / Уровень = " + Bars.Low[bsy] + " / Люфт = " + Bars.Low[bsy] * 0.9996 + " | Снятие заявки от люфта = " + ((Bars.Low[bsy] * 0.9996) * 0.996));
                        DrawLineToBSYAndBPY(bsy, bar - 1, bar, "Воздушный", Color.DarkGray);
                        //				ShortAtLimit( bar + 0, Bars.High[bar - 1] * 0.9996, "Шорт от воздушного уровня сопротивления");   // Для захода по люфту, если на bar + 0 - то заход на БПУ2
                        continue;
                    }
                    if (FindModelRepeatLevel(bar, listHighFractal, listLowFractal, out bsy) == "Long")// && DateStart(bar))// && DefenitionAreaNearLevel(bar) == "Long" && ExceptionBars(bar)  && longTrade)
                    {
                        //	PrintDebug("Повторяющийся уровень: БСУ - " + bsy + " / БПУ1 - " + (bar - 1) + " / БПУ2 - " + bar + " / Уровень = " + Bars.Low[bsy] + " / Люфт = " + Bars.Low[bsy] * 1.0004 + " | Снятие заявки от люфта = " + ((Bars.Low[bsy] * 1.0004) * 1.004));
                        DrawLineToBSYAndBPY(bsy, bar - 1, bar, "Повторяющийся", Color.Fuchsia);
                        //	BuyAtMarket(bar + 0, "Лонг от повторяющегося уровня поддержки"); // Если заход на bar + 1, то вход на баре сразу после БПУ2
                        BuyAtLimit(bar + 0, Bars.Low[bar - 1] * 1.0004, "Лонг от повторяющегося уровня поддержки"); // Для захода по люфту, если на bar + 0 - то заход на БПУ2
                        continue;
                    }
                    if (FindModelMirrorLevel(bar, listHighFractal, listLowFractal, out bsy) == "Long")// && DateStart(bar))// && DefenitionAreaNearLevel(bar) == "Long" && ExceptionBars(bar) && longTrade)
                    {
                        //	PrintDebug("Зеркальный уровень: БСУ - " + bsy + " / БПУ1 - " + (bar - 1) + " / БПУ2 - " + bar + " / Уровень = " + Bars.High[bsy] + " / Люфт = " + Bars.High[bsy] * 1.0004 + " | Снятие заявки от люфта = " + ((Bars.High[bsy] * 1.0004) * 1.004));
                        DrawLineToBSYAndBPY(bsy, bar - 1, bar, "Зеркальный", Color.Blue);
                        //	BuyAtMarket(bar + 0, "Лонг от зеркального уровня поддержки"); // Если заход на bar + 1, то вход на баре сразу после БПУ2
                        BuyAtLimit(bar + 0, Bars.Low[bar - 1] * 1.0004, "Лонг от зеркального уровня поддержки"); // Для захода по люфту, если на bar + 0 - то заход на БПУ2
                        continue;
                    }
                    if (AirLevel(bar, out bsy) == "Long")//&& DateStart(bar))// && DefenitionAreaNearLevel(bar) == "Nothing" && ExceptionBars(bar) && longTrade)
                    {
                        //		PrintDebug("Воздушный уровень: БСУ - " + bsy + " / БПУ1 - " + (bar - 1) + " / БПУ2 - " + bar + " / Уровень = " + Bars.High[bsy] + " / Люфт = " + Bars.High[bsy] * 1.0004 + " | Снятие заявки от люфта = " + ((Bars.High[bsy] * 1.0004) * 1.004));
                        DrawLineToBSYAndBPY(bsy, bar - 1, bar, "Воздушный", Color.DarkGray);
                        //			BuyAtLimit( bar + 0, Bars.Low[bar - 1] * 1.0004, "Лонг от воздушного уровня поддержки" ); // Для захода по люфту, если на bar + 0 - то заход на БПУ2
                        continue;
                    }
                }
            }
        }
    }
}

