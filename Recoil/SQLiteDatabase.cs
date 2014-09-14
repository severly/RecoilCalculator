using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace RecoilCalculator
{

    class SQLiteDatabase
    {
        String dbConnection;
        String dbFile = @Application.StartupPath + "\\products.s3db";

        /// <summary>
        ///     Default Constructor for SQLiteDatabase Class.
        /// </summary>
        public SQLiteDatabase()
        {
            //dbFile = @Application.StartupPath + "\\productsfloat.s3db";
            dbConnection = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", dbFile);
        }


        /// <summary>
        ///     Allows the programmer to easily insert into the DB
        /// </summary>
        /// <param name="tableName">The table into which we insert the data.</param>
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Insert(String product, double mass)
        {
            using (var factory = new System.Data.SQLite.SQLiteFactory())
            using (System.Data.Common.DbConnection dbConn = factory.CreateConnection())
            {
                dbConn.ConnectionString = dbConnection;
                dbConn.Open();
                using (System.Data.Common.DbCommand cmd = dbConn.CreateCommand())
                {
                    //parameterized insert
                    cmd.CommandText = @"INSERT INTO PRODUCTS (PRODUCT,MASS) VALUES(@product,@mass)";

                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = "@product";
                    p1.Value = product;

                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = "@mass";
                    p2.Value = mass;

                    cmd.Parameters.Add(p1);
                    cmd.Parameters.Add(p2);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception crap)
                    {
                        MessageBox.Show(crap.Message);
                        return false;
                    }
                    cmd.Dispose();
                }

                if (dbConn.State != System.Data.ConnectionState.Closed) dbConn.Close();
                dbConn.Dispose();
                factory.Dispose();  
            }
            return true;
        }

        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        /// 
        public int ExecuteNonQuery(string sql)
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
            int rowsUpdated = mycommand.ExecuteNonQuery();
            cnn.Close();
            return rowsUpdated;
        }

        public bool makeTable()
        {
            if (File.Exists(dbFile))
                return false;

            using ( var factory = new System.Data.SQLite.SQLiteFactory() )
            using ( System.Data.Common.DbConnection dbConn = factory.CreateConnection() )
            {
                dbConn.ConnectionString = dbConnection;
                dbConn.Open();
                using (System.Data.Common.DbCommand cmd = dbConn.CreateCommand())
                {
                    //create table
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS PRODUCTS (PRODUCT text primary key, MASS real);";
                    cmd.ExecuteNonQuery();

                    //parameterized insert
                    /*
                    cmd.CommandText = @"INSERT INTO PRODUCTS (PRODUCT,MASS) VALUES(@product,@mass)";

                    var p1 = cmd.CreateParameter();
                    p1.ParameterName = "@product";
                    p1.Value = "Select A Product";

                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = "@mass";
                    p2.Value = 0;

                    cmd.Parameters.Add(p1);
                    cmd.Parameters.Add(p2);

                    cmd.ExecuteNonQuery();
                     * 

                    //read from the table
                    cmd.CommandText = @"SELECT PRODUCT, MASS FROM PRODUCTS";
                    using (System.Data.Common.DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string product = reader.GetString(0);
                            double mass = reader.GetDouble(1);
                            Console.WriteLine("record read as product: {0} mass: {1}", product, mass);
                        }
                    }
                     * 
                     * */

                    cmd.Dispose();
                }

                if (dbConn.State != System.Data.ConnectionState.Closed) 
                    dbConn.Close();

                dbConn.Dispose();
                factory.Dispose();
            }
            return true;
        }


        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        public DataTable GetDataTable()
        {
            DataTable dt = new DataTable();
            String query = "select * from PRODUCTS";
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                SQLiteCommand cmd = new SQLiteCommand(cnn);
                cmd.CommandText = query;

                SQLiteDataReader reader = cmd.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                cnn.Close();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return dt;
        }



        /// <summary>
        ///     Allows the programmer to retrieve single items from the DB.
        /// </summary>
        /// <param name="sql">The query to run.</param>
        /// <returns>A string.</returns>
        public string ExecuteScalar(string sql)
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
            object value = mycommand.ExecuteScalar();
            cnn.Close();
            if (value != null)
            {
                return value.ToString();
            }
            return "";
        }

        /// <summary>
        ///     Allows the programmer to easily delete rows from the DB.
        /// </summary>
        /// <param name="tableName">The table from which to delete.</param>
        /// <param name="where">The where clause for the delete.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Delete(String name)
        {   
            try
            {
                this.ExecuteNonQuery( String.Format("delete from PRODUCTS where PRODUCT = '{0}'", name) );
            }
            catch (Exception fail)
            {
                MessageBox.Show("Delete failed for " + name + ":\n" + fail.Message);
                return false;
            }
            return true;
        }



        /// <summary>
        ///     Allows the programmer to easily delete all data from the DB.
        /// </summary>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearDB()
        {
            DataTable tables;
            try
            {
                tables = this.GetDataTable();
                foreach (DataRow table in tables.Rows)
                {
                    this.ClearTable(table["NAME"].ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Allows the user to easily clear all data from a specific table.
        /// </summary>
        /// <param name="table">The name of the table to clear.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearTable(String table)
        {
            try
            {

                this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }

    } // class sqlitedatabase

}// namespace recoilcalculator