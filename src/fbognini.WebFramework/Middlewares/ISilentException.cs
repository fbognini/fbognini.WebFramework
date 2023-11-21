using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Middlewares;

/// <summary>
/// If an exception implement ISilentException it won't be logged as an error by middleware
/// </summary>
public interface ISilentException
{
}
