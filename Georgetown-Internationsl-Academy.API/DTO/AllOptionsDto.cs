namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class AllOptionsDto
    {
        public List<SelectOptionIntDto> SalaryHeads { get; set; }
        public List<SelectOptionIntDto> BudgetCodes { get; set; }
        public List<SelectOptionIntDto> Banks { get; set; }
        public List<SelectOptionIntDto> BankBranches { get; set; }
        public List<SelectOptionIntDto> OverTimesTypes { get; set; }
    }
}
