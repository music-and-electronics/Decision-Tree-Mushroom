using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace DT_SQL
{
    class Fisrt_Layer_Data_Transportation
    {
        private DataBase DB = new DataBase();
        private List<string> Field_names = new List<string>();
        public Dictionary<string, List<char[]>> Data_from_table = new Dictionary<string, List<char[]>>();
        public Dictionary<char, int> Total_judgement_field_dictionary = new Dictionary<char, int>();
        public int Total_count { get; set; }

        public Fisrt_Layer_Data_Transportation()
        {
            Get_Field_Names();
            Bring_Data_From_Table();
        }

        private void Get_Field_Names()
        {
            DB.sqlConnection.Open();

            string search_query = "SHOW COLUMNS FROM mushroom_table";

            MySqlDataReader sqlSearchResult;
            MySqlCommand sqlCommand = new MySqlCommand(search_query,DB.sqlConnection);
            sqlSearchResult = sqlCommand.ExecuteReader();

            while (sqlSearchResult.Read())
            {
                Field_names.Add(sqlSearchResult.GetString(0));
            }

            DB.sqlConnection.Close();
        }

        private void Bring_Data_From_Table()
        {
            foreach (string field_name in Field_names)
            {
                DB.sqlConnection.Open();

                //Case to parse general field data
                if (field_name != "class" && field_name != "idx")
                {
                    string search_query = $"SELECT {field_name},{Field_names[1]} FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;
                    MySqlCommand sqlCommand = new MySqlCommand(search_query,DB.sqlConnection);
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
                else if (field_name == "class")
                {
                    string search_query = $"SELECT class FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;

                    MySqlCommand sqlCommand = new MySqlCommand(search_query,DB.sqlConnection);
                    sqlSearchResult = sqlCommand.ExecuteReader();

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

                DB.sqlConnection.Close();
            }
        }
    }

    public class First_Layer_DT_Calculation
    {
        Dictionary<string, Dictionary<char, Dictionary<char, int>>> complete_data_set_for_DT
        = new Dictionary<string, Dictionary<char, Dictionary<char, int>>>();

        public Dictionary<string, double> gain_results = new Dictionary<string, double>();
        Fisrt_Layer_Data_Transportation data_from_DB = new Fisrt_Layer_Data_Transportation();


        public First_Layer_DT_Calculation()
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
                            value[list_data_set[1]] = 1;
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
                complete_data_set_for_DT.Add(value_from_dataset.Key, field_data_set);
            }
        }

        private double Entropy(double probabililty)
        {
            return -probabililty * Math.Log(probabililty, 2);
        }

        private double Calculate_Gain(Dictionary<char, Dictionary<char, int>> Calculation_Data)
        {
            //전체 게인을 위한 위한 확률 계산
            double total_probability1 = (double)data_from_DB.Total_judgement_field_dictionary['p'] / (double)data_from_DB.Total_count;
            double total_probability2 = (double)data_from_DB.Total_judgement_field_dictionary['e'] / (double)data_from_DB.Total_count;

            //전체 데이터에 대한 게인 계산
            double gain_result = -total_probability1 * Math.Log(total_probability1, 2) - total_probability2 * Math.Log(total_probability2, 2);

            foreach (var data in Calculation_Data)
            {

                if (Calculation_Data[data.Key].ContainsKey('p') == true && Calculation_Data[data.Key].ContainsKey('e') == true)
                {
                    //해당 field에 대한 게인 계산
                    double sum          = (double)Calculation_Data[data.Key]['p'] + (double)Calculation_Data[data.Key]['e'];
                    double probability1 = (double)Calculation_Data[data.Key]['p'] / sum;
                    double probability2 = (double)Calculation_Data[data.Key]['e'] / sum;
                    double gain         = (sum / (double)data_from_DB.Total_count) * (Entropy(probability1) + Entropy(probability2));
                    gain_result        -= gain;
                }
            }
            return gain_result;
        }

        //Dictionary에 저장된 데이터와 게인 계산 결과를 출력하는 함수
        private void Print_All_Result(Dictionary<string, Dictionary<char, Dictionary<char, int>>> complete_data_set_for_DT)
        {

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

            foreach (KeyValuePair<string, double> gain_result in gain_results)
            {
                Console.WriteLine($"{gain_result.Key} : {gain_result.Value}");
            }
        }

        public string Return_Max_Value()
        {

            string max_gain_key = "";

            foreach (KeyValuePair<string, double> gain_result in gain_results)
            {
                if(gain_results.Values.Max()==gain_result.Value)
                {
                    max_gain_key= gain_result.Key;
                }
            }

            return max_gain_key;

        }
          
    }

}