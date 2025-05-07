using CFIT.AppLogger;
using Fenix2GSX.AppConfig;
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
                BaseAddress = new(Config.SimbriefUrlBase)
            };
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected virtual async Task<JsonNode> GetJsonNode()
        {
            Logger.Debug($"Requesting SimBrief ...");
            return JsonNode.Parse(await HttpClient.GetStringAsync(string.Format(Config.SimbriefUrlPath, SimbriefUser), Token));
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

        public virtual async Task<string> GetDestinationIcao()
        {
            try
            {
                var json = await GetJsonNode();
                if (GetJsonString(json["origin"]!["icao_code"], out string icao))
                {
                    Logger.Debug($"Departure ICAO received: {icao}");
                    return icao;
                }
            }
            catch
            {
                Logger.Warning($"Error while fetching SimBrief Departure ICAO");
            }

            return "";
        }
    }
}
