using Microsoft.OData.Edm;

namespace PE_2.API.Model.Handbag
{
    public class LeopardProfileRequest
    {
        public int LeopardTypeId { get; set; }
        public string LeopardProfileName { get; set; } = null!;
        public int Weight { get; set; }
        public string Characteristics { get; set; }
        public string CareNeeds { get; set; }
        public Date ModifiedDate { get; set; }
    }
}
