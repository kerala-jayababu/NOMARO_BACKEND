using Microsoft.AspNetCore.Http.HttpResults;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeavePassageAmountDetailsDto
    {
        public int IdLeavePassageAmount { get; set; }   // 0 = new, >0 = update
        public int IdEmployee { get; set; }
        public decimal Amount { get; set; }
        public int IdFinancialYear { get; set; }        // will be set in controller
        public DateTime FinancialYearFrom { get; set; } // optional for insert
        public DateTime FinancialYearTo { get; set; }   // optional for insert
        public int? PaidIdSalaryMonth { get; set; }
        public int? CreatedBy { get; set; }
        public  DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class LeavePassageReversalDto
    {
        public int IdLeavePassageAmount { get; set; }   // 0 = new, >0 = update
        public int IdEmployee { get; set; }

        public int IdFinancialYear { get; set; }
        public int ReversalMonth { get; set; }
        public decimal ReversalAmount { get; set; }
        public string? Remarks { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class LeavePassageAdditionDto
    {
        public int IdLeavePassageAmount { get; set; }   // 0 = new, >0 = update
        public int IdEmployee { get; set; }

        public int IdFinancialYear { get; set; }
        public int ReversalMonth { get; set; }
        public decimal ReversalAmount { get; set; }
        public string? Remarks { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
