using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuideLogAnalyzer
{
    public class LogEntry
    {
        public LogEntry()
        { }
    }

    class LogReader
    {
        public enum LogVal
        {
            ElapsedSecs,
            RefCentroidX,
            RefCentroidY,
            CurCentroidX,
            CurCentroidY,
            GuideErrX,
            GuideErrY,
            TotGuideErr,
            XPlusRelay,
            XMinusRelay,
            YPlusRelay,
            YMinusRelay,
            PECIndexRA,
            PECIndexDec,
            GuideStarSignal,
            UUID
        }

        public enum Calibration
        //Components of the Calibration Velocity Vectors
        {
            XPlus,
            XMinus,
            YPlus,
            YMinus
        }

        public enum Vector
        //Elements of each Calibration Velocity Vector (Pixels/Sec)
        {
            Speed,    //Pix per sec
            XSpeed,         //Pix per sec
            YSpeed,         //Pix per sec
            Angle           //Degrees
        }

        //Calibration Determined Velocity Vectors(pixels/sec)
        //      [Velocity, X component, Y component, Angle]
        double[] XPlusSpeed = { 0, 0, 0, 0 };
        double[] XMinusSpeed = { 0, 0, 0, 0 };
        double[] YPlusSpeed = { 0, 0, 0, 0 };
        double[] YMinusSpeed = { 0, 0, 0, 0 };
        //AO Autoguiding
        //int AO_Aggressiveness = 6;
        //int AO_Slew_Rate = 500;
        //int AO_Relay_Threshold = 50;
        //double AO_Relay_Bump_Size = 0.10;
        //int AO_Track_Box_Size = 32;

        double[,] LogArray = new double[1, 16];
        int LogLength = 0;

        public LogReader(string logFilePath)
        {
            //Determine how many lines are in the file (faster than doing this on the run)
            TextReader logDataFile = File.OpenText(logFilePath);
            int numLines = 0;
            string tempLine;
            do
            {
                tempLine = logDataFile.ReadLine();
                numLines++;
            } while (tempLine != null);
            numLines--;
            //Close the file and reopen
            logDataFile.Close();
            logDataFile = File.OpenText(logFilePath);
            ////Start reading in the "header" information, line by line

            ////Pick up Calibration Vectors
            while (!logDataFile.ReadLine().Contains("Velocity Vectors"))
            { };
            XPlusSpeed = StripFour(logDataFile.ReadLine());
            XMinusSpeed = StripFour(logDataFile.ReadLine());
            YPlusSpeed = StripFour(logDataFile.ReadLine());
            YMinusSpeed = StripFour(logDataFile.ReadLine());

            ////Pick up AO configuration, if any
            //while (!logDataFile.ReadLine().Contains("AO Autoguiding"))
            //{ };
            //AO_Aggressiveness = GrabInteger(logDataFile.ReadLine());
            //AO_Slew_Rate = GrabInteger(logDataFile.ReadLine());
            //AO_Relay_Threshold = GrabInteger(logDataFile.ReadLine());
            //AO_Relay_Bump_Size = GrabDouble(logDataFile.ReadLine());
            //AO_Track_Box_Size = GrabInteger(logDataFile.ReadLine());

            //Close the file and reopen
            logDataFile.Close();
            logDataFile = File.OpenText(logFilePath);
            ////Start reading in the "header" information, line by line          //Skip to data section
            while (!logDataFile.ReadLine().Contains("Elapsed"))
            { numLines -= 1; }

            //Sweep the log data into an array, starting with the first line (hope there is one)

            string scanLine = null;
            LogArray = new double[numLines, 16];
            string[] LogLine = new string[16];
            char delimiter = Convert.ToChar("|");
            string[] gElements = new string[1];

            bool reSettingError = false;
            LogLength = 0;
            scanLine = logDataFile.ReadLine();
            while (scanLine != null)
            {
                gElements = scanLine.Split(delimiter);
                try
                {
                    LogArray[LogLength, 0] = Convert.ToDouble(gElements[1]);
                    //Continue conversion of file
                    LogArray[LogLength, 1] = Convert.ToDouble(gElements[2]);
                    LogArray[LogLength, 2] = Convert.ToDouble(gElements[3]);
                    LogArray[LogLength, 3] = Convert.ToDouble(gElements[4]);
                    LogArray[LogLength, 4] = Convert.ToDouble(gElements[5]);
                    LogArray[LogLength, 5] = Convert.ToDouble(gElements[6]);
                    LogArray[LogLength, 6] = Convert.ToDouble(gElements[7]);
                    LogArray[LogLength, 7] = Convert.ToDouble(gElements[8]);
                    LogArray[LogLength, 8] = Convert.ToDouble(gElements[9]);
                    LogArray[LogLength, 9] = Convert.ToDouble(gElements[10]);
                    LogArray[LogLength, 10] = Convert.ToDouble(gElements[11]);
                    LogArray[LogLength, 11] = Convert.ToDouble(gElements[12]);
                    LogArray[LogLength, 12] = Convert.ToDouble(gElements[13]);
                    LogArray[LogLength, 13] = Convert.ToDouble(gElements[14]);
                    LogArray[LogLength, 14] = ConvertSat(gElements[15]);
                    LogArray[LogLength, 15] = 0;
                    LogLength++;
                }
                catch
                {
                    if (!reSettingError)
                    {
                        reSettingError = true;
                        System.Windows.Forms.DialogResult sNext = System.Windows.Forms.MessageBox.Show("Unexpected file contents encountered.  " +
                         "Log probably contains mid-run correction setting changes which will compromise frequency analysis. Continue anyway?",
                     "Data Conversion Error",
                     System.Windows.Forms.MessageBoxButtons.YesNo);
                        if (sNext == System.Windows.Forms.DialogResult.No) return;
                    }
                }
                scanLine = logDataFile.ReadLine();
            }
            return;
        }

        public double GetLogValue(int lineNum, LogReader.LogVal entry)
        //Method for retrieving a single entry from a single line of the log
        {
            return LogArray[lineNum, (int)entry];
        }

        public int GetLogSize()
        {
            //return LogArray.GetLength(0)
            return LogLength;
        }

        public double GetMountVelocityVector(Calibration wVector, Vector wComponent)
        //returns the velocity vector components: wVector::: 0 = X+, 1 = X-, 2 = Y+, 3 = Y-
        //  wComponent::: 0 = Speed, 1 = X component, 2 = Y component, 3 = Angle
        {
            switch (wVector)
            {
                case Calibration.XPlus:
                    return XPlusSpeed[(int)wComponent];
                case Calibration.XMinus:
                    return XMinusSpeed[(int)wComponent];
                case Calibration.YPlus:
                    return YPlusSpeed[(int)wComponent];
                case Calibration.YMinus:
                    return YMinusSpeed[(int)wComponent];
                default:
                    return 0.0;
            }
        }

        private double[] StripFour(string sLine)
        {
            //Strip four numeric substrings from the input string and return as array of doubles
            double[] vArray = { 0, 0, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                vArray[i] = Convert.ToDouble(ParseNextNumber(sLine, i + 1));
            }
            return vArray;
        }

        private string ParseNextNumber(string sText, int nth)
        //returns the nth inclusion (starting at 1) of an embedded numeric in a string
        {
            string sRet = "";
            //Clean out chars:
            sText = sText.Replace("(", " ");
            sText = sText.Replace(")", " ");
            sText = sText.Replace(",", " ");
            //loop through to the nth occurance of an equals sign
            //grab the digits until a space or end of line are found
            for (int i = 0; i < nth; i++)
            {
                sText = sText.Substring(sText.IndexOf("=") + 1);
                sText = sText.Trim();
                int iLen = sText.IndexOf(" ");
                if (iLen < 0) { iLen = sText.Length; }
                sRet = sText.Substring(0, iLen);
            }
            return sRet;
        }

        private int GrabInteger(string itext)
        {
            //picks an numeric substring out of a string and converts it to an integer.
            int gi = 0;
            string iStr = itext.Substring(itext.IndexOf("=") + 1);
            iStr = iStr.Replace(")", "");
            try
            { gi = Convert.ToInt32(iStr); }
            catch
            { gi = 0; }
            return gi;
        }

        private bool GrabEnabled(string itext)
        {
            //returns false is string contains "Not" as in Not Enabled
            return !itext.Contains("Not");
        }

        private Double GrabDouble(string itext)
        {
            //returns double value for numeric in string
            double gd = 0;
            string iNum = itext.Substring(itext.IndexOf("=") + 1);
            try
            { gd = Convert.ToDouble(iNum); }
            catch
            { gd = 0; }
            return gd;
        }

        private DateTime GrabDateTime(string itext)
        {
            //returns datetime value for oddly formated date and time in string
            //DateTime iNum = DateTime.ParseExact(itext.Substring(itext.IndexOf("="),"yy/mm/);
            string iStr = itext.Substring(itext.IndexOf("=") + 1);
            iStr = iStr.Replace("STD", "");
            return DateTime.Parse(iStr);
        }

        private double ConvertSat(string satText)
        {
            if (satText.Contains("Sat")) return 1;
            if (satText.Contains("Lost")) return 2;
            else return 0;
        }
    }
}



