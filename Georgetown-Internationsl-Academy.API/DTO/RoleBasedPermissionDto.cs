namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class RoleBasedPermissionDto
    {
        public int? IdRolePermission { get; set; }
        public int IdDesignation { get; set; }
        public int IdPayrollScreen { get; set; } 
        public string Permission { get; set; }
    }
}
