# GuideLogAnalyzer
Graphical data reader and frequency analysis for TheSkyX guider logs.

Guide Log Profiler

Rick McAlister, 02/15/18

Overview: 

Guide Log Profiler graphs TheSkyX autoguide log file error correction data in both time and frequency, and calculates some overall properties of the logged guiding behavior.  As such, this tool can be used to examine guider effectiveness, contrast the effects of different guider control configurations, compare before and after PEC training, and characterize the behavior of tracking inaccuracies such as caused by mechanical problems.
  
Description:

This application is driven off the selection of a TSX autoguide log file using the Browse command.  Once a file is selected the basic information fields of the log are parsed and displayed above the three graphs.  These fields should be self-explanatory.

The first graph displays the guider error data in X, Y and total amplitude in arcseconds over the course of the log.
The next two graphs display the error data translated to its frequency domain.  The middle graph displays the data over it's full range in order to visualize the lowest frequency (displayed as period in minutes) components.  These components are normally associated with mechanical inconsistencies which PEC addresses.  The bottom graph looks only at frequency (displayed as period in seconds) components between 0 and 30 seconds.  These are frequencies normally associated with seeing effects.

The fields under the graphs define or calculate characteristics of the guiding itself.  Sampling, Image Scale, Axis Control, Min and Max Move and Aggressiveness are taken directly from the log fields.  If AO was enabled, then those fields are also displayed from the log.

Error Statistics, in the Analysis section, displays derived values for RMS, Mean Period and Bracket.  

•	RMS is the mean X, Y and total error over the whole logging period.  

•	Mean Period is the "middle" frequency (displayed as a period - inverse of frequency) whereby the sum of the frequency magnitudes aboth this frequency are equal to the sum of the frequency magnitudes below.  This value is an indicator whether the error frequency is biased towards higher or lower frequencies.  

•	Bracket is a calculation of the number of successive errors that bracket the zero position.  That is, if a error is logged in the positive (or negative), then after correction is logged in the negative (or positive), then it is considered to "bracket" zero.  This is a sign that the guider may be over-correcting.

% Corrected, in the Analysis section, displays correction statistics.  Note that TheSkyX does not log AO corrections so this section (and the next) will only show mount corrections while AO is enabled.  This section tabulates the number of non-zero corrections made by the guider as a percentage of all correction cycles.  With accurate alignment and a good PEC, the guider should not need to make corrections on every cycle.  If these values are excessive, then one can assume that the guider is merely chasing excursions due to seeing.

Correction Statistics has two derived values:

Overshoot is a condition where a correction is made in one direction followed by a correctioin in the opposite direction.   Each overshoot may be either a result of seeing or of excessive correction.  If a large  percentage of corrections are overshoots, this suggests the latter.

Undershoot is a condition where a correction is made in one direction followed by a second correction in the same direction.  Each undershoot may be a result of either seeing excursions or insufficient correction. If a large percentage of corrections are undershoots, this suggests the latter.

Commands:

Browse:  Locate and open a TheSkyX Autoguide Log file..  

Help: Read this information.

About:  Some very brief information about the application.

Print: Prints the window as a single page to the default printer.

Close: The application is terminated.

Requirements: 

Guide Log Analyzer is a Windows Forms executable, written in C#.  Guider Log files used by this app may, but not necessarily must, have been produced by TheSkyX Pro 10.5.0.  The application may also require installation of Microsoft Powerpack 3.0 for charting (can be found at https://www.microsoft.com/en-us/download/details.aspx?id=25169).   The application runs as an uncertified, standalone application under Windows 7, 8 and 10.  

Installation:  

Download GuideLogAnalyzer.zip to your local drive from the publish directory.  Extract all contents to a default directory (e.g. GuideLogAnalyzer).  Launch setup.exe from that directory.  Upon completion, an application icon will have been added to the start menu under "TSXToolKit" with the name "Guide Log Analyzer".  This application can be pinned to the Start if desired.

Support:  

This application was written for the public domain and as such is unsupported. The developer wishes you his best and hopes everything works out.  If you find a problem or want to suggest addional features, please contact the author and he'll see what he can do.
