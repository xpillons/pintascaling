using Pinta.AutoScaling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinta.AutoScaling.Interfaces
{
    public interface ILoadWatcher
    {
        ClusterLoad GetVMLoad(string poolName);
        ConfigPool GetPools();
    }
}
