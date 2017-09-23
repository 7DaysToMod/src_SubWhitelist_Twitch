using System;
using SDTM;
using SDTM.Servers;
using System.Net;
using SubWhitelist;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using SDTM.Data;

namespace SubWhitelist_Twitch
{
	public class EndPoint_TwitchSignup:WWWEndpointProvider
	{
		public EndPoint_TwitchSignup ()
		{
			this.acl = new string[]{ "public" };
		}

		public override WWWResponse ProcessRequest (WWWRequest _request)
		{
			WWWUser user = _request.User;
			HttpListenerRequest request = _request._request;

			//string template;
			string outputStr = "";
			string redirectURL = "http://" + request.Url.Host + ":" + request.Url.Port;

			if (request.QueryString["code"]!=null) {
				string accessToken = request.QueryString ["code"];//getAccessToken (request.QueryString ["code"], redirectURL+"/subwhitelist/signup/twitch");
				if (accessToken.IndexOf ("error:") == 0) {
					outputStr = "{\"error\":\"" + accessToken.Replace ("error:", "") + "\"}";
				} else {
					string twitchName = subwhitelist.authProviders["twitch"].Validate (accessToken);

					if (twitchName.IndexOf ("error:") == 0) {
						outputStr = twitchName.Replace ("error:", "");
					} else {
						if (!user.vars.ContainsKey ("sub-type")) {
							user.vars.Add ("sub-type", "twitch");
							user.vars.Add ("sub-id", twitchName);
							user.vars.Add ("sub-token", accessToken);
						}

						return new WWWResponse("/subwhitelist/steamauth/",302);
					}
				}
			} else {
				//string twitchClientId = "";
				//if (subwhitelist.Settings.Get ("client_id")!=null) {
				//	twitchClientId = subwhitelist.Settings.Get("client_id");
				//}
				string formStr = SDTM.Servers.HTTP.WWW._templates["subwhitelisttwitchsignupreturn"];//"<a href=\"http://www.twitch.com/oauth2/authorize?response_type=code&client_id="+twitchClientId+"&redirect_uri="+redirectURL+"/subwhitelist/signup/\">\n\t        \t<img src=\"https://s3.amazonaws.com/twitch_public_assets/toolbox/twitch_logo.png\" height=\"50\"/> Connect with twitch</a>";

				//formStr = formStr.Replace ("{twitchClientId}", twitchClientId);
				//formStr = formStr.Replace ("{redirectURL}", redirectURL+"/subwhitelist/signup/");

				outputStr = formStr;
			}

			return new WWWResponse(outputStr);
		}

		public static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			bool isOk = true;
			// If there are errors in the certificate chain, look at each error to determine the cause.
			if (sslPolicyErrors != SslPolicyErrors.None) {
				for (int i=0; i<chain.ChainStatus.Length; i++) {
					if (chain.ChainStatus [i].Status != X509ChainStatusFlags.RevocationStatusUnknown) {
						chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
						chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
						chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
						chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
						bool chainIsValid = chain.Build ((X509Certificate2)certificate);
						if (!chainIsValid) {
							isOk = false;
						}
					}
				}
			}
			return isOk;
		}

		private string getAccessToken(string authCode, string redirect){
			//string streamerName = "";
			if (subwhitelist.Settings.Get("twitch_name")!="") {
				//streamerName = subwhitelist.Settings.Get("twitch_name");
				//ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

				string twitchClientId = "";
				string twitchClientSecret = "";

				if (subwhitelist.Settings.Get ("twitch_client_id")!=null) {
					twitchClientId = subwhitelist.Settings.Get("twitch_client_id");
				}

				if (subwhitelist.Settings.Get ("twitch_client_secret")!=null) {
					twitchClientSecret = subwhitelist.Settings.Get("twitch_client_secret");
				}

				string url = "https://api.twitch.tv/kraken/user";
				//string urlParams = "code=" + authCode + "&grant_type=authorization_code&client_id=" + twitchClientId + "&client_secret=" + twitchClientSecret + "&redirect_uri=" + redirect;
				string accessToken = "";

				try{
					using (WebClient client = new WebClient ()) {
						client.Headers.Add("Accept", "application/vnd.twitchtv.v5+json");
						client.Headers.Add("Authorization", "OAuth "+authCode);
						client.Headers.Add("Client-ID", twitchClientId);
						string response = client.DownloadString(url);

						//string res = System.Text.Encoding.UTF8.GetString(response);
                        //Log.Out(response);
                        Hashtable jsonObj = (Hashtable)JSON.JsonDecode(response);


						accessToken = jsonObj["id"].ToString();
                        Log.Out(accessToken);
					}
				}catch(Exception e){
					Console.WriteLine (e.Message);
				}

				return accessToken;
			} else {

				return "error:no twitch name";
			}
		}
	}
}

