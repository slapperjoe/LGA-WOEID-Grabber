using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGAGrabber
{
	class Program
	{

		private static void GetYahooLoginUrl() {
			var client = new RestClient("https://api.login.yahoo.com/oauth2/request_auth");//"https://service.endpoint.com/api/oauth2/token");
			var request = new RestRequest(Method.GET);
			request.AddParameter("client_id",
				"dj0yJmk9UnE0bHd2TWJxU3k0JnM9Y29uc3VtZXJzZWNyZXQmc3Y9MCZ4PWQ4");
			request.AddParameter("redirect_uri", "oob");
			request.AddParameter("response_type", "code");
			request.AddParameter("language", "en-au");
			IRestResponse response = client.Execute(request);
			var uri = response.ResponseUri;
		}

		public static void GetYahooTokens(string code) {
			var client2 = new RestClient("https://api.login.yahoo.com/oauth2/get_token");
			var request2 = new RestRequest(Method.POST);
			request2.AddHeader("content-type", "application/x-www-form-urlencoded");
			request2.AddParameter("application/x-www-form-urlencoded",
				"client_id=dj0yJmk9UnE0bHd2TWJxU3k0JnM9Y29uc3VtZXJzZWNyZXQmc3Y9MCZ4PWQ4&" +
				"client_secret=947259dc4567058acb93e5a1b29a3209919c9ad0&" +
				"grant_type=authorization_code&" +
				"code=" + code + "&" +
				"redirect_uri=oob&response_type=code&language=en-au", ParameterType.RequestBody);
			IRestResponse response2 = client2.Execute(request2);
			dynamic json = JsonConvert.DeserializeObject(response2.Content);
			//var bearerToken = json.access_token;
			//var refreshToken =
		}

		public static void RefreshYahooToken(string refreshToken) {
			var client4 = new RestClient("https://api.login.yahoo.com/oauth2/get_token");
			var request4 = new RestRequest(Method.POST);
			request4.AddHeader("content-type", "application/x-www-form-urlencoded");
			request4.AddParameter("application/x-www-form-urlencoded",
				"client_id=dj0yJmk9UnE0bHd2TWJxU3k0JnM9Y29uc3VtZXJzZWNyZXQmc3Y9MCZ4PWQ4&" +
				"client_secret=947259dc4567058acb93e5a1b29a3209919c9ad0&" +
				"grant_type=refresh_token&" +
				"refresh_token=" + refreshToken + "&" +
				"redirect_uri=oob", ParameterType.RequestBody);
			IRestResponse response4 = client4.Execute(request4);
			dynamic json = JsonConvert.DeserializeObject(response4.Content);
			//var bearerToken = json.access_token;
		}

		public static void ProcessYahoo(string bearerToken) {
			using (var ctx = new DSDIPEntities())
			{
				var lgas = ctx.LGAs.ToList();

				lgas.ForEach(lga =>
				{
					var cutoff = lga.LGAName2016.IndexOf("(");
					var name = lga.LGAName2016;
					if (cutoff > 0)
					{
						name = name.Substring(0, cutoff - 1);
					}
					if (!ctx.LGAWOEIds.Any(l => l.LGACode == lga.LGACode2016))
					{
						var client3 = new RestClient("https://api.gemini.yahoo.com/v3/rest/dictionary/woeid?location=" + name);
						var request3 = new RestRequest(Method.GET);
						request3.AddHeader("Authorization", "Bearer " + bearerToken);//json.access_token.Value);
						IRestResponse response3 = client3.Execute(request3);
						dynamic json3 = JsonConvert.DeserializeObject(response3.Content);

						var resp = json3.response as JArray;
						var woeResult = resp.Where(b => b["name"].ToString().Contains("Australia"))
							.Select(a => new { woeid = a["woeid"].ToString(), name = a["name"].ToString(), type = a["type"].ToString() })
							.ToList();

						foreach (var woe in woeResult)
						{
							ctx.LGAWOEIds.Add(new LGAWOEId
							{
								LGACode = lga.LGACode2016,
								LGAName = lga.LGAName2016,
								WOEName = woe.name,
								WOEId = Int32.Parse(woe.woeid),
								State = lga.StateCode,
								Type = woe.type
							});
						}
						if (woeResult.Count() == 0)
						{
							ctx.LGAWOEIds.Add(new LGAWOEId
							{
								LGACode = lga.LGACode2016,
								LGAName = lga.LGAName2016,
								WOEName = "??",
								WOEId = 0,
								State = lga.StateCode,
								Type = "??"
							});
						}
						ctx.SaveChanges();
					}
				});
			}
		}

		static void Main(string[] args)
		{
			ProcessYahoo("Ykxp.q6YsF_.fTjIzz6WxfwmIQxKZtzYrRwn7iSrO.1pkPA2GyZ8HuIj7OQHVFzkX4rt9yMWDDKodUIPM1Zx28.D8iS3RvZBI5aDr.0jOCqZK0tpDJcjnqcmWxYKg5QYz5W08Xb.mxtptimi3Ne87w0LjoH41J0qcbxOJIo3Dxz1BN65mgblUZwd855njxYrx7fMgE5XtXyEyYXsF4.CCBV9hTzW9QV4I7HsGK.4rRjzkrM6clMihUG35xDiuukVj1UWE75rMTfOV4ygVsgd5qnFD.IIMc2w1T0iWvmjT6CkcJc9rhDNir2FhjNgQH9H7O9DPMxMaAf8YRKMsqWiVHJw3JBVHPVntXyP9I6_FZi7CpWcm.Ttqw2.2kIJXTEzVoXkncNZkctDW.h9oOeQeIZxYqJeD40LVcCUheBTVNQrIziwpzS3y0tvBYv2Spp9U7vyiV4lAm74cFSvNieusUDAsvnuL2VMnIsyUQLFbfEzY.r6YHJb9BHVK43OnlS4tcPrifrZW3I8JIt9Zg_VTMawR7UQCtHYufmBaAgPtK0erZGlm5hTDDHep5f_nn3MKVTSoRAs05jdr_OBv85t48uO..DEC6d9GgsRFAR0JZoeSR7WIFiBK3DrISKR7yQX8_K7qkibW8YOjJQrH1yI4B909w_iUCVyEZ7WfXY1qYb824i74LtU6TwyTBGcIg82zsVwGOtEj7DgAVXYTXCbgny2NAVQuSgcZ.ru2kqmm83E2ik7TgioviEN0aWHQOjFWL9v9tdCAf1CEdTX6CaFaHzXylgrB20PrgIVSdSWMioH6EvGcCOTq0x7CZwb_wocisWzEHH7icgGDzcfFu6i8KvoPQ7JZbYu6gxoSW4_fYLZQC1bzh1Cym34mRWfcCyNN.MI6dps58EeWyBypO.sa75UPjmGmuVVjSObuhtYNWok95eNqo.Fn3.WCStfSfDziF_QbPsFtc7pLIYOtDp7uvDPznoiztRT3Y0wIOF2iLEzEiREIpdmaQVsuIX77FqWKsqg4b_9Z9dNmGQomjDZPbhfNaIkqlFcFGoZKvINjM6IFyEWK61sxG4HAbCDHxWBCc4kHfQayLOz");

			
		}
	}
}
