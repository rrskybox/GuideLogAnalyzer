///
/// Guide Log Analyzer Application
/// Rick McAlister, 2018
/// 
/// V1.0 -  Initial release (02-15-18)
/// V1.1 - Added a test while loading lines of data for incompatibilites
///         eg changing guider controls during a run will cause a new set of control
///         entries (min,max,aggressiveness) to be logged.  This will really confuse the
///         conversion routine. So, it burps an error and stops adding data at that point now.
/// V1.2  - Added a Wander error estimate:  2 x RMS difference between the error average and absolute position
/// V1.3  - Removed Wander; added Wobble indicator.
/// V1.4  - Added check for PE Index (worm) data presence
/// V1.5  - Added vertical annotation lines for worm gear periods based on Paramount type
///         Changed frequency graph to spline type and removed explicit data points
///

using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Deployment.Application;

namespace GuideLogAnalyzer
{
    public partial class FormGuideLogAnalyzer : Form
    {
        public Series tGraphT;
        public Series tGraphX;
        public Series tGraphY;
        public Series fGraphT;
        public Series fGraphX;
        public Series fGraphY;
        public Series mGraphT;
        public Series mGraphX;
        public Series mGraphY;
        public Series cGraphPX;
        public Series cGraphPY;

        private double enteredImageScale = 1;

        public FormGuideLogAnalyzer()
        {
            InitializeComponent();
            printDocument1.PrintPage += new PrintPageEventHandler(printDocument1_PrintPage);
            // Acquire the version information and put it in the form header
            try { this.Text = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString(); }
            catch { this.Text = " in Debug"; } //probably in debug, no version info available
            this.Text = "Guide Log Analyzer V" + this.Text;
        }

        private void GuideLogFilePathBrowse_Click(object sender, EventArgs e)
        {

            DialogResult GuideLogPathResult = GuideLogFilenameDialog.ShowDialog();
            GuideLogFilePath.Text = GuideLogFilenameDialog.FileName;
            CompileForm();
        }

        private void CompileForm()
        {

            if (!GuideLogFilePath.Text.Contains(".log"))
            {
                MessageBox.Show("File not a log file");
                return;
            }

            PrepGraphs();

            //Acquire header object (XML)
            LogHeaderXML xH = new LogHeaderXML(GuideLogFilePath.Text);
            //Fill in basic information
            BuildBox.Text = xH.Detail(xH.TheSkyXBuild);
            MountBox.Text = xH.Detail(xH.Mount);
            CameraBox.Text = xH.Detail(xH.Camera);
            DateBox.Text = (xH.Detail(xH.LocalStartDateTime)).Split(' ')[0];
            TimeBox.Text = (xH.Detail(xH.LocalStartDateTime)).Split(' ')[1];
            ExposureBox.Text = (xH.Detail(xH.ExposureTime));
            BinningBox.Text = (xH.Detail(xH.BinX) + " x " + xH.Detail(xH.BinY));
            CalDecBox.Text = (xH.Detail(xH.Calibrationdeclination));
            CurrentDecBox.Text = (xH.Detail(xH.Declinationnow));
            CalPABox.Text = (xH.Detail(xH.Calibrationpositionangle));
            CurrentPABox.Text = (xH.Detail(xH.Positionanglenow));

            AxisControlBox.Text = xH.Detail(xH.XPlusAxis) +
                                        " / " +
                                        xH.Detail(xH.XMinusAxis) +
                                        " / " +
                                        xH.Detail(xH.YPlusAxis) +
                                        " / " +
                                        xH.Detail(xH.YMinusAxis);
            AxisControlBox.Text = AxisControlBox.Text.Replace("Not Enabled", "Off");
            AxisControlBox.Text = AxisControlBox.Text.Replace("Enabled", "On");
            MountControlGroup.Text = xH.Detail(xH.Autoguide);
            AggressivenessBox.Text = xH.Detail(xH.AggressivenessFactorXPlus) +
                                      " / " +
                                      xH.Detail(xH.AggressivenessFactorXMinus) +
                                      " / " +
                                      xH.Detail(xH.AggressivenessFactorYPlus) +
                                      " / " +
                                      xH.Detail(xH.AggressivenessFactorYMinus);
            MoveBox.Text = xH.Detail(xH.MinimumMove) + " (sec) / " + xH.Detail(xH.MaximumMove) + " (sec)";

            //if (Convert.ToInt16(xH.Detail("BinX") != 0) int binY = Convert.ToInt16(xH.Detail("BinY"));
            // int binX = if (Convert.ToInt16(xH.Detail("BinX"));
            //int binY = Convert.ToInt16(xH.Detail("BinY"));
            int binX = Convert.ToInt16(xH.Detail("BinX"));
            int binY = Convert.ToInt16(xH.Detail("BinY"));

            double scaleX; // = 1.0 / (double)binX;
            double scaleY; // = 1.0 / (double)binY;
            double scaleT;

            double xSlope = 0;
            double ySlope = 0;
            double tSlope = 0;

            string scaleXString = xH.Detail("ImageScaleXatbinning" + binX.ToString("0"));
            string scaleYString = xH.Detail("ImageScaleYatbinning" + binY.ToString("0"));

            if (scaleXString.Contains("NA")) scaleX = enteredImageScale;
            else scaleX = Convert.ToDouble(scaleXString);
            if (scaleYString.Contains("NA")) scaleY = enteredImageScale;
            else scaleY = Convert.ToDouble(scaleYString);

            scaleT = Math.Sqrt(Math.Pow(scaleX, 2) + Math.Pow(scaleY, 2));
            //double scaleT = scaleX;
            ImageScaleBox.Text = scaleXString + " / " + scaleYString;

            AOAggressivenessBox.Text = xH.Detail(xH.AOAggressiveness);
            AORelayBumpSizeBox.Text = xH.Detail(xH.AORelayBumpSize);
            AORelayThresholdBox.Text = xH.Detail(xH.AORelayThreshold);
            AOSlewRateBox.Text = xH.Detail(xH.AOSlewRate);
            AOTrackSizeBox.Text = xH.Detail(xH.AOTrackBoxSize);

            //Acquire data from log 
            LogReader guideLog = new LogReader(GuideLogFilePath.Text);
            //Make a complex vector of guideLog data
            int vLen = guideLog.GetLogSize() - 1;
            Complex[] errorValsTcplx = new Complex[vLen];
            Complex[] errorValsXcplx = new Complex[vLen];
            Complex[] errorValsYcplx = new Complex[vLen];
            double[] errorValsTdbl = new double[vLen];
            double[] errorValsXdbl = new double[vLen];
            double[] errorValsYdbl = new double[vLen];
            Double[] correctionXPlus = new double[vLen];
            Double[] correctionXMinus = new double[vLen];
            Double[] correctionYPlus = new double[vLen];
            Double[] correctionYMinus = new double[vLen];
            double[] wormIndexRA = new double[vLen];
            double[] wormIndexDec = new double[vLen];
            double[] guideStarSignal = new double[vLen];

            double FFTTimeIncrement = 0; //time per sample in sec
            double nowTime = 0;

            //time domain plot of log data, total, X and Y, mean, and guidestarsignal
            for (int i = 0; i < vLen; i++)
            {
                errorValsTdbl[i] = guideLog.GetLogValue(i, LogReader.LogVal.TotGuideErr) * scaleT;
                errorValsXdbl[i] = guideLog.GetLogValue(i, LogReader.LogVal.GuideErrX) * scaleX;
                errorValsYdbl[i] = guideLog.GetLogValue(i, LogReader.LogVal.GuideErrY) * scaleY;

                correctionXPlus[i] = guideLog.GetLogValue(i, LogReader.LogVal.XPlusRelay);
                correctionXMinus[i] = guideLog.GetLogValue(i, LogReader.LogVal.XMinusRelay);
                correctionYPlus[i] = guideLog.GetLogValue(i, LogReader.LogVal.XPlusRelay);
                correctionYMinus[i] = guideLog.GetLogValue(i, LogReader.LogVal.YMinusRelay);
                wormIndexRA[i] = guideLog.GetLogValue(i, LogReader.LogVal.PECIndexRA);
                wormIndexDec[i] = guideLog.GetLogValue(i, LogReader.LogVal.PECIndexDec);
                guideStarSignal[i] = guideLog.GetLogValue(i, LogReader.LogVal.GuideStarSignal);
            }

            if (RemoveDriftCheckBox.Checked)
            {
                tSlope = Analysis.RemoveOffsetAndSlope(ref errorValsTdbl,false);
                xSlope = Analysis.RemoveOffsetAndSlope(ref errorValsXdbl,false);
                ySlope = Analysis.RemoveOffsetAndSlope(ref errorValsYdbl,false);
            }
            else
            {
                tSlope = Analysis.RemoveOffsetAndSlope(ref errorValsTdbl, true); //Slope only
                xSlope = Analysis.RemoveOffsetAndSlope(ref errorValsXdbl, true); //Slope only
                ySlope = Analysis.RemoveOffsetAndSlope(ref errorValsYdbl, true); //Slope only

            }

            for (int i = 0; i < vLen; i++)
            {
                errorValsTcplx[i] = new Complex(errorValsTdbl[i], 0);
                errorValsXcplx[i] = new Complex(errorValsXdbl[i], 0);
                errorValsYcplx[i] = new Complex(errorValsYdbl[i], 0);
            }


            for (int i = 0; i < vLen; i++)
            {
                nowTime = guideLog.GetLogValue(i, LogReader.LogVal.ElapsedSecs);
                if (i < (vLen - 1))
                {
                    FFTTimeIncrement += guideLog.GetLogValue(i + 1, LogReader.LogVal.ElapsedSecs) - nowTime;
                }
                tGraphT.Points.AddXY((nowTime / 60), errorValsTcplx[i].Real);
                tGraphX.Points.AddXY((nowTime / 60), errorValsXcplx[i].Real);
                tGraphY.Points.AddXY((nowTime / 60), errorValsYcplx[i].Real);
            }
            tGraphT.Color = Color.Red;
            tGraphX.Color = Color.Blue;
            tGraphY.Color = Color.Green;

            Show();
            //Determine if wormIndex data is present and post results
            double pesum = 0;
            for (int i = 0; i < vLen; i++) { pesum += (wormIndexDec[i] + wormIndexRA[i]); }
            if (pesum == 0) PEIndexTextBox.Text = "No";
            else PEIndexTextBox.Text = "Yes";

            //frequency domain plot of log data, long period and short period
            FourierTransform.DFT(errorValsTcplx, FourierTransform.Direction.Forward);
            FourierTransform.DFT(errorValsXcplx, FourierTransform.Direction.Forward);
            FourierTransform.DFT(errorValsYcplx, FourierTransform.Direction.Forward);
            //
            int FFTLen = errorValsTcplx.Length;
            FFTTimeIncrement /= (vLen - 1);
            double FFTSampleRate = 1 / FFTTimeIncrement; //frequency increment per sample in cycles/sec
            SampleRateBox.Text = FFTTimeIncrement.ToString("00.00") + " (sec) / " + FFTSampleRate.ToString("0.00") + " (cps)";
            double FFTmagT = 0;
            double FFTmagX = 0;
            double FFTmagY = 0;
            double FFTperiod = 0;
            //double maxFreq = FFTFreqIncrement * (FFTLen / 2);
            int maxLongPeriod = 6; //minutes
            double maxShortPeriod = .5; //minutes
            double[,] errorFreq = new double[FFTLen / 2, 3];

            for (int i = 1; i < (FFTLen / 2); i++)
            {
                //fGraph.Points.Add(XPlusVals[i].Real);
                //FFTmagT = Math.Pow(errorValsTcplx[i].Real, 2) + Math.Pow(errorValsTcplx[i].Imaginary, 2);
                //FFTmagX = Math.Pow(errorValsXcplx[i].Real, 2) + Math.Pow(errorValsTcplx[i].Imaginary, 2);
                //FFTmagY = Math.Pow(errorValsYcplx[i].Real, 2) + Math.Pow(errorValsTcplx[i].Imaginary, 2);

                FFTmagT = errorValsTcplx[i].Magnitude;
                FFTmagX = errorValsXcplx[i].Magnitude;
                FFTmagY = errorValsYcplx[i].Magnitude;

                errorFreq[i, 0] = FFTmagT;
                errorFreq[i, 1] = FFTmagX;
                errorFreq[i, 2] = FFTmagY;

                FFTperiod = FFTLen / (i * FFTSampleRate * 60);
                if (FFTperiod <= maxLongPeriod)
                {
                    fGraphT.Points.AddXY(FFTperiod, FFTmagT);
                    fGraphX.Points.AddXY(FFTperiod, FFTmagX);
                    fGraphY.Points.AddXY(FFTperiod, FFTmagY);
                    fGraphT.Color = Color.Red;
                    fGraphX.Color = Color.Blue;
                    fGraphY.Color = Color.Green;
                }
                if (FFTperiod <= maxShortPeriod)
                {
                    mGraphT.Points.AddXY((FFTperiod * 60), FFTmagT);
                    mGraphX.Points.AddXY((FFTperiod * 60), FFTmagX);
                    mGraphY.Points.AddXY((FFTperiod * 60), FFTmagY);
                    mGraphT.Color = Color.Red;
                    mGraphX.Color = Color.Blue;
                    mGraphY.Color = Color.Green;
                }
            }
            //Calibration Plot
            double xpAngle = guideLog.GetMountVelocityVector(LogReader.Calibration.XPlus, LogReader.Vector.Angle);
            double xmAngle = guideLog.GetMountVelocityVector(LogReader.Calibration.XMinus, LogReader.Vector.Angle);
            double ypAngle = guideLog.GetMountVelocityVector(LogReader.Calibration.YPlus, LogReader.Vector.Angle);
            double ymAngle = guideLog.GetMountVelocityVector(LogReader.Calibration.YMinus, LogReader.Vector.Angle);
            double xpSpeed = Math.Abs(guideLog.GetMountVelocityVector(LogReader.Calibration.XPlus, LogReader.Vector.XSpeed));
            double xmSpeed = Math.Abs(guideLog.GetMountVelocityVector(LogReader.Calibration.XMinus, LogReader.Vector.XSpeed));
            double ypSpeed = Math.Abs(guideLog.GetMountVelocityVector(LogReader.Calibration.YPlus, LogReader.Vector.YSpeed));
            double ymSpeed = Math.Abs(guideLog.GetMountVelocityVector(LogReader.Calibration.YMinus, LogReader.Vector.YSpeed));

            cGraphPX.Points.Add(0, 0);
            cGraphPX.Points.AddXY(xpAngle, xpSpeed);
            cGraphPX.Points[cGraphPX.Points.Count - 1].MarkerStyle = MarkerStyle.Cross;
            cGraphPX.Points.Add(0, 0);
            cGraphPX.Points.AddXY(xmAngle, xmSpeed);
            cGraphPX.Points[cGraphPX.Points.Count - 1].MarkerStyle = MarkerStyle.Circle;
            cGraphPY.Points.Add(0, 0);
            cGraphPY.Points.AddXY(ypAngle, ypSpeed);
            cGraphPY.Points[cGraphPY.Points.Count - 1].MarkerStyle = MarkerStyle.Cross;
            cGraphPY.Points.Add(0, 0);
            cGraphPY.Points.AddXY(ymAngle, ymSpeed);
            cGraphPY.Points[cGraphPY.Points.Count - 1].MarkerStyle = MarkerStyle.Circle;

            foreach (System.Windows.Forms.DataVisualization.Charting.DataPoint point in cGraphPX.Points)
            {
                if ((double)point.YValues.GetValue(0) == 0)
                {
                    point.IsValueShownAsLabel = false;
                    point.MarkerStyle = MarkerStyle.None;
                }
                else point.IsValueShownAsLabel = true;
            }

            foreach (System.Windows.Forms.DataVisualization.Charting.DataPoint point in cGraphPY.Points)
            {
                if ((double)point.YValues.GetValue(0) == 0)
                {
                    point.IsValueShownAsLabel = false;
                    point.MarkerStyle = MarkerStyle.None;
                }
                else point.IsValueShownAsLabel = true;
            }
            //Add vertical lines for Paramount 
            ChartArea CA = chart2.ChartAreas[0];
            double hPeriod = 0;
            if (MountBox.Text.Contains("MX")) hPeriod = 229.77;
            else if (MountBox.Text.Contains("MYT")) hPeriod = 269.26;
            else if (MountBox.Text.Contains("ME")) hPeriod = 149.99;

            if (hPeriod != 0)
            {
                chart2.Annotations.Clear();

                double harmonic0 = hPeriod / 60;
                double harmonic1 = harmonic0 / 2;
                double harmonic2 = harmonic0 / 3;
                double harmonic3 = harmonic0 / 4;
                double harmonic4 = harmonic0 / 5;
                double harmonic5 = harmonic0 / 6;
                double harmonic6 = harmonic0 / 7;
                double harmonic7 = harmonic0 / 8;


                VerticalLineAnnotation VA0 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine0",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,
                    X = harmonic0
                };
                VerticalLineAnnotation VA1 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine1",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,
                    X = harmonic1
                };
                VerticalLineAnnotation VA2 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine2",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,
                    X = harmonic2
                };
                VerticalLineAnnotation VA3 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine3",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,         // use your numbers!
                    X = harmonic3
                };
                VerticalLineAnnotation VA4 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine4",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,         // use your numbers!
                    X = harmonic4
                };
                VerticalLineAnnotation VA5 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine5",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,         // use your numbers!
                    X = harmonic5
                };
                VerticalLineAnnotation VA6 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine6",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,         // use your numbers!
                    X = harmonic6
                };

                VerticalLineAnnotation VA7 = new VerticalLineAnnotation
                {
                    AxisX = CA.AxisX,
                    AllowMoving = false,
                    IsInfinitive = true,
                    ClipToChartArea = CA.Name,
                    Name = "myLine7",
                    LineColor = Color.DarkOrange,
                    LineWidth = 2,         // use your numbers!
                    X = harmonic7
                };

                chart2.Annotations.Add(VA0);
                chart2.Annotations.Add(VA1);
                chart2.Annotations.Add(VA2);
                chart2.Annotations.Add(VA3);
                chart2.Annotations.Add(VA4);
                chart2.Annotations.Add(VA5);
                chart2.Annotations.Add(VA6);
                chart2.Annotations.Add(VA7);

            }

            Show();

            //Determine the percentage of saturated star observations
            double percentSat = Analysis.PercentSaturated(guideStarSignal);
            double percentLost = Analysis.PercentLost(guideStarSignal);
            SaturatedTextBox.Text = percentSat.ToString("0.0") + "% / " + percentLost.ToString("0.0") + "%";

            //Determine percentage of moves vrs non-moves in X and Y
            double movePointsXPlus = Analysis.PercentErrorsCorrected(correctionXPlus);
            double movePointsXMinus = Analysis.PercentErrorsCorrected(correctionXMinus);
            double movePointsYPlus = Analysis.PercentErrorsCorrected(correctionYPlus);
            double movePointsYMinus = Analysis.PercentErrorsCorrected(correctionYMinus);
            CorrectionEachPercentBox.Text = movePointsXPlus.ToString("0") + "% / " +
                                                    movePointsXMinus.ToString("0") + "% / " +
                                                    movePointsYPlus.ToString("0") + "% / " +
                                                    movePointsYMinus.ToString("0") + "%";
            double movePointsX = Analysis.PercentErrorsCorrected(correctionXPlus, correctionXMinus);
            double movePointsY = Analysis.PercentErrorsCorrected(correctionYPlus, correctionYMinus);
            CorrectionXorYPercentBox.Text = movePointsX.ToString("0") + "% / " + movePointsY.ToString("0") + "%";
            double movePoints = Analysis.PercentErrorsCorrected(correctionXPlus, correctionXMinus, correctionYPlus, correctionYMinus);
            CorrectionXandYPercentBox.Text = movePoints.ToString("0") + "%";
            Show();

            double switchBackX = Analysis.PercentSwitchBacks(correctionXPlus, correctionXMinus);
            double switchBackY = Analysis.PercentSwitchBacks(correctionYPlus, correctionYMinus);
            SwitchBackPercentBox.Text = switchBackX.ToString("0") + "% / " + switchBackY.ToString("0") + "%";
            Show();

            double TrackingX = Analysis.PercentTracking(correctionXPlus, correctionXMinus);
            double TrackingY = Analysis.PercentTracking(correctionYPlus, correctionYMinus);
            TrackingPercentBox.Text = TrackingX.ToString("0") + "% / " + TrackingY.ToString("0") + "%";
            Show();

            double overshootX = Analysis.PercentOvershoot(errorValsXdbl);
            double overshootY = Analysis.PercentOvershoot(errorValsYdbl);
            OvershootPercentBox.Text = overshootX.ToString("0") + "% / " + overshootY.ToString("0") + "%";
            Show();

            double[] RMSStats = Analysis.RMSError(errorValsXdbl, errorValsYdbl, errorValsTdbl);
            ErrorRMSBox.Text = RMSStats[0].ToString("0.0") + " / " + RMSStats[1].ToString("0.0") + " / " + RMSStats[2].ToString("0.0");
            Show();

            double[] mdrStats = Analysis.MDRStats(errorValsXcplx, errorValsYcplx, errorValsTcplx, FFTSampleRate);
            //double sigToNoiseAvg = (mdrStats[0] - mdrStats[1]) / mdrStats[1];
            MDRBox.Text = (mdrStats[2] * 100).ToString("0") + " %";

            double[] FreqMedStats = Analysis.FrequencyMedian(errorFreq);
            double iToF = FFTSampleRate / FFTLen;
            FrequencyBalanceBox.Text = (1 / (iToF * FreqMedStats[0])).ToString("0") + " sec / " + (1 / (iToF * FreqMedStats[1])).ToString("0") + " sec";

            AvgDriftTextBox.Text = (xSlope * 60.0 / FFTTimeIncrement).ToString("0.0") + " / " + (ySlope * 60.0 / FFTTimeIncrement).ToString("0.0") + " / " + (tSlope * 60.0 / FFTTimeIncrement).ToString("0.0");


            Show();
        }

        private void PrepGraphs()
        {
            //Prep the time domain graph
            chart1.Series.Clear();
            tGraphT = new Series("Total Error")
            {
                XValueType = ChartValueType.Int32,
                ChartType = SeriesChartType.FastLine,
                MarkerStyle = MarkerStyle.Circle
            };

            chart1.Series.Add(tGraphT);
            tGraphX = new Series("X Error");
            chart1.Series.Add(tGraphX);
            tGraphX.XValueType = ChartValueType.Int32;
            tGraphX.ChartType = SeriesChartType.FastLine;
            tGraphX.MarkerStyle = MarkerStyle.Circle;

            tGraphY = new Series("Y Error");
            chart1.Series.Add(tGraphY);
            tGraphY.XValueType = ChartValueType.Int32;
            tGraphY.ChartType = SeriesChartType.FastLine;
            tGraphY.MarkerStyle = MarkerStyle.Circle;

            //Prep the longer frequency domain chart
            chart2.Series.Clear();
            fGraphT = new Series("Total Error");
            chart2.Series.Add(fGraphT);
            fGraphT.XValueType = ChartValueType.Int32;
            fGraphT.ChartType = SeriesChartType.Spline;
            //fGraphT.MarkerStyle = MarkerStyle.Circle;

            fGraphX = new Series("X Error");
            chart2.Series.Add(fGraphX);
            fGraphX.XValueType = ChartValueType.Int32;
            fGraphX.ChartType = SeriesChartType.Spline;
            //fGraphX.MarkerStyle = MarkerStyle.Circle;

            fGraphY = new Series("Y Error");
            chart2.Series.Add(fGraphY);
            fGraphY.XValueType = ChartValueType.Int32;
            fGraphY.ChartType = SeriesChartType.Spline;
            //fGraphY.MarkerStyle = MarkerStyle.Circle;

            //Prep the shorter frequency domain chart
            chart3.Series.Clear();
            mGraphT = new Series("Total Error");
            chart3.Series.Add(mGraphT);
            mGraphT.XValueType = ChartValueType.Int32;
            mGraphT.ChartType = SeriesChartType.Spline;
            // mGraphT.MarkerStyle = MarkerStyle.Circle;

            mGraphX = new Series("X Error");
            chart3.Series.Add(mGraphX);
            mGraphX.XValueType = ChartValueType.Int32;
            mGraphX.ChartType = SeriesChartType.Spline;
            //mGraphX.MarkerStyle = MarkerStyle.Circle;

            mGraphY = new Series("Y Error");
            chart3.Series.Add(mGraphY);
            mGraphY.XValueType = ChartValueType.Int32;
            mGraphY.ChartType = SeriesChartType.Spline;
            //mGraphY.MarkerStyle = MarkerStyle.Circle;

            chart4.Series.Clear();
            cGraphPX = new Series("CalibrationPX");
            chart4.Series.Add(cGraphPX);
            cGraphPX.ChartType = SeriesChartType.Polar;
            cGraphPX.XValueType = ChartValueType.Double;
            cGraphPX.YValueType = ChartValueType.Double;
            cGraphPX.MarkerStyle = MarkerStyle.Circle;
            cGraphPX.MarkerColor = Color.Blue;
            cGraphPX.SetCustomProperty("PolarDrawingStyle", "Line");

            cGraphPY = new Series("CalibrationPY");
            chart4.Series.Add(cGraphPY);
            cGraphPY.ChartType = SeriesChartType.Polar;
            cGraphPY.XValueType = ChartValueType.Double;
            cGraphPY.YValueType = ChartValueType.Double;
            cGraphPY.MarkerStyle = MarkerStyle.Circle;
            cGraphPY.MarkerColor = Color.Green;
            cGraphPY.SetCustomProperty("PolarDrawingStyle", "Line");

            ChartArea cArea = chart4.ChartAreas.FindByName("CalibrationChart");
            cArea.AxisX.LabelStyle.Enabled = false;
            cArea.AxisX.Interval = 90;
            cArea.AxisY.Enabled = AxisEnabled.False;

            //cGraphP.IsValueShownAsLabel = true;
            cGraphPX.LabelForeColor = Color.Blue;
            cGraphPX.LabelFormat = "0.00";
            cGraphPX.SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
            cGraphPY.LabelForeColor = Color.Green;
            cGraphPY.LabelFormat = "0.00";
            cGraphPY.SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
            //cGraph.SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Right;

            return;
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            string aboutMessage = "TSX Toolkit Application\r\n\r\n" +
                "Guide Log Analyzer\r\n\r\n" +
                "Rick McAlister (2018)";
            System.Windows.Forms.MessageBox.Show(aboutMessage, "About", MessageBoxButtons.OK);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void PrintButton_Click(object sender, EventArgs e)
        {
            CaptureScreen();
            printDocument1.DefaultPageSettings.Landscape = true;
            printDocument1.Print();
            return;
        }

        Bitmap memoryImage;

        private void CaptureScreen()
        {
            Graphics myGraphics = this.CreateGraphics();
            Size s = this.Size;
            memoryImage = new Bitmap(s.Width, s.Height, myGraphics);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            memoryGraphics.CopyFromScreen(this.Location.X, this.Location.Y, 0, 0, s);
            return;
        }

        private void printDocument1_PrintPage(System.Object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawImage(memoryImage, e.PageBounds);
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            Form fHelp = new FormHelp();
            fHelp.Show();
        }

        private void RemoveDriftCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CompileForm();
            return;
        }

        private void ImageScaleBox_TextChanged(object sender, EventArgs e)
        {
            //Convert decimal entry to image scale
            try
            {
                enteredImageScale = Convert.ToDouble(ImageScaleBox.Text);
                CompileForm();
            }
            catch { return; } //do nothing if it won't convert cleanly
                              //otherwise, update the image scale values
        }
    }
}

