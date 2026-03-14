
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
namespace Georgetown_Internationsl_Academy.API.Services.Interface
{
    public interface IAuditService
    {
        /// <summary>
        /// Generic audit log method. Call this after every Create/Update/Delete operation.
        /// </summary>
        /// <param name="actionType">  "Create" | "Update" | "Delete"          </param>
        /// <param name="entityName">  Table/entity name e.g. "Employee"       </param>
        /// <param name="entityId">    Primary key of the affected record       </param>
        /// <param name="actionDetails">Object or anonymous type with the data </param>
        Task LogAuditAsync(
            string actionType,
            string entityName,
            int entityId,
            object actionDetails
        );
    }
}
