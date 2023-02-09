using fbognini.Core.Data.Pagination;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Validation
{
    public class PaginationAdvancedSinceQueryValidator : AbstractValidator<PaginationAdvancedSinceQuery>
    {
        public PaginationAdvancedSinceQueryValidator()
        {
            Include(new PaginationQueryValidator());
        }
    }

    public class PaginationSinceQueryValidator : AbstractValidator<PaginationSinceQuery>
    {
        public PaginationSinceQueryValidator()
        {
            Include(new PaginationQueryValidator());
            RuleFor(x => x.Since).GreaterThan(0).When(x => x.Since.HasValue);
        }
    }

    public class PaginationOffsetQueryValidator : AbstractValidator<PaginationOffsetQuery>
    {
        public PaginationOffsetQueryValidator()
        {
            Include(new PaginationQueryValidator());
            RuleFor(x => x.PageNumber).GreaterThan(0).When(x => x.PageNumber.HasValue);
        }
    }

    public class PaginationQueryValidator : AbstractValidator<PaginationQuery>
    {
        public PaginationQueryValidator()
        {
            RuleFor(x => x.PageSize).GreaterThan(1).When(x => x.PageSize.HasValue);
        }
    }
}
