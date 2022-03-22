using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubModUpdateCheckerConsole.Interfaces
{
    internal interface IMainManager
    {
        Task Initialize();
        Task UpdateAsync();
        void ImportCsv();
    }
}
