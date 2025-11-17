using Microsoft.OData.Edm;

    public class LeopardProfileResponse
    {
        public int LeopardProfileId { get; set; }
        public int LeopardTypeId { get; set; } 
        public string LeopardProfileName { get; set; } = null!;
        public double Weight { get; set; }
        public string Characteristics { get; set; }
        public string CareNeeds { get; set; }
        public Date ModifiedDate { get; set; }
    }

