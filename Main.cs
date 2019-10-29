using System.Collections.Generic;

namespace DT_SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            First_Layer_DT_Calculation first_layer_calculation = new First_Layer_DT_Calculation();
            Second_Layer_DT_Caculation second_Layer_caculation = new Second_Layer_DT_Caculation(first_layer_calculation.Return_Max_Value());
        }
    }
}
