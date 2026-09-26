using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Nomaro.API.DTO
{
 

    public class BambooHRDirectoryDto
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool PhotoUploaded { get; set; }
        public string PhotoUrl { get; set; }
        public bool CanUploadPhoto { get; set; }        
    }

   
}

