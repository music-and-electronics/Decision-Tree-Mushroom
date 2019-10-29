using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace DT_SQL
{
    class Data_Transportation
    {
        private MySqlConnection sqlConnection ;
        private List<string> Field_names = new List<string>();
        public Dictionary<string, List<char[]>> Data_from_table = new Dictionary<string, List<char[]>>();
        public Dictionary<char, int> Total_judgement_field_dictionary = new Dictionary<char, int>();
        public int Total_count { get; set; }

        public Data_Transportation()
        {
            SQL_Connection();
            Get_Field_Names();
            Bring_Data_From_Table();
        }

        private void SQL_Connection()
        {
            Console.Write("Server:");
            string IP=Console.ReadLine();
            Console.Write("PWD:");
            string PWD=Console.ReadLine();
            sqlConnection= new MySqlConnection($"SERVER={IP};DATABASE=study;Uid=john;PWD={PWD};");
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

                //Case to parse general field data
                if (field_name != "class" && field_name != "idx")
                {
                    string search_query = $"SELECT {field_name},{Field_names[1]} FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;
                    MySqlCommand sqlCommand = new MySqlCommand(search_query, sqlConnection);
                    sqlSearchResult = sqlCommand.ExecuteReader();
                    List<char[]> field_data = new List<char[]>();

                    while (sqlSearchResult.Read())
                    {
                        char[] field_data_set = new char[2];
                        field_data_set[0] = sqlSearchResult.GetChar(0);
                        field_data_set[1] = sqlSearchResult.GetChar(1);
                        field_data.Add(field_data_set);
                    }

                    Data_from_table.Add(field_name, field_data);
                }

                //Case to parse total judgement which is used for total gain calculation
                else if (field_name=="class")
                {
                    string search_query = $"SELECT class FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;

                    MySqlCommand sqlCommand = new MySqlCommand(search_query, sqlConnection);
                    sqlSearchResult         = sqlCommand.ExecuteReader();

                    int total_count = 0;

                    while (sqlSearchResult.Read())
                    {

                        if (Total_judgement_field_dictionary.ContainsKey(sqlSearchResult.GetChar(0)) == false)
                        {
                            Total_judgement_field_dictionary.Add(sqlSearchResult.GetChar(0), 1);
                        }

                        else
                        {
                            Total_judgement_field_dictionary[sqlSearchResult.GetChar(0)]++;
                        }

                        total_count++;
                    }

                    Total_count = total_count;
                }

                sqlConnection.Close();
            }
        }
    }

    class DT_Calculation 
    {
        Dictionary<string, Dictionary<char, Dictionary<char, int>>> complete_data_set_for_DT = new Dictionary<string, Dictionary<char, Dictionary<char, int>>>();

        Data_Transportation data_from_DB = new Data_Transportation();

        public DT_Calculation()
        {
            Mapping_mushroom_data_set();
            Print_All_Result(complete_data_set_for_DT);
        }

        //DB에서 가져온 버섯 데이터를 사용하기 위해 dictionary에 파싱하는 함수
        private void Mapping_mushroom_data_set()
        {
            foreach (KeyValuePair<string, List<char[]>> value_from_dataset in data_from_DB.Data_from_table)
            {

                Dictionary<char, Dictionary<char, int>> field_data_set = new Dictionary<char, Dictionary<char, int>>();

                if (value_from_dataset.Key != "total_count")
                {
                    foreach (char[] list_data_set in value_from_dataset.Value)
                    {

                        if (field_data_set.ContainsKey(list_data_set[0]) == false)
                        {
                            Dictionary<char, int> value = new Dictionary<char, int>();
                            value[list_data_set[1]]  = 1;
                            field_data_set.Add(list_data_set[0], value);
                        }

                        else
                        {
                            if (field_data_set[list_data_set[0]].ContainsKey(list_data_set[1]) == false)
                            {
                                field_data_set[list_data_set[0]].Add(list_data_set[1], 0);
                            }

                            else
                            {
                                field_data_set[list_data_set[0]][list_data_set[1]]++;
                            }
                        }
                    }
                }
                complete_data_set_for_DT.Add(value_from_dataset.Key,field_data_set);
            }
        }

        private double Entropy(double probabililty)
        {
            return -probabililty*Math.Log(probabililty,2);
        }

        private double Calculate_Gain(Dictionary<char, Dictionary<char, int>> Calculation_Data)
        {
            //전체 게인을 위한 위한 확률 계산
            double total_probability1 = (double)data_from_DB.Total_judgement_field_dictionary['p'] / (double)data_from_DB.Total_count;
            double total_probability2 = (double)data_from_DB.Total_judgement_field_dictionary['e'] / (double)data_from_DB.Total_count;

            //전체 데이터에 대한 게인 계산
            double gain_result        = -total_probability1 * Math.Log(total_probability1, 2) - total_probability2 * Math.Log(total_probability2, 2);

            foreach (var data in Calculation_Data)
            {
                
                if (Calculation_Data[data.Key].ContainsKey('p') == true && Calculation_Data[data.Key].ContainsKey('e') == true)
                {
                    //해당 field에 대한 게인 계산
                    double sum           = (double)Calculation_Data[data.Key]['p'] + (double)Calculation_Data[data.Key]['e'];
                    double probability1  = (double)Calculation_Data[data.Key]['p'] / sum;
                    double probability2  = (double)Calculation_Data[data.Key]['e'] / sum;
                    double gain          = (sum / (double)data_from_DB.Total_count) * (Entropy(probability1)+Entropy(probability2));
                    gain_result         -= gain;
                }
            }
            return gain_result;
        }

        //Dictionary에 저장된 데이터와 게인 계산 결과를 출력하는 함수
        private void Print_All_Result(Dictionary<string, Dictionary<char, Dictionary<char, int>>> complete_data_set_for_DT)
        {
            Dictionary<string, double> gain_results = new Dictionary<string, double>();

            foreach (KeyValuePair<string, Dictionary<char, Dictionary<char, int>>> mushroom_field in complete_data_set_for_DT)
            {

                Console.WriteLine(mushroom_field.Key + ":");

                foreach (KeyValuePair<char, Dictionary<char, int>> judgement_data in mushroom_field.Value)
                {

                    foreach (KeyValuePair<char, int> judgement_value in judgement_data.Value)
                    {
                        Console.WriteLine($"\t{judgement_data.Key}[{judgement_value.Key}]:{judgement_value.Value}");
                    }

                    double gain = Calculate_Gain(mushroom_field.Value);
                    gain_results[mushroom_field.Key] = gain;
                }

            }

            Console.WriteLine("total count : " + data_from_DB.Total_count.ToString());

            foreach (KeyValuePair<char, int> data in data_from_DB.Total_judgement_field_dictionary)
            {
                Console.WriteLine($"{data.Key} : {data.Value}");
            }

            foreach (KeyValuePair<string, double> data in gain_results)
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
            Console.Write("");
        }
    }
}