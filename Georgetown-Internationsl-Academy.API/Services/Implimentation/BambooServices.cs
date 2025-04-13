using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iText.Kernel.Pdf.Canvas.Wmf;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Crmf;
using RestSharp;
using System.Xml.Serialization;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class BambooServices : IBambooServices
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BambooServices> _logger;
     
        public BambooServices(IConfiguration configuration, ILogger<BambooServices> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<BambooHRDetailsDto>> SyncEmployeesFromBambooHR()
        {
            var result = new List<BambooHRDetailsDto>();
            try
            {
                var baseURL = _configuration["BambooHR:BaseUrl"];
                var apikey = _configuration["BambooHR:ApiKey"];
                var photoSavePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");

                if (string.IsNullOrWhiteSpace(baseURL) || string.IsNullOrWhiteSpace(apikey))
                {
                    _logger.LogError("BambooHR BaseUrl or ApiKey is missing in configuration.");
                    return new List<BambooHRDetailsDto>();
                }

                if (!Directory.Exists(photoSavePath))
                    Directory.CreateDirectory(photoSavePath);

                var fullUrl = $"{baseURL}/employees/directory";
                var client = new RestClient();
                var request = new RestRequest(fullUrl, Method.Get);

                var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apikey}:x"));
                request.AddHeader("Authorization", $"Basic {token}");

                RestResponse response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError("Failed to retrieve data from BambooHR. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, response.Content);
                    return new List<BambooHRDetailsDto>();
                }

                var serializer = new XmlSerializer(typeof(EmployeeDirectoryDto));
                using var reader = new StringReader(response.Content);
                var directory = (EmployeeDirectoryDto)serializer.Deserialize(reader);

                var rawEmployees = directory?.Employees ?? new List<EmployeeRawDto>();

                foreach (var emp in rawEmployees)
                {
                    try
                    {
                        var detailUrl = $"{baseURL}/employees/{emp.Id}?fields=displayName,firstName,LastName,gender,dateofBirth,address1,address2,middleName,workPhone,mobilePhone,city,state,zipcode,JoiningDate,commissionDate,supervisor,status,terminationDate,department,jobTitle,workEmail";

                        var detailRequest = new RestRequest(detailUrl, Method.Get);
                        detailRequest.AddHeader("Authorization", $"Basic {token}");

                        var detailResponse = await client.ExecuteAsync(detailRequest);
                        if (!detailResponse.IsSuccessful)
                        {
                            _logger.LogWarning("Failed to get details for employee ID {Id}", emp.Id);
                            continue;
                        }

                        var detailSerializer = new XmlSerializer(typeof(BambooHREmployeeXmlDto));
                        using var detailReader = new StringReader(detailResponse.Content);
                        var detailedRaw = (BambooHREmployeeXmlDto)detailSerializer.Deserialize(detailReader);

                        var mapped = BambooEmployeeMapper.ToDetailsDto(detailedRaw);
                        result.Add(mapped);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while processing employee ID {Id}", emp.Id);
                    }
                }

                return result;

               


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing employees from BambooHR");
                return new List<BambooHRDetailsDto>();
            }
        }



    }

}
