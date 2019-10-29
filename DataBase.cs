using MySql.Data.MySqlClient;
using System;

namespace DT_SQL
{
    class DataBase
    {
        public MySqlConnection sqlConnection;
        public DataBase()
        {
            SQL_Connection();
        }
        private void SQL_Connection()
        {
            Console.Write("Server:");
            string IP = Console.ReadLine();
            Console.Write("PWD:");
            string PWD = Console.ReadLine();
            sqlConnection = new MySqlConnection($"SERVER={IP};DATABASE=study;Uid=john;PWD={PWD};");
        }
    }
}