using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Restaurace.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
    }
}
