using CFIT.AppLogger;
using Fenix2GSX.AppConfig;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fenix2GSX.GSX.Menu
{
    public class GsxOperator(string title, int number, bool gsxChoice = false)
    {
        public virtual string Title { get; set; } = title;
        public virtual int Number { get; set; } = number;
        public virtual bool GsxChoice { get; set; } = gsxChoice;

        public static GsxOperator OperatorSelection(AircraftProfile profile, List<string> menuLines)
        {
            var operators = ParseOperators(menuLines);

            GsxOperator gsxOperator = operators.Where(o => o.GsxChoice).First();
            foreach (var preference in profile.OperatorPreferences)
            {
                var query = operators.Where(o => o.Title.Contains(preference, StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                {
                    gsxOperator = query.First();
                    Logger.Debug($"Found preferred Operator: '{gsxOperator.Title}'");
                    break;
                }    
            }

            return gsxOperator;
        }

        public static List<GsxOperator> ParseOperators(List<string> menuLines)
        {
            int lineCounter = 1;
            List<GsxOperator> operators = [];
            foreach (var line in menuLines)
            {
                bool gsxChoice = line.Contains(GsxConstants.GsxChoice, StringComparison.InvariantCultureIgnoreCase);
                operators.Add(new(line.Replace(GsxConstants.GsxChoice, "").TrimEnd(), lineCounter, gsxChoice));
                lineCounter++;
            }

            return operators;
        }
    }
}
