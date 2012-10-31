using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EraS.Services;

namespace EraS.MessageHandlers.ErasComponents
{
    class StatisticsComponent : DefaultComponent
    {
        public StatisticsComponent() : base("Statistics")
        {
            //TODO: move functions from StatisticsService to here
            foreach (var function in StatisticsService.Functions)
                Functions.Add(function.Key, function.Value);
        }
    }
}
