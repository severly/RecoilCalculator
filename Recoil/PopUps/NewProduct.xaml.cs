using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RecoilCalculator.PopUps
{
    /// <summary>
    /// Interaction logic for NewProduct.xaml
    /// </summary>
    public partial class NewProduct : Window
    {
        SQLiteDatabase product_db;
        DataTable product_table;

        public static String product = " ";
        private double kgToLbs = 2.204622;

        public NewProduct()
        {
            product_db = new SQLiteDatabase();
            product_db.makeTable();
            product_table = product_db.GetDataTable();

            InitializeComponent();
            createScaleBox();
        }

        private void createScaleBox()
        {
            ScaleBox.Items.Add("lbs");
            ScaleBox.Items.Add("g");
            ScaleBox.Items.Add("kg");
        }

        // Add button click function
        // Check if mass/weight value is a number, then add pair to database
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            double value = 0;

            try
            {
                value = Convert.ToDouble(massText.Text);
            }
            catch (FormatException)
            {
                System.Windows.MessageBox.Show(Window.GetWindow(this), "Mass/weight must be a sequence of digits", "Warning");
            }
            catch (OverflowException)
            {
                System.Windows.MessageBox.Show(Window.GetWindow(this), "Mass/weight is too large.", "Warning");
            }

            // Check if product name exists in database already
            if( productExists(productText.Text) )
                return;

            switch( ScaleBox.Text )
            {
                case "lbs":
                    value /= kgToLbs;
                    break;

                case "g":
                    value /= 1000;
                    break;

                case "kg":
                    break;
            }
            

            // Add product-mass pair to database
            product_db.Insert(productText.Text, value);
            product = productText.Text;

            this.DialogResult = true;
            this.Close();
        }

        // Return false and close window if cancel button is clicked
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Return true if product exists in database
        private Boolean productExists(String productName)
        {
            // Check if product already exists
            foreach (DataRow row in product_table.Rows)
            {
                if (row[0].Equals(productName))
                {
                    System.Windows.MessageBox.Show(Window.GetWindow(this), "Product already exists.", "Warning");
                    productText.Text = "Product";
                    massText.Text = "Mass";
                    return true;
                }
            }
            return false;
        }

        // Clear each text box upon focus change to get ready for user input
        private void TextGotFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).Text.Equals("Product") || ((TextBox)sender).Text.Equals("Mass/Weight"))
                ((TextBox)sender).Clear();
        }
 
    }
}
