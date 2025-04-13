using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Helpers
{
    public static class BambooMapper
    {
        public static BambooHRDirectoryDto ToFlat(EmployeeRawDto dto)
        {
            string Get(string key) => dto.Fields?.FirstOrDefault(f => f.Id == key)?.Value;

            return new BambooHRDirectoryDto
            {
                Id = dto.Id,
                DisplayName = Get("displayName"),
                FirstName = Get("firstName"),
                LastName = Get("lastName"),
                PhotoUploaded = bool.TryParse(Get("photoUploaded"), out var uploaded) && uploaded,
                PhotoUrl = Get("photoUrl"),
                CanUploadPhoto = Get("canUploadPhoto")?.ToLower() == "yes",             
            };
        }
    }

}
