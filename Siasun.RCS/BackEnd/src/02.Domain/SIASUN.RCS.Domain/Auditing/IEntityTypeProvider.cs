using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Auditing
{
    public interface IEntityTypeProvider
    {
        List<Type> GetEntityTypes();
    }
}
