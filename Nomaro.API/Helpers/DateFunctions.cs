using System.Globalization;

namespace Nomaro.API.Helpers
{
    public static class DateFunctions
    {

        public static string ConvertDateToGuyanaDateFormatString(DateTime date)
        {
            // Uses the company's configured date format (from NOMARO_GLOBAL), default dd/MM/yyyy.
            return CompanyLocale.FormatDate(date);
        }
    }
}

