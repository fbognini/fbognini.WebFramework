using System.Collections.Generic;

namespace fbognini.WebFramework
{
    public class BaseVm<TDto>
        where TDto : class
    {
        public bool Search { get; set; }
        public IList<TDto> Response { get; set; }
    }
}
