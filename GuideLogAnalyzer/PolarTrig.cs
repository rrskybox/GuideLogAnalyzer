using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuideLogAnalyzer
{
    public class PolarTrig
    {
        public static double DegToRad(double degrees)
        //Converts angle in degrees to angle in radians
        { return ((degrees / 360) * (2 * Math.PI)); }

        public static double RadToDeg(double rad)
        //Converts angle in radians to angle in degrees
        { return ((rad / (2 * Math.PI)) * 360); }

        public static double RotateX(double inX, double inY, double angleR)
        //Computes X coordinate of a rotation on X,Y through a rotation of angle degrees
        //X in pixels, Y in pixels, angle in radians
        //x' = x cos deltaPA - y sin deltaPA
        // x'=x * cos theta - y * sin theta
        {
                       return ((inX * Math.Cos(angleR)) - (inY * Math.Sin(angleR)));
        }

        public static double RotateY(double inX, double inY, double angleR)
        //Computes Y coordinate of a rotation on X,Y through a rotation of angle degrees
        //X in pixels, Y in pixels, angle in radians
        //y' = y cos deltaPA + x sin deltaPA
        // y'=x * sin theta + y * cos theta
        {
            return ((inX * Math.Sin(angleR)) + (inY * Math.Cos(angleR)));
        }


    }
}
