using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Util
{
    public static class Alfanumerico
    {
        public static string ConvertToBase36(int number)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (number == 0)
                return "0";

            string result = "";
            int current = number;

            while (current > 0)
            {
                int remainder = current % 36;
                result = chars[remainder] + result; // prepend el dígito
                current /= 36;
            }

            return result;
        }

        public static int ConvertFromBase36(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            value = value.Trim().ToUpper();
            int result = 0;

            foreach (char c in value)
            {
                int digit = chars.IndexOf(c);
                if (digit < 0)
                    return 0;

                result = result * 36 + digit;
            }

            return result;
        }
    }
}
