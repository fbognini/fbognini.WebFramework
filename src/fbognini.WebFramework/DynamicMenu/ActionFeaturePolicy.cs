using fbognini.WebFramework.Utilities;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fbognini.WebFramework.DynamicMenu
{
    public class ActionFeaturePolicy
    {
        public ActionFeaturePolicy(FeatureGateAttribute? attribute)
        {
            if (attribute == null)
            {
                return;
            }

            Features = attribute.Features;
            RequirementType = attribute.RequirementType;
        }

        public IEnumerable<string>? Features { get; }
        public RequirementType RequirementType { get; }

        public async Task<bool> IsPolicysValid(IFeatureManager featureManager)
        {
            if (Features == null || !Features.Any())
            {
                return true;
            }

            return RequirementType != RequirementType.All
                ? await Features.AnyAsync(async (string feature) => await featureManager.IsEnabledAsync(feature))
                : await Features.AllAsync(async (string feature) => await featureManager.IsEnabledAsync(feature));
        }
    }

}
