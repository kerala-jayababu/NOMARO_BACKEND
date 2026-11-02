namespace Nomaro.API.DTO
{
    /// <summary>Birthdays and work anniversaries for today and the next few days.</summary>
    public class CelebrationsDto
    {
        public DateTime Today { get; set; }
        public int UpcomingDays { get; set; }
        public List<CelebrationEmployeeDto> TodaysBirthdays { get; set; } = new();
        public List<CelebrationEmployeeDto> UpcomingBirthdays { get; set; } = new();
        public List<CelebrationEmployeeDto> TodaysWorkAnniversaries { get; set; } = new();
        public List<CelebrationEmployeeDto> UpcomingWorkAnniversaries { get; set; } = new();
    }

    public class CelebrationEmployeeDto
    {
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        /// <summary>Photo from EmployeePhotoFilePath, when the file exists.</summary>
        public byte[]? EmployeePhoto { get; set; }

        /// <summary>Date of the birthday / anniversary this year (or next year when it falls after 31-Dec).</summary>
        public DateTime OccasionDate { get; set; }
        /// <summary>0 = today, 1..n = in n days.</summary>
        public int DaysAway { get; set; }

        /// <summary>Work anniversaries only.</summary>
        public DateTime? JoiningDate { get; set; }
        public int? YearsOfService { get; set; }

        /// <summary>Whether a congratulations email can be sent (employee has an email address).</summary>
        public bool HasEmail { get; set; }
        /// <summary>When the congratulations email for this occasion was sent (from Notifications); null = not sent.</summary>
        public DateTime? EmailSentOn { get; set; }
    }

    /// <summary>A holiday for the Holiday Calendar, with the holiday type's display name.</summary>
    public class HolidayCalendarDto
    {
        public int IdHoliday { get; set; }
        public DateTime HolidayDate { get; set; }
        public string? HolidayType { get; set; }
        public string? HolidayTypeName { get; set; }
        public string? HolidayDescription { get; set; }
    }

    public class SendCongratulationsDto
    {
        public int IdEmployee { get; set; }
        /// <summary>BIRTHDAY or WORKANNIVERSARY</summary>
        public string? Occasion { get; set; }
    }
}
