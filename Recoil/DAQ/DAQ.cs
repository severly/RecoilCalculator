using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MccDaq;
using System.Windows;
using System.Runtime.InteropServices;

namespace RecoilCalculator.DAQ
{
    
    public class DAQBoard
    {
        private MccBoard daqBoard;

        public const ushort TRIGGER_FOUND = 1;
        public const ushort TRIGGER_NOTFOUND = 2;
        public const ushort TRIGGER_SEARCHING = 4;

        public static int sampleRate = 24000;
        public static double sampleRateF = sampleRate;
        public static double mVoltsPerLb = 0.972F; // DAQ Calibration constant
        public static float voltsPerLb = 0.000972F; // DAQ Calibration constant

        public static double shortTomVolts = 3.2768;
        public static float triggerForce = 20F; // Trigger force in lbs
        public static float triggerForceVolts = triggerForce * voltsPerLb;
        private static short triggerLevel = (short) (triggerForce * mVoltsPerLb * shortTomVolts + 8192);

        public static float preTriggerTime = .005F; // 5 ms;
        public static float postTriggerTime = .010F; // 5 ms;
        private static int preTriggerPoints = (int) (sampleRate * preTriggerTime);
        private static int postTriggerPoints = (int) (sampleRate * postTriggerTime);
        private static int totalTriggerPoints = preTriggerPoints + postTriggerPoints;

        private static int totalElements = sampleRate * 30; // keep 30 seconds of data in buffer
        private static int NumElements = totalElements - totalElements % 31;

        public double[] daqData; // array to hold final output values
        public short[] ADData = new short[NumElements]; //  array to hold the input values
        private IntPtr MemHandle;		//  define a variable to contain the handle for
                                        //  memory allocated by Windows through MccDaq.MccService.WinBufAlloc()

        private static int lastCurCount = 0;
        private static int firstpoint = 0;
        private static int triggerPoint = 0;
        private static int preTriggerPoint = 0;
        private static int curCount, curIndex;
        private Boolean triggerFound = false;

        public string statusText = "Waiting...";

        public DAQBoard()
        {
            InitUL();
        }

        /*
         * Initializes connection with the MccDaq board
         */
        private void InitUL()
        {
            /*
             * Initiate error handling
             *  activating error handling will trap errors like bad channel numbers and non-configured conditions.
             *  Parameters:
             *    MccDaq.ErrorReporting.PrintAll :all warnings and errors encountered will be printed
             *    MccDaq.ErrorHandling.StopAll   :if any error is encountered, the program will stop
             */

            MccDaq.ErrorInfo ULStat;
            ULStat = MccDaq.MccService.ErrHandling(MccDaq.ErrorReporting.PrintAll, MccDaq.ErrorHandling.StopFatal);

            // set aside memory to hold data
            MemHandle = MccService.WinBufAllocEx(NumElements);
            if (MemHandle == IntPtr.Zero || MemHandle == null)
            {
                MessageBox.Show("Could not allocate memory.\n", "Error");
                return;
            }

            daqBoard = new MccDaq.MccBoard(0);

            
        }

        public void stopScan()
        {
            daqBoard.StopBackground(FunctionType.AiFunction);
        }

        // Begin receiving data from the DAQ, store in buffer pointed to by MemHandle
        public void StartScan()
        {

            MccDaq.ErrorInfo ULStat;
            MccDaq.Range Range = Range.Bip10Volts; //Voltage range of the DAQ

            firstpoint = 0; // Reset first point
            lastCurCount = 0; // reset curcount
            triggerFound = false; // Reset trigger

            int Chan = 0;               // lowChannel/highChannel number;
            int CBCount = NumElements;  // Number of data points to collect;
            int CBRate = sampleRate;    // Acquire data at 48 kHz;
            object Options;             // data collection options;

            // Options. No need continuous, it will stop at NumElements 
            Options =  MccDaq.ScanOptions.Background | MccDaq.ScanOptions.BlockIo;

            // Collect the values with AInScan() and store in memory at MemHandle
            ULStat = daqBoard.AInScan(Chan, Chan, CBCount, ref CBRate, Range,
                                      MemHandle, (MccDaq.ScanOptions) Options);
        }

        // Every 30ms, check data for trigger 
        public ushort CheckForTrigger()
        {
            MccDaq.ErrorInfo ULStat;
            ushort trigStatus = TRIGGER_SEARCHING;
            short status;

            // Get staus of Analog Input background operation
            ULStat = daqBoard.GetStatus(out status, out curCount, out curIndex, MccDaq.FunctionType.AiFunction);

            int totalSamples = curCount - lastCurCount;

            statusText = totalSamples.ToString();

            
            if ( status == 0 )
            {
                return TRIGGER_NOTFOUND;
            }

             /*
            if (status == 0)
            {
                MccService.WinBufToArray(MemHandle, ADData, 0, NumElements-1);
                return TRIGGER_FOUND;
            }
            else
                return TRIGGER_SEARCHING;

             */

            if ( triggerFound )
            {
                daqBoard.StopBackground(FunctionType.AiFunction);
                preTriggerPoint = triggerPoint - preTriggerPoints;
                MccService.WinBufToArray(MemHandle, ADData, preTriggerPoint, totalTriggerPoints);
                return TRIGGER_FOUND;
            }

            MccService.WinBufToArray(MemHandle, ADData, lastCurCount, totalSamples);

            for(int i = 0; i < totalSamples; i++)
            {
                if (ADData[i] >= triggerLevel)   
                {
                    
                    triggerFound = true;
                    triggerPoint = lastCurCount + i;

                    // If there aren't enough sample points after the trigger, 
                    // wait for one more call to CheckForTrigger()
                    if (totalSamples - i < postTriggerPoints) break;

                    daqBoard.StopBackground(FunctionType.AiFunction);

                    // If there aren't enough points before the trigger, grab more data points
                    // from the windows buffer. If there are too many, toss some samples.
                    preTriggerPoint = triggerPoint - preTriggerPoints;

                    // Let's not have a negative array index
                    if (preTriggerPoint < 0)
                    {
                        preTriggerPoint = 0;
                    }

                    // Likewise, let's not go out of bounds by cutting of the excess
                    if ( (preTriggerPoint + totalTriggerPoints) > NumElements )
                        totalTriggerPoints -= (preTriggerPoint + totalTriggerPoints) - NumElements;

                    MccService.WinBufToArray(MemHandle, ADData, preTriggerPoint, totalTriggerPoints);
                    trigStatus = TRIGGER_FOUND;
                    break;
                }
            }

            firstpoint = curIndex;
            lastCurCount = curCount - 1;

            return trigStatus;
        }

        /*
         * When DAQ conversion is complete, this method will extract data from the Windows
         * buffer and convert the data to double precision format
         */
        public void ConvertData()
        {
            float dataVal;
            daqData = new double[totalTriggerPoints];

            for (int i = 0; i < totalTriggerPoints; i++)
            {
                daqBoard.ToEngUnits(Range.Bip10Volts, ADData[i], out dataVal);
                daqData[i] = (double)dataVal / voltsPerLb;
            }
        }



        /*
         * Accessor/manipulator functions
         * 
         */
        public void setPreTrigTime(float value)
        {
            preTriggerTime = value/1000;
            preTriggerPoints = (int)(sampleRate * preTriggerTime);
            totalTriggerPoints = preTriggerPoints + postTriggerPoints;
        }

        public void setPostTrigTime(float value)
        {
            postTriggerTime = value;
            postTriggerPoints = (int)(sampleRate * postTriggerTime);
            totalTriggerPoints = preTriggerPoints + postTriggerPoints;
        }

        public void setTrigLevel(float value)
        {
            triggerForce = value; // trigger force in lbs
            //triggerForceVolts = triggerForce * voltsPerLb; // convert from lbs to volts
            triggerLevel = (short)(triggerForce * mVoltsPerLb * shortTomVolts + 8192);

            MessageBox.Show("Trigger at " + triggerLevel.ToString());
        }


        public String getPreTrigTime()
        {
            return (preTriggerTime*1000).ToString() + " ms";
        }

        public String getPostTrigTime()
        {
            return (postTriggerTime*1000).ToString() + " ms";
        }

        public String getTrigForce()
        {
            return triggerForce.ToString() + " lbs";
        }

    }
}
