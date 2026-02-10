using JSEA_Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces
{
    public interface IUserRepository
    {
        User? GetByEmail(string email);
        void Update(User user);
    }
}
