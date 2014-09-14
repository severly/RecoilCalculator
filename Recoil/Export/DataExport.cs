using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data;

namespace RecoilCalculator.Export
{
    using System.Xml.Xsl;
    using System.IO;
    using OxyPlot;
    using OxyPlot.Reporting;
    using OxyPlot.Pdf;

    class DataExport
    {
        public static void CreateWorkbook(double[] daqData, String product, float sampleRate)
        {
            String saveFile = product + "_Recoil"; 
            sampleRate /= 1000;
            var sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.FileName = saveFile;
            sfd.DefaultExt = ".csv";
            sfd.Filter = "Comma Separated Doc t (*.csv) | *.csv";

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                saveFile = sfd.FileName;
            else
                return;

            

            float timeIndex = 0;

            try
            {
                StreamWriter myWriter = new StreamWriter(saveFile);
                myWriter.WriteLine("Time [ms], Force [lbs]");
                foreach (double point in daqData)
                {
                    myWriter.WriteLine("{0}, {1}", (timeIndex++ / sampleRate) , point);
                }

                myWriter.Close();
            }
            catch (Exception crap)
            {
                System.Windows.MessageBox.Show("Failed to export data:\n" + crap.Message, "Warning");
            }

        }

        public static void GenerateReport(String productName, PlotModel model)
        {
            String saveFile = productName;

            var sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.FileName = saveFile;
            sfd.DefaultExt = ".pdf";
            sfd.Filter = "PDF Document (*.pdf) | *.pdf";

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                saveFile = sfd.FileName;
            else
                return;

            try
            {
                var r = new Report();
                var main = new ReportSection();
                main.AddPlot(model, "Recoil Force", 500, 350);
                r.Add(main);

                ReportStyle style = new ReportStyle();
                PdfReportWriter w = new PdfReportWriter(saveFile);
                w.WriteReport(r, style);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Failed to export plot:\n" + e.Message, "Warning");
            }
        }

    }
}
