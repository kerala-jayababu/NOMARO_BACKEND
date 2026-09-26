using System.Globalization;

namespace Nomaro.API.Helpers
{
    public static class DateFunctions
    {

        public static string ConvertDateToGuyanaDateFormatString(DateTime date)
        {

            var culture = new CultureInfo("en-GY");
            return date.ToString("dd/MM/yyyy", culture);
        }
    }
}

