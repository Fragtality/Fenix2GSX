using CFIT.AppLogger;
using Fenix2GSX.AppConfig;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Fenix2GSX.Aircraft
{
    public class Flightplan
    {
        public virtual Config Config => AppService.Instance.Config;
        public virtual string SimbriefUser => AppService.Instance.GsxService.AircraftInterface.SimbriefUser;
        public virtual CancellationToken Token => AppService.Instance.Token;
        protected virtual HttpClient HttpClient { get; }

        public Flightplan()
        {
            HttpClient = new()
            {
                BaseAddress = new(Config.SimbriefUrlBase),
                Timeout = TimeSpan.FromMilliseconds(Config.HttpRequestTimeoutMs)
            };
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public virtual async Task<JsonNode> GetSimbriefJson()
        {
            if (long.TryParse(SimbriefUser, out _))
            {
                Logger.Debug($"Requesting SimBrief (via Userid) ...");
                return JsonNode.Parse(await HttpClient.GetStringAsync(string.Format(Config.SimbriefUrlPathId, SimbriefUser), Token));
            }
            else
            {
                Logger.Debug($"Requesting SimBrief (via Username) ...");
                return JsonNode.Parse(await HttpClient.GetStringAsync(string.Format(Config.SimbriefUrlPathName, SimbriefUser), Token));
            }
        }

        protected virtual bool GetJsonString(JsonNode node, out string value)
        {
            value = "";
            if (node!.GetValueKind() == System.Text.Json.JsonValueKind.String)
            {
                value = node!.GetValue<string>();
                return true;
            }
            else
                return false;
        }

        public virtual async Task<string> GetDestinationIcao(JsonNode json)
        {
            try
            {
                if (GetJsonString(json["origin"]!["icao_code"], out string icao))
                {
                    Logger.Debug($"Departure ICAO received: {icao}");
                    return icao;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error while extracting Departure ICAO from SimBrief (Exception: {ex.GetType().Name})");
            }

            return "";
        }

        public virtual async Task<DateTime> GetScheduledOut(JsonNode json)
        {
            try
            {
                if (GetJsonString(json["times"]!["sched_out"], out string estOut))
                {
                    Logger.Debug($"Out-Time received: {estOut}");
                    return DateTime.Parse(estOut).ToUniversalTime();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error while extracting scheduled Out-Time from SimBrief (Exception: {ex.GetType().Name})");
            }

            return DateTime.Now;
        }
    }
}
