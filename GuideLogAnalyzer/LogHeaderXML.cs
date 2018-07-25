//This class converts the header of an autoguider log to XML format

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace GuideLogAnalyzer
{
    class LogHeaderXML
    {

        string XdecXMLDeclaration = "<?xml version=\"1.0\"?>";
        string XdecDocType = "<!DOCTYPE TSXAutoguiderLog>";
        
        string XdecRootName = "TSXLog";

        private XDocument xLogHdr;

        public LogHeaderXML(string fName)
        {
            string ldocTxt = null;
            string xName = null;
            string xVal = null;
            string[] ldocSplit = { null, null };
            XElement xEl;
            XDeclaration xDec = new XDeclaration(XdecXMLDeclaration, XdecDocType, "");
            xLogHdr = new XDocument(xDec);
            xLogHdr.Add(new XElement(XdecRootName, ""));
            TextReader logDataFile = File.OpenText(fName);
            //Retrieve the build number out of the first line -- it's different because it has no equal
            ldocTxt = logDataFile.ReadLine();
            ldocTxt = ldocTxt.Split(' ')[5];
            ldocTxt = ldocTxt.Replace(")", "");
            xEl = new XElement(TheSkyXBuild , ldocTxt);
            xLogHdr.Root.Add(xEl);
            //Now get the rest of the header information

            do
            {
                ldocTxt = logDataFile.ReadLine();
                if (ldocTxt != null)
                {
                    ldocTxt = ldocTxt.Replace("using", "=");
                    if (ldocTxt.Contains("="))
                    {
                        ldocSplit = ldocTxt.Split(Convert.ToChar("="));
                        xName = (ldocSplit[0].Trim()).Replace(" ", "");
                        xName = xName.Replace("(", "");
                        xName = xName.Replace(")", "");

                        xVal = ldocSplit[1].Trim();
                        xEl = new XElement(xName, xVal);
                        xLogHdr.Root.Add(xEl);
                    }
                }
            } while (ldocTxt != null);
            return;
        }

        public string Detail(string sName)
        {
            XElement xHeaderGroup = xLogHdr.Element(XdecRootName);
            XElement xentry =  xHeaderGroup.Element(sName);
            if (xentry == null)
            {
                return "NA";
            }
            else
            {
                return xentry.Value;
            }
        }

        public string TheSkyXAutoguidingLog = "TheSkyXAutoguidingLog";
        public string TheSkyXBuild = "TheSkyXBuild";
        public string LocalStartDateTime = "LocalStartDateTime";
        public string UTCStartDateTime = "UTCStartDateTime";
        public string Camera = "Camera";
        public string Mount = "Mount";
        public string XPlusAxis = "XPlusAxis";
        public string XMinusAxis = "XMinusAxis";
        public string YPlusAxis = "YPlusAxis";
        public string YMinusAxis = "YMinusAxis";
        public string ExposureTime = "ExposureTime";
        public string AggressivenessFactorXPlus = "AggressivenessFactorXPlus";
        public string AggressivenessFactorXMinus = "AggressivenessFactorXMinus";
        public string AggressivenessFactorYPlus = "AggressivenessFactorYPlus";
        public string AggressivenessFactorYMinus = "AggressivenessFactorYMinus";
        public string CalibrationTimeX = "CalibrationTimeX";
        public string CalibrationTimeY = "CalibrationTimeY";
        public string Calibrationdeclination = "Calibrationdeclination";
        public string Declinationnow = "Declinationnow";
        public string Calibrationpositionangle = "Calibrationpositionangle";
        public string Positionanglenow = "Positionanglenow";
        public string MinimumMove = "MinimumMove";
        public string MaximumMove = "MaximumMove";
        public string BacklashX = "BacklashX";
        public string BacklashY = "BacklashY";
        public string DelayAfterMove = "DelayAfterMove";
        public string BinX = "BinX";
        public string BinY = "BinY";
        public string ImageScaleXatbinning1 = "ImageScaleXatbinning1";
        public string ImageScaleYatbinning1 = "ImageScaleYatbinning1";
        public string AutoguideusingDirectGuide = "AutoguideusingDirectGuide";
        public string CalibrationDeterminedVelocityVectorspixelssec = "CalibrationDeterminedVelocityVectorspixelssec";
        public string XPlusSpeed = "XPlusSpeed";
        public string XPlusX = "XPlusX";
        public string XPlusY = "XPlusY";
        public string XPlusAngle = "XPlusAngle";
        public string XMinusSpeed = "XMinusSpeed";
        public string XMinusX = "XMinusX";
        public string XMinusY = "XMinusY";
        public string XMinusAngle = "XMinusAngle";
        public string YPlusSpeed = "YPlusSpeed";
        public string YPlusX = "YPlusX";
        public string YPlusY = "YPlusY";
        public string YPlusAngle = "YPlusAngle";
        public string YMinusSpeed = "YMinusSpeed";
        public string YMinusX = "YMinusX";
        public string YMinusY = "YMinusY";
        public string YMinusAngle = "YMinusAngle";
        public string AOAutoguiding = "AOAutoguiding";
        public string AOAggressiveness = "AOAggressiveness";
        public string AOSlewRate = "AOSlewRate";
        public string AORelayThreshold = "AORelayThreshold";
        public string AORelayBumpSize = "AORelayBumpSize";
        public string AOTrackBoxSize = "AOTrackBoxSize";
        public string Autoguide = "Autoguide";

    }
}
