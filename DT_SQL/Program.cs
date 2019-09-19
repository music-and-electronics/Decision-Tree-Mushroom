using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace DT_SQL
{
    class Data_Transportation
    {
        private MySqlConnection sqlConnection = new MySqlConnection("SERVER=192.168.0.52;DATABASE=study;Uid=john;PWD=Artec100;");
        private List<string> Field_names = new List<string>();
        protected Dictionary<string, List<char[]>> Data_from_table = new Dictionary<string, List<char[]>>();
        protected Dictionary<char, int> Total_class_dictionary = new Dictionary<char, int>();
        protected int Total_count { get; set; }

        protected Data_Transportation()
        {
            Get_Field_Names();
            Bring_Data_From_Table();
        }

        private void Get_Field_Names()
        {
            sqlConnection.Open();
            
            string search_query = "SHOW COLUMNS FROM mushroom_table";

            MySqlDataReader sqlSearchResult;
            MySqlCommand sqlCommand = new MySqlCommand(search_query, sqlConnection);
            sqlSearchResult         = sqlCommand.ExecuteReader();

            while (sqlSearchResult.Read())
            {
                Field_names.Add(sqlSearchResult.GetString(0));
            }

            sqlConnection.Close();
        }

        private void Bring_Data_From_Table()
        {
            foreach (string field_name in Field_names)
            {
                sqlConnection.Open();

                if (field_name != "class")
                {
                    string search_query = $"SELECT {field_name},{Field_names[1]} FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;
                    MySqlCommand sqlCommand = new MySqlCommand(search_query, sqlConnection);
                    sqlSearchResult         = sqlCommand.ExecuteReader();
                    List<char[]> field_data = new List<char[]>();

                    while (sqlSearchResult.Read())
                    {
                        char[] data_set = new char[2];
                        data_set[0]     = sqlSearchResult.GetChar(0);
                        data_set[1]     = sqlSearchResult.GetChar(1);
                        field_data.Add(data_set);
                    }

                    Data_from_table.Add(field_name, field_data);
                }

                else if(field_name!="idx")
                {
                    string search_query = $"SELECT class FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;

                    MySqlCommand sqlCommand = new MySqlCommand(search_query, sqlConnection);
                    sqlSearchResult         = sqlCommand.ExecuteReader();

                    int total_count = 0;

                    while (sqlSearchResult.Read())
                    {

                        if (Total_class_dictionary.ContainsKey(sqlSearchResult.GetChar(0)) == false)
                        {
                            Total_class_dictionary.Add(sqlSearchResult.GetChar(0), 1);
                        }

                        else
                        {
                            Total_class_dictionary[sqlSearchResult.GetChar(0)]++;
                        }

                        total_count++;
                    }

                    Total_count = total_count;
                }

                sqlConnection.Close();
            }
        }
    }

    class DT_Calculation : Data_Transportation
    {
        Dictionary<string, Dictionary<char, Dictionary<char, int>>> complete_data_set_for_DT = new Dictionary<string, Dictionary<char, Dictionary<char, int>>>();

        public DT_Calculation()
        {
            Mapping_mushroom_data_set();
            Print_All_Result(complete_data_set_for_DT);
        }

        private void Mapping_mushroom_data_set()
        {
            foreach (KeyValuePair<string, List<char[]>> dictionary_dataset_from_table in Data_from_table)
            {

                Dictionary<char, Dictionary<char, int>> data_set = new Dictionary<char, Dictionary<char, int>>();

                if (dictionary_dataset_from_table.Key != "total_count")
                {
                    foreach (char[] list_data_set in dictionary_dataset_from_table.Value)
                    {

                        if (data_set.ContainsKey(list_data_set[0]) == false)
                        {
                            Dictionary<char, int> value = new Dictionary<char, int>();
                            value[list_data_set[1]]  = 1;
                            data_set.Add(list_data_set[0], value);
                        }

                        else
                        {
                            if (data_set[list_data_set[0]].ContainsKey(list_data_set[1]) == false)
                            {
                                data_set[list_data_set[0]].Add(list_data_set[1], 0);
                            }

                            else
                            {
                                data_set[list_data_set[0]][list_data_set[1]]++;
                            }
                        }
                    }
                }
                complete_data_set_for_DT.Add(dictionary_dataset_from_table.Key,data_set);
            }
        }

        private double Calculate_Gain(Dictionary<char, Dictionary<char, int>> Calculation_Data)
        {
            double total_probability1 = (double)Total_class_dictionary['p'] / (double)Total_count;
            double total_probability2 = (double)Total_class_dictionary['e'] / (double)Total_count;
            double gain_result        = -total_probability1 * Math.Log(total_probability1, 2) - total_probability2 * Math.Log(total_probability2, 2);

            foreach (KeyValuePair<char, Dictionary<char, int>> data in Calculation_Data)
            {
                if (Calculation_Data[data.Key].ContainsKey('p') == true && Calculation_Data[data.Key].ContainsKey('e') == true)
                {
                    double sum           = (double)Calculation_Data[data.Key]['p'] + (double)Calculation_Data[data.Key]['e'];
                    double probability1  = (double)Calculation_Data[data.Key]['p'] / sum;
                    double probability2  = (double)Calculation_Data[data.Key]['e'] / sum;
                    double gain          = (sum / (double)Total_count) * (-probability1 * Math.Log(probability1, 2) - probability2 * Math.Log(probability2, 2));
                    gain_result         -= gain;
                }
            }
            return gain_result;
        }

        private void Print_All_Result(Dictionary<string, Dictionary<char, Dictionary<char, int>>> complete_data_set_for_DT)
        {
            Dictionary<string, double> result_gain = new Dictionary<string, double>();

            foreach (KeyValuePair<string, Dictionary<char, Dictionary<char, int>>> data_set in complete_data_set_for_DT)
            {

                Console.WriteLine(data_set.Key + ":");

                foreach (KeyValuePair<char, Dictionary<char, int>> data in data_set.Value)
                {

                    foreach (KeyValuePair<char, int> value in data.Value)
                    {
                        Console.WriteLine($"\t{data.Key}[{value.Key}]:{value.Value}");
                    }

                    double gain = Calculate_Gain(data_set.Value);
                    result_gain[data_set.Key] = gain;
                }

            }

            Console.WriteLine("total count : " + Total_count.ToString());

            foreach (KeyValuePair<char, int> data in Total_class_dictionary)
            {
                Console.WriteLine($"{data.Key} : {data.Value}");
            }

            foreach (KeyValuePair<string, double> data in result_gain)
            {
                Console.WriteLine($"{data.Key} : {data.Value}");
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            DT_Calculation calculation= new DT_Calculation();   
        }
    }
}