
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace RecoilCalculator
{
    /// <summary>
    /// Interaction logic for RecoilCalcPage.xaml
    /// </summary>
    
    public partial class RecoilPlotPage : Window
    {
        
        SQLiteDatabase product_db;
        DataTable product_table;
        DAQ.DAQBoard daqBoard = new DAQ.DAQBoard();

        private ViewModels.PlotWindowModel plotModel;

        private System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
        private long lastUpdateMilliSeconds;

        private double kgToLbs = 2.204622;
        private double lbsToKg = 0.453592;

        private double currMass = 0;
        private String currProduct = " ";

        public RecoilPlotPage()
        {
            // Open database of product-mass pairs
            product_db = new SQLiteDatabase();
            // If the database file doesn't exist, make one
            product_db.makeTable();
            // Import the datatable from the database
            product_table = product_db.GetDataTable();

            // Create the plot 
            plotModel = new ViewModels.PlotWindowModel();
            DataContext = plotModel; // Assign plot to XAML Binding

            // GUI Initialization
            InitializeComponent();
            createProductBox();
            loadDefaults();
        }

        /*
         * DAQ Access functions 
         */
        private void StartClick(object sender, RoutedEventArgs e)
        {
            if ((StartBtn.Content as string) == "Start")
            {
                if (productListBox.SelectedIndex == 0)
                {
                    MessageBox.Show(Window.GetWindow(this), "Select a product first.", "Warning");
                    return;
                }

                daqBoard.StartScan();

                StartBtn.Content = "Stop";

                // Real time updating and trigger checking
                CompositionTarget.Rendering += CompositionTargetRendering;
                stopwatch.Start();
            }
            else
            {
                // Stops real time updating
                CompositionTarget.Rendering -= CompositionTargetRendering;
                stopwatch.Stop();
                stopwatch.Reset();

                daqBoard.stopScan();
                statusText.Text = "Data acquisition stopped";
                StartBtn.Content = "Start";
            }
        }

        // Calls trigger function every 30 ms
        private void CompositionTargetRendering(object sender, EventArgs e)
        {
            long updatePeriod = 30; // How often to update in ms
            if (stopwatch.ElapsedMilliseconds > lastUpdateMilliSeconds + updatePeriod)
            {
                lastUpdateMilliSeconds = stopwatch.ElapsedMilliseconds;

                ushort TriggerStatus = daqBoard.CheckForTrigger();
                switch (TriggerStatus)
                {
                    case DAQ.DAQBoard.TRIGGER_SEARCHING:
                        statusText.Text = "Waiting for trigger " 
                            + (lastUpdateMilliSeconds/1000).ToString() + " s";
                        return;
                    case DAQ.DAQBoard.TRIGGER_NOTFOUND:
                        statusText.Text = "Trigger not found";
                        return;
                    case DAQ.DAQBoard.TRIGGER_FOUND:
                        triggerFoundAction();
                        return;
                }
            }
        }

        /*
         * When trigger is found, stop checking for trigger, disable stopwatch,
         * convert ushort daq data to double format, plot the data, and calculate
         * relevant values
         */
        private void triggerFoundAction()
        {
            statusText.Text = "Done";

            CompositionTarget.Rendering -= CompositionTargetRendering;
            stopwatch.Stop();

            daqBoard.ConvertData();

            String recoilStr = calculateRecoil();
            RecoilRO.Text = recoilStr;
            PeakRO.Text = calculatePeak();

            // Tell view model to plot data currently stored in daqBoard
            plotModel.LoadData(daqBoard.daqData, DAQ.DAQBoard.sampleRateF, currProduct, recoilStr);

            // Reset start button
            StartBtn.Content = "Start";
        }


        // Calculate recoil force based on daqBoard data
        private String calculateRecoil()
        {
            double velocity = 0;
            double recoilEnergy;
            double joulesToFP = 0.73756;

            /*
             * Acceleration = recoil Force / Gun mass
             * Velocity = integrate(Acceleration)
             * Recoil Energy = Gun Mass/ 2 * Velocity ^2
             */
            foreach (double data in daqBoard.daqData)
            {
                velocity += (data * lbsToKg) / currMass;
            }

            recoilEnergy = (currMass / 2) * velocity * velocity;
            recoilEnergy *= joulesToFP; // convert to Foot-Pounds

            return recoilEnergy.ToString("G3") + " ft / lbs";
        }

        private String calculatePeak()
        {
            return daqBoard.daqData.Max().ToString("G3") + " lbs";
        }

        /*
         * ------------------------------------------------------------------------
         * Button Click Event Handlers
         * ------------------------------------------------------------------------
         */

        

        private void ResetClick(object sender, RoutedEventArgs e)
        {
            plotModel.clearPlot();
            RecoilRO.Text = " ";
            PeakRO.Text = " ";
        }

        /*
         * Product Database functions
         */

        // Load the default Trigger level, pre and post trigger times into GUI
        private void loadDefaults()
        {
            TriggerLvl.Text = daqBoard.getTrigForce();
            PreTrig.Text = daqBoard.getPreTrigTime();
            PostTrig.Text = daqBoard.getPostTrigTime();
        }

        // Use the datatable returned from DB to create a dropdown selection list
        private void createProductBox()
        {
            product_table = product_db.GetDataTable();

            productListBox.Items.Clear();
            productListBox.Items.Add("Select A Product");

            foreach (DataRow row in product_table.Select("", "Product ASC")) // Loop over the rows
                productListBox.Items.Add(row.ItemArray[0]);// Invokes ToString abstract method.

            productListBox.SelectedIndex = 0;
        }

        // Handles delete key press on combobox
        private void DeleteProduct(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            if (productListBox.SelectedIndex == 0) return;

            MessageBoxResult result = MessageBox.Show("Delete " + currProduct + " from database?",
                "Warning", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                product_db.Delete( productListBox.SelectedItem.ToString() );
                createProductBox();
            }
        }

        // Handles enter key events on main GUI
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;

            TextBox box = (TextBox) sender;
            float value = 0;
            try
            {
                value = Convert.ToSingle(box.Text);
            }
            catch (FormatException)
            {
                box.Clear();
                MessageBox.Show(Window.GetWindow(this), "Value must be a sequence of digits\nNote: Units are implicit", "Warning");
                return;
            }
            catch (OverflowException)
            {
                box.Clear();
                MessageBox.Show(Window.GetWindow(this), "Value is too large.", "Warning");
                return;
            }


            switch ( ((TextBox)sender).Name )
            {
                case "TriggerLvl" :
                    daqBoard.setTrigLevel( value );
                    box.Text += " lbs";
                    return;

                case "PreTrig" :
                    daqBoard.setPreTrigTime( value );
                    box.Text += " ms";
                    return;

                case "PostTrig" :
                    daqBoard.setPostTrigTime( value );
                    box.Text += " ms";
                    return;
            }
        }


        // Update displayed mass based on dropdown menu selection
        private void productListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (productListBox.SelectedIndex == 0)
                return;

            String name;
            try
            {
                name = ((ComboBox)sender).SelectedItem.ToString();
            }
            catch (Exception crap)
            {
                return;
            }

            currProduct = name;
            
            foreach (DataRow row in product_table.Rows)
            {
                if (name.Equals( (string)row[0]) )
                {
                    currMass = (double)row[1];
                    double weight = currMass * kgToLbs;
                    WeightRO.Text = weight.ToString("N2") + " lbs";
                }
            }
        }

        // Clear each text box upon focus change to get ready for user input
        private void TextGotFocus(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus((TextBox)sender);
            ((TextBox)sender).SelectAll();
            e.Handled = true;
        }

        protected void TextLostFocus(object sender, RoutedEventArgs e)
        {
            // When the RichTextBox loses focus the user can no longer see the selection.
            // This is a hack to make the RichTextBox think it did not lose focus.
            e.Handled = true;
        }

        /*
         * ----------------------------------------------------------
         * File Menu Options
         * ----------------------------------------------------------
         */

        /// <summary>
        ///     Event handler for file menu-> export plot
        /// </summary>
        /// <param name="sender">Button sender</param>
        /// <param name="e"></param>
        /// <returns>Void</returns>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Export.DataExport.GenerateReport( productListBox.SelectedItem.ToString() , plotModel.model);
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            Window add = new PopUps.NewProduct();
            if (add.ShowDialog() == true)
            {
                product_table = product_db.GetDataTable();
                createProductBox();  
            }
        }

        /// <summary>
        ///     Event handler for file menu-> exit
        /// </summary>
        /// <returns>Exits application</returns>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ImportXls_Click(object sender, RoutedEventArgs e)
        {

        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var about = new PopUps.AboutBox();
            about.Show();
        }

        private void Contact_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("info@signalinnovation.com", "Info");
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            if (daqBoard.daqData.Length == 0)
            {
                MessageBox.Show(Window.GetWindow(this), "Data must be acquired first.");
                return;
            }

            Export.DataExport.CreateWorkbook(daqBoard.daqData, currProduct,(float) DAQ.DAQBoard.sampleRateF);
        }
    }
}
