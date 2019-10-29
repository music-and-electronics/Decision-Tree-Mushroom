using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace DT_SQL
{
    class Second_Layer_Data_Transportation
    {
        private DataBase DB = new DataBase();
        private List<string> Field_names = new List<string>();
        public Dictionary<string, List<char[]>> Data_from_table = new Dictionary<string, List<char[]>>();
        public Dictionary<char, int> Total_judgement_field_dictionary = new Dictionary<char, int>();
        public int Total_count { get; set; }

        private string max_gain_key { get; set; }

        public Second_Layer_Data_Transportation(string max_gain_key)
        {
            this.max_gain_key = max_gain_key;

        }

        private void Get_Field_Names()
        {
            DB.sqlConnection.Open();

            string search_query = "SHOW COLUMNS FROM mushroom_table";

            MySqlDataReader sqlSearchResult;
            MySqlCommand sqlCommand = new MySqlCommand(search_query, DB.sqlConnection);
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
                if (field_name != "class" && field_name != "idx" && field_name != max_gain_key)
                {
                    string search_query = $"SELECT {field_name},{Field_names[1]} FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;
                    MySqlCommand sqlCommand = new MySqlCommand(search_query, DB.sqlConnection);
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
                    string search_query = $"SELECT {field_name} FROM mushroom_table";

                    MySqlDataReader sqlSearchResult;

                    MySqlCommand sqlCommand = new MySqlCommand(search_query, DB.sqlConnection);
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
    public class Second_Layer_DT_Caculation
    {
        private string max_gain_key { get; set; }
        Second_Layer_Data_Transportation second_Layer;

        public Second_Layer_DT_Caculation(string max_gain_key)
        {
            this.max_gain_key = max_gain_key;
            second_Layer = new Second_Layer_Data_Transportation(max_gain_key);
        }
    }

}