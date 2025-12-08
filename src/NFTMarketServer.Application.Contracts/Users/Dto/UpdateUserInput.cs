using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Users.Dto
{
    public class UpdateUserInput
    {
        [MinLength(1),MaxLength(15)]
        public string Name { get; set; }
        [MaxLength(50)]
        public string Email { get; set; }
        [MaxLength(50)]
        public string Twitter { get; set; }
        [MaxLength(50)]
        public string Instagram { get; set; }
    }
}