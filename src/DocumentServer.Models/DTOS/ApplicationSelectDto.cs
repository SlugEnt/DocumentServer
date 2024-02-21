using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Models.DTOS
{
    // Used for Simple list of applications. Used commonly for Select Drop Downs.
    public class ApplicationSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}