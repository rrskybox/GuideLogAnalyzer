using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GuideLogAnalyzer
{
    public static class Analysis
    {

        public static double PercentSaturated(double[] correctionVal)
        {
            //Counts the number of errors that result in a non-zero correction
            //  as a percentage of total error points (all)
            double nmCount = 0;
            for (int i = 0; i < correctionVal.Length; i++)
            {
                if (correctionVal[i] == 1)
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionVal.Length);
        }

        public static double PercentLost(double[] correctionVal)
        {
            //Counts the number of errors that result in a non-zero correction
            //  as a percentage of total error points (all)
            double nmCount = 0;
            for (int i = 0; i < correctionVal.Length; i++)
            {
                if (correctionVal[i] == 2)
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionVal.Length);
        }

        public static double PercentErrorsCorrected(double[] correctionVal)
        {
            //Counts the number of errors that result in a non-zero correction
            //  as a percentage of total error points (all)
            double nmCount = 0;
            for (int i = 0; i < correctionVal.Length; i++)
            {
                if (correctionVal[i] != 0)
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionVal.Length);
        }

        public static double PercentErrorsCorrected(double[] correctionPlusVal, double[] correctionMinusVal)
        {
            //Counts the number of errors that result in a non-zero correction (X,Y)
            //  as a percentage of total error points
            double nmCount = 0;
            for (int i = 0; i < correctionPlusVal.Length; i++)
            {
                if ((correctionPlusVal[i] != 0) || (correctionMinusVal[i] != 0))
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionPlusVal.Length);
        }

        public static double PercentErrorsCorrected(double[] correctionXPlusVal, double[] correctionXMinusVal, double[] correctionYPlusVal, double[] correctionYMinusVal)
        {
            //Counts the number of errors that result in a non-zero correction (X+,X-,Y+,Y-)
            //  as a percentage of total error points
            double nmCount = 0;
            for (int i = 0; i < correctionXPlusVal.Length; i++)
            {
                if ((correctionXPlusVal[i] > 0) || (correctionXMinusVal[i] > 0) || (correctionYPlusVal[i] > 0) || (correctionYMinusVal[i] > 0))
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionXPlusVal.Length);
        }

        public static double PercentSwitchBacks(double[] correctionPlusVal, double[] correctionMinusVal)
        {
            //Counting the number of times that a correction in one direction is followed by a correction in the other direction
            //  as a percentage of correction cycles
            double nmCount = 0;
            for (int i = 0; i < correctionPlusVal.Length - 1; i++)
            {
                if (((correctionPlusVal[i] > 0) && (correctionMinusVal[i + 1] > 0)) || ((correctionMinusVal[i] > 0) && (correctionPlusVal[i + 1] > 0)))
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionPlusVal.Length);
        }

        public static double PercentTracking(double[] correctionPlusVal, double[] correctionMinusVal)
        {
            //Counting the number of times that a correction in one direction is followed by a correction in the same direction
            //  as a percentage of correction cycles
            double nmCount = 0;
            for (int i = 0; i < correctionPlusVal.Length - 1; i++)
            {
                if (((correctionPlusVal[i] > 0) && (correctionPlusVal[i + 1] > 0)) || ((correctionMinusVal[i] > 0) && (correctionMinusVal[i + 1] > 0)))
                { nmCount += 1; }
            }
            return ((nmCount * 100) / correctionPlusVal.Length);
        }

        public static double PercentOvershoot(double[] errorVal)
        {
            //Counting the number of times that a positive (negative) error is followed by a negative (positive) error
            //  as a percentage of correction cycles
            double nmCount = 0;
            for (int i = 0; i < errorVal.Length - 1; i++)
            {
                if (((errorVal[i] > 0) && (errorVal[i + 1] < 0)) || ((errorVal[i] < 0) && (errorVal[i + 1] > 0)))
                { nmCount += 1; }
            }
            return ((nmCount * 100) / errorVal.Length);
        }

        public static double[] RMSError(double[] errorXVal, double[] errorYVal, double[] errorTVal)
        {
            //Root mean sum of the squares of all error points
            double dCount = errorXVal.Length;
            double sumXsquared = 0;
            double sumYsquared = 0;
            double sumXYsquared = 0;
            double[] RMS = new double[3];

            for (int i = 0; i < dCount; i++)
            {
                sumXsquared += Math.Pow(errorXVal[i], 2);
                sumYsquared += Math.Pow(errorYVal[i], 2);
                sumXYsquared += Math.Pow(errorTVal[i], 2);
            }
            RMS[0] = Math.Sqrt(sumXsquared / dCount);
            RMS[1] = Math.Sqrt(sumYsquared / dCount);
            RMS[2] = Math.Sqrt(sumXYsquared / dCount);

            return (RMS);
        }

        public static double[] MDRStats(Complex[] freqXVal, Complex[] freqYVal, Complex[] freqTVal, double DFTSampleRate)
        {
            //Compute the sum of difference in energies of X and Y at frequencies above 30 seconds, but below the drift (0 period value)
            //  DFTSampleRate is in cycles/second/sample (0-N/2)

            const double minPeriod = 30;  //Seconds per cycle
            //Calculate lowest frequency sample
            int maxFFTsample = (int)(minPeriod / DFTSampleRate);


            double[] DriftVec = new double[3] { 0, 0, 0 };
            if (maxFFTsample > freqXVal.Length) maxFFTsample = freqXVal.Length;

            for (int i = 1; i < maxFFTsample; i++)
            {
                DriftVec[0] += freqXVal[i].Magnitude;
                DriftVec[1] += freqYVal[i].Magnitude;
                DriftVec[2] += (freqXVal[i].Magnitude - freqYVal[i].Magnitude) / freqYVal[i].Magnitude;
            }
            DriftVec[0] = DriftVec[0] / maxFFTsample;
            DriftVec[1] = DriftVec[1] / maxFFTsample;
            DriftVec[2] = DriftVec[2] / maxFFTsample;
            return (DriftVec);
        }

        public static double[] FrequencyMedian(double[,] errorVals)
        {
            //Determines the midpoint frequency for X,Y and total points
            //find the mean of the frequency measurements such that
            //the total of magnitudes above a particular frequency is equal
            //to the total of magnitudes below it.
            double dCount = errorVals.Length / 3;
            double sumXsquared = 0;
            double sumYsquared = 0;
            double sumTsquared = 0;
            double[] meanFreqs = new double[3];

            //Get the total of all magnitudes
            for (int i = 0; i < dCount; i++)
            {
                sumXsquared += errorVals[i, 0];
                sumYsquared += errorVals[i, 1];
                sumTsquared += errorVals[i, 2];
            }
            //Find the frequency where the total is half the value
            double xMean = 0;
            double yMean = 0;
            double tMean = 0;
            for (int i = 0; i < dCount; i++)
            {
                xMean += errorVals[i, 0];
                if (xMean < (sumXsquared / 2))
                { meanFreqs[0] = i + 1; }
                yMean += errorVals[i, 1];
                if (yMean < (sumYsquared / 2))
                { meanFreqs[1] = i + 1; }
                tMean += errorVals[i, 2];
                if (tMean < (sumTsquared / 2))
                { meanFreqs[2] = i + 1; }
            }

            return (meanFreqs);
        }

        public static void RemoveOffsetAndSlope(ref double[] tData)
        {
            int dCount = tData.Length;
            double sumXsquared = 0;
            double sumYsquared = 0;
            double sumXY = 0;
            double sumX = 0;
            double sumY = 0;

            //Determine offset and slope of data using linear regression
            // m = ( N*Σ(xy) − Σx Σy))/( N* Σ(x^2) − (Σx)^2)
            // b =  (Σy − m Σx) / N
            // where x is time domain and y is amplitude
            // where N is number of points

            //double[] flatData = new double[dCount];

            for (int i = 0; i < dCount; i++)
            {
                sumY += tData[i];
                sumX += i;
                sumYsquared += Math.Pow(tData[i], 2);
                sumXsquared += Math.Pow(i, 2);
                sumXY += (i * tData[i]);
            }

            double slope = ((dCount * sumXY) - (sumX * sumY)) / ((dCount * sumXsquared) - Math.Pow(sumX, 2));
            double offset = (sumY - (slope * sumX)) / dCount;

            for (int i = 0; i < dCount; i++)
            {
                tData[i] = tData[i] - (slope * i) - offset;
            }
            return;

        }
    }
}
